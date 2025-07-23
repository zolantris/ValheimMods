// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

        public void GenerateFromHeights(float[,] heights, Vector3 chunkOrigin, int chunkSize, float maxHeight)
        {
            // Read heights and generate voxel data (or mesh) accordingly.
            // Only consider heights that fall within chunk bounds.
        }

        public void Generate(Vector3 chunkWorldOrigin, int chunkSize, float maxHeight, Func<Vector3, float> heightSampler)
        {
            int resolution = chunkSize; // 1:1 voxel per unit, adjust if needed
            float voxelSize = 1f;

            _vertices.Clear();
            _triangles.Clear();

            int index = 0;

            for (int x = 0; x < resolution; x++)
            for (int z = 0; z < resolution; z++)
            {
                Vector3 worldXZ = chunkWorldOrigin + new Vector3(x * voxelSize, 0f, z * voxelSize);
                float height = heightSampler(worldXZ) * maxHeight; // <- Critical!
                int voxelCount = Mathf.Max(1, Mathf.CeilToInt(height / voxelSize));

                for (int y = 0; y < voxelCount; y++)
                {
                    Vector3 cubePos = new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
                    AddCube(cubePos, ref index);
                }
            }

            var mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_vertices);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateNormals();

            GetComponent<MeshFilter>().mesh = mesh;
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
