using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
namespace ValheimVehicles.Integrations.PowerSystem;

/// <summary>
/// Meant for Valheim objects and bridging between valheim objects and our network data. This should not be tested within Unity as it's an integration-only component.
/// </summary>
public static class PowerSystemRegistry
{
  // Backing sets/maps (all use reference or value equality)
  private static readonly Dictionary<ZDOID, PowerNetworkData> _byZdoid = new();
  private static readonly Dictionary<ZDO, PowerNetworkData> _byZdo = new();
  private static readonly Dictionary<PowerSystemComputeData, PowerNetworkData> _byData = new();

  // precomputed lists for quick lookups
  private static readonly List<PowerStorageData> _storages = new();
  private static readonly List<PowerSourceData> _sources = new();
  private static readonly List<PowerConduitData> _conduits = new();
  private static readonly List<PowerConsumerData> _consumers = new();

  // linq batches for common types, for faster lookups
  private static readonly Dictionary<Type, IList> _typeBatches = new();

  // Register new PowerNetworkData (called during ZDO spawn/load)
  public static PowerNetworkData Register(ZDO zdo, PowerSystemComputeData data)
  {
    var d = new PowerNetworkData(zdo, data);
    _byZdoid[d.Zdoid] = d;
    _byZdo[d.Zdo] = d;
    _byData[d.Data] = d;

    // Add to precomputed list by type
    if (data is PowerStorageData storage) _storages.Add(storage);
    else if (data is PowerSourceData source) _sources.Add(source);
    else if (data is PowerConduitData conduit) _conduits.Add(conduit);
    else if (data is PowerConsumerData consumer) _consumers.Add(consumer);

    PowerNetworkRebuildScheduler.Trigger();

    return d;
  }

  public static void Unregister(ZDO zdo)
  {
    if (!_byZdo.TryGetValue(zdo, out var d))
    {
      PowerNetworkRebuildScheduler.Trigger();
      return;
    }

    _byZdoid.Remove(d.Zdoid);
    _byZdo.Remove(d.Zdo);
    _byData.Remove(d.Data);

    // Remove from precomputed list by type
    if (d.Data is PowerStorageData storage) _storages.Remove(storage);
    else if (d.Data is PowerSourceData source) _sources.Remove(source);
    else if (d.Data is PowerConduitData conduit) _conduits.Remove(conduit);
    else if (d.Data is PowerConsumerData consumer) _consumers.Remove(consumer);

    PowerNetworkRebuildScheduler.Trigger();
  }

  public static IReadOnlyList<PowerStorageData> GetAllStorages()
  {
    return _storages;
  }
  public static IReadOnlyList<PowerSourceData> GetAllSources()
  {
    return _sources;
  }
  public static IReadOnlyList<PowerConduitData> GetAllConduits()
  {
    return _conduits;
  }
  public static IReadOnlyList<PowerConsumerData> GetAllConsumers()
  {
    return _consumers;
  }

  // Optionally: get all PowerNetworkData or raw PowerSystemComputeData
  public static IReadOnlyCollection<PowerNetworkData> GetAll()
  {
    return _byZdoid.Values;
  }
  public static IReadOnlyCollection<ZDO> GetAllZDOs()
  {
    return _byZdo.Keys;
  }
  public static IReadOnlyCollection<PowerSystemComputeData> GetAllData()
  {
    return _byData.Keys;
  }


  public static IReadOnlyList<T> GetAllOfType<T>() where T : PowerSystemComputeData
  {
    if (typeof(T) == typeof(PowerStorageData)) return (IReadOnlyList<T>)_storages;
    if (typeof(T) == typeof(PowerSourceData)) return (IReadOnlyList<T>)_sources;
    if (typeof(T) == typeof(PowerConduitData)) return (IReadOnlyList<T>)_conduits;
    if (typeof(T) == typeof(PowerConsumerData)) return (IReadOnlyList<T>)_consumers;

    // fallback: slow path, dynamically filter and cache for less common types
    if (!_typeBatches.TryGetValue(typeof(T), out var batch))
    {
      var list = _byData.Keys.OfType<T>().ToList();
      _typeBatches[typeof(T)] = list;
      return list;
    }
    return (IReadOnlyList<T>)batch;
  }

  public static bool ContainsZDO(ZDO zdo)
  {
    return _byZdo.ContainsKey(zdo);
  }

  public static bool TryGetByZdo(ZDO zdo, out PowerNetworkData data)
  {
    return _byZdo.TryGetValue(zdo, out data);
  }
  public static bool TryGetByZdoid(ZDOID zdoid, out PowerNetworkData data)
  {
    return _byZdoid.TryGetValue(zdoid, out data);
  }
  public static bool TryGetByData(PowerSystemComputeData data, out PowerNetworkData pnd)
  {
    return _byData.TryGetValue(data, out pnd);
  }

  // (Optional) TryGet with type constraint for Data
  public static bool TryGetData<T>(ZDO zdo, out T typedData) where T : PowerSystemComputeData
  {
    if (TryGetByZdo(zdo, out var pnd) && pnd.Data is T t)
    {
      typedData = t;
      return true;
    }
    typedData = null;
    return false;
  }

  // for component Integrations to avoid desyncs and gross per-component logic to wait for data.

  public static float MaxPowerSystemCoroutineWaitTime = 10f;

  /// <summary>
  /// Waits for the PowerStorageData for this ZNetView to be available, then invokes the callback.
  ///
  /// Bails if MonoBehavior is null or the ZNetView is null or zdo is null or data is null or data is mismatched.
  /// </summary>
  public static Coroutine WaitForPowerSystemNodeData<T>(this MonoBehaviour runner, Action<T> onAvailable) where T : PowerSystemComputeData
  {
    return runner.StartCoroutine(WaitForNodeData(runner, onAvailable));
  }

  public static Coroutine WaitForPowerSystemNodeData<T>(this MonoBehaviour runner, Action<T, ZNetView> onAvailable) where T : PowerSystemComputeData
  {
    return runner.StartCoroutine(WaitForNodeData(runner, onAvailable));
  }

  public static IEnumerator WaitForNodeData<T>(MonoBehaviour runner, Action<T> onAvailable) where T : PowerSystemComputeData
  {
    yield return WaitForNodeData<T>(runner, (data, _) =>
    {
      onAvailable.Invoke(data);
    });
  }
  /// <summary>
  /// Coroutine: Waits until a ZDO is registered in the PowerSystemRegistry,
  /// then returns the strongly typed PowerData via callback or as yield return.
  /// </summary>
  public static IEnumerator WaitForNodeData<T>(MonoBehaviour runner, Action<T, ZNetView> onAvailable) where T : PowerSystemComputeData
  {
    ZNetView? netView = null;
    yield return ValheimExtensions.WaitForZNetView(runner, (nv) =>
    {
      netView = nv;
    });

    if (!netView || netView == null)
    {
      yield break;
    }

    var timer = Stopwatch.StartNew();

    T data = null;
    // Poll until the node is registered
    while (runner && timer.ElapsedMilliseconds < MaxPowerSystemCoroutineWaitTime && !TryGetData(netView.GetZDO(), out data))
      yield return null;

    if (!runner)
    {
#if DEBUG
      LoggerProvider.LogDebug("Runner is null, aborting WaitForNodeData");
#endif
      yield break;
    }
    if (data == null || data is not T)
    {
      LoggerProvider.LogWarning($"Current node type: <{data?.GetType()}> does not match requested type {typeof(T)}");
      yield break;
    }

    onAvailable.Invoke(data, netView);
  }
}