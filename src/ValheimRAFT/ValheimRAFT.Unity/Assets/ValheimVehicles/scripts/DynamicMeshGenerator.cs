using System.Collections.Generic;
using UnityEngine;

public class DynamicMeshGenerator : MonoBehaviour
{
    // Maximum number of vertices allowed
    private const int MaxVertices = 100;

    // Reference to the MeshFilter component
    private MeshFilter meshFilter;
    
    // List to store the vertices
    private List<Vector3> vertices = new List<Vector3>();

    void Start()
    {
        // Get the MeshFilter component or create one if not present
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        // Initialize the mesh
        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;
    }

    void Update()
    {
        // Example: Generate a new vertex when user clicks left mouse button
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 10f; // Distance from camera to the mesh (adjust as needed)

            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            AddVertex(worldPosition);
        }

        // Example: Rebuild mesh when vertices change
        UpdateMesh();
    }

    // Method to add a new vertex
    private void AddVertex(Vector3 vertex)
    {
        if (vertices.Count < MaxVertices)
        {
            vertices.Add(vertex);
        }
    }

    // Method to update the mesh
    private void UpdateMesh()
    {
        Mesh mesh = meshFilter.mesh;
        mesh.Clear();

        // Assign vertices
        mesh.vertices = vertices.ToArray();

        // Automatically calculate normals and other required mesh data
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        // Generate triangles to form a proper mesh structure
        List<int> triangles = TriangulateVertices(vertices);

        // Assign triangles to the mesh
        mesh.triangles = triangles.ToArray();
    }

    // Method to triangulate vertices
    private List<int> TriangulateVertices(List<Vector3> vertices)
    {
        List<int> triangles = new List<int>();

        // Triangulate using a simple algorithm (ear clipping method)
        if (vertices.Count >= 3)
        {
            // Create a list of indices for the vertices
            List<int> indices = new List<int>();
            for (int i = 0; i < vertices.Count; i++)
            {
                indices.Add(i);
            }

            int index = 0;
            while (indices.Count >= 3)
            {
                // Find an ear (triangle) to cut off
                int i0 = indices[index];
                int i1 = indices[(index + 1) % indices.Count];
                int i2 = indices[(index + 2) % indices.Count];

                Vector3 v0 = vertices[i0];
                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];

                // Check if this triangle is an ear
                bool isEar = IsEar(v0, v1, v2, vertices);

                if (isEar)
                {
                    // Add triangle indices
                    triangles.Add(i0);
                    triangles.Add(i1);
                    triangles.Add(i2);

                    // Remove the middle vertex from the indices list
                    indices.RemoveAt((index + 1) % indices.Count);
                }
                else
                {
                    // Move to the next vertex triplet
                    index = (index + 1) % indices.Count;
                }
            }
        }

        return triangles;
    }

    // Helper method to check if a triangle formed by three vertices is an ear
    private bool IsEar(Vector3 v0, Vector3 v1, Vector3 v2, List<Vector3> vertices)
    {
        // Check if no other vertex is inside the triangle
        for (int i = 0; i < vertices.Count; i++)
        {
            if (vertices[i] != v0 && vertices[i] != v1 && vertices[i] != v2)
            {
                if (IsPointInTriangle(vertices[i], v0, v1, v2))
                {
                    return false;
                }
            }
        }

        // Otherwise, it's an ear
        return true;
    }

    // Helper method to check if a point is inside a triangle
    private bool IsPointInTriangle(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 e0 = v1 - v0;
        Vector3 e1 = v2 - v1;
        Vector3 e2 = v0 - v2;

        Vector3 vp0 = point - v0;
        Vector3 vp1 = point - v1;
        Vector3 vp2 = point - v2;

        bool isInside0 = Vector3.Cross(e0, vp0).y >= 0;
        bool isInside1 = Vector3.Cross(e1, vp1).y >= 0;
        bool isInside2 = Vector3.Cross(e2, vp2).y >= 0;

        return isInside0 && isInside1 && isInside2;
    }
}
