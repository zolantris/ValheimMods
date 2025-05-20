// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using ValheimVehicles.SharedScripts;
using ZdoWatcher;

namespace ValheimVehicles.Integrations
{
  public abstract class PowerZDOWatcherDelegate
  {
    private static readonly List<ZDO> AllTrackedZDOs = new();
    private static readonly Dictionary<ZDOID, ZDO> ZdoMap = new();
    private static readonly HashSet<int> ValidPrefabHashes = new();

    public static void RegisterToZdoManager()
    {
      Init();

      ZdoWatchController.OnDeserialize += Add;
      ZdoWatchController.OnLoad += Add;
      ZdoWatchController.OnReset += Remove;

      LoggerProvider.LogInfo("[PowerZDOWatcherDelegate] Initialized with prefab hash filters and ZDO hooks.");
    }


    public static void Init()
    {
      // Register all known power-related prefab hashes
      RegisterPowerHash(PrefabNameHashes.Mechanism_Power_Pylon);
      RegisterPowerHash(PrefabNameHashes.Mechanism_Power_Source_Coal);
      RegisterPowerHash(PrefabNameHashes.Mechanism_Power_Source_Eitr);
      RegisterPowerHash(PrefabNameHashes.Mechanism_Power_Consumer_Charge_Plate);
      RegisterPowerHash(PrefabNameHashes.Mechanism_Power_Consumer_Drain_Plate);
      RegisterPowerHash(PrefabNameHashes.Mechanism_Power_Storage_Eitr);
    }

    /// <summary>
    /// Add a prefab hash to be tracked by the power system.
    /// Should be called at startup.
    /// </summary>
    public static void RegisterPowerHash(int prefabHash)
    {
      ValidPrefabHashes.Add(prefabHash);
    }

    public static bool IsPowerZDO(ZDO zdo)
    {
      return zdo != null && ValidPrefabHashes.Contains(zdo.GetPrefab());
    }

    public static void Add(ZDO zdo)
    {
      if (!IsPowerZDO(zdo)) return;
      if (ZdoMap.ContainsKey(zdo.m_uid)) return;

      ZdoMap[zdo.m_uid] = zdo;
      AllTrackedZDOs.Add(zdo);

      PowerNetworkRebuildScheduler.Trigger();
    }

    public static void Remove(ZDO zdo)
    {
      if (!IsPowerZDO(zdo)) return;

      if (ZdoMap.Remove(zdo.m_uid))
        AllTrackedZDOs.Remove(zdo);

      PowerNetworkRebuildScheduler.Trigger();
    }

    public static IEnumerable<ZDO> GetAll()
    {
      return AllTrackedZDOs;
    }

    public static bool TryGet(ZDOID id, out ZDO zdo)
    {
      return ZdoMap.TryGetValue(id, out zdo);
    }

    public static bool IsTracked(ZDOID id)
    {
      return ZdoMap.ContainsKey(id);
    }

    public static void Clear()
    {
      AllTrackedZDOs.Clear();
      ZdoMap.Clear();
    }
  }
}