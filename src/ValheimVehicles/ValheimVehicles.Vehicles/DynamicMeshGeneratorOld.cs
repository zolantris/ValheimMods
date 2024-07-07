using System.Collections.Generic;
using UnityEngine;

public class DynamicMeshGeneratorOld : MonoBehaviour
{
  // Maximum number of vertices allowed
  private const int MaxVertices = 10000;

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
    // int[] triangles = TriangulateVertices(vertices);

    // Assign triangles to the mesh
    // mesh.triangles = triangles;
  }

  // Method to triangulate vertices using Unity's Triangulator class
  // private int[] TriangulateVertices(List<Vector3> vertices)
  // {
  //   // Convert 3D vertices to 2D (ignoring Y axis)
  //   List<Vector2> vertices2D = new List<Vector2>();
  //   for (int i = 0; i < vertices.Count; i++)
  //   {
  //     vertices2D.Add(new Vector2(vertices[i].x, vertices[i].z));
  //   }
  //
  //   // Triangulate using Unity's Triangulator class
  //   Triangulator tr = new Triangulator(vertices2D.ToArray());
  //   int[] indices = tr.Triangulate();
  //
  //   // Adjust indices for 3D mesh vertices
  //   List<int> meshTriangles = new List<int>();
  //   for (int i = 0; i < indices.Length; i++)
  //   {
  //     meshTriangles.Add(indices[i]);
  //   }
  //
  //   return meshTriangles.ToArray();
  // }
}