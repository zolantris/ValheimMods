// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.SharedScripts.ValheimVehicles.SharedScripts;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class ZoneInstance : MonoBehaviour
  {
    [Header("Optional Visuals")]
    public Material terrainMaterial;
    public Material cubeMaterial;
    public Material fillerMaterial;
    private readonly List<GameObject> _chunkGOs = new();
    // routines
    private CoroutineHandle _buildZoneRoutine;
    private GameObject _surfaceGO;
    private ZoneData _zoneData;
    private ZoneTerrainInstanceController terrainInstanceController;

    private ZoneTerrainConfig _terrainConfig => _zoneData.TerrainConfig;

    public void Awake()
    {
      _buildZoneRoutine ??= new CoroutineHandle(this);
    }

    public void Init(ZoneData zoneData)
    {
      _buildZoneRoutine ??= new CoroutineHandle(this);
      
      _zoneData = zoneData;
      _zoneData.ZoneGO = gameObject;

      // Clone config and store in ZoneData
      _zoneData.TerrainConfig = _zoneData.TerrainConfig.Clone();

      terrainInstanceController = ZoneTerrainInstanceController.Init(gameObject, _zoneData);
      ZoneUtil.ApplyMaterialValues(this, terrainInstanceController);
      terrainInstanceController.RebuildEverything();

      BuildZone();
    }

    public void BuildZone()
    {
      if (_buildZoneRoutine.IsRunning) return;
      _buildZoneRoutine.Start(BuildZoneRoutine());
    }

    public IEnumerator BuildZoneRoutine()
    {
      yield return terrainInstanceController.RebuildEverythingRoutine();
      
      if (_terrainConfig.generateSurfaceMesh)
      {
        yield return GenerateSurfaceMeshRoutine(obj =>
        {
          _surfaceGO = obj;
        });
      }

      // Attach additional controllers if needed
      if (_terrainConfig.generateVoxelChunks)
      {
        yield return GenerateChunksRoutine(chunks =>
        {
          _chunkGOs.ForEach(Destroy);
          _chunkGOs.Clear();
          _chunkGOs.AddRange(chunks);
        });
      }      
    }

    private IEnumerator GenerateSurfaceMeshRoutine(Action<GameObject> onComplete)
    {
      var surfaceGO = new GameObject("SurfaceMesh")
      {
        transform =
        {
          parent = transform,
          position = transform.position
        }
      };

      var generator = surfaceGO.AddComponent<TerrainSurfaceMeshGenerator>();
      generator.resolution = _terrainConfig.heightmapResolution;
      generator.size = _terrainConfig.terrainWorldSize;
      generator.maxHeight = _terrainConfig.maxHeight;
      generator.material = terrainMaterial;
      
      onComplete.Invoke(surfaceGO);
      
      yield return null;
    }

    private IEnumerator GenerateChunksRoutine(Action<List<GameObject>> onComplete)
    {
      int chunkSize = _terrainConfig.chunkSize;
      var terrainWorldSize = _terrainConfig.terrainWorldSize;
      int chunksX = Mathf.CeilToInt(terrainWorldSize / chunkSize);
      int chunksZ = Mathf.CeilToInt(terrainWorldSize / chunkSize);
      
      var nextFramePause = Time.time + 10f;
      var maxInitPerFrame = 10;
      var currentInit = 0;
      var chunkList = new List<GameObject>();

      for (int x = 0; x < chunksX; x++)
      for (int z = 0; z < chunksZ; z++)
      {
        currentInit++;
        if (currentInit > maxInitPerFrame)
        {
          yield return null;
          currentInit = 0;
        }
        if (Time.time > nextFramePause)
        { 
          yield return null;
          nextFramePause = Time.time + 10f;
        }

        Vector3 chunkPos = _zoneData.ZoneOrigin + new Vector3(x * chunkSize, 0f, z * chunkSize);
        
        var chunkGO = new GameObject($"Chunk_{x}_{z}");
        chunkGO.transform.SetParent(transform);
        chunkGO.transform.position = chunkPos;

        var meshRenderer = chunkGO.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = cubeMaterial;
        
        var filter = chunkGO.AddComponent<MeshFilter>();

        var chunk = chunkGO.AddComponent<VoxelChunkGenerator>();
        chunk.Generate(chunkPos, chunkSize, _terrainConfig.maxHeight, pos => TerrainHeightSampler.SampleHeight(pos, _terrainConfig.worldSeed));
        chunkList.Add(chunkGO);
      }
      onComplete.Invoke(chunkList);
    }
  }
}