// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;
using ZdoWatcher;

namespace ValheimVehicles.Integrations
{
  public static class PowerZDONetworkManager
  {
    private static readonly List<ZDO> _trackedZDOs = new();
    private static readonly Dictionary<ZDOID, ZDO> _zdoMap = new();
    private static readonly HashSet<int> _powerHashes = new();
    private static readonly float MaxJoinDistance = 16f;
    private static readonly float MaxJoinSqr = MaxJoinDistance * MaxJoinDistance;
    private static readonly Dictionary<string, List<ZDO>> _networks = new();
    public static IReadOnlyDictionary<string, List<ZDO>> Networks => _networks;

    // New: Map of ZDOID to their associated PowerDataBase
    private static readonly Dictionary<ZDOID, PowerDataBase> _powerDataMap = new();

    public static void Init()
    {
      RegisterHashes();
      ZdoWatchController.OnDeserialize += TryAdd;
      ZdoWatchController.OnLoad += TryAdd;
      ZdoWatchController.OnReset += TryRemove;
      LoggerProvider.LogInfo("[PowerZDONetworkManager] Init complete.");
    }

    private static void RegisterHashes()
    {
      _powerHashes.Add(PrefabNameHashes.Mechanism_Power_Pylon);
      _powerHashes.Add(PrefabNameHashes.Mechanism_Power_Source_Coal);
      _powerHashes.Add(PrefabNameHashes.Mechanism_Power_Source_Eitr);
      _powerHashes.Add(PrefabNameHashes.Mechanism_Power_Consumer_Charge_Plate);
      _powerHashes.Add(PrefabNameHashes.Mechanism_Power_Consumer_Drain_Plate);
      _powerHashes.Add(PrefabNameHashes.Mechanism_Power_Storage_Eitr);
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
      _powerDataMap.Remove(zdo.m_uid);

      PowerNetworkRebuildScheduler.Trigger();
    }

    public static IEnumerable<ZDO> GetAllTrackedZDOs()
    {
      return _trackedZDOs;
    }

    public static void RebuildClusters()
    {
      _networks.Clear();

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

      LoggerProvider.LogInfo($"[PowerZDONetworkManager] Rebuilt {_networks.Count} networks from {_trackedZDOs.Count} ZDOs.");
    }

    // New API for retrieving data
    public static bool TryGetData<T>(ZDO zdo, [NotNullWhen(true)] out T data, bool CanAdd = false) where T : PowerDataBase
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

    private static void RegisterPowerData(ZDO zdo)
    {
      if (_powerDataMap.ContainsKey(zdo.m_uid)) return;

      PowerDataBase data = null;

      switch (zdo.m_prefab)
      {
        case var p when p == PrefabNameHashes.Mechanism_Power_Consumer_Charge_Plate:
        case var p2 when p2 == PrefabNameHashes.Mechanism_Power_Consumer_Drain_Plate:
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
  }
}