// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
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
    PlayerEitrRPC.Register();
  }

  // Do nothing for fixed update. Hosts can run for it. But a host/client could freeze and not run this causing massive desyncs for non-hosts. 
  protected override void FixedUpdate()
  {
    SimulateOnClientAndServer();
  }

  /// <summary>
  /// For nesting in a loop so O(n) can be reached.
  /// </summary>
  /// <param name="consumer"></param>
  /// <param name="status"></param>
  /// <param name="poweredOrInactiveConsumers"></param>
  /// <param name="inactiveDemandingConsumers"></param>
  public static void GetNetworkHealthStatusEnumeration(PowerConsumerData? consumer, ref string status, ref int poweredOrInactiveConsumers, ref int inactiveDemandingConsumers)
  {
    if (consumer == null && poweredOrInactiveConsumers == 0 && inactiveDemandingConsumers == 0)
    {
      status = ModTranslations.Power_NetworkInfo_NetworkFullPower;
      return;
    }

    if (!consumer.IsActive && consumer.IsDemanding)
    {
      poweredOrInactiveConsumers++;
    }
    else
    {
      inactiveDemandingConsumers++;
    }

    if (inactiveDemandingConsumers == 0)
    {
      status = ModTranslations.Power_NetworkInfo_NetworkFullPower;
      return;
    }

    if (inactiveDemandingConsumers > 0 && poweredOrInactiveConsumers > 0)
    {
      status = ModTranslations.Power_NetworkInfo_NetworkPartialPower;
      return;
    }

    status = ModTranslations.Power_NetworkInfo_NetworkLowPower;
  }

  public static void Client_SyncNetworkStats(string networkId)
  {
    if (!PowerSystemClusterManager.TryBuildPowerNetworkSimData(networkId, out var simData))
    {
      LoggerProvider.LogError($"Failed to build sim data for network {networkId}");
      return;
    }

    var deltaTime = Time.fixedDeltaTime;

    var totalDemand = 0f;
    var totalCapacity = 0f;

    var totalSupply = 0f;
    var totalFuel = 0f;
    var totalFuelCapacity = 0f;


    var poweredOrInactiveConsumers = 0;
    var inactiveDemandingConsumers = 0;
    var NetworkConsumerPowerStatus = "";

    // Default Needed if there are no consumers.
    GetNetworkHealthStatusEnumeration(null, ref NetworkConsumerPowerStatus, ref poweredOrInactiveConsumers, ref inactiveDemandingConsumers);

    foreach (var data in simData.Consumers)
    {
      totalDemand += data.GetRequestedEnergy(deltaTime);
      GetNetworkHealthStatusEnumeration(data, ref NetworkConsumerPowerStatus, ref poweredOrInactiveConsumers, ref inactiveDemandingConsumers);
    }

    foreach (var data in simData.Storages)
    {
      totalSupply += data.Energy;
      totalCapacity += data.EnergyCapacity;
    }

    foreach (var data in simData.Sources)
    {
      totalFuel += data.Fuel;
      totalFuelCapacity += data.FuelCapacity;
    }

    var newData = new PowerSystemSimulator.PowerSystemDisplayData
    {
      NetworkConsumerPowerStatus = NetworkConsumerPowerStatus,
      NetworkPowerSupply = MathUtils.RoundToHundredth(totalSupply),
      NetworkPowerCapacity = MathUtils.RoundToHundredth(totalCapacity),
      NetworkPowerDemand = MathUtils.RoundToHundredth(totalDemand),
      NetworkFuelSupply = MathUtils.RoundToHundredth(totalFuel),
      NetworkFuelCapacity = MathUtils.RoundToHundredth(totalFuelCapacity)
    };
    newData.Cached_NetworkDataString = GenerateNetworkDataString(networkId, newData);
    UpdateNetworkPowerData(networkId, newData);
  }

  public static void Client_SyncActiveInstances()
  {

    foreach (var powerStorageComponentIntegration in PowerStorageBridge.Instances)
    {
      powerStorageComponentIntegration.Data.Load();
    }

    foreach (var powerStorageComponentIntegration in PowerConduitPlateBridge.Instances)
    {
      powerStorageComponentIntegration.Data.Load();
    }
    foreach (var powerStorageComponentIntegration in PowerSourceBridge.Instances)
    {
      powerStorageComponentIntegration.Data.Load();
    }
    foreach (var powerStorageComponentIntegration in PowerConsumerBridge.Instances)
    {
      powerStorageComponentIntegration.Data.Load();
    }
  }

  public void SimulateOnClientAndServer()
  {
    if (!isActiveAndEnabled || !ZNet.instance || !ZoneSystem.instance) return;
    if (Time.time < _nextUpdate) return;
    _nextUpdate = Time.time + _updateInterval;

    LoggerProvider.LogInfoDebounced($"_networks, {powerNodeNetworks.Count}, Consumers, {Consumers.Count}, Conduits, {Conduits.Count}, Storages, {Storages.Count}, Sources, {Sources.Count}");

    foreach (var pair in PowerSystemClusterManager.Networks)
    {
      var nodes = pair.Value;
      var networkId = pair.Key;

      LoggerProvider.LogInfoDebounced($"Pair Key: {networkId}, nodes: {nodes.Count}");

      if (nodes.Count == 0)
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

      // simulates entire network if there is a network item loaded in the loaded zonesystem.
      var isLoadedInZone = nodes.Any((x) => ZoneSystem.instance.IsZoneLoaded(x.GetPosition()));
      if (!isLoadedInZone)
        continue;

      if (ZNet.instance.IsServer())
      {
        if (PowerSystemClusterManager.TryBuildPowerNetworkSimData(networkId, out var simData))
        {
          simData.NetworkId = networkId;
          simData.DeltaTime = Time.fixedDeltaTime;
          PowerSystemSimulator.Simulate(simData);
          var zdoidNodes = nodes.Select(x => x.m_uid).ToList();
          PowerSystemRPC.Request_PowerZDOsChangedToNearbyPlayers(networkId, zdoidNodes, simData);
        }
        else
        {
          LoggerProvider.LogError($"Failed to build sim data for network {networkId}");
        }
      }

      // only dedicated server.
      if (!ZNet.instance.IsDedicated())
      {
        Client_SyncActiveInstances();
        Client_SyncNetworkStats(networkId);
      }
    }

    foreach (var key in _networksToRemove)
    {
      powerNodeNetworks.Remove(key);
    }

    _networksToRemove.Clear();
  }
}