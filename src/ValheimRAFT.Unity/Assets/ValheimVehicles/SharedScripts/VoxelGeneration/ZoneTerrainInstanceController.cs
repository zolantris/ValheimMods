// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using UnityEngine;
using ValheimVehicles.SharedScripts.ValheimVehicles.SharedScripts;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class ZoneTerrainInstanceController : MonoBehaviour
  {
    [SerializeField] public int heightmapResolution = 129;
    [SerializeField] public float terrainWorldSize = 64f;
    [SerializeField] public float maxHeight = 60f;
    [SerializeField] public int chunkSize = 16;
    [SerializeField] public int worldSeed;
    [SerializeField] public bool generateUnityTerrain = true;
    [SerializeField] public bool generateVoxelChunks = true;
    [SerializeField] public bool generateSurfaceMesh = true;
    [SerializeField] public bool canRegenerateOnChange;

    [Header("Optional Visuals")]
    public Material terrainMaterial;
    public Material cubeMaterial;
    public Material fillerMaterial;

    [Header("integration values")]
    [SerializeField] public bool isRuntimeInjected;
    [SerializeField] private ZoneTerrainConfig _config;
    [SerializeField] private ZoneInstance _zoneInstance;
    public Vector3 _zoneOrigin;

    private bool _hasInit;
    private float[,] _heights;
    private string _lastConfigHash;
    private Terrain _terrain;

    private CoroutineHandle _terrainGeneratorRoutine;
    private GameObject _terrainGO;
    private ZoneData _zoneData;

    public void Awake()
    {
      _terrainGeneratorRoutine ??= new CoroutineHandle(this);
    }

    private void FixedUpdate()
    {
      if (canRegenerateOnChange && HasConfigChanged())
      {
        RebuildEverything();
      }
    }

    public void OnEnable()
    {
      _terrainGeneratorRoutine ??= new CoroutineHandle(this);
    }

    public void OnDisable()
    {
      StopAllCoroutines();
    }

    public static ZoneTerrainInstanceController Init(GameObject obj, ZoneData zoneData)
    {
      var ctrl = obj.AddComponent<ZoneTerrainInstanceController>();
      ctrl.ApplyConfig(zoneData.TerrainConfig);
      ctrl._zoneData = zoneData;

      ctrl._hasInit = true;
      return ctrl;
    }

    public void ApplyConfig(ZoneTerrainConfig config)
    {
      _config = config.Clone();
      config.ApplyToSpawner(this);
    }

    [ContextMenu("Update Seed")]
    private void UpdateSeed(int low, int high)
    {
      _config.UpdateSeed(low,high);
      worldSeed = _config.worldSeed;
    }

    [ContextMenu("Force Rebuild")]
    public void ForceRebuild()
    {
      RebuildEverything();
    }

    public bool HasConfigChanged()
    {
      return _lastConfigHash != ComputeConfigHash();
    }

    public IEnumerator RebuildEverythingRoutine()
    {
      if (isRuntimeInjected)
      {
        TryAttachRuntimeReader();
        yield break;
      }
      
      if (generateUnityTerrain)
      {
        yield return GenerateTerrain(output =>
        {
          _terrainGO = output;
        });
      }
      
      _lastConfigHash = ComputeConfigHash(); // ✅ set hash after full build
      yield return null;
    }

    public void RebuildEverything()
    {
      if (_terrainGeneratorRoutine.IsRunning) return;
      ClearGeneratedObjects();
      _terrainGeneratorRoutine.Start(RebuildEverythingRoutine());
    }

    private void TryAttachRuntimeReader()
    {
      var existingTerrain = GetComponentInChildren<Terrain>();
      if (!existingTerrain) return;
  
      var runtimeIntegrator = gameObject.AddComponent<TerrainRuntimeVoxelIntegrator>();
      runtimeIntegrator.Initialize(existingTerrain, this);
    }

    private void ClearGeneratedObjects()
    {
      if (_terrainGO) Destroy(_terrainGO);
    }

    private string ComputeConfigHash()
    {
      unchecked
      {
        var data = $"{heightmapResolution}-{terrainWorldSize}-{maxHeight}-{chunkSize}-{worldSeed}";
        if (terrainMaterial) data += terrainMaterial.name;
        if (cubeMaterial) data += cubeMaterial.name;
        if (fillerMaterial) data += fillerMaterial.name;

        return data.GetHashCode().ToString();
      }
    }

    private IEnumerator GenerateTerrain(Action<GameObject> onComplete)
    {
      TerrainData terrainData = new TerrainData
      {
        heightmapResolution = heightmapResolution,
        size = new Vector3(terrainWorldSize, maxHeight, terrainWorldSize)
      };

      _heights = new float[heightmapResolution, heightmapResolution];
      
      var nextFramePause = Time.time + 10f;

      for (int x = 0; x < heightmapResolution; x++)
      for (int z = 0; z < heightmapResolution; z++)
      {
        if (Time.time > nextFramePause)
        {
          yield return null;
          nextFramePause = Time.time + 10f;
        }
          
        float normX = (float)x / (heightmapResolution - 1);
        float normZ = (float)z / (heightmapResolution - 1);

        Vector3 worldPos = new Vector3(
          normX * terrainWorldSize,
          0f,
          normZ * terrainWorldSize
        ) + _zoneData.ZoneOrigin;

        float worldHeight = TerrainHeightSampler.SampleHeight(worldPos, worldSeed);
        _heights[z, x] = worldHeight; // ✅ Already normalized 0–1
      }

      terrainData.SetHeights(0, 0, _heights);

      // todo might not need this (But for testing it's worth having.
      _terrain = null;
      
      _terrain = GetComponent<Terrain>();
      if (!_terrain)
      {
        var terrainGO = Terrain.CreateTerrainGameObject(terrainData);
        _terrain = terrainGO.GetComponent<Terrain>();
        _terrain.transform.position = transform.position;
      }

      if (!_terrain)
      {
        yield break;
      }

      _terrain.gameObject.name = "Terrain";

      ApplyDebugTexture(_terrain, _heights);
      
      onComplete.Invoke(_terrain.gameObject);
    }

    private void ApplyDebugTexture(Terrain terrain, float[,] heights)
    {
      Texture2D tex = new Texture2D(heightmapResolution, heightmapResolution);
      for (int x = 0; x < heightmapResolution; x++)
      for (int z = 0; z < heightmapResolution; z++)
      {
        float h = heights[z, x];
        tex.SetPixel(x, z, new Color(h, h, h));
      }

      tex.Apply();

      var terrainLayer = new TerrainLayer
      {
        diffuseTexture = tex,
        tileSize = new Vector2(terrainWorldSize, terrainWorldSize),
        tileOffset = Vector2.zero
      };

      terrain.terrainData.terrainLayers = new[] { terrainLayer };
    }
  }
}
