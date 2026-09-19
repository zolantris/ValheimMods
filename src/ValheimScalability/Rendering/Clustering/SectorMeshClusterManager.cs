using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimScalability.BepInExConfig;

namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Client-side owner for world-sector optimized rendering controllers.
///
/// Active sectors are never constrained by warm-cache limits.
/// Inactive sectors can retain generated cluster data for fast revisits, but the
/// warm cache is bounded by TTL, zone count, and approximate generated-mesh memory.
/// </summary>
public sealed class SectorMeshClusterManager : MonoBehaviour
{
  private const float ActiveTouchGraceSeconds = 3f;
  private const float MinimumVisitCountIntervalSeconds = 30f;
  private const int MaxColdHistoryEntries = 4096;

  public static SectorMeshClusterManager Instance { get; private set; }

  public static SectorMeshClusterManager EnsureAttached()
  {
    if (Instance)
    {
      return Instance;
    }

    if (Application.isBatchMode ||
        !ZNetScene.instance)
    {
      return null;
    }

    var existing =
      ZNetScene.instance
        .GetComponent<SectorMeshClusterManager>();

    return existing
      ? existing
      : ZNetScene.instance.gameObject
        .AddComponent<SectorMeshClusterManager>();
  }

  private readonly Dictionary<Vector2s, SectorMeshClusterController>
    _sectorControllers = new();

  private readonly Dictionary<Vector2s, SectorCacheState>
    _sectorCacheStates = new();

  private readonly Dictionary<int, Vector2s>
    _networkObjectSectors = new();

  private readonly List<Vector2s>
    _deadSectorBuffer = new();

  private readonly List<int>
    _networkIdBuffer = new();

  private readonly List<WarmCacheCandidate>
    _warmCandidateBuffer = new();

  private readonly List<Vector2s>
    _historyPruneBuffer = new();

  private float _nextMaintenanceTime;

  private void Awake()
  {
    if (Instance &&
        Instance != this)
    {
      Destroy(this);
      return;
    }

    Instance = this;
  }

  public void RegisterNetworkObject(
    ZDO zdo,
    ZNetView nview)
  {
    if (zdo == null ||
        !nview ||
        Application.isBatchMode)
    {
      return;
    }

    // A ValheimRAFT/vehicle piece can reach AddInstance before it has been
    // activated and reparented beneath the vehicle Rigidbody. Persisted parent
    // ids are therefore checked before hierarchy state.
    if (DynamicHierarchyBatchExclusion.ShouldExclude(
          zdo,
          nview.gameObject))
    {
      return;
    }

    var sector =
      zdo.GetSector();

    var controller =
      GetOrCreateController(sector);

    ActivateSector(
      sector,
      controller,
      true);

    if (controller.Register(
          nview.gameObject))
    {
      _networkObjectSectors[
        nview.gameObject.GetInstanceID()] =
        sector;
    }
  }

  public void UnregisterNetworkObject(
    ZNetView nview)
  {
    if (!nview)
    {
      return;
    }

    var instanceId =
      nview.gameObject.GetInstanceID();

    if (_networkObjectSectors.TryGetValue(
          instanceId,
          out var sector))
    {
      if (_sectorControllers.TryGetValue(
            sector,
            out var controller) &&
          controller)
      {
        controller.Unregister(
          nview.gameObject);
      }

      _networkObjectSectors.Remove(
        instanceId);

      return;
    }

    var zdo =
      nview.GetZDO();

    sector =
      zdo != null
        ? zdo.GetSector()
        : ZoneSystem.GetZone(
          nview.transform.position);

    if (_sectorControllers.TryGetValue(
          sector,
          out var fallbackController) &&
        fallbackController)
    {
      fallbackController.Unregister(
        nview.gameObject);
    }
  }

  public void RegisterZoneHierarchy(
    Vector2s sector,
    GameObject hierarchyRoot)
  {
    if (!hierarchyRoot ||
        Application.isBatchMode)
    {
      return;
    }

    var controller =
      GetOrCreateController(sector);

    ActivateSector(
      sector,
      controller,
      true);

    controller.RegisterHierarchy(
      hierarchyRoot);

    // A controller may have been cold-evicted while ZNetScene still retained
    // network instances. Reacquire any already-existing network objects in this
    // sector so returning to a cold zone does not require those objects to be
    // recreated before clustering can resume.
    RegisterExistingNetworkObjectsInSector(
      sector,
      controller);
  }

  public void RegisterLocationHierarchy(
    GameObject locationRoot)
  {
    if (!locationRoot ||
        Application.isBatchMode)
    {
      return;
    }

    var sector =
      ZoneSystem.GetZone(
        locationRoot.transform.position);

    var controller =
      GetOrCreateController(sector);

    ActivateSector(
      sector,
      controller,
      true);

    controller.RegisterHierarchy(
      locationRoot);
  }

  public void NotifyDamage(
    GameObject damagedObject)
  {
    if (!damagedObject ||
        Application.isBatchMode)
    {
      return;
    }

    var nview =
      damagedObject
        .GetComponentInParent<ZNetView>();

    Vector2s sector;

    if (nview &&
        _networkObjectSectors.TryGetValue(
          nview.gameObject.GetInstanceID(),
          out var registeredSector))
    {
      sector =
        registeredSector;
    }
    else if (nview &&
             nview.GetZDO() != null)
    {
      sector =
        nview.GetZDO().GetSector();
    }
    else
    {
      sector =
        ZoneSystem.GetZone(
          damagedObject.transform.position);
    }

    if (_sectorControllers.TryGetValue(
          sector,
          out var controller) &&
        controller)
    {
      ActivateSector(
        sector,
        controller,
        false);

      controller.NotifyDamage(
        damagedObject);
    }
  }

  public void UpdateLocalPlayerProximity(
    Vector3 playerPosition,
    Camera camera)
  {
    if (ValheimScalabilityConfig.Mode !=
        ClusterPresentationMode.Adaptive)
    {
      return;
    }

    foreach (var pair in
             _sectorControllers)
    {
      var controller =
        pair.Value;

      if (!controller ||
          controller.IsWarmCached)
      {
        continue;
      }

      controller.UpdateLocalPlayerProximity(
        playerPosition,
        camera);
    }
  }

  public void ClearLocalPlayerProximity()
  {
    foreach (var controller in
             _sectorControllers.Values)
    {
      if (controller &&
          !controller.IsWarmCached)
      {
        controller.ClearLocalPlayerProximity();
      }
    }
  }

  /// <summary>
  /// Rare runtime-config path.
  /// Live/active controllers rebuild immediately. Warm controllers drop their
  /// cached generated representation and rebuild when reactivated.
  /// </summary>
  public void RebuildAllForConfigurationChange()
  {
    ClusterBatchFilter.Invalidate();
    SectorMeshClusterSettings.InvalidateLayerCache();

    foreach (var controller in
             _sectorControllers.Values)
    {
      if (controller)
      {
        controller.RebuildForConfigurationChange();
      }
    }
  }

  private SectorMeshClusterController
    GetOrCreateController(
      Vector2s sector)
  {
    if (_sectorControllers.TryGetValue(
          sector,
          out var existing) &&
        existing)
    {
      return existing;
    }

    var holder =
      new GameObject(
        $"ValheimScalability_SectorMeshCluster_{sector.x}_{sector.y}");

    holder.transform.SetParent(
      transform,
      true);

    holder.transform.SetPositionAndRotation(
      ZoneSystem.GetZonePos(sector),
      Quaternion.identity);

    holder.transform.localScale =
      Vector3.one;

    var controller =
      holder.AddComponent<SectorMeshClusterController>();

    controller.Initialize(sector);

    _sectorControllers[sector] =
      controller;

    var state =
      GetOrCreateCacheState(sector);

    var now =
      Time.unscaledTime;

    state.LastTouchTime = now;
    state.LastActiveTime = now;

    if (state.VisitCount <= 0)
    {
      state.VisitCount = 1;
      state.LastVisitCountTime = now;
    }
    else if (!state.IsActive &&
             now - state.LastVisitCountTime >=
             MinimumVisitCountIntervalSeconds)
    {
      // This is a return to a sector whose controller was previously cold-evicted.
      // Preserve the lightweight history and count it as another distinct visit.
      state.VisitCount++;
      state.LastVisitCountTime = now;
    }

    state.IsActive = true;

    return controller;
  }

  private SectorCacheState
    GetOrCreateCacheState(
      Vector2s sector)
  {
    if (_sectorCacheStates.TryGetValue(
          sector,
          out var existing))
    {
      return existing;
    }

    var created =
      new SectorCacheState();

    _sectorCacheStates.Add(
      sector,
      created);

    return created;
  }

  private void ActivateSector(
    Vector2s sector,
    SectorMeshClusterController controller,
    bool countVisit)
  {
    if (!controller)
    {
      return;
    }

    var state =
      GetOrCreateCacheState(sector);

    var now =
      Time.unscaledTime;

    state.LastTouchTime = now;
    state.LastActiveTime = now;

    if (!state.IsActive)
    {
      state.IsActive = true;

      if (countVisit &&
          now - state.LastVisitCountTime >=
          MinimumVisitCountIntervalSeconds)
      {
        state.VisitCount++;
        state.LastVisitCountTime = now;
      }

      controller.ExitWarmCache();

      if (ValheimScalabilityConfig.IsCacheDiagnosticsEnabled)
      {
        Debug.Log(
          $"[ValheimScalability] Warm-cache hit/reactivation sector={sector.x},{sector.y} visits={state.VisitCount} retained={FormatMegabytes(controller.EstimatedGeneratedMemoryBytes)} MB");
      }

      return;
    }

    if (controller.IsWarmCached)
    {
      controller.ExitWarmCache();
    }
  }

  private void RegisterExistingNetworkObjectsInSector(
    Vector2s sector,
    SectorMeshClusterController controller)
  {
    if (!controller ||
        !ZNetScene.instance)
    {
      return;
    }

    foreach (var pair in
             ZNetScene.instance.m_instances)
    {
      var zdo =
        pair.Key;

      var nview =
        pair.Value;

      if (zdo == null ||
          !nview ||
          zdo.GetSector() != sector)
      {
        continue;
      }

      if (controller.Register(
            nview.gameObject))
      {
        _networkObjectSectors[
          nview.gameObject.GetInstanceID()] =
          sector;
      }
    }
  }

  private void Update()
  {
    if (Application.isBatchMode ||
        Time.unscaledTime <
        _nextMaintenanceTime)
    {
      return;
    }

    var now =
      Time.unscaledTime;

    _nextMaintenanceTime =
      now +
      ValheimScalabilityConfig.MaintenanceInterval;

    _deadSectorBuffer.Clear();

    foreach (var pair in
             _sectorControllers)
    {
      var sector =
        pair.Key;

      var controller =
        pair.Value;

      if (!controller)
      {
        _deadSectorBuffer.Add(
          sector);

        continue;
      }

      var state =
        GetOrCreateCacheState(sector);

      var zoneLoaded =
        ZoneSystem.instance &&
        ZoneSystem.instance.m_zones.ContainsKey(
          sector);

      var recentlyTouched =
        now - state.LastTouchTime <=
        ActiveTouchGraceSeconds;

      if (zoneLoaded ||
          recentlyTouched)
      {
        if (!state.IsActive ||
            controller.IsWarmCached)
        {
          ActivateSector(
            sector,
            controller,
            zoneLoaded);
        }

        state.LastActiveTime = now;

        controller.Tick();
        continue;
      }

      if (state.IsActive)
      {
        state.IsActive = false;
        state.WarmSince = now;
        controller.EnterWarmCache();

        if (ValheimScalabilityConfig.IsCacheDiagnosticsEnabled)
        {
          Debug.Log(
            $"[ValheimScalability] Sector entered warm cache sector={sector.x},{sector.y} visits={state.VisitCount} retained={FormatMegabytes(controller.EstimatedGeneratedMemoryBytes)} MB");
        }
      }
    }

    RemoveDeadControllerEntries();

    EnforceWarmCacheLimits(now);
    PruneColdHistory();
  }

  private void EnforceWarmCacheLimits(
    float now)
  {
    _warmCandidateBuffer.Clear();

    long totalWarmBytes = 0L;
    var totalWarmZones = 0;
    var frequentWarmZones = 0;

    foreach (var pair in
             _sectorControllers)
    {
      var sector =
        pair.Key;

      var controller =
        pair.Value;

      if (!controller ||
          !_sectorCacheStates.TryGetValue(
            sector,
            out var state) ||
          state.IsActive)
      {
        continue;
      }

      var bytes =
        controller.EstimatedGeneratedMemoryBytes;

      var isFrequent =
        state.VisitCount >=
        ValheimScalabilityConfig.FrequentZoneVisitThreshold;

      var candidate =
        new WarmCacheCandidate(
          sector,
          controller,
          state,
          bytes,
          isFrequent);

      _warmCandidateBuffer.Add(
        candidate);

      totalWarmBytes += bytes;
      totalWarmZones++;

      if (isFrequent)
      {
        frequentWarmZones++;
      }
    }

    if (_warmCandidateBuffer.Count == 0)
    {
      return;
    }

    if (!ValheimScalabilityConfig.IsWarmCacheEnabled)
    {
      foreach (var candidate in
               _warmCandidateBuffer)
      {
        EvictSector(
          candidate.Sector,
          "warm cache disabled");
      }

      return;
    }

    var maxSingleBytes =
      ValheimScalabilityConfig.SingleWarmZoneMemoryLimitBytes;

    foreach (var candidate in
             _warmCandidateBuffer)
    {
      if (candidate.Evicted)
      {
        continue;
      }

      var retention =
        candidate.IsFrequent
          ? ValheimScalabilityConfig.FrequentWarmRetentionSeconds
          : ValheimScalabilityConfig.WarmRetentionSeconds;

      var age =
        Mathf.Max(
          0f,
          now - candidate.State.WarmSince);

      if (retention >= 0f &&
          age > retention)
      {
        MarkEvicted(
          candidate,
          ref totalWarmZones,
          ref frequentWarmZones,
          ref totalWarmBytes);

        EvictSector(
          candidate.Sector,
          candidate.IsFrequent
            ? "frequent-zone TTL expired"
            : "warm-zone TTL expired");

        continue;
      }

      if (maxSingleBytes > 0L &&
          candidate.Bytes > maxSingleBytes)
      {
        MarkEvicted(
          candidate,
          ref totalWarmZones,
          ref frequentWarmZones,
          ref totalWarmBytes);

        EvictSector(
          candidate.Sector,
          "single-zone warm memory limit exceeded");
      }
    }

    var frequentLimit =
      ValheimScalabilityConfig.FrequentWarmZoneLimit;

    if (frequentLimit >= 0 &&
        frequentWarmZones > frequentLimit)
    {
      _warmCandidateBuffer.Sort(
        WarmCacheCandidate.CompareFrequentEvictionOrder);

      foreach (var candidate in
               _warmCandidateBuffer)
      {
        if (frequentWarmZones <= frequentLimit)
        {
          break;
        }

        if (candidate.Evicted ||
            !candidate.IsFrequent)
        {
          continue;
        }

        MarkEvicted(
          candidate,
          ref totalWarmZones,
          ref frequentWarmZones,
          ref totalWarmBytes);

        EvictSector(
          candidate.Sector,
          "frequent warm-zone limit exceeded");
      }
    }

    var zoneLimit =
      ValheimScalabilityConfig.WarmZoneLimit;

    var memoryLimit =
      ValheimScalabilityConfig.WarmMeshMemoryLimitBytes;

    var exceedsZoneLimit =
      zoneLimit > 0 &&
      totalWarmZones > zoneLimit;

    var exceedsMemoryLimit =
      memoryLimit > 0L &&
      totalWarmBytes > memoryLimit;

    if (!exceedsZoneLimit &&
        !exceedsMemoryLimit)
    {
      return;
    }

    // Evict non-frequent, oldest, and larger entries first. Frequent zones are
    // protected as long as possible but still obey the global hard memory/count cap.
    _warmCandidateBuffer.Sort(
      WarmCacheCandidate.CompareGlobalEvictionOrder);

    foreach (var candidate in
             _warmCandidateBuffer)
    {
      exceedsZoneLimit =
        zoneLimit > 0 &&
        totalWarmZones > zoneLimit;

      exceedsMemoryLimit =
        memoryLimit > 0L &&
        totalWarmBytes > memoryLimit;

      if (!exceedsZoneLimit &&
          !exceedsMemoryLimit)
      {
        break;
      }

      if (candidate.Evicted)
      {
        continue;
      }

      MarkEvicted(
        candidate,
        ref totalWarmZones,
        ref frequentWarmZones,
        ref totalWarmBytes);

      EvictSector(
        candidate.Sector,
        exceedsMemoryLimit
          ? "warm mesh-memory budget exceeded"
          : "warm-zone count limit exceeded");
    }
  }

  private static void MarkEvicted(
    WarmCacheCandidate candidate,
    ref int totalWarmZones,
    ref int frequentWarmZones,
    ref long totalWarmBytes)
  {
    if (candidate.Evicted)
    {
      return;
    }

    candidate.Evicted = true;

    totalWarmZones =
      Mathf.Max(
        0,
        totalWarmZones - 1);

    if (candidate.IsFrequent)
    {
      frequentWarmZones =
        Mathf.Max(
          0,
          frequentWarmZones - 1);
    }

    totalWarmBytes =
      Math.Max(
        0L,
        totalWarmBytes -
        candidate.Bytes);
  }

  private void EvictSector(
    Vector2s sector,
    string reason)
  {
    if (!_sectorControllers.TryGetValue(
          sector,
          out var controller))
    {
      return;
    }

    var bytes =
      controller
        ? controller.EstimatedGeneratedMemoryBytes
        : 0L;

    if (ValheimScalabilityConfig.IsCacheDiagnosticsEnabled)
    {
      Debug.Log(
        $"[ValheimScalability] Cold-evict sector={sector.x},{sector.y} reason='{reason}' releasing≈{FormatMegabytes(bytes)} MB");
    }

    if (controller)
    {
      Destroy(
        controller.gameObject);
    }

    _sectorControllers.Remove(
      sector);

    if (_sectorCacheStates.TryGetValue(
          sector,
          out var state))
    {
      state.IsActive = false;
    }

    RemoveNetworkMappingsForSector(
      sector);
  }

  private void RemoveNetworkMappingsForSector(
    Vector2s sector)
  {
    _networkIdBuffer.Clear();

    foreach (var pair in
             _networkObjectSectors)
    {
      if (pair.Value == sector)
      {
        _networkIdBuffer.Add(
          pair.Key);
      }
    }

    foreach (var instanceId in
             _networkIdBuffer)
    {
      _networkObjectSectors.Remove(
        instanceId);
    }

    _networkIdBuffer.Clear();
  }

  private void RemoveDeadControllerEntries()
  {
    foreach (var sector in
             _deadSectorBuffer)
    {
      _sectorControllers.Remove(
        sector);

      RemoveNetworkMappingsForSector(
        sector);

      if (_sectorCacheStates.TryGetValue(
            sector,
            out var state))
      {
        state.IsActive = false;
      }
    }

    _deadSectorBuffer.Clear();
  }

  private void PruneColdHistory()
  {
    if (_sectorCacheStates.Count <=
        MaxColdHistoryEntries)
    {
      return;
    }

    _historyPruneBuffer.Clear();

    foreach (var pair in
             _sectorCacheStates)
    {
      if (_sectorControllers.ContainsKey(
            pair.Key))
      {
        continue;
      }

      _historyPruneBuffer.Add(
        pair.Key);
    }

    _historyPruneBuffer.Sort(
      CompareHistoryAge);

    var removeCount =
      Mathf.Min(
        _historyPruneBuffer.Count,
        _sectorCacheStates.Count -
        MaxColdHistoryEntries);

    for (var index = 0;
         index < removeCount;
         ++index)
    {
      _sectorCacheStates.Remove(
        _historyPruneBuffer[index]);
    }

    _historyPruneBuffer.Clear();
  }

  private int CompareHistoryAge(
    Vector2s left,
    Vector2s right)
  {
    var leftTime =
      _sectorCacheStates.TryGetValue(
        left,
        out var leftState)
        ? leftState.LastActiveTime
        : float.MinValue;

    var rightTime =
      _sectorCacheStates.TryGetValue(
        right,
        out var rightState)
        ? rightState.LastActiveTime
        : float.MinValue;

    return leftTime.CompareTo(
      rightTime);
  }

  private static string FormatMegabytes(
    long bytes)
  {
    return (
      bytes /
      (1024d * 1024d))
      .ToString("F1");
  }

  private void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
    }

    _sectorControllers.Clear();
    _sectorCacheStates.Clear();
    _networkObjectSectors.Clear();

    InstancedMaterialCache.Clear();
  }

  private sealed class SectorCacheState
  {
    public bool IsActive;
    public int VisitCount;
    public float LastVisitCountTime;
    public float LastTouchTime;
    public float LastActiveTime;
    public float WarmSince;
  }

  private sealed class WarmCacheCandidate
  {
    public readonly Vector2s Sector;
    public readonly SectorMeshClusterController Controller;
    public readonly SectorCacheState State;
    public readonly long Bytes;
    public readonly bool IsFrequent;

    public bool Evicted;

    public WarmCacheCandidate(
      Vector2s sector,
      SectorMeshClusterController controller,
      SectorCacheState state,
      long bytes,
      bool isFrequent)
    {
      Sector = sector;
      Controller = controller;
      State = state;
      Bytes = bytes;
      IsFrequent = isFrequent;
    }

    public static int CompareFrequentEvictionOrder(
      WarmCacheCandidate left,
      WarmCacheCandidate right)
    {
      if (left.IsFrequent !=
          right.IsFrequent)
      {
        return left.IsFrequent
          ? -1
          : 1;
      }

      var activeComparison =
        left.State.LastActiveTime.CompareTo(
          right.State.LastActiveTime);

      if (activeComparison != 0)
      {
        return activeComparison;
      }

      // If equally old, remove the larger entry first.
      return right.Bytes.CompareTo(
        left.Bytes);
    }

    public static int CompareGlobalEvictionOrder(
      WarmCacheCandidate left,
      WarmCacheCandidate right)
    {
      if (left.IsFrequent !=
          right.IsFrequent)
      {
        return left.IsFrequent
          ? 1
          : -1;
      }

      var activeComparison =
        left.State.LastActiveTime.CompareTo(
          right.State.LastActiveTime);

      if (activeComparison != 0)
      {
        return activeComparison;
      }

      return right.Bytes.CompareTo(
        left.Bytes);
    }
  }
}
