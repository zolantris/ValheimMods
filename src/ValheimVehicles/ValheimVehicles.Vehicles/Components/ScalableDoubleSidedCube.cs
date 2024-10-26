using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ScalableDoubleSidedCube : MonoBehaviour
{
    public Vector3 CubeSize = new Vector3(1, 1, 1);
    private List<MeshFilter> faceMeshes;

    private void Awake()
    {
        GenerateCubeFaces();
        UpdateFacePositions();
    }

    private void Update()
    {
        UpdateFacePositions();
    }

    private void GenerateCubeFaces()
    {
        faceMeshes = new List<MeshFilter>();
        
        for (int i = 0; i < 6; i++)  // 6 faces of the cube
        {
            // Create two meshes per face for double-sided rendering
            for (int j = 0; j < 2; j++) 
            {
                var face = new GameObject($"Face_{i}_{j}", typeof(MeshFilter), typeof(MeshRenderer));
                face.transform.SetParent(transform);
                
                var meshFilter = face.GetComponent<MeshFilter>();
                var meshRenderer = face.GetComponent<MeshRenderer>();
                
                meshRenderer.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;
                
                meshFilter.mesh = CreateFaceMesh(i, j == 1);
                faceMeshes.Add(meshFilter);
            }
        }
    }

    private Mesh CreateFaceMesh(int faceIndex, bool isFlipped)
    {
        Vector3[] vertices = new Vector3[4];
        int[] triangles = isFlipped ? new int[] { 0, 2, 1, 2, 0, 3 } : new int[] { 0, 1, 2, 0, 2, 3 };
        Vector2[] uv = new Vector2[4];

        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(1, 1);
        uv[3] = new Vector2(0, 1);

        // Define vertices for each face
        switch (faceIndex)
        {
            case 0: // Front
                vertices[0] = new Vector3(-0.5f, -0.5f, 0.5f);
                vertices[1] = new Vector3(0.5f, -0.5f, 0.5f);
                vertices[2] = new Vector3(0.5f, 0.5f, 0.5f);
                vertices[3] = new Vector3(-0.5f, 0.5f, 0.5f);
                break;
            case 1: // Back
                vertices[0] = new Vector3(0.5f, -0.5f, -0.5f);
                vertices[1] = new Vector3(-0.5f, -0.5f, -0.5f);
                vertices[2] = new Vector3(-0.5f, 0.5f, -0.5f);
                vertices[3] = new Vector3(0.5f, 0.5f, -0.5f);
                break;
            case 2: // Left
                vertices[0] = new Vector3(-0.5f, -0.5f, -0.5f);
                vertices[1] = new Vector3(-0.5f, -0.5f, 0.5f);
                vertices[2] = new Vector3(-0.5f, 0.5f, 0.5f);
                vertices[3] = new Vector3(-0.5f, 0.5f, -0.5f);
                break;
            case 3: // Right
                vertices[0] = new Vector3(0.5f, -0.5f, 0.5f);
                vertices[1] = new Vector3(0.5f, -0.5f, -0.5f);
                vertices[2] = new Vector3(0.5f, 0.5f, -0.5f);
                vertices[3] = new Vector3(0.5f, 0.5f, 0.5f);
                break;
            case 4: // Top
                vertices[0] = new Vector3(-0.5f, 0.5f, 0.5f);
                vertices[1] = new Vector3(0.5f, 0.5f, 0.5f);
                vertices[2] = new Vector3(0.5f, 0.5f, -0.5f);
                vertices[3] = new Vector3(-0.5f, 0.5f, -0.5f);
                break;
            case 5: // Bottom
                vertices[0] = new Vector3(-0.5f, -0.5f, -0.5f);
                vertices[1] = new Vector3(0.5f, -0.5f, -0.5f);
                vertices[2] = new Vector3(0.5f, -0.5f, 0.5f);
                vertices[3] = new Vector3(-0.5f, -0.5f, 0.5f);
                break;
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
            uv = uv
        };
        mesh.RecalculateNormals();
        return mesh;
    }

    private void UpdateFacePositions()
    {
        Vector3 halfSize = CubeSize * 0.5f;
        
        for (int i = 0; i < 6; i++) 
        {
            Vector3 facePosition;
            Vector3 faceScale;
            Quaternion faceRotation;

            // Calculate the face position, rotation, and scale
            switch (i)
            {
                case 0: // Front
                    facePosition = new Vector3(0, 0, halfSize.z);
                    faceRotation = Quaternion.identity;
                    break;
                case 1: // Back
                    facePosition = new Vector3(0, 0, -halfSize.z);
                    faceRotation = Quaternion.Euler(0, 180, 0);
                    break;
                case 2: // Left
                    facePosition = new Vector3(-halfSize.x, 0, 0);
                    faceRotation = Quaternion.Euler(0, -90, 0);
                    break;
                case 3: // Right
                    facePosition = new Vector3(halfSize.x, 0, 0);
                    faceRotation = Quaternion.Euler(0, 90, 0);
                    break;
                case 4: // Top
                    facePosition = new Vector3(0, halfSize.y, 0);
                    faceRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case 5: // Bottom
                    facePosition = new Vector3(0, -halfSize.y, 0);
                    faceRotation = Quaternion.Euler(-90, 0, 0);
                    break;
                default:
                    facePosition = Vector3.zero;
                    faceRotation = Quaternion.identity;
                    break;
            }

            faceScale = new Vector3(CubeSize.x, CubeSize.y, 1);
            faceMeshes[i * 2].transform.localPosition = facePosition;
            faceMeshes[i * 2].transform.localRotation = faceRotation;
            faceMeshes[i * 2].transform.localScale = faceScale;

            faceMeshes[i * 2 + 1].transform.localPosition = facePosition;
            faceMeshes[i * 2 + 1].transform.localRotation = faceRotation;
            faceMeshes[i * 2 + 1].transform.localScale = faceScale;
        }
    }
}
