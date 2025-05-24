// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;
using ZdoWatcher;

namespace ValheimVehicles.Integrations
{
  public static class PowerSystemClusterManager
  {
    internal static readonly HashSet<int> _powerHashes = new();
    internal static readonly Dictionary<int, string> _powerHashToName = new();
    internal static readonly Dictionary<string, int> _powerNameToHash = new();
    internal static readonly float MaxJoinDistance = 16f;
    internal static readonly float MaxJoinSqr = MaxJoinDistance * MaxJoinDistance;

    internal static readonly Dictionary<string, List<ZDO>> _networks = new();
    public static IReadOnlyDictionary<string, List<ZDO>> Networks => _networks;
    internal static readonly Dictionary<string, PowerSimulationData> _cachedSimulateData = new();

    public static void Init()
    {
      RegisterHashes();
      ZdoWatchController.OnDeserialize += zdo => RegisterPowerData(zdo);
      ZdoWatchController.OnLoad += zdo => RegisterPowerData(zdo);
      ZdoWatchController.OnReset += zdo => RemovePowerData(zdo);
      LoggerProvider.LogInfo("[PowerZDONetworkManager] Init complete.");
    }

    private static void RegisterHashes()
    {
      // power pylons/extenders/utility
      RegisterHash(PrefabNameHashes.Mechanism_Power_Pylon, PrefabNames.Mechanism_Power_Pylon);

      // power sources
      RegisterHash(PrefabNameHashes.Mechanism_Power_Source_Coal, PrefabNames.Mechanism_Power_Source_Coal);
      RegisterHash(PrefabNameHashes.Mechanism_Power_Source_Eitr, PrefabNames.Mechanism_Power_Source_Eitr);

      // power-conduits/activators
      RegisterHash(PrefabNameHashes.Mechanism_Power_Conduit_Charge_Plate, PrefabNames.Mechanism_Power_Conduit_Charge_Plate);
      RegisterHash(PrefabNameHashes.Mechanism_Power_Conduit_Drain_Plate, PrefabNames.Mechanism_Power_Conduit_Drain_Plate);

      // power-consumer mechanisms
      RegisterHash(PrefabNameHashes.Mechanism_Power_Consumer_Swivel, PrefabNames.SwivelPrefabName);
      RegisterHash(PrefabNameHashes.Mechanism_Power_Consumer_LandVehicle, PrefabNames.LandVehicle);

#if DEBUG
      // todo add for water vehicles
      // _powerHashes.Add(PrefabNameHashes.Mechanism_Power_Consumer_WaterVehicle);
#endif

      // power storages
      RegisterHash(PrefabNameHashes.Mechanism_Power_Storage_Eitr, PrefabNames.Mechanism_Power_Storage_Eitr);
    }

    public static void RegisterHash(int hash, string hashName)
    {
      _powerHashes.Add(hash);
      if (!_powerHashToName.ContainsKey(hash)) _powerHashToName[hash] = hashName;
      if (!_powerNameToHash.ContainsKey(hashName)) _powerNameToHash[hashName] = hash;
    }

    // --- Registration/Removal, all handled by PowerNetworkDataManager ---

    private static void RegisterPowerData(ZDO zdo)
    {
      if (zdo == null || !_powerHashes.Contains(zdo.m_prefab)) return;
      if (PowerSystemRegistry.ContainsZDO(zdo)) return; // already registered

      PowerSystemComputeData data = null;

      switch (zdo.m_prefab)
      {
        case var p when p == PrefabNameHashes.Mechanism_Power_Conduit_Charge_Plate:
        case var p2 when p2 == PrefabNameHashes.Mechanism_Power_Conduit_Drain_Plate:
          data = new PowerConduitData(zdo)
          {
            Mode = PowerConduitData.GetConduitVariant(zdo)
          };
          break;
        case var p when p == PrefabNameHashes.Mechanism_Power_Storage_Eitr:
          data = new PowerStorageData(zdo);
          break;
        case var p when p == PrefabNameHashes.Mechanism_Power_Consumer_Swivel:
          data = new PowerConsumerData(zdo);
          break;
        case var p when p == PrefabNameHashes.Mechanism_Power_Source_Eitr:
        case var p2 when p2 == PrefabNameHashes.Mechanism_Power_Source_Coal:
          data = new PowerSourceData(zdo);
          break;
        case var p when p == PrefabNameHashes.Mechanism_Power_Pylon:
          data = new PowerPylonData(zdo);
          break;
        default:
          return;
      }

      if (data != null)
      {
        PowerSystemRegistry.Register(zdo, data);
      }
    }

    private static void RemovePowerData(ZDO zdo)
    {
      if (zdo == null) return;
      PowerSystemRegistry.Unregister(zdo);
    }

    // --- Cluster/network rebuilds and logging ---

    public static void RebuildClusters()
    {
      _networks.Clear();
      ResetPowerNetworkSimDataCache();

      var zdos = PowerSystemRegistry.GetAllZDOs();
      var unvisited = new HashSet<ZDO>(zdos);
      var pending = new Queue<ZDO>();

      while (unvisited.Count > 0)
      {
        var root = unvisited.First();
        unvisited.Remove(root);
        var cluster = new List<ZDO>();
        var networkId = Guid.NewGuid().ToString();

        pending.Clear();
        pending.Enqueue(root);

        while (pending.Count > 0)
        {
          var current = pending.Dequeue();
          cluster.Add(current);
          current.Set(VehicleZdoVars.PowerSystem_NetworkId, networkId);

          foreach (var other in unvisited.ToList())
          {
            if ((current.GetPosition() - other.GetPosition()).sqrMagnitude <= MaxJoinSqr)
            {
              pending.Enqueue(other);
              unvisited.Remove(other);
            }
          }
        }

        if (cluster.Count > 0)
        {
          var maxDist = cluster.Max(z => Vector3.Distance(z.GetPosition(), cluster[0].GetPosition()));
          if (maxDist > 250f)
            LoggerProvider.LogWarning($"[PowerZDONetworkManager] Large network {networkId} spans {maxDist:F1} units");

          _networks[networkId] = cluster;
        }
      }

      NetworkRebuildLogger();
    }

    public static void NetworkRebuildLogger()
    {
      LoggerProvider.LogDebug($"[PowerZDONetworkManager] Rebuilt {_networks.Count} networks from {PowerSystemRegistry.GetAllZDOs().Count} ZDOs.");

#if DEBUG
      foreach (var keyValuePair in _networks)
      {
        var loggerString = $"[PowerZDONetworkManager] Network {keyValuePair.Key} has {keyValuePair.Value.Count} ZDOs.";
        var nearest = string.Empty;
        keyValuePair.Value.ForEach(zdo =>
        {
          if (zdo == null) return;
          if (!_powerHashToName.TryGetValue(zdo.m_prefab, out var prefabName))
          {
            prefabName = "Unknown";
          }
          if (nearest == string.Empty)
          {
            nearest = $"\nPosition: {zdo.GetPosition()}";
            loggerString += nearest;
          }
          loggerString += $"\n\tPrefabName: <{prefabName}>, Hash: <{zdo.m_prefab}>";
        });
        LoggerProvider.LogDebugDebounced(loggerString);
      }
#endif
    }

    // --- Simulation Data Caching/Rebuild ---

    public static bool TryBuildPowerNetworkSimData(string networkId, out PowerSimulationData simData)
    {
      if (!_cachedSimulateData.TryGetValue(networkId, out simData))
      {
        if (!Networks.TryGetValue(networkId, out var zdos) || zdos == null)
          return false;

        BuildPowerNetworkSimData(networkId, zdos);
        simData = _cachedSimulateData[networkId]; // safe: just built
      }
      return simData != null;
    }

    public static void ResetPowerNetworkSimDataCache()
    {
      _cachedSimulateData.Clear();
    }

    public static void BuildPowerNetworkSimData(string networkId, List<ZDO> zdos)
    {
      var simData = new PowerSimulationData();

      LoggerProvider.LogDev($"[SIM] Rebuilding {networkId} with {zdos.Count} ZDOs");

      foreach (var zdo in zdos)
      {
        var prefab = zdo.GetPrefab();
        if (prefab == PrefabNameHashes.Mechanism_Power_Source_Coal ||
            prefab == PrefabNameHashes.Mechanism_Power_Source_Eitr)
        {
          if (PowerComputeFactory.TryCreateSource(zdo, out var source))
            simData.Sources.Add(source);
        }
        else if (prefab == PrefabNameHashes.Mechanism_Power_Storage_Eitr)
        {
          if (PowerComputeFactory.TryCreateStorage(zdo, out var storage))
            simData.Storages.Add(storage);
        }
        else if (prefab == PrefabNameHashes.Mechanism_Power_Consumer_Swivel || prefab == PrefabNameHashes.Mechanism_Power_Consumer_LandVehicle)
        {
          if (PowerComputeFactory.TryCreateConsumer(zdo, out var consumer))
            simData.Consumers.Add(consumer);
        }
        else if (prefab == PrefabNameHashes.Mechanism_Power_Conduit_Charge_Plate ||
                 prefab == PrefabNameHashes.Mechanism_Power_Conduit_Drain_Plate)
        {
          if (PowerComputeFactory.TryCreateConduit(zdo, out var conduit))
          {
            conduit.PlayerDataById.Clear();
            simData.Conduits.Add(conduit);
          }
        }
      }

      _cachedSimulateData[networkId] = simData;
    }
  }
}