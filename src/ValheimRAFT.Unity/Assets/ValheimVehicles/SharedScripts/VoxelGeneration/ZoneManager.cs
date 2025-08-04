// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zolantris.Shared;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class ZoneManager : MonoBehaviour
  {
    [Header("Config")]
    [SerializeField] private ZoneTerrainConfig terrainConfig;
    [SerializeField] private int activeRadius = 2;
    [SerializeField] public bool canRandomizeWorldSeed;

    [Header("Optional")]
    [SerializeField] private bool useBuildPositionOverride;
    [SerializeField] private Vector3 buildPositionOverride;

    [Header("Optional Visuals")]
    public Material terrainMaterial;
    public Material cubeMaterial;
    public Material fillerMaterial;
    private readonly Dictionary<Vector3Int, ZoneData> _activeZoneData = new();

    private readonly Dictionary<Vector3Int, ZoneInstance> _activeZones = new();
    public CoroutineHandle _zoneCreatorHandler;

    private void Start()
    {
      _zoneCreatorHandler ??= new CoroutineHandle(this);
      _zoneCreatorHandler.Start(CreateAllZones());
    }

    private void FixedUpdate()
    {
     
    }

    private void OnEnable()
    {
      _zoneCreatorHandler ??= new CoroutineHandle(this);
    }

    private void OnDisable()
    {
      StopAllCoroutines();
    }

    public IEnumerator CreateAllZones()
    {
      Vector3 observerPos = useBuildPositionOverride ? buildPositionOverride : Camera.main.transform.position;
      Vector3Int currentZone = ZoneUtil.GetZoneCoord(observerPos);

      HashSet<Vector3Int> desiredZones = new();

      for (int x = -activeRadius; x <= activeRadius; x++)
      for (int z = -activeRadius; z <= activeRadius; z++)
      {
        Vector3Int coord = currentZone + new Vector3Int(x, 0, z);
        desiredZones.Add(coord);
        if (!_activeZones.ContainsKey(coord))
        {
          yield return CreateZone(coord);
        }
      }

      // Remove zones outside active radius
      var toRemove = new List<Vector3Int>();
      foreach (var kvp in _activeZones)
      {
        if (!desiredZones.Contains(kvp.Key))
        {
          Destroy(kvp.Value.gameObject);
          toRemove.Add(kvp.Key);
        }
      }

      foreach (var key in toRemove)
      {
        _activeZones.Remove(key);
      }
    }

    private ZoneData CreateZoneData(Vector3Int coord, GameObject zoneObj)
    {
      // Clone terrain config so each zone has isolated settings
      var config = terrainConfig.Clone();
      
      if (canRandomizeWorldSeed)
      {
        config.UpdateSeed(0, 90000);
      }
      
      // Create ZoneData
      var zoneData = new ZoneData
      {
        Coord = coord,
        ZoneGO = zoneObj,
        TerrainConfig = config,
        IsActive = true,
        LastUsedTime = Time.time
      };
      
      return zoneData;
    }

    private IEnumerator CreateZone(Vector3Int zoneCoord)
    {
      var zoneGO = new GameObject($"Zone_{zoneCoord.x}_{zoneCoord.z}");
      zoneGO.transform.SetParent(transform);
      
      var zoneData = CreateZoneData(zoneCoord, zoneGO);
      zoneGO.transform.position = zoneData.ZoneOrigin;
      
      var zoneInstance = zoneGO.AddComponent<ZoneInstance>();
      ZoneUtil.ApplyMaterialValues(this, zoneInstance);
      
      zoneInstance.Init(zoneData);

      _activeZones[zoneCoord] = zoneInstance;
      _activeZoneData[zoneCoord] = zoneData;
      yield return null;
    }
  }
}
