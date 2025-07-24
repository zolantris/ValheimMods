// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using Random = UnityEngine.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{
  [Serializable]
  public class ZoneTerrainConfig
  {
    public int heightmapResolution = 129;
    public float terrainWorldSize = 64f;
    public float maxHeight = 60f;
    public int chunkSize = 16;
    public int worldSeed = 12345;
    public bool generateUnityTerrain = true;
    public bool generateVoxelChunks = true;
    public bool generateSurfaceMesh = true;
    public bool canRegenerateOnChange = true;

    public void ApplyToSpawner(ZoneTerrainInstanceController spawner)
    {
      spawner.heightmapResolution = heightmapResolution;
      spawner.terrainWorldSize = terrainWorldSize;
      spawner.maxHeight = maxHeight;
      spawner.chunkSize = chunkSize;
      spawner.worldSeed = worldSeed;
      spawner.generateUnityTerrain = generateUnityTerrain;
      spawner.generateVoxelChunks = generateVoxelChunks;
      spawner.generateSurfaceMesh = generateSurfaceMesh;
      spawner.canRegenerateOnChange = canRegenerateOnChange;
    }

    public ZoneTerrainConfig Clone()
    {
      var cloneObj= new ZoneTerrainConfig
      {
        heightmapResolution = heightmapResolution,
        terrainWorldSize = terrainWorldSize,
        maxHeight = maxHeight,
        chunkSize = chunkSize,
        worldSeed = worldSeed,
        generateUnityTerrain = generateUnityTerrain,
        generateVoxelChunks = generateVoxelChunks,
        generateSurfaceMesh = generateSurfaceMesh,
        canRegenerateOnChange = canRegenerateOnChange
      };
      return cloneObj;
    }

    public void UpdateSeed(int low, int high)
    {
      worldSeed = Random.Range(low, high);
    }

    public static ZoneTerrainConfig CreateDefault()
    {
      return new ZoneTerrainConfig();
    }
  }
}