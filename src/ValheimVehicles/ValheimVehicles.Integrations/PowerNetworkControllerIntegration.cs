// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.RPC;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using Logger = HarmonyLib.Tools.Logger;

namespace ValheimVehicles.Integrations;

public partial class PowerNetworkControllerIntegration : PowerNetworkController
{
  private readonly List<string> _networksToRemove = new();
  public static bool CanRunClientOnDedicated = false;

  public float lastUpdate = 0f;
  public float lastDeltaTime = 0f;
  public bool canLateUpdate = false;


  protected override void Update()
  {
    base.Update();

    if (Time.time < _nextUpdate) return;
    lastUpdate = _nextUpdate;
    _nextUpdate = Time.time + _updateInterval;
    canLateUpdate = true;
    lastDeltaTime = _nextUpdate - lastUpdate;

    // doing this outside physics update is better otherwise host will pause power simulation when pausing.
    Server_Simulate();
  }

  // Do nothing for fixed update. Hosts can run for it. But a host/client could freeze and not run this causing massive desyncs for non-hosts. 
  protected override void FixedUpdate()
  {
    // SimulateOnClientAndServer();
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

  public static void Client_SyncNetworkStats(string networkId, float dt)
  {
    if (!PowerSystemClusterManager.TryBuildPowerNetworkSimData(networkId, out var simData))
    {
      LoggerProvider.LogError($"Failed to build sim data for network {networkId}");
      return;
    }

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
      totalDemand += data.GetRequestedEnergy(dt);
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


  public void Server_Simulate()
  {
    if (!isActiveAndEnabled || !ZNet.instance || !ZoneSystem.instance || !ZNet.instance.IsServer()) return;

    foreach (var pair in PowerSystemClusterManager.Networks)
    {
      var nodes = pair.Value;
      var networkId = pair.Key;

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

      // simulates entire network if there is a network item loaded in each peer's area.
      if (!RPCUtils.HasNearbyPlayersOrPeers(nodes, PowerSystemConfig.PowerSimulationDistanceThreshold.Value)) return;


      if (PowerSystemClusterManager.TryBuildPowerNetworkSimData(networkId, out var simData))
      {
        simData.NetworkId = networkId;
        simData.DeltaTime = lastDeltaTime;
        PowerSystemSimulator.Simulate(simData);
        var zdoidNodes = nodes.Select(x => x.m_uid).ToList();
        PowerSystemRPC.Request_PowerZDOsChangedToNearbyPlayers(networkId, zdoidNodes, simData);
      }
      else
      {
        LoggerProvider.LogError($"Failed to build sim data for network {networkId}");
      }
    }

    foreach (var key in _networksToRemove)
    {
      powerNodeNetworks.Remove(key);
    }

    _networksToRemove.Clear();
  }


  public void LateUpdate()
  {
    if (!canLateUpdate || !ZNet.instance) return;
    if (ZNet.instance.IsDedicated() && !CanRunClientOnDedicated) return;

    canLateUpdate = false;
    foreach (var pair in PowerSystemClusterManager.Networks)
    {
      var networkId = pair.Key;
      Client_SyncActiveInstances();
      Client_SyncNetworkStats(networkId, lastDeltaTime);
    }
  }
}