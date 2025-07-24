// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;
using ValheimVehicles.SharedScripts.ValheimVehicles.SharedScripts;

#endregion

namespace ValheimVehicles.SharedScripts
{
  [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
  public class TerrainSurfaceMeshGenerator : MonoBehaviour
  {
    public int resolution = 32; // grid divisions per side
    public float size = 128f; // world size
    public float maxHeight = 60f;
    public Material material;

    private void Start()
    {
      Generate();
    }

    public void Generate()
    {
      int vertCount = (resolution + 1);
      Vector3[] vertices = new Vector3[vertCount * vertCount];
      Vector2[] uvs = new Vector2[vertices.Length];
      int[] triangles = new int[resolution * resolution * 6];

      float step = size / resolution;
      int index = 0;

      for (int z = 0; z <= resolution; z++)
      {
        for (int x = 0; x <= resolution; x++)
        {
          float worldX = transform.position.x + x * step;
          float worldZ = transform.position.z + z * step;

          float height = TerrainHeightSampler.SampleHeight(new Vector3(worldX, 0f, worldZ));
          height = Mathf.Clamp(height, 0f, maxHeight);

          vertices[index] = new Vector3(x * step, height, z * step);
          uvs[index] = new Vector2((float)x / resolution, (float)z / resolution);
          index++;
        }
      }

      int triIndex = 0;
      for (int z = 0; z < resolution; z++)
      {
        for (int x = 0; x < resolution; x++)
        {
          int topLeft = z * (resolution + 1) + x;
          int topRight = topLeft + 1;
          int bottomLeft = topLeft + (resolution + 1);
          int bottomRight = bottomLeft + 1;

          // Triangle 1
          triangles[triIndex++] = topLeft;
          triangles[triIndex++] = bottomLeft;
          triangles[triIndex++] = topRight;

          // Triangle 2
          triangles[triIndex++] = topRight;
          triangles[triIndex++] = bottomLeft;
          triangles[triIndex++] = bottomRight;
        }
      }

      Mesh mesh = new Mesh
      {
        vertices = vertices,
        triangles = triangles,
        uv = uvs
      };

      mesh.RecalculateNormals();
      mesh.RecalculateBounds();

      var filter = GetComponent<MeshFilter>();
      var renderer = GetComponent<MeshRenderer>();

      filter.sharedMesh = mesh;
      renderer.sharedMaterial = material;
    }
  }
}
