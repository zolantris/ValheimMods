// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class TerrainRuntimeVoxelIntegrator : MonoBehaviour
  {
    private ZoneTerrainInstanceController _spawner;
    private Terrain _terrain;

    public void Initialize(Terrain terrain, ZoneTerrainInstanceController spawner)
    {
      _terrain = terrain;
      _spawner = spawner;

      GenerateVoxelsFromTerrain();
    }

    private void GenerateVoxelsFromTerrain()
    {
      var data = _terrain.terrainData;
      int res = data.heightmapResolution;
      var heights = data.GetHeights(0, 0, res, res);

      int chunkSize = _spawner.chunkSize;
      int chunksX = Mathf.CeilToInt(data.size.x / chunkSize);
      int chunksZ = Mathf.CeilToInt(data.size.z / chunkSize);

      for (int x = 0; x < chunksX; x++)
      for (int z = 0; z < chunksZ; z++)
      {
        Vector3 chunkPos = new Vector3(
          _terrain.transform.position.x + x * chunkSize,
          _terrain.transform.position.y,
          _terrain.transform.position.z + z * chunkSize
        );

        var chunkGO = new GameObject($"RuntimeChunk_{x}_{z}");
        chunkGO.transform.parent = _terrain.transform;
        chunkGO.transform.position = chunkPos;

        var meshFilter = chunkGO.AddComponent<MeshFilter>();
        var meshRenderer = chunkGO.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = _spawner.cubeMaterial;

        var chunk = chunkGO.AddComponent<VoxelChunkGenerator>();
        chunk.GenerateFromHeights(heights, chunkPos, chunkSize, _spawner.maxHeight);
      }
    }
  }
}