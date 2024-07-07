using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dynamically generates meshes and uses the Delaunay method to efficiently elminate bad triangles
/// </summary>
public class DynamicMeshGenerator : MonoBehaviour
{
  public int numberOfVertices = 10000;
  public Vector3 bounds = new Vector3(200, 50, 200);
  public Material meshMaterial;

  void Start()
  {
    // Generate random vertices
    // List<Vector3> vertices = GenerateRandomVertices(numberOfVertices, bounds);
    //
    // // Perform Delaunay triangulation
    // List<Triangle> triangles = PerformDelaunayTriangulation(vertices);
    //
    // // Create Unity mesh object
    // Mesh delaunayMesh = GenerateUnityMesh(vertices, triangles);
    // GameObject meshObject =
    //   new GameObject("DelaunayMesh", typeof(MeshFilter), typeof(MeshRenderer));
    // meshObject.transform.SetParent(transform);
    // meshObject.GetComponent<MeshFilter>().mesh = delaunayMesh;
    // meshObject.GetComponent<MeshRenderer>().material = meshMaterial;
  }
}