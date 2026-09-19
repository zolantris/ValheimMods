using System.Collections.Generic;
using UnityEngine;
using ValheimScalability.BepInExConfig;

namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Client-side owner for all world-sector optimized rendering controllers.
/// </summary>
public sealed class SectorMeshClusterManager : MonoBehaviour
{
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

  private readonly Dictionary<int, Vector2s>
    _networkObjectSectors = new();

  private readonly List<Vector2s>
    _deadSectorBuffer = new();

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

    var sector =
      zdo.GetSector();

    var controller =
      GetOrCreateController(sector);

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

    GetOrCreateController(sector)
      .RegisterHierarchy(
        hierarchyRoot);
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

    GetOrCreateController(sector)
      .RegisterHierarchy(
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
      controller.NotifyDamage(
        damagedObject);
    }
  }

  public void UpdateLocalPlayerProximity(
    Vector3 playerPosition,
    Camera camera)
  {
    foreach (var controller in
             _sectorControllers.Values)
    {
      if (controller)
      {
        controller.UpdateLocalPlayerProximity(
          playerPosition,
          camera);
      }
    }
  }

  public void ClearLocalPlayerProximity()
  {
    foreach (var controller in
             _sectorControllers.Values)
    {
      if (controller)
      {
        controller.ClearLocalPlayerProximity();
      }
    }
  }

  /// <summary>
  /// Rare runtime-config path.
  /// Controllers retain all known live roots but rebuild their cell/filter representation.
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

    return controller;
  }

  private void Update()
  {
    if (Application.isBatchMode ||
        Time.unscaledTime <
        _nextMaintenanceTime)
    {
      return;
    }

    _nextMaintenanceTime =
      Time.unscaledTime +
      ValheimScalabilityConfig.MaintenanceInterval;

    foreach (var controller in
             _sectorControllers.Values)
    {
      if (controller)
      {
        controller.Tick();
      }
    }

    CleanupDeadSectors();
  }

  private void CleanupDeadSectors()
  {
    if (!ZoneSystem.instance)
    {
      return;
    }

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

      var zoneStillLoaded =
        ZoneSystem.instance.m_zones
          .ContainsKey(sector);

      if (!zoneStillLoaded &&
          !controller.HasRegisteredObjects)
      {
        Destroy(
          controller.gameObject);

        _deadSectorBuffer.Add(
          sector);
      }
    }

    foreach (var sector in
             _deadSectorBuffer)
    {
      _sectorControllers.Remove(
        sector);
    }

    _deadSectorBuffer.Clear();
  }

  private void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
    }

    _sectorControllers.Clear();
    _networkObjectSectors.Clear();

    InstancedMaterialCache.Clear();
  }
}
