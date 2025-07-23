// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class SimulatedTerrainSpawner : MonoBehaviour
  {
    public int heightmapResolution = 129; // Must be power-of-two + 1
    public float terrainWorldSize = 128f;
    public float maxHeight = 60f;
    public int chunkSize = 16;

    [Header("Optional Visuals")]
    public Material terrainMaterial;
    public Material cubeMaterial;
    public Material fillerMaterial;
    private float[,] _heights;

    private Terrain _terrain;

    private void Start()
    {
      var seed = Random.Range(0, 800000); 
      
      LoggerProvider.LogDebug($"Seed {seed}");
      ZoneTerrainGenerator.SetSeed(seed);
      GenerateTerrain();
      GenerateChunks();
    }

    private void GenerateTerrain()
    {
      TerrainData terrainData = new TerrainData
      {
        heightmapResolution = heightmapResolution,
        size = new Vector3(terrainWorldSize, maxHeight, terrainWorldSize)
      };

      _heights = new float[heightmapResolution, heightmapResolution];

      for (int x = 0; x < heightmapResolution; x++)
      for (int z = 0; z < heightmapResolution; z++)
      {
        float normX = (float)x / (heightmapResolution - 1);
        float normZ = (float)z / (heightmapResolution - 1);

        Vector3 worldPos = new Vector3(
          normX * terrainWorldSize + transform.position.x,
          0f,
          normZ * terrainWorldSize + transform.position.z
        );

        float worldHeight = ZoneTerrainGenerator.SampleHeight(worldPos);
        _heights[z, x] = Mathf.Clamp01(worldHeight / maxHeight);
      }

      terrainData.SetHeights(0, 0, _heights);
      _terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
      _terrain.transform.position = transform.position;

      ApplyDebugTexture(_terrain, _heights);
    }

    private void ApplyDebugTexture(Terrain terrain, float[,] heights)
    {
      Texture2D tex = new Texture2D(heightmapResolution, heightmapResolution);
      for (int x = 0; x < heightmapResolution; x++)
      for (int z = 0; z < heightmapResolution; z++)
      {
        float h = heights[z, x];
        tex.SetPixel(x, z, new Color(h, h, h)); // grayscale based on height
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

    private void GenerateChunks()
    {
      int chunksX = Mathf.CeilToInt(terrainWorldSize / chunkSize);
      int chunksZ = Mathf.CeilToInt(terrainWorldSize / chunkSize);

      for (int x = 0; x < chunksX; x++)
      for (int z = 0; z < chunksZ; z++)
      {
        Vector3 chunkPos = new Vector3(
          transform.position.x + x * chunkSize,
          transform.position.y,
          transform.position.z + z * chunkSize
        );

        var chunkGO = new GameObject($"Chunk_{x}_{z}");
        chunkGO.transform.parent = transform;
        chunkGO.transform.position = chunkPos;

        var meshFilter = chunkGO.AddComponent<MeshFilter>();
        var meshRenderer = chunkGO.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = cubeMaterial;

        var chunk = chunkGO.AddComponent<VoxelChunkGenerator>();
        chunk.Generate(chunkPos, chunkSize, maxHeight); // ✅ UPDATED
      }
    }
  }
}
