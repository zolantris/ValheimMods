// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations;

public partial class PowerNetworkControllerIntegration : PowerNetworkController
{
  private readonly List<string> _networksToRemove = new();

  // prefabHash to Related component
  public static Dictionary<int, List<object>> PowerPrefabDataControllers = new();

  public override void Awake()
  {
    LoggerProvider.LogDebug("Called Awake with debug");
    LoggerProvider.LogMessage("Called Awake with Message");
    base.Awake();
    LoggerProvider.LogMessage("Called post awake with message");
    StartCoroutine(DelayedRegister());
  }

  // public static void RegisterPowerComponentByZdo(ZDO zdo)
  // {
  //   RegisterControllerForPrefab(zdo.GetPrefab());
  //   RequestRebuildNetwork();
  // }

  public IEnumerator DelayedRegister()
  {
    while (ZNet.instance == null || ZRoutedRpc.instance == null)
    {
      yield return null;
    }
    ZDOClaimUtility.RegisterClaimZdoRpc();
    RegisterRebuildRpc();
  }

  // DO nothing for fixed update. Hosts cannot run FixedUpdate on server I think...
  protected override void FixedUpdate() {}
  protected override void Update()
  {
    base.Update();
    SimulateOnClientAndServer();
  }

  /// <summary>
  /// Data-only updates are applied to ZDOs.
  /// </summary>
  /// <param name="zdos"></param>
  public static void RequestRebuildNetworkWithZDOs(List<ZDOID> zdos)
  {
    var pkg = new ZPackage();
    pkg.Write(zdos.Count);
    foreach (var zdo in zdos)
    {
      pkg.Write(zdo); // ZDOID has a Write(ZPackage) overload
    }

    ZRoutedRpc.instance.InvokeRoutedRPC(
      ZRoutedRpc.instance.GetServerPeerID(),
      nameof(RequestRebuildNetworkWithZDOs),
      pkg
    );
  }

  private static bool _rebuildRegistered;
  public void RegisterRebuildRpc()
  {
    if (_rebuildRegistered || ZRoutedRpc.instance == null)
      return;

    if (ZNet.instance.IsServer())
    {
      ZRoutedRpc.instance.Register<ZPackage>(nameof(RequestRebuildNetworkWithZDOs), Server_HandleRebuildRequest);
    }
    else
    {
      ZRoutedRpc.instance.Register<ZPackage>(nameof(RequestRebuildNetworkWithZDOs), (_, __) => {});
    }

    _rebuildRegistered = true;
  }

  public void Server_HandleRebuildRequest(long sender, ZPackage pkg)
  {
    if (!ZNet.instance.IsServer())
      return;

    pkg.SetPos(0);
    var count = pkg.ReadInt();
    var ids = new List<ZDOID>(count);

    for (var i = 0; i < count; i++)
    {
      try
      {

        ids.Add(pkg.ReadZDOID());
      }
      catch (Exception e)
      {
        LoggerProvider.LogError($"Error while reading zdoid {e}");
      }
    }

    try
    {
      LoggerProvider.LogInfo($"[ZDORebuild] Received rebuild request with {ids.Count} ZDOIDs");
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"{e}");
    }

    // var registerInstances = new List<GameObject>();
    List<ZDO> zdos = new();
    foreach (var id in ids)
    {
      var zdo = ZDOMan.instance.GetZDO(id);
      if (zdo == null || !zdo.Persistent)
      {
        ZLog.LogWarning($"[ZDORebuild] Skipping invalid ZDO {id}");
        continue;
      }

      if (zdo.GetOwner() != ZDOMan.GetSessionID())
      {
        LoggerProvider.LogDebug($"[ZDORebuild] was not owner calling setowner");
        zdo.SetOwner(ZDOMan.GetSessionID());
      }

      zdos.Add(zdo);
    }
    StartCoroutine(ForceSpawnOnServerIfItDoesNotExist(zdos));
  }

  public override void Host_SimulateNetwork(string networkId)
  {
    if (!TryBuildPowerNetworkSimData(networkId, out var simData)) return;

    var deltaTime = Time.fixedDeltaTime;
    var changedZDOs = new HashSet<ZDOID>();

    var totalDemand = 0f;

    // Step 1: Total demand
    foreach (var (storage, _) in simData.Storages)
    {
      totalDemand += Mathf.Max(0f, storage.MaxCapacity - storage.StoredEnergy);
    }

    var conduitDemand = 0f;
    foreach (var (conduit, _) in simData.Conduits)
    {
      conduit.SanitizePlayers();
      conduitDemand += PowerConduitPlateComponentIntegration.GetAverageEitr(conduit.Players);
    }

    totalDemand += conduitDemand;
    var isDemanding = totalDemand > 0f;

    // Step 2: Peek storage discharge
    var suppliedFromStorage = 0f;
    var storageDischargeMap = new Dictionary<PowerStorageData, float>();

    var remainingDemand = totalDemand;
    foreach (var (storage, _) in simData.Storages)
    {
      if (remainingDemand <= 0f) break;

      var dischargeAmount = Mathf.Min(storage.StoredEnergy, remainingDemand);
      if (dischargeAmount > 0f)
      {
        storageDischargeMap[storage] = dischargeAmount;
        suppliedFromStorage += dischargeAmount;
        remainingDemand -= dischargeAmount;
      }
    }

    // Step 3: Offer from sources
    var offeredFromSources = 0f;
    var sourceOfferMap = new Dictionary<PowerSourceData, float>();

    const float fuelToEnergy = 10f;
    const float maxFuelRate = 0.1f;

    foreach (var (source, _) in simData.Sources)
    {
      if (remainingDemand <= 0f && !NeedsCharging(simData.Storages)) break;

      var burnable = Mathf.Min(source.Fuel, maxFuelRate * deltaTime);
      var potentialEnergy = burnable * fuelToEnergy;
      var clamped = Mathf.Min(potentialEnergy, source.OutputRate * deltaTime);

      if (clamped > 0f)
      {
        sourceOfferMap[source] = clamped;
        offeredFromSources += clamped;
        remainingDemand -= clamped;
      }
    }

    var totalAvailable = offeredFromSources + suppliedFromStorage;

    // Step 4: Apply to conduits (players needing Eitr)
    foreach (var (conduit, zdo) in simData.Conduits)
    {
      if (totalAvailable <= 0f || conduit.Players.Count == 0) continue;

      var perPlayer = totalAvailable / conduit.Players.Count;
      var used = 0f;

      foreach (var player in conduit.Players)
      {
        if (player.m_eitr < player.m_maxEitr - 0.1f)
        {
          PowerConduitPlateComponentIntegration.Request_AddEitr(player, perPlayer);
          used += perPlayer;
        }
      }

      totalAvailable -= used;
      changedZDOs.Add(zdo.m_uid);
    }

    // Step 5: Feed true surplus into storage
    foreach (var (storage, zdo) in simData.Storages)
    {
      if (totalAvailable <= 0f) break;

      var remainingCap = Mathf.Max(0f, storage.MaxCapacity - storage.StoredEnergy);
      var accepted = Mathf.Min(remainingCap, totalAvailable);

      storage.StoredEnergy += accepted;
      totalAvailable -= accepted;

      changedZDOs.Add(zdo.m_uid);
    }

    // Step 6: Final fuel/discharge commit
    var totalUsed = offeredFromSources + suppliedFromStorage - totalAvailable;
    var usedFromStorage = Mathf.Clamp(totalUsed - offeredFromSources, 0f, suppliedFromStorage);
    var usedFromSources = Mathf.Clamp(totalUsed - usedFromStorage, 0f, offeredFromSources);

    // Discharge
    var toDischarge = usedFromStorage;
    foreach (var kvp in storageDischargeMap)
    {
      var commit = Mathf.Min(kvp.Value, toDischarge);
      kvp.Key.StoredEnergy -= commit;
      toDischarge -= commit;
      if (toDischarge <= 0f) break;
    }

    // Fuel burn
    var toBurn = usedFromSources / fuelToEnergy;
    foreach (var kvp in sourceOfferMap)
    {
      var maxFuel = kvp.Value / fuelToEnergy;
      var burn = Mathf.Min(toBurn, maxFuel);
      kvp.Key.Fuel -= burn;
      toBurn -= burn;
      if (toBurn <= 0f) break;
    }

    // Step 7: ZDO Set
    foreach (var (source, zdo) in simData.Sources)
    {
      zdo.Set(VehicleZdoVars.Power_StoredFuel, source.Fuel);
      changedZDOs.Add(zdo.m_uid);
    }

    foreach (var (storage, zdo) in simData.Storages)
    {
      zdo.Set(VehicleZdoVars.Power_StoredEnergy, storage.StoredEnergy);
      changedZDOs.Add(zdo.m_uid);
    }

    // Step 8: Fire one RPC
    if (changedZDOs.Count > 0)
    {
      var pos = GetNetworkFallbackPosition(simData);
      PowerSystemRPC.SendPowerZDOsChangedToNearbyPlayers(networkId, changedZDOs.ToList(), simData);
    }
  }

  public void SimulateOnClientAndServer()
  {
    if (!isActiveAndEnabled || !ZNet.instance || !ZoneSystem.instance) return;
    if (Time.time < _nextUpdate) return;
    _nextUpdate = Time.time + _updateInterval;

    LoggerProvider.LogInfoDebounced($"_networks, {_networks.Count}, Consumers, {Consumers.Count}, Conduits, {Conduits.Count}, Storages, {Storages.Count}, Sources, {Sources.Count}");


    foreach (var pair in PowerZDONetworkManager.Networks)
    {
      var nodes = pair.Value;

      LoggerProvider.LogInfoDebounced($"Pair Key: {pair.Key}, nodes: {nodes.Count}");

      if (nodes == null || nodes.Count == 0)
      {
        _networksToRemove.Add(pair.Key);
        continue;
      }

      nodes.RemoveAll(n => n == null);

      if (nodes.Count == 0)
      {
        _networksToRemove.Add(pair.Key);
        continue;
      }

      var currentZone = ZoneSystem.GetZone(nodes[0].GetPosition());
      if (!ZoneSystem.instance.IsZoneLoaded(currentZone))
        continue;

      if (ZNet.instance.IsServer())
      {
        Host_SimulateNetworkZDOFull(pair.Key);
      }
    }

    // todo clean this up or keep it but its for client only code. Components do nothing on server since most of them would have to be rendered.
    foreach (var pair in _networks)
    {
      var nodes = pair.Value;
      if (!ZNet.instance.IsDedicated())
      {
        Client_SimulateNetwork(nodes, pair.Key);
      }
      //
      // if (ZNet.instance.IsServer())
      // {
      //   SyncNetworkState(nodes);
      // }
      // else
      // {
      //   SyncNetworkStateClient(nodes);
      // }
    }

    foreach (var key in _networksToRemove)
    {
      _networks.Remove(key);
    }

    _networksToRemove.Clear();
  }


}