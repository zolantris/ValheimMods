using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using ValheimScalability.BepInExConfig;

namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Owns optimized rendering for one Valheim sector, subdivided into smaller cells.
///
/// Static geometry -> Mesh.CombineMeshes.
/// Wind/vegetation geometry -> GPU instancing, preserving per-object transforms.
/// Unsupported/user-excluded geometry -> original MeshRenderers.
/// </summary>
public sealed class SectorMeshClusterController : MonoBehaviour
{
  private readonly Dictionary<GameObject, int> _rootToCell = new();
  private readonly Dictionary<int, ClusterCellState> _cells = new();
  private readonly HashSet<GameObject> _hierarchyCandidateBuffer = new();
  private readonly List<GameObject> _deadRootBuffer = new();

  private Vector2s _sector;
  private bool _initialized;
  private int _cellDivisions;
  private float _zoneSize;
  private float _cellSize;
  private float _nextDestroyedRootCleanupTime;
  private bool _isWarmCached;

  public Vector2s Sector => _sector;

  public bool HasRegisteredObjects =>
    _rootToCell.Count > 0;

  public bool IsWarmCached =>
    _isWarmCached;

  public long EstimatedGeneratedMemoryBytes
  {
    get
    {
      long bytes = 0;

      foreach (var cell in _cells.Values)
      {
        bytes +=
          cell.EstimatedGeneratedMemoryBytes;
      }

      return bytes;
    }
  }

  public void Initialize(Vector2s sector)
  {
    if (_initialized)
    {
      return;
    }

    _initialized = true;
    _sector = sector;

    transform.SetPositionAndRotation(
      ZoneSystem.GetZonePos(sector),
      Quaternion.identity);

    transform.localScale = Vector3.one;

    RefreshCellDimensions();
  }

  /// <summary>
  /// Registration is intentionally broader than current user filters.
  /// Filters are evaluated when cells build so config can be changed at runtime.
  /// </summary>
  public bool Register(GameObject root)
  {
    if (!root ||
        !_initialized ||
        _rootToCell.ContainsKey(root) ||
        !CanRegisterRoot(root))
    {
      return false;
    }

    var cellIndex =
      GetCellIndex(root.transform.position);

    var cell =
      GetOrCreateCell(cellIndex);

    cell.RegisteredRoots.Add(root);
    cell.Dirty = true;

    cell.NextBuildTime = Mathf.Max(
      cell.NextBuildTime,
      Time.unscaledTime +
      ValheimScalabilityConfig.RegistrationSettleDelay);

    _rootToCell.Add(root, cellIndex);
    return true;
  }

  /// <summary>
  /// One-time discovery for non-ZDO zone/location hierarchy content.
  /// Network children are deliberately skipped; they arrive through ZNetScene.AddInstance.
  /// </summary>
  public void RegisterHierarchy(GameObject hierarchyRoot)
  {
    if (!hierarchyRoot)
    {
      return;
    }

    _hierarchyCandidateBuffer.Clear();

    var renderers =
      hierarchyRoot.GetComponentsInChildren<MeshRenderer>(true);

    foreach (var renderer in renderers)
    {
      if (!renderer)
      {
        continue;
      }

      if (renderer.GetComponentInParent<ZNetView>())
      {
        continue;
      }

      var candidate =
        FindTopLevelChild(
          hierarchyRoot.transform,
          renderer.transform);

      if (candidate)
      {
        _hierarchyCandidateBuffer.Add(
          candidate.gameObject);
      }
    }

    foreach (var candidate in _hierarchyCandidateBuffer)
    {
      Register(candidate);
    }

    _hierarchyCandidateBuffer.Clear();
  }

  public void Unregister(GameObject root)
  {
    if (!root ||
        !_rootToCell.TryGetValue(
          root,
          out var cellIndex) ||
        !_cells.TryGetValue(
          cellIndex,
          out var cell))
    {
      return;
    }

    _rootToCell.Remove(root);
    cell.RegisteredRoots.Remove(root);
    cell.DamagedRoots.Remove(root);
    cell.SourceRenderersByRoot.Remove(root);

    if (cell.CurrentlyHiddenRenderersByRoot.TryGetValue(
          root,
          out var hidden))
    {
      RestoreRendererList(hidden);
      cell.CurrentlyHiddenRenderersByRoot.Remove(root);
    }

    InvalidateCellForRemovedGeometry(cell);
  }

  public bool NotifyDamage(GameObject damagedObject)
  {
    if (!damagedObject)
    {
      return false;
    }

    var root =
      FindRegisteredRoot(
        damagedObject.transform);

    if (!root ||
        !_rootToCell.TryGetValue(
          root,
          out var cellIndex) ||
        !_cells.TryGetValue(
          cellIndex,
          out var cell))
    {
      return false;
    }

    cell.DamagedRoots.Add(root);
    cell.Dirty = true;
    cell.ContainsStaleGeometry = cell.ClusterBuilt;

    cell.SuspendedUntil = Mathf.Max(
      cell.SuspendedUntil,
      Time.unscaledTime +
      ValheimScalabilityConfig.DamageCooldown);

    TransitionPresentation(
      cell,
      OptimizedPresentation.None);

    if (_isWarmCached)
    {
      DestroyOptimizedRepresentations(cell);
      cell.ClusterBuilt = false;
    }

    return true;
  }

  public void UpdateLocalPlayerProximity(
    Vector3 playerPosition,
    Camera camera)
  {
    if (_isWarmCached)
    {
      return;
    }

    var mode =
      ValheimScalabilityConfig.Mode;

    Plane[] cameraPlanes = null;

    if (mode == ClusterPresentationMode.Adaptive &&
        ValheimScalabilityConfig.OnlyUnclusterVisibleCells &&
        camera)
    {
      cameraPlanes =
        GeometryUtility.CalculateFrustumPlanes(camera);
    }

    foreach (var cell in _cells.Values)
    {
      if (mode != ClusterPresentationMode.Adaptive)
      {
        cell.NearLocalPlayer = false;
        cell.VisibleToLocalPlayer = false;
        ApplyPresentation(cell);
        continue;
      }

      cell.NearLocalPlayer =
        GetCardinalDistanceToBounds(
          playerPosition,
          cell.WorldBounds) <=
        ValheimScalabilityConfig.UnclusterRadius;

      if (!cell.NearLocalPlayer)
      {
        cell.VisibleToLocalPlayer = false;
      }
      else if (!ValheimScalabilityConfig.OnlyUnclusterVisibleCells ||
               !camera ||
               cameraPlanes == null)
      {
        cell.VisibleToLocalPlayer = true;
      }
      else
      {
        cell.VisibleToLocalPlayer =
          GeometryUtility.TestPlanesAABB(
            cameraPlanes,
            cell.WorldBounds);
      }

      ApplyPresentation(cell);
    }
  }

  public void ClearLocalPlayerProximity()
  {
    foreach (var cell in _cells.Values)
    {
      cell.NearLocalPlayer = false;
      cell.VisibleToLocalPlayer = false;
      ApplyPresentation(cell);
    }
  }

  /// <summary>
  /// Moves this sector into the bounded warm cache.
  /// Generated meshes are retained, but all optimized rendering is disabled.
  /// </summary>
  public void EnterWarmCache()
  {
    if (_isWarmCached)
    {
      return;
    }

    _isWarmCached = true;

    // Capture dead roots before freezing maintenance. If a cluster is already
    // stale, do not retain a representation that cannot safely be reused.
    CleanupDestroyedRoots();

    foreach (var cell in _cells.Values)
    {
      if (cell.CurrentPresentation !=
          OptimizedPresentation.None)
      {
        TransitionPresentation(
          cell,
          OptimizedPresentation.None);
      }

      if (cell.ContainsStaleGeometry)
      {
        DestroyOptimizedRepresentations(cell);
        cell.ClusterBuilt = false;
      }

      cell.NearLocalPlayer = false;
      cell.VisibleToLocalPlayer = false;
    }
  }

  /// <summary>
  /// Reactivates a warm sector. If nothing changed while it was inactive,
  /// the existing generated meshes can be presented again without recombining.
  /// </summary>
  public void ExitWarmCache()
  {
    if (!_isWarmCached)
    {
      return;
    }

    _isWarmCached = false;

    foreach (var cell in _cells.Values)
    {
      cell.CurrentPresentation =
        OptimizedPresentation.Uninitialized;
    }
  }

  /// <summary>
  /// Rare path used after relevant config changes.
  /// All live roots are retained, damaged-root exclusions are preserved,
  /// and the cell layout/filtering is rebuilt from current config.
  /// </summary>
  public void RebuildForConfigurationChange()
  {
    var roots =
      new List<GameObject>(_rootToCell.Count);

    var damaged =
      new HashSet<GameObject>();

    foreach (var pair in _rootToCell)
    {
      if (pair.Key)
      {
        roots.Add(pair.Key);
      }
    }

    foreach (var cell in _cells.Values)
    {
      foreach (var root in cell.DamagedRoots)
      {
        if (root)
        {
          damaged.Add(root);
        }
      }

      RestoreOriginalRenderers(cell);

      cell.CurrentPresentation =
        OptimizedPresentation.Uninitialized;

      DestroyOptimizedRepresentations(cell);
    }

    _rootToCell.Clear();
    _cells.Clear();

    RefreshCellDimensions();

    foreach (var root in roots)
    {
      Register(root);

      if (!damaged.Contains(root) ||
          !_rootToCell.TryGetValue(
            root,
            out var cellIndex) ||
          !_cells.TryGetValue(
            cellIndex,
            out var cell))
      {
        continue;
      }

      cell.DamagedRoots.Add(root);
    }
  }

  public void Tick()
  {
    if (!_initialized ||
        _isWarmCached)
    {
      return;
    }

    if (Time.unscaledTime >= _nextDestroyedRootCleanupTime)
    {
      _nextDestroyedRootCleanupTime =
        Time.unscaledTime + 2.0f;

      CleanupDestroyedRoots();
    }

    foreach (var cell in _cells.Values)
    {
      if (ValheimScalabilityConfig.Mode ==
          ClusterPresentationMode.Off)
      {
        if (cell.CurrentPresentation !=
            OptimizedPresentation.None)
        {
          TransitionPresentation(
            cell,
            OptimizedPresentation.None);
        }

        continue;
      }

      if (cell.RegisteredRoots.Count == 0)
      {
        RestoreOriginalRenderers(cell);

        cell.CurrentPresentation =
          OptimizedPresentation.Uninitialized;

        DestroyOptimizedRepresentations(cell);

        cell.ClusterBuilt = false;
        cell.Dirty = true;
        cell.ContainsStaleGeometry = false;
        continue;
      }

      if (CanBuildCell(cell))
      {
        BuildCluster(cell);
      }

      ApplyPresentation(cell);
    }
  }

  /// <summary>
  /// Manual instanced draws must be submitted every frame while the optimized
  /// representation is active.
  /// </summary>
  private void LateUpdate()
  {
    if (_isWarmCached ||
        ValheimScalabilityConfig.Mode ==
          ClusterPresentationMode.Off ||
        !ValheimScalabilityConfig.IsGpuInstancingEnabled)
    {
      return;
    }

    foreach (var cell in _cells.Values)
    {
      if (cell.InstancedPresentation ==
          OptimizedPresentation.None)
      {
        continue;
      }

      foreach (var batch in cell.InstancedBatches)
      {
        if (cell.InstancedPresentation ==
              OptimizedPresentation.PieceLayerOnly &&
            batch.Layer !=
              SectorMeshClusterSettings.PieceLayer)
        {
          continue;
        }

        if (batch.Draw())
        {
          continue;
        }

        HandleInstancingFailure(
          cell,
          batch.SourceMaterial);

        // Cell presentation was invalidated. Do not submit remaining stale batches.
        break;
      }
    }
  }

  private bool CanBuildCell(
    ClusterCellState cell)
  {
    if (!cell.Dirty ||
        Time.unscaledTime < cell.NextBuildTime ||
        Time.unscaledTime < cell.SuspendedUntil)
    {
      return false;
    }

    if (ValheimScalabilityConfig.Mode ==
        ClusterPresentationMode.Off)
    {
      return false;
    }

    // Avoid spending rebuild time on a cell whose originals are intentionally
    // being shown to the local player. It will rebuild after the player leaves.
    if (ValheimScalabilityConfig.Mode ==
          ClusterPresentationMode.Adaptive &&
        cell.NearLocalPlayer &&
        cell.VisibleToLocalPlayer)
    {
      return false;
    }

    return true;
  }

  private bool ShouldForceOriginals(
    ClusterCellState cell)
  {
    if (ValheimScalabilityConfig.Mode ==
        ClusterPresentationMode.Off)
    {
      return true;
    }

    if (Time.unscaledTime <
        cell.SuspendedUntil)
    {
      return true;
    }

    return cell.Dirty &&
           cell.ContainsStaleGeometry;
  }

  private void ApplyPresentation(
    ClusterCellState cell)
  {
    var desired =
      DeterminePresentation(cell);

    if (cell.CurrentPresentation == desired)
    {
      return;
    }

    TransitionPresentation(
      cell,
      desired);
  }

  private OptimizedPresentation DeterminePresentation(
    ClusterCellState cell)
  {
    if (ValheimScalabilityConfig.Mode ==
          ClusterPresentationMode.Off ||
        !cell.ClusterBuilt ||
        ShouldForceOriginals(cell))
    {
      return OptimizedPresentation.None;
    }

    var adaptiveNear =
      ValheimScalabilityConfig.Mode ==
        ClusterPresentationMode.Adaptive &&
      cell.NearLocalPlayer &&
      cell.VisibleToLocalPlayer;

    if (!adaptiveNear)
    {
      return OptimizedPresentation.All;
    }

    return ValheimScalabilityConfig.KeepPieceLayerClusteredNearPlayer
      ? OptimizedPresentation.PieceLayerOnly
      : OptimizedPresentation.None;
  }

  /// <summary>
  /// Renderer.enabled changes are expensive when repeated across thousands of pieces.
  /// Only perform them when the cell actually changes presentation state.
  /// </summary>
  private void TransitionPresentation(
    ClusterCellState cell,
    OptimizedPresentation desired)
  {
    // Restore only when leaving/changing an optimized presentation.
    // Never restore+hide the same renderer every maintenance tick.
    if (cell.CurrentPresentation != OptimizedPresentation.None)
    {
      RestoreOriginalRenderers(cell);
    }

    SetOptimizedPresentation(
      cell,
      desired);

    if (desired == OptimizedPresentation.All)
    {
      HideSourceRenderers(
        cell,
        null);
    }
    else if (desired ==
             OptimizedPresentation.PieceLayerOnly)
    {
      HideSourceRenderers(
        cell,
        SectorMeshClusterSettings.PieceLayer);
    }
    else
    {
      RestoreOriginalRenderers(cell);
    }

    cell.CurrentPresentation =
      desired;

    if (ValheimScalabilityConfig.IsBatchDiagnosticsEnabled)
    {
      Debug.Log(
        $"[ValheimScalability] Presentation sector={_sector.x},{_sector.y} cell={cell.Index} -> {desired}; roots={cell.RegisteredRoots.Count}; optimizedSources={cell.SourceRenderersByRoot.Count}; hiddenRenderers={CountHiddenRenderers(cell)}; generatedRenderers={cell.GeneratedRenderers.Count}; instancedBatches={cell.InstancedBatches.Count}");
    }
  }

  private static int CountHiddenRenderers(
    ClusterCellState cell)
  {
    var count = 0;

    foreach (var renderers in
             cell.CurrentlyHiddenRenderersByRoot.Values)
    {
      count += renderers.Count;
    }

    return count;
  }

  private void BuildCluster(
    ClusterCellState cell)
  {
    RestoreOriginalRenderers(cell);

    SetOptimizedPresentation(
      cell,
      OptimizedPresentation.None);

    cell.CurrentPresentation =
      OptimizedPresentation.Uninitialized;

    DestroyOptimizedRepresentations(cell);

    cell.SourceRenderersByRoot.Clear();

    var combinedBuckets =
      new Dictionary<CombinedBucketKey, CombinedBucket>();

    var instancedBuckets =
      new Dictionary<InstancedBucketKey, InstancedBucket>();

    var estimatedOriginalDrawCalls = 0;

    foreach (var root in cell.RegisteredRoots)
    {
      if (!root ||
          cell.DamagedRoots.Contains(root) ||
          !CanRegisterRoot(root))
      {
        continue;
      }

      estimatedOriginalDrawCalls +=
        CollectRootGeometry(
          root,
          cell,
          combinedBuckets,
          instancedBuckets);
    }

    var estimatedOptimizedDrawCalls =
      CountCombinedDrawCalls(combinedBuckets) +
      CountInstancedDrawCalls(instancedBuckets);

    var estimatedSavings =
      estimatedOriginalDrawCalls -
      estimatedOptimizedDrawCalls;

    if (estimatedSavings <
        ValheimScalabilityConfig.MinimumEstimatedDrawCallSavings)
    {
      cell.SourceRenderersByRoot.Clear();
      cell.ClusterBuilt = false;
      cell.Dirty = false;
      cell.ContainsStaleGeometry = false;
      return;
    }

    BuildCombinedMeshes(
      cell,
      combinedBuckets);

    BuildInstancedBatches(
      cell,
      instancedBuckets);

    cell.ClusterBuilt =
      cell.GeneratedRenderers.Count > 0 ||
      cell.InstancedBatches.Count > 0;

    UpdateEstimatedGeneratedMemory(cell);

    cell.Dirty = false;
    cell.ContainsStaleGeometry = false;
  }

  /// <summary>
  /// Returns the number of original renderer/submesh draw submissions represented.
  /// A renderer is only optimized when every submesh is safely representable.
  /// </summary>
  private int CollectRootGeometry(
    GameObject root,
    ClusterCellState cell,
    Dictionary<CombinedBucketKey, CombinedBucket> combinedBuckets,
    Dictionary<InstancedBucketKey, InstancedBucket> instancedBuckets)
  {
    var estimatedOriginalDrawCalls = 0;

    var sourceRenderers =
      new HashSet<MeshRenderer>();

    var rootNetView =
      root.GetComponent<ZNetView>();

    var renderers =
      root.GetComponentsInChildren<MeshRenderer>(true);

    foreach (var renderer in renderers)
    {
      if (!TryGetRendererStrategy(
            root,
            rootNetView,
            renderer,
            out var strategy,
            out var selectedLodGroup,
            out var mesh,
            out var materials,
            out var instancedMaterials))
      {
        continue;
      }

      var added = false;

      if (strategy ==
          ClusterRenderStrategy.CombinedMesh)
      {
        var clusterMatrix =
          transform.worldToLocalMatrix *
          renderer.transform.localToWorldMatrix;

        for (var subMeshIndex = 0;
             subMeshIndex < mesh.subMeshCount;
             ++subMeshIndex)
        {
          var material =
            materials[subMeshIndex];

          var key =
            new CombinedBucketKey(
              material,
              renderer.gameObject.layer,
              renderer.lightmapIndex,
              renderer.shadowCastingMode,
              renderer.receiveShadows,
              renderer.lightProbeUsage,
              renderer.reflectionProbeUsage,
              renderer.renderingLayerMask,
              renderer.sortingLayerID,
              renderer.sortingOrder);

          if (!combinedBuckets.TryGetValue(
                key,
                out var bucket))
          {
            bucket =
              new CombinedBucket(
                material);

            combinedBuckets.Add(
              key,
              bucket);
          }

          bucket.Instances.Add(
            new CombineInstance
            {
              mesh = mesh,
              subMeshIndex = subMeshIndex,
              transform = clusterMatrix,
              lightmapScaleOffset =
                renderer.lightmapScaleOffset
            });
        }

        added = true;
      }
      else if (strategy ==
               ClusterRenderStrategy.Instanced)
      {
        var worldMatrix =
          renderer.transform.localToWorldMatrix;

        for (var subMeshIndex = 0;
             subMeshIndex < mesh.subMeshCount;
             ++subMeshIndex)
        {
          var sourceMaterial =
            materials[subMeshIndex];

          var instancedMaterial =
            instancedMaterials[subMeshIndex];

          var key =
            new InstancedBucketKey(
              mesh,
              subMeshIndex,
              sourceMaterial,
              instancedMaterial,
              renderer.gameObject.layer,
              renderer.shadowCastingMode,
              renderer.receiveShadows,
              renderer.lightProbeUsage);

          if (!instancedBuckets.TryGetValue(
                key,
                out var bucket))
          {
            bucket = new InstancedBucket();
            instancedBuckets.Add(
              key,
              bucket);
          }

          bucket.Matrices.Add(worldMatrix);
        }

        added = true;
      }

      if (!added)
      {
        continue;
      }

      estimatedOriginalDrawCalls +=
        mesh.subMeshCount;

      sourceRenderers.Add(renderer);

      if (selectedLodGroup)
      {
        AddAllLodRenderers(
          selectedLodGroup,
          sourceRenderers);
      }
    }

    if (sourceRenderers.Count == 0)
    {
      return 0;
    }

    var sourceList =
      new List<MeshRenderer>(
        sourceRenderers.Count);

    foreach (var renderer in sourceRenderers)
    {
      if (renderer)
      {
        sourceList.Add(renderer);
      }
    }

    if (sourceList.Count > 0)
    {
      cell.SourceRenderersByRoot[root] =
        sourceList;
    }

    return estimatedOriginalDrawCalls;
  }

  private bool TryGetRendererStrategy(
    GameObject root,
    ZNetView rootNetView,
    MeshRenderer renderer,
    out ClusterRenderStrategy strategy,
    out LODGroup selectedLodGroup,
    out Mesh mesh,
    out Material[] materials,
    out Material[] instancedMaterials)
  {
    strategy = ClusterRenderStrategy.Original;
    selectedLodGroup = null;
    mesh = null;
    materials = null;
    instancedMaterials = null;

    if (!renderer ||
        !renderer.enabled ||
        !renderer.gameObject.activeInHierarchy ||
        renderer.forceRenderingOff)
    {
      return false;
    }

    // A nominally-static root can still contain a nested moving physics
    // hierarchy. Never merge a renderer whose current ancestors contain a
    // Rigidbody into a world-sector mesh.
    if (DynamicHierarchyBatchExclusion.ShouldExcludeRenderer(
          renderer) ||
        HasUnsafeDynamicRendering(
          renderer.gameObject))
    {
      return false;
    }

    if (!ClusterBatchFilter.IsLayerAllowed(
          renderer.gameObject.layer))
    {
      return false;
    }

    if (!ClusterBatchFilter.IsObjectAllowed(
          root.name,
          renderer.gameObject.name))
    {
      return false;
    }

    if (ValheimScalabilityConfig.SkipPropertyBlockRenderers &&
        renderer.HasPropertyBlock())
    {
      return false;
    }

    if (HasAnimatorBetween(
          renderer.transform,
          root.transform))
    {
      return false;
    }

    var rendererNetView =
      renderer.GetComponentInParent<ZNetView>();

    if (rootNetView)
    {
      if (rendererNetView &&
          rendererNetView != rootNetView)
      {
        return false;
      }
    }
    else if (rendererNetView)
    {
      return false;
    }

    if (!IsSelectedLodRenderer(
          renderer,
          root.transform,
          out selectedLodGroup))
    {
      return false;
    }

    var meshFilter =
      renderer.GetComponent<MeshFilter>();

    if (!meshFilter ||
        !meshFilter.sharedMesh)
    {
      return false;
    }

    mesh =
      meshFilter.sharedMesh;

    materials =
      renderer.sharedMaterials;

    if (mesh.subMeshCount <= 0 ||
        materials.Length < mesh.subMeshCount)
    {
      return false;
    }

    for (var subMeshIndex = 0;
         subMeshIndex < mesh.subMeshCount;
         ++subMeshIndex)
    {
      var material =
        materials[subMeshIndex];

      if (!material ||
          !ClusterBatchFilter.IsMaterialAllowed(material) ||
          !ClusterBatchFilter.IsShaderAllowed(material.shader))
      {
        return false;
      }
    }

    var wantsInstancing =
      ClusterBatchFilter.IsInstancingCandidate(
        root.name,
        renderer.gameObject.name,
        materials);

    if (wantsInstancing)
    {
      // DrawMeshInstanced cannot safely reproduce every MeshRenderer state.
      // Keep these renderers original rather than silently changing their visuals.
      if (!ValheimScalabilityConfig.IsGpuInstancingEnabled ||
          !SystemInfo.supportsInstancing ||
          IsLightmapped(renderer) ||
          renderer.lightProbeUsage == LightProbeUsage.UseProxyVolume ||
          renderer.sortingLayerID != 0 ||
          renderer.sortingOrder != 0)
      {
        return false;
      }

      instancedMaterials =
        new Material[mesh.subMeshCount];

      for (var subMeshIndex = 0;
           subMeshIndex < mesh.subMeshCount;
           ++subMeshIndex)
      {
        var instancedMaterial =
          InstancedMaterialCache.GetOrCreate(
            materials[subMeshIndex]);

        if (!instancedMaterial)
        {
          // Critical safety rule:
          // wind candidates never fall back to CombineMeshes.
          // Doing so can collapse per-object wind origins and synchronize sway.
          return false;
        }

        instancedMaterials[subMeshIndex] =
          instancedMaterial;
      }

      strategy =
        ClusterRenderStrategy.Instanced;

      return true;
    }

    if (!mesh.isReadable)
    {
      return false;
    }

    strategy =
      ClusterRenderStrategy.CombinedMesh;

    return true;
  }

  private static bool IsLightmapped(
    MeshRenderer renderer)
  {
    return renderer.lightmapIndex >= 0 &&
           renderer.lightmapIndex < 65534;
  }

  private static int CountCombinedDrawCalls(
    Dictionary<CombinedBucketKey, CombinedBucket> buckets)
  {
    var count = 0;

    foreach (var pair in buckets)
    {
      if (pair.Value.Material &&
          pair.Value.Instances.Count > 0)
      {
        ++count;
      }
    }

    return count;
  }

  private static int CountInstancedDrawCalls(
    Dictionary<InstancedBucketKey, InstancedBucket> buckets)
  {
    var count = 0;

    foreach (var bucket in buckets.Values)
    {
      if (bucket.Matrices.Count == 0)
      {
        continue;
      }

      count += Mathf.CeilToInt(
        bucket.Matrices.Count /
        (float) SectorMeshClusterSettings.MaxInstancedMatricesPerDraw);
    }

    return count;
  }

  private void BuildCombinedMeshes(
    ClusterCellState cell,
    Dictionary<CombinedBucketKey, CombinedBucket> buckets)
  {
    foreach (var pair in buckets)
    {
      var key = pair.Key;
      var bucket = pair.Value;

      var material =
        bucket.Material;

      if (!material ||
          bucket.Instances.Count == 0)
      {
        continue;
      }

      if (ValheimScalabilityConfig.IsBatchDiagnosticsEnabled)
      {
        Debug.Log(
          $"[ValheimScalability] Combined batch sector={_sector.x},{_sector.y} cell={cell.Index} material='{material.name}' materialKey={key.MaterialIdentity} instances={bucket.Instances.Count} layer={key.Layer} lightmap={key.LightmapIndex} shadows={key.ShadowCastingMode} receiveShadows={key.ReceiveShadows} lightProbes={key.LightProbeUsage} reflectionProbes={key.ReflectionProbeUsage} renderingLayerMask={key.RenderingLayerMask} sortingLayer={key.SortingLayerId} sortingOrder={key.SortingOrder}");
      }

      var combinedMesh = new Mesh
      {
        name =
          $"ValheimScalability_Sector_{_sector.x}_{_sector.y}_Cell_{cell.Index}_{material.name}",
        indexFormat = IndexFormat.UInt32
      };

      var hasLightmapData =
        key.LightmapIndex >= 0 &&
        key.LightmapIndex < 65534;

      combinedMesh.CombineMeshes(
        bucket.Instances.ToArray(),
        true,
        true,
        hasLightmapData);

      combinedMesh.RecalculateBounds();

      var meshObject = new GameObject(
        $"Combined_{_sector.x}_{_sector.y}_Cell_{cell.Index}_{material.name}_x{bucket.Instances.Count}");

      meshObject.layer =
        key.Layer;

      meshObject.transform.SetParent(
        transform,
        false);

      meshObject.transform.localPosition =
        Vector3.zero;

      meshObject.transform.localRotation =
        Quaternion.identity;

      meshObject.transform.localScale =
        Vector3.one;

      var meshFilter =
        meshObject.AddComponent<MeshFilter>();

      meshFilter.sharedMesh =
        combinedMesh;

      var meshRenderer =
        meshObject.AddComponent<MeshRenderer>();

      meshRenderer.sharedMaterial =
        material;

      meshRenderer.shadowCastingMode =
        key.ShadowCastingMode;

      meshRenderer.receiveShadows =
        key.ReceiveShadows;

      meshRenderer.lightProbeUsage =
        key.LightProbeUsage;

      meshRenderer.reflectionProbeUsage =
        key.ReflectionProbeUsage;

      meshRenderer.renderingLayerMask =
        key.RenderingLayerMask;

      meshRenderer.sortingLayerID =
        key.SortingLayerId;

      meshRenderer.sortingOrder =
        key.SortingOrder;

      if (hasLightmapData)
      {
        meshRenderer.lightmapIndex =
          key.LightmapIndex;
      }

      meshRenderer.enabled = false;

      cell.GeneratedMeshes.Add(
        combinedMesh);

      cell.GeneratedObjects.Add(
        meshObject);

      cell.GeneratedRenderers.Add(
        new GeneratedRenderer(
          meshRenderer,
          key.Layer));
    }
  }

  private static void BuildInstancedBatches(
    ClusterCellState cell,
    Dictionary<InstancedBucketKey, InstancedBucket> buckets)
  {
    foreach (var pair in buckets)
    {
      var key = pair.Key;
      var bucket = pair.Value;

      if (!key.Mesh ||
          !key.SourceMaterial ||
          !key.InstancedMaterial ||
          bucket.Matrices.Count == 0)
      {
        continue;
      }

      cell.InstancedBatches.Add(
        new InstancedClusterBatch(
          key.Mesh,
          key.SubMeshIndex,
          key.SourceMaterial,
          key.InstancedMaterial,
          key.Layer,
          key.ShadowCastingMode,
          key.ReceiveShadows,
          key.LightProbeUsage,
          bucket.Matrices));
    }
  }

  private void HandleInstancingFailure(
    ClusterCellState cell,
    Material sourceMaterial)
  {
    InstancedMaterialCache.MarkUnsupported(
      sourceMaterial);

    TransitionPresentation(
      cell,
      OptimizedPresentation.None);

    cell.Dirty = true;
    cell.ContainsStaleGeometry = true;
    cell.NextBuildTime =
      Time.unscaledTime +
      ValheimScalabilityConfig.RemovalRebuildDelay;
  }

  private bool CanRegisterRoot(
    GameObject root)
  {
    if (!root ||
        root.GetComponentInParent<Character>() ||
        DynamicHierarchyBatchExclusion.ShouldExclude(root) ||
        HasUnsafeDynamicRendering(root))
    {
      return false;
    }

    var renderers =
      root.GetComponentsInChildren<MeshRenderer>(true);

    foreach (var renderer in renderers)
    {
      if (renderer &&
          renderer.GetComponent<MeshFilter>())
      {
        return true;
      }
    }

    return false;
  }

  private void InvalidateCellForRemovedGeometry(
    ClusterCellState cell)
  {
    cell.Dirty = true;
    cell.ContainsStaleGeometry =
      cell.ClusterBuilt;

    cell.NextBuildTime = Mathf.Max(
      cell.NextBuildTime,
      Time.unscaledTime +
      ValheimScalabilityConfig.RemovalRebuildDelay);

    if (!cell.ContainsStaleGeometry)
    {
      return;
    }

    TransitionPresentation(
      cell,
      OptimizedPresentation.None);

    if (_isWarmCached)
    {
      DestroyOptimizedRepresentations(cell);
      cell.ClusterBuilt = false;
    }
  }

  private void RefreshCellDimensions()
  {
    _zoneSize =
      ZoneSystem.instance
        ? ZoneSystem.instance.m_zoneSize
        : 64f;

    _cellDivisions =
      ValheimScalabilityConfig.Mode ==
        ClusterPresentationMode.FullCluster &&
      ValheimScalabilityConfig.UseSingleCellInFullCluster
        ? 1
        : Mathf.Clamp(
          ValheimScalabilityConfig.CellDivisions,
          1,
          8);

    _cellSize =
      _zoneSize /
      _cellDivisions;
  }

  private ClusterCellState GetOrCreateCell(
    int cellIndex)
  {
    if (_cells.TryGetValue(
          cellIndex,
          out var existing))
    {
      return existing;
    }

    var cell =
      new ClusterCellState(
        cellIndex,
        GetCellBounds(cellIndex),
        Time.unscaledTime +
        ValheimScalabilityConfig.InitialBuildDelay);

    _cells.Add(
      cellIndex,
      cell);

    return cell;
  }

  private int GetCellIndex(
    Vector3 worldPosition)
  {
    var sectorCenter =
      ZoneSystem.GetZonePos(_sector);

    var half =
      _zoneSize * 0.5f;

    var localX =
      Mathf.Clamp(
        worldPosition.x -
        (sectorCenter.x - half),
        0f,
        _zoneSize - 0.001f);

    var localZ =
      Mathf.Clamp(
        worldPosition.z -
        (sectorCenter.z - half),
        0f,
        _zoneSize - 0.001f);

    var x =
      Mathf.Clamp(
        Mathf.FloorToInt(
          localX /
          _cellSize),
        0,
        _cellDivisions - 1);

    var z =
      Mathf.Clamp(
        Mathf.FloorToInt(
          localZ /
          _cellSize),
        0,
        _cellDivisions - 1);

    return
      z * _cellDivisions +
      x;
  }

  private Bounds GetCellBounds(
    int cellIndex)
  {
    var x =
      cellIndex %
      _cellDivisions;

    var z =
      cellIndex /
      _cellDivisions;

    var sectorCenter =
      ZoneSystem.GetZonePos(_sector);

    var half =
      _zoneSize * 0.5f;

    var minX =
      sectorCenter.x -
      half +
      x * _cellSize;

    var minZ =
      sectorCenter.z -
      half +
      z * _cellSize;

    var center =
      new Vector3(
        minX + _cellSize * 0.5f,
        sectorCenter.y,
        minZ + _cellSize * 0.5f);

    return new Bounds(
      center,
      new Vector3(
        _cellSize,
        ValheimScalabilityConfig.VisibilityHeight,
        _cellSize));
  }

  private static float GetCardinalDistanceToBounds(
    Vector3 point,
    Bounds bounds)
  {
    var min =
      bounds.min;

    var max =
      bounds.max;

    var dx =
      point.x < min.x
        ? min.x - point.x
        : point.x > max.x
          ? point.x - max.x
          : 0f;

    var dz =
      point.z < min.z
        ? min.z - point.z
        : point.z > max.z
          ? point.z - max.z
          : 0f;

    return dx + dz;
  }

  private GameObject FindRegisteredRoot(
    Transform start)
  {
    var current = start;

    while (current)
    {
      if (_rootToCell.ContainsKey(
            current.gameObject))
      {
        return current.gameObject;
      }

      current = current.parent;
    }

    return null;
  }

  private static Transform FindTopLevelChild(
    Transform hierarchyRoot,
    Transform item)
  {
    if (!hierarchyRoot ||
        !item)
    {
      return null;
    }

    if (item ==
        hierarchyRoot)
    {
      return item;
    }

    var current =
      item;

    while (current.parent &&
           current.parent != hierarchyRoot)
    {
      current =
        current.parent;
    }

    return current.parent ==
           hierarchyRoot
      ? current
      : null;
  }

  private static bool IsSelectedLodRenderer(
    MeshRenderer renderer,
    Transform root,
    out LODGroup lodGroup)
  {
    lodGroup =
      FindNearestLodGroup(
        renderer.transform,
        root);

    if (!lodGroup)
    {
      return true;
    }

    var lods =
      lodGroup.GetLODs();

    if (lods.Length == 0)
    {
      return true;
    }

    var selectedIndex = 0;

    if (ValheimScalabilityConfig.UseLowestLod)
    {
      for (var index =
             lods.Length - 1;
           index >= 0;
           --index)
      {
        if (lods[index].renderers != null &&
            lods[index].renderers.Length > 0)
        {
          selectedIndex = index;
          break;
        }
      }
    }

    foreach (var selectedRenderer in
             lods[selectedIndex].renderers)
    {
      if (selectedRenderer ==
          renderer)
      {
        return true;
      }
    }

    return false;
  }

  private static LODGroup FindNearestLodGroup(
    Transform start,
    Transform root)
  {
    var current =
      start;

    while (current)
    {
      var lodGroup =
        current.GetComponent<LODGroup>();

      if (lodGroup)
      {
        return lodGroup;
      }

      if (current ==
          root)
      {
        break;
      }

      current =
        current.parent;
    }

    return null;
  }

  private static void AddAllLodRenderers(
    LODGroup lodGroup,
    HashSet<MeshRenderer> target)
  {
    foreach (var lod in
             lodGroup.GetLODs())
    {
      if (lod.renderers == null)
      {
        continue;
      }

      foreach (var renderer in
               lod.renderers)
      {
        if (renderer is MeshRenderer meshRenderer &&
            meshRenderer)
        {
          target.Add(
            meshRenderer);
        }
      }
    }
  }

  /// <summary>
  /// Static sector mesh combining cannot safely represent geometry whose vertices
  /// or transforms are controlled by cloth, skinning, or animation systems.
  ///
  /// This is intentionally conservative. A prefab containing one of these
  /// components remains on its original renderers rather than risking a missing,
  /// frozen, or incorrectly deformed banner/sail/cloth object.
  /// </summary>
  private static bool HasUnsafeDynamicRendering(
    GameObject root)
  {
    if (!root)
    {
      return true;
    }

    if (root.GetComponentInChildren<Cloth>(
          true))
    {
      return true;
    }

    if (root.GetComponentInChildren<SkinnedMeshRenderer>(
          true))
    {
      return true;
    }

    if (root.GetComponentInChildren<Animator>(
          true))
    {
      return true;
    }

    if (root.GetComponentInChildren<Animation>(
          true))
    {
      return true;
    }

    return false;
  }

  private static bool HasAnimatorBetween(
    Transform start,
    Transform root)
  {
    var current =
      start;

    while (current)
    {
      if (current.GetComponent<Animator>())
      {
        return true;
      }

      if (current ==
          root)
      {
        break;
      }

      current =
        current.parent;
    }

    return false;
  }

  private static void HideSourceRenderers(
    ClusterCellState cell,
    int? onlyLayer)
  {
    foreach (var pair in
             cell.SourceRenderersByRoot)
    {
      var root =
        pair.Key;

      if (!root ||
          cell.DamagedRoots.Contains(root))
      {
        continue;
      }

      if (!cell.CurrentlyHiddenRenderersByRoot.TryGetValue(
            root,
            out var hidden))
      {
        hidden =
          new List<MeshRenderer>();

        cell.CurrentlyHiddenRenderersByRoot.Add(
          root,
          hidden);
      }

      foreach (var renderer in
               pair.Value)
      {
        if (!renderer ||
            !renderer.enabled ||
            !renderer.gameObject.activeInHierarchy)
        {
          continue;
        }

        if (onlyLayer.HasValue &&
            renderer.gameObject.layer !=
            onlyLayer.Value)
        {
          continue;
        }

        renderer.enabled =
          false;

        if (!hidden.Contains(renderer))
        {
          hidden.Add(renderer);
        }
      }
    }
  }

  private static void RestoreOriginalRenderers(
    ClusterCellState cell)
  {
    foreach (var pair in
             cell.CurrentlyHiddenRenderersByRoot)
    {
      RestoreRendererList(
        pair.Value);
    }

    cell.CurrentlyHiddenRenderersByRoot.Clear();
  }

  private static void RestoreRendererList(
    List<MeshRenderer> renderers)
  {
    foreach (var renderer in
             renderers)
    {
      if (renderer)
      {
        renderer.enabled =
          true;
      }
    }

    renderers.Clear();
  }

  private static void SetOptimizedPresentation(
    ClusterCellState cell,
    OptimizedPresentation presentation)
  {
    cell.InstancedPresentation =
      presentation;

    foreach (var generated in
             cell.GeneratedRenderers)
    {
      if (!generated.Renderer)
      {
        continue;
      }

      generated.Renderer.enabled =
        presentation == OptimizedPresentation.All ||
        presentation == OptimizedPresentation.PieceLayerOnly &&
        generated.Layer ==
        SectorMeshClusterSettings.PieceLayer;
    }
  }

  private static void DestroyOptimizedRepresentations(
    ClusterCellState cell)
  {
    SetOptimizedPresentation(
      cell,
      OptimizedPresentation.None);

    foreach (var generatedObject in
             cell.GeneratedObjects)
    {
      if (generatedObject)
      {
        Destroy(generatedObject);
      }
    }

    foreach (var generatedMesh in
             cell.GeneratedMeshes)
    {
      if (generatedMesh)
      {
        Destroy(generatedMesh);
      }
    }

    cell.GeneratedObjects.Clear();
    cell.GeneratedRenderers.Clear();
    cell.GeneratedMeshes.Clear();
    cell.InstancedBatches.Clear();
    cell.EstimatedGeneratedMemoryBytes = 0L;
  }

  private static void UpdateEstimatedGeneratedMemory(
    ClusterCellState cell)
  {
    long bytes = 0;

    foreach (var mesh in cell.GeneratedMeshes)
    {
      if (!mesh)
      {
        continue;
      }

      try
      {
        bytes +=
          Profiler.GetRuntimeMemorySizeLong(mesh);
      }
      catch
      {
        // Conservative fallback for unusual Unity/runtime configurations.
        bytes +=
          (long) mesh.vertexCount *
          64L;
      }
    }

    foreach (var batch in cell.InstancedBatches)
    {
      if (batch != null)
      {
        bytes +=
          batch.EstimatedMemoryBytes;
      }
    }

    // MeshRenderer/MeshFilter/GameObject overhead is small compared with large
    // generated meshes but still non-zero. Include a modest approximation.
    bytes +=
      (long) cell.GeneratedObjects.Count *
      2048L;

    cell.EstimatedGeneratedMemoryBytes =
      bytes;
  }

  private void CleanupDestroyedRoots()
  {
    _deadRootBuffer.Clear();

    foreach (var pair in
             _rootToCell)
    {
      if (!pair.Key)
      {
        _deadRootBuffer.Add(
          pair.Key);
      }
    }

    foreach (var root in
             _deadRootBuffer)
    {
      if (!_rootToCell.TryGetValue(
            root,
            out var cellIndex))
      {
        continue;
      }

      _rootToCell.Remove(root);

      if (!_cells.TryGetValue(
            cellIndex,
            out var cell))
      {
        continue;
      }

      cell.RegisteredRoots.Remove(root);
      cell.DamagedRoots.Remove(root);
      cell.SourceRenderersByRoot.Remove(root);
      cell.CurrentlyHiddenRenderersByRoot.Remove(root);

      InvalidateCellForRemovedGeometry(
        cell);
    }

    _deadRootBuffer.Clear();
  }

  private void OnDisable()
  {
    foreach (var cell in
             _cells.Values)
    {
      SetOptimizedPresentation(
        cell,
        OptimizedPresentation.None);

      RestoreOriginalRenderers(cell);

      cell.CurrentPresentation =
        OptimizedPresentation.Uninitialized;
    }
  }

  private void OnDestroy()
  {
    foreach (var cell in
             _cells.Values)
    {
      RestoreOriginalRenderers(cell);
      DestroyOptimizedRepresentations(cell);
    }

    _rootToCell.Clear();
    _cells.Clear();
  }

  private enum OptimizedPresentation
  {
    Uninitialized = -1,
    None = 0,
    All = 1,
    PieceLayerOnly = 2
  }

  private sealed class ClusterCellState
  {
    public readonly int Index;
    public readonly Bounds WorldBounds;

    public readonly HashSet<GameObject>
      RegisteredRoots = new();

    public readonly HashSet<GameObject>
      DamagedRoots = new();

    public readonly Dictionary<GameObject, List<MeshRenderer>>
      SourceRenderersByRoot = new();

    public readonly Dictionary<GameObject, List<MeshRenderer>>
      CurrentlyHiddenRenderersByRoot = new();

    public readonly List<GeneratedRenderer>
      GeneratedRenderers = new();

    public readonly List<Mesh>
      GeneratedMeshes = new();

    public readonly List<GameObject>
      GeneratedObjects = new();

    public readonly List<InstancedClusterBatch>
      InstancedBatches = new();

    public long EstimatedGeneratedMemoryBytes;

    public bool ClusterBuilt;
    public bool Dirty = true;
    public bool ContainsStaleGeometry;

    public bool NearLocalPlayer;
    public bool VisibleToLocalPlayer;

    public float NextBuildTime;
    public float SuspendedUntil;

    public OptimizedPresentation
      InstancedPresentation =
        OptimizedPresentation.None;

    public OptimizedPresentation
      CurrentPresentation =
        OptimizedPresentation.Uninitialized;

    public ClusterCellState(
      int index,
      Bounds worldBounds,
      float nextBuildTime)
    {
      Index = index;
      WorldBounds = worldBounds;
      NextBuildTime = nextBuildTime;
    }
  }

  private readonly struct GeneratedRenderer
  {
    public readonly MeshRenderer Renderer;
    public readonly int Layer;

    public GeneratedRenderer(
      MeshRenderer renderer,
      int layer)
    {
      Renderer = renderer;
      Layer = layer;
    }
  }

  private sealed class CombinedBucket
  {
    public readonly Material Material;

    public readonly List<CombineInstance>
      Instances = new();

    public CombinedBucket(
      Material material)
    {
      Material = material;
    }
  }

  private sealed class InstancedBucket
  {
    public readonly List<Matrix4x4>
      Matrices = new();
  }

  private readonly struct CombinedBucketKey :
    IEquatable<CombinedBucketKey>
  {
    public readonly MaterialBatchIdentity MaterialIdentity;
    public readonly int Layer;
    public readonly int LightmapIndex;
    public readonly ShadowCastingMode ShadowCastingMode;
    public readonly bool ReceiveShadows;
    public readonly LightProbeUsage LightProbeUsage;
    public readonly ReflectionProbeUsage ReflectionProbeUsage;
    public readonly uint RenderingLayerMask;
    public readonly int SortingLayerId;
    public readonly int SortingOrder;

    public CombinedBucketKey(
      Material material,
      int layer,
      int lightmapIndex,
      ShadowCastingMode shadowCastingMode,
      bool receiveShadows,
      LightProbeUsage lightProbeUsage,
      ReflectionProbeUsage reflectionProbeUsage,
      uint renderingLayerMask,
      int sortingLayerId,
      int sortingOrder)
    {
      MaterialIdentity =
        MaterialBatchIdentity.From(material);

      Layer = layer;
      LightmapIndex = lightmapIndex;
      ShadowCastingMode = shadowCastingMode;
      ReceiveShadows = receiveShadows;
      LightProbeUsage = lightProbeUsage;
      ReflectionProbeUsage = reflectionProbeUsage;
      RenderingLayerMask = renderingLayerMask;
      SortingLayerId = sortingLayerId;
      SortingOrder = sortingOrder;
    }

    public bool Equals(
      CombinedBucketKey other)
    {
      return MaterialIdentity.Equals(other.MaterialIdentity) &&
             Layer == other.Layer &&
             LightmapIndex == other.LightmapIndex &&
             ShadowCastingMode == other.ShadowCastingMode &&
             ReceiveShadows == other.ReceiveShadows &&
             LightProbeUsage == other.LightProbeUsage &&
             ReflectionProbeUsage == other.ReflectionProbeUsage &&
             RenderingLayerMask == other.RenderingLayerMask &&
             SortingLayerId == other.SortingLayerId &&
             SortingOrder == other.SortingOrder;
    }

    public override bool Equals(
      object obj)
    {
      return obj is CombinedBucketKey other &&
             Equals(other);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        var hash =
          MaterialIdentity.GetHashCode();

        hash =
          (hash * 397) ^
          Layer;

        hash =
          (hash * 397) ^
          LightmapIndex;

        hash =
          (hash * 397) ^
          (int) ShadowCastingMode;

        hash =
          (hash * 397) ^
          ReceiveShadows.GetHashCode();

        hash =
          (hash * 397) ^
          (int) LightProbeUsage;

        hash =
          (hash * 397) ^
          (int) ReflectionProbeUsage;

        hash =
          (hash * 397) ^
          (int) RenderingLayerMask;

        hash =
          (hash * 397) ^
          SortingLayerId;

        hash =
          (hash * 397) ^
          SortingOrder;

        return hash;
      }
    }
  }

  private readonly struct InstancedBucketKey :
    IEquatable<InstancedBucketKey>
  {
    public readonly Mesh Mesh;
    public readonly int SubMeshIndex;
    public readonly Material SourceMaterial;
    public readonly Material InstancedMaterial;
    public readonly int Layer;
    public readonly ShadowCastingMode ShadowCastingMode;
    public readonly bool ReceiveShadows;
    public readonly LightProbeUsage LightProbeUsage;

    public InstancedBucketKey(
      Mesh mesh,
      int subMeshIndex,
      Material sourceMaterial,
      Material instancedMaterial,
      int layer,
      ShadowCastingMode shadowCastingMode,
      bool receiveShadows,
      LightProbeUsage lightProbeUsage)
    {
      Mesh = mesh;
      SubMeshIndex = subMeshIndex;
      SourceMaterial = sourceMaterial;
      InstancedMaterial = instancedMaterial;
      Layer = layer;
      ShadowCastingMode = shadowCastingMode;
      ReceiveShadows = receiveShadows;
      LightProbeUsage = lightProbeUsage;
    }

    public bool Equals(
      InstancedBucketKey other)
    {
      return Mesh == other.Mesh &&
             SubMeshIndex == other.SubMeshIndex &&
             SourceMaterial == other.SourceMaterial &&
             Layer == other.Layer &&
             ShadowCastingMode == other.ShadowCastingMode &&
             ReceiveShadows == other.ReceiveShadows &&
             LightProbeUsage == other.LightProbeUsage;
    }

    public override bool Equals(
      object obj)
    {
      return obj is InstancedBucketKey other &&
             Equals(other);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        var hash =
          Mesh
            ? Mesh.GetInstanceID()
            : 0;

        hash =
          (hash * 397) ^
          SubMeshIndex;

        hash =
          (hash * 397) ^
          (SourceMaterial
            ? SourceMaterial.GetInstanceID()
            : 0);

        hash =
          (hash * 397) ^
          Layer;

        hash =
          (hash * 397) ^
          (int) ShadowCastingMode;

        hash =
          (hash * 397) ^
          ReceiveShadows.GetHashCode();

        hash =
          (hash * 397) ^
          (int) LightProbeUsage;

        return hash;
      }
    }
  }
}
