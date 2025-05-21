// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;
using ZdoWatcher;

namespace ValheimVehicles.Integrations
{
  public static class PowerZDONetworkManager
  {
    internal static readonly List<ZDO> _trackedZDOs = new();
    internal static readonly Dictionary<ZDOID, ZDO> _zdoMap = new();
    internal static readonly HashSet<int> _powerHashes = new();
    internal static readonly Dictionary<int, string> _powerHashToName = new();
    internal static readonly Dictionary<string, int> _powerNameToHash = new();
    internal static readonly float MaxJoinDistance = 16f;
    internal static readonly float MaxJoinSqr = MaxJoinDistance * MaxJoinDistance;
    internal static readonly Dictionary<string, List<ZDO>> _networks = new();
    public static IReadOnlyDictionary<string, List<ZDO>> Networks => _networks;
    internal static readonly Dictionary<string, PowerNetworkSimData> _cachedSimulateData = new();
    // New: Map of ZDOID to their associated PowerDataBase
    internal static readonly Dictionary<ZDOID, PowerSystemComputeData> _powerDataMap = new();
    public static readonly Dictionary<ZDOID, IPowerComputeBase> activePowerDataComponents = new();

    public static void Init()
    {
      RegisterHashes();
      ZdoWatchController.OnDeserialize += TryAdd;
      ZdoWatchController.OnLoad += TryAdd;
      ZdoWatchController.OnReset += TryRemove;
      LoggerProvider.LogInfo("[PowerZDONetworkManager] Init complete.");
    }

    public static void RegisterHash(int hash, string hashName)
    {
      _powerHashes.Add(hash);
      if (!_powerHashToName.ContainsKey(hash)) _powerHashToName[hash] = hashName;
      if (!_powerNameToHash.ContainsKey(hashName)) _powerNameToHash[hashName] = hash;
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

    public static void TryAdd(ZDO zdo)
    {
      if (zdo == null || _zdoMap.ContainsKey(zdo.m_uid) || !_powerHashes.Contains(zdo.m_prefab)) return;
      _trackedZDOs.Add(zdo);
      _zdoMap[zdo.m_uid] = zdo;

      // Register PowerData
      RegisterPowerData(zdo);

      PowerNetworkRebuildScheduler.Trigger();
    }

    public static void TryRemove(ZDO zdo)
    {
      if (zdo == null || !_zdoMap.ContainsKey(zdo.m_uid)) return;
      _trackedZDOs.Remove(zdo);
      _zdoMap.Remove(zdo.m_uid);

      // Remove PowerData
      RemovePowerData(zdo);

      PowerNetworkRebuildScheduler.Trigger();
    }

    public static IEnumerable<ZDO> GetAllTrackedZDOs()
    {
      return _trackedZDOs;
    }

    public static void NetworkRebuildLogger()
    {
      LoggerProvider.LogDebug($"[PowerZDONetworkManager] Rebuilt {_networks.Count} networks from {_trackedZDOs.Count} ZDOs.");

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

    public static void RebuildClusters()
    {
      _networks.Clear();
      ResetPowerNetworkSimDataCache();

      var unvisited = new HashSet<ZDO>(_trackedZDOs);
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
          current.Set(VehicleZdoVars.Power_NetworkId, networkId);

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

    // New API for retrieving data
    public static bool TryGetData<T>(ZDO zdo, [NotNullWhen(true)] out T data, bool CanAdd = false) where T : PowerSystemComputeData
    {
      if (_powerDataMap.TryGetValue(zdo.m_uid, out var baseData) && baseData is T typedData)
      {
        data = typedData;
        return true;
      }

      if (CanAdd)
      {
        TryAdd(zdo);
        if (_powerDataMap.TryGetValue(zdo.m_uid, out baseData) && baseData is T newTypedData)
        {
          data = newTypedData;
          return true;
        }
      }

      data = null;
      return false;
    }


    /// <summary>
    /// For removing and side-effects of removing data.
    /// </summary>
    /// <param name="zdo"></param>
    private static void RemovePowerData(ZDO zdo)
    {
      _powerDataMap.Remove(zdo.m_uid);
    }


    public static void RegisterPowerComponentUpdater(ZDOID zdoid, IPowerComputeBase powerComputeComponent)
    {
      if (!activePowerDataComponents.ContainsKey(zdoid))
      {
        activePowerDataComponents.Add(zdoid, powerComputeComponent);
      }
    }

    public static bool TryGetActiveComponentUpdater(ZDOID zdoid, [NotNullWhen(true)] out IPowerComputeBase powerComputeComponent)
    {
      return activePowerDataComponents.TryGetValue(zdoid, out powerComputeComponent);
    }

    public static void RemovePowerComponentUpdater(ZDOID zdoid)
    {
      activePowerDataComponents.Remove(zdoid);
    }

    private static void RegisterPowerData(ZDO zdo)
    {
      if (_powerDataMap.ContainsKey(zdo.m_uid)) return;

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

        // 🔋 Power Storage
        case var p when p == PrefabNameHashes.Mechanism_Power_Storage_Eitr:
          data = new PowerStorageData(zdo);
          break;

        case var p when p == PrefabNameHashes.Mechanism_Power_Consumer_Swivel:
          // case var p1 when p1 == PrefabNameHashes.Mechanism_Power_Consumer_LandVehicle:
          // case var p2 when p2 == PrefabNameHashes.Mechanism_Power_Consumer_WaterVehicle:
          data = new PowerConsumerData(zdo);
          break;

        // ⚡ Power Source
        case var p when p == PrefabNameHashes.Mechanism_Power_Source_Eitr:
        case var p2 when p2 == PrefabNameHashes.Mechanism_Power_Source_Coal:
          data = new PowerSourceData(zdo);
          break;

        // 🗼 Power Pylon
        case var p when p == PrefabNameHashes.Mechanism_Power_Pylon:
          data = new PowerPylonData(zdo);
          break;

        default:
          return;
      }

      if (data != null)
      {
        _powerDataMap[zdo.m_uid] = data;
      }
    }


    public static bool TryBuildPowerNetworkSimData(string networkId, out PowerNetworkSimData simData)
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
      var simData = new PowerNetworkSimData();

      LoggerProvider.LogDev($"[SIM] Rebuilding {networkId} with {zdos.Count} ZDOs");

      foreach (var (source, zdo) in simData.Sources)
      {
        LoggerProvider.LogDev($"[SIM] Source: {zdo.m_uid}, output = {source.OutputRate}");
      }

      foreach (var (storage, zdo) in simData.Storages)
      {
        LoggerProvider.LogDev($"[SIM] Storage: {zdo.m_uid}, stored = {storage.StoredEnergy}");
      }

      foreach (var (conduit, zdo) in simData.Conduits)
      {
        LoggerProvider.LogDev($"[SIM] Conduit: {zdo.m_uid}, players = {conduit.Players.Count}");
      }

      foreach (var zdo in zdos)
      {
        var prefab = zdo.GetPrefab();
        if (prefab == PrefabNameHashes.Mechanism_Power_Source_Coal ||
            prefab == PrefabNameHashes.Mechanism_Power_Source_Eitr)
        {
          if (PowerComputeFactory.TryCreateSource(zdo, out var source))
            simData.Sources.Add((source, zdo));
        }
        else if (prefab == PrefabNameHashes.Mechanism_Power_Storage_Eitr)
        {
          if (PowerComputeFactory.TryCreateStorage(zdo, out var storage))
            simData.Storages.Add((storage, zdo));
        }
        else if (prefab == PrefabNameHashes.Mechanism_Power_Consumer_Swivel || prefab == PrefabNameHashes.Mechanism_Power_Consumer_LandVehicle)
        {
          if (PowerComputeFactory.TryCreateConsumer(zdo, out var consumer))
          {
            simData.Consumers.Add((consumer, zdo));
          }
        }
        else if (prefab == PrefabNameHashes.Mechanism_Power_Conduit_Charge_Plate ||
                 prefab == PrefabNameHashes.Mechanism_Power_Conduit_Drain_Plate)
        {
          if (PowerComputeFactory.TryCreateConduit(zdo, out var conduit))
          {
            var zdoid = zdo.m_uid;
            conduit.PlayerIds.Clear();
            conduit.Players.Clear();

            if (TryGetData<PowerConduitData>(zdo, out var data))
            {
              conduit.PlayerIds.Clear();
              conduit.Players.Clear();

              foreach (var pid in data.PlayerIds)
              {
                conduit.PlayerIds.Add(pid);
                var player = Player.GetPlayer(pid);
                if (player != null)
                {
                  conduit.Players.Add(player);
                }
              }
            }

            simData.Conduits.Add((conduit, zdo));
          }
        }
      }

      _cachedSimulateData[networkId] = simData;
    }
  }
}