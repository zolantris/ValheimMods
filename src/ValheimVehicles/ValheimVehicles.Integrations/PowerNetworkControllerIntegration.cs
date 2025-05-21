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

  public override void Awake()
  {
    LoggerProvider.LogDebug("Called Awake with debug");
    LoggerProvider.LogMessage("Called Awake with Message");
    base.Awake();
    LoggerProvider.LogMessage("Called post awake with message");
    StartCoroutine(DelayedRegister());
  }

  public IEnumerator DelayedRegister()
  {
    while (ZNet.instance == null || ZRoutedRpc.instance == null)
    {
      yield return null;
    }

    ZDOClaimUtility.RegisterClaimZdoRpc();
    PowerSystemRPC.Register();
    StartDirtyNetworkCoroutine();
  }

  // Do nothing for fixed update. Hosts can run for it. But a host/client could freeze and not run this causing massive desyncs for non-hosts. 
  protected override void FixedUpdate()
  {
    SimulateOnClientAndServer();
  }

  protected override void Update()
  {
    base.Update();
  }

  public void SimulateAllNetworks(List<IPowerNode> allNodes)
  {
    var groupedByNetwork = new Dictionary<string, List<IPowerNode>>();
    CachedSimulateData.Clear();

    foreach (var node in allNodes)
    {
      if (!ZNetScene.instance || node == null) continue;

      var netView = node.gameObject.GetComponent<ZNetView>();
      if (!netView || !netView.IsValid()) continue;

      var zdo = netView.GetZDO();
      if (zdo == null) continue;

      var networkId = zdo.GetString(VehicleZdoVars.Power_NetworkId);
      if (string.IsNullOrEmpty(networkId)) continue;

      if (!groupedByNetwork.TryGetValue(networkId, out var list))
      {
        list = new List<IPowerNode>();
        groupedByNetwork[networkId] = list;
      }

      list.Add(node);
    }

    foreach (var kvp in groupedByNetwork)
    {
      var networkId = kvp.Key;
      var nodes = kvp.Value;

      MarkNetworkDirty(networkId);
      Client_SimulateNetwork(nodes, networkId, false);
    }
  }
  private static readonly HashSet<string> _dirtyNetworkIds = new();
  private float _lastSimulateTime;

  private Coroutine _simulateCoroutine;

  private const float IdleDelay = 1f;
  private const float MaxDelay = 3f;

  public void StartDirtyNetworkCoroutine()
  {
    if (_simulateCoroutine != null) return;
    _simulateCoroutine = StartCoroutine(SimulateDirtyNetworksCoroutine());
  }

  public void StopDirtyNetworkCoroutine()
  {
    if (_simulateCoroutine != null)
    {
      StopCoroutine(_simulateCoroutine);
      _simulateCoroutine = null;
    }
  }

  public static void MarkNetworkDirty(string networkId)
  {
    _dirtyNetworkIds.Add(networkId);
  }

  private IEnumerator SimulateDirtyNetworksCoroutine()
  {
    while (isActiveAndEnabled)
    {
      yield return new WaitForSeconds(0.5f);

      if (_dirtyNetworkIds.Count == 0) continue;

      var now = Time.time;
      var canRun = now - _lastSimulateTime >= IdleDelay;

      if (canRun || now - _lastSimulateTime >= MaxDelay)
      {
        foreach (var networkId in _dirtyNetworkIds.ToList())
        {
          if (!PowerZDONetworkManager.Networks.TryGetValue(networkId, out var zdos)) continue;

          var nodes = GetNodesFromZDOs(zdos);
          Client_SimulateNetwork(nodes, networkId);
        }

        _dirtyNetworkIds.Clear();
        _lastSimulateTime = now;
      }
    }
  }

  public override IEnumerator RequestRebuildPowerNetworkCoroutine()
  {
    // Skip this as it does too much now. Only need to reference our ZDOS then match them to powernodes
    // yield return base.RequestRebuildPowerNetworkCoroutine();

    // Updates the power nodes with the latest simulation. This is for all rendered nodes and will only be accurate on clients.
    UpdateAllPowerNodes();

    yield return null;
    // for creating a network of IPowerNodes using our new logic
    SimulateAllNetworks(AllPowerNodes);

    yield return null;
  }

  private static List<IPowerNode> GetNodesFromZDOs(List<ZDO> zdos)
  {
    var nodes = new List<IPowerNode>();
    foreach (var zdo in zdos)
    {
      if (ZNetScene.instance == null) continue;

      var go = ZNetScene.instance.FindInstance(zdo.m_uid);
      if (go == null) continue;

      var components = go.GetComponents<MonoBehaviour>();
      foreach (var comp in components)
      {
        if (comp is IPowerNode node)
        {
          nodes.Add(node);
          break;
        }
      }
    }
    return nodes;
  }

  public override void Host_SimulateNetwork(string networkId)
  {
    if (!TryBuildPowerNetworkSimData(networkId, out var simData))
    {
      LoggerProvider.LogError($"Failed to build sim data for network {networkId}");
      return;
    }

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
      conduit.ResolvePlayersFromIds();
      conduitDemand += PowerConduitData.GetAverageEitr(conduit.Players);
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

      var clamped = source.GetMaxPotentialOutput(deltaTime);

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
      var burntEnergy = kvp.Key.ProducePower(kvp.Value);
      toBurn -= kvp.Key.EstimateFuelCost(burntEnergy);
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

  private static bool NeedsCharging(List<(PowerStorageData storage, ZDO zdo)> storages)
  {
    foreach (var (storage, _) in storages)
    {
      if (storage.StoredEnergy < storage.MaxCapacity)
        return true;
    }
    return false;
  }

  private static Vector3 GetNetworkFallbackPosition(PowerNetworkSimData simData)
  {
    if (simData.Conduits.Count > 0)
      return simData.Conduits[0].Item2.GetPosition();
    if (simData.Storages.Count > 0)
      return simData.Storages[0].Item2.GetPosition();
    if (simData.Sources.Count > 0)
      return simData.Sources[0].Item2.GetPosition();
    return Vector3.zero;
  }

  public void Client_SyncNetwork(string networkId)
  {
    if (!TryBuildPowerNetworkSimData(networkId, out var simData))
    {
      LoggerProvider.LogError($"Failed to build sim data for network {networkId}");
      return;
    }

    var deltaTime = Time.fixedDeltaTime;

    var totalDemand = 0f;

    // pure power units
    var totalConduitDemand = 0f;

    // pure power units
    var totalConduitSupply = 0f;

    var totalSupply = 0f;
    var totalFuel = 0f;

    var chargeConduits = new List<PowerConduitData>();
    var drainConduits = new List<PowerConduitData>();

    foreach (var (data, zdo) in simData.Conduits)
    {
      data.Load();
      if (data.Mode == PowerConduitMode.Drain)
      {
        drainConduits.Add(data);
      }
      if (data.Mode == PowerConduitMode.Charge)
      {
        chargeConduits.Add(data);
      }
    }

    foreach (var (data, zdo) in simData.Consumers)
    {
      data.Load();
    }

    foreach (var (data, zdo) in simData.Storages)
    {
      data.Load();
    }

    foreach (var (data, zdo) in simData.Sources)
    {
      data.Load();
    }
  }

  public void SimulateOnClientAndServer()
  {
    if (!isActiveAndEnabled || !ZNet.instance || !ZoneSystem.instance) return;
    if (Time.time < _nextUpdate) return;
    _nextUpdate = Time.time + _updateInterval;

    LoggerProvider.LogInfoDebounced($"_networks, {powerNodeNetworks.Count}, Consumers, {Consumers.Count}, Conduits, {Conduits.Count}, Storages, {Storages.Count}, Sources, {Sources.Count}");

    // todo remove this, for testing right now.
    CachedSimulateData.Clear();

    foreach (var pair in PowerZDONetworkManager.Networks)
    {
      var nodes = pair.Value;
      var networkId = pair.Key;


      // todo remove this, for testing right now.
      MarkNetworkDirty(networkId);

      LoggerProvider.LogInfoDebounced($"Pair Key: {networkId}, nodes: {nodes.Count}");

      if (nodes == null || nodes.Count == 0)
      {
        _networksToRemove.Add(networkId);
        continue;
      }

      nodes.RemoveAll(n => n == null);

      if (nodes.Count == 0)
      {
        _networksToRemove.Add(networkId);
        continue;
      }

      var isLoadedInZone = nodes.Any((x) => ZoneSystem.instance.IsZoneLoaded(x.GetPosition()));
      if (isLoadedInZone)
        continue;

      if (ZNet.instance.IsServer())
      {
        Host_SimulateNetwork(networkId);
      }
      else
      {
        Client_SyncNetwork(networkId);
      }
    }

    // // todo clean this up or keep it but its for client only code. Components do nothing on server since most of them would have to be rendered.
    // foreach (var pair in powerNodeNetworks)
    // {
    //   var nodes = pair.Value;
    //   var networkId = pair.Key;
    //   if (!ZNet.instance.IsDedicated())
    //   {
    //     Client_SimulateNetwork(nodes, networkId);
    //   }
    //   //
    //   if (ZNet.instance.IsServer())
    //   {
    //     SyncNetworkState(nodes);
    //   }
    //   else
    //   {
    //     SyncNetworkStateClient(nodes);
    //   }
    // }

    foreach (var key in _networksToRemove)
    {
      powerNodeNetworks.Remove(key);
    }

    _networksToRemove.Clear();
  }
}