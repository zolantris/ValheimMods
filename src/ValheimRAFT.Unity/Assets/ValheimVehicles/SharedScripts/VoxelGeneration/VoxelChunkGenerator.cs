// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VoxelChunkGenerator : MonoBehaviour
    {
        [SerializeField] private float fillerYOffset = 0.01f;
        private readonly List<int> _fillerTriangles = new();

        private readonly List<Vector3> _fillerVertices = new();
        private readonly List<int> _triangles = new();

        private readonly List<Vector3> _vertices = new();
        private Mesh _cubeMeshInstance;

        private Mesh _mesh;
        private MeshFilter _meshFilter;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _mesh = new Mesh { name = "VoxelChunkMesh" };
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cubeMeshInstance = primitive.GetComponent<MeshFilter>().sharedMesh;
            Destroy(primitive);
        }

        public void Generate(Vector3 worldOrigin, int chunkSize, float maxTerrainHeight)
        {
            _vertices.Clear();
            _triangles.Clear();
            _fillerVertices.Clear();
            _fillerTriangles.Clear();

            int index = 0;

            for (int x = 0; x < chunkSize; x++)
            for (int z = 0; z < chunkSize; z++)
            {
                Vector3 worldXZ = worldOrigin + new Vector3(x + 0.5f, 0, z + 0.5f);
                float terrainHeight = ZoneTerrainGenerator.SampleHeight(worldXZ);

                // Convert world Y height to local Y height
                float localHeight = terrainHeight - worldOrigin.y;
                int maxY = Mathf.FloorToInt(localHeight);
                maxY = Mathf.Clamp(maxY, 0, chunkSize - 1);

                for (int y = 0; y <= maxY; y++)
                {
                    Vector3 localVoxelPos = new Vector3(x, y, z);
                    AddCube(localVoxelPos, ref index);

                    // Add top filler quad slightly above each cube (for debug visualization)
                    AddFillerQuad(new Vector3(x + 0.5f, y + 1f + fillerYOffset, z + 0.5f));
                }
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateNormals();
            _meshFilter.sharedMesh = _mesh;

            CreateFillerMesh();
        }

        private void AddCube(Vector3 offset, ref int index)
        {
            var verts = new List<Vector3>();
            _cubeMeshInstance.GetVertices(verts);
            var triangles = _cubeMeshInstance.triangles;

            for (int i = 0; i < verts.Count; i++)
                _vertices.Add(offset + verts[i]);

            for (int i = 0; i < triangles.Length; i++)
                _triangles.Add(index + triangles[i]);

            index += verts.Count;
        }

        private void AddFillerQuad(Vector3 center)
        {
            int baseIndex = _fillerVertices.Count;

            _fillerVertices.Add(center + new Vector3(-0.5f, 0f, -0.5f));
            _fillerVertices.Add(center + new Vector3(-0.5f, 0f, 0.5f));
            _fillerVertices.Add(center + new Vector3(0.5f, 0f, 0.5f));
            _fillerVertices.Add(center + new Vector3(0.5f, 0f, -0.5f));

            _fillerTriangles.Add(baseIndex + 0);
            _fillerTriangles.Add(baseIndex + 1);
            _fillerTriangles.Add(baseIndex + 2);
            _fillerTriangles.Add(baseIndex + 0);
            _fillerTriangles.Add(baseIndex + 2);
            _fillerTriangles.Add(baseIndex + 3);
        }

        private void CreateFillerMesh()
        {
            var fillerGO = new GameObject("VoxelFillers", typeof(MeshFilter), typeof(MeshRenderer));
            fillerGO.transform.SetParent(transform);
            fillerGO.transform.localPosition = Vector3.zero;

            var fillerMesh = new Mesh { name = "VoxelFillerMesh" };
            fillerMesh.SetVertices(_fillerVertices);
            fillerMesh.SetTriangles(_fillerTriangles, 0);
            fillerMesh.RecalculateNormals();

            var meshFilter = fillerGO.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = fillerMesh;

            var meshRenderer = fillerGO.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
        }
    }
}
