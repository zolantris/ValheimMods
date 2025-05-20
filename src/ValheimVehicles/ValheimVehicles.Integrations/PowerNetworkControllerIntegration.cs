// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;

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

  public void SimulateOnClientAndServer()
  {
    if (!isActiveAndEnabled || !ZNet.instance) return;
    if (Time.time < _nextUpdate) return;
    _nextUpdate = Time.time + _updateInterval;

    LoggerProvider.LogInfoDebounced($"_networks, {_networks.Count}, Consumers, {Consumers.Count}, Conduits, {Conduits.Count}, Storages, {Storages.Count}, Sources, {Sources.Count}");

    foreach (var pair in _networks)
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

      var currentZone = ZoneSystem.GetZone(nodes[0].Position);
      if (!ZoneSystem.instance.IsZoneLoaded(currentZone))
        continue;

      if (ZNet.instance.IsServer())
      {
        Host_SimulateNetwork(nodes, pair.Key);
      }

      if (!ZNet.instance.IsDedicated())
      {
        Client_SimulateNetwork(nodes, pair.Key);
      }

      if (ZNet.instance.IsServer())
      {
        SyncNetworkState(nodes);
      }
      else
      {
        SyncNetworkStateClient(nodes);
      }
    }

    foreach (var key in _networksToRemove)
    {
      _networks.Remove(key);
    }

    _networksToRemove.Clear();
  }


}