using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Prefabs;

namespace ValheimVehicles.Plugins;

public class ConvexHullMeshGenerator : MonoBehaviour
{
  public List<Vector3> points; // Input points for convex hull calculation
  private GameObject previewObject;

  private List<Vector3> testPoints =
  [
    new Vector3(1.969051f, 0.03499834f, 4.98811f),
    new Vector3(4.969148f, 12.019f, -9.012998f),
    new Vector3(6.969134f, 12.01901f, -9.013051f),
    new Vector3(4.969227f, 12.019f, 0.9873559f),
    new Vector3(6.969123f, 12.019f, -1.013096f),
    new Vector3(6.969193f, 12.019f, 0.987216f),
    new Vector3(4.9692f, 12.01901f, -7.012808f),
    new Vector3(4.969123f, 12.019f, -1.013102f),
    new Vector3(8.9692f, 12.019f, 0.9871707f),
    new Vector3(8.969112f, 12.01899f, -9.013078f),
    new Vector3(6.969166f, 12.019f, 2.987098f),
    new Vector3(4.969077f, 12.01901f, 2.986752f),
    new Vector3(8.9691f, 12.01898f, -7.013141f),
    new Vector3(6.969106f, 12.01901f, -7.013146f),
    new Vector3(4.969126f, 12.019f, -3.013086f),
    new Vector3(4.969161f, 12.01901f, -5.012967f),
    new Vector3(8.969239f, 12.019f, 2.987372f),
    new Vector3(6.969143f, 12.019f, -3.012992f),
    new Vector3(6.969165f, 12.019f, -5.012909f),
    new Vector3(8.969099f, 12.01899f, -3.01317f),
    new Vector3(8.969064f, 12.01898f, -5.013295f),
    new Vector3(8.969152f, 12.019f, -1.01297f),
    new Vector3(5.969101f, 0.03500491f, -3.011944f),
    new Vector3(9.753064f, 1.019002f, -7.011898f),
    new Vector3(9.753211f, 5.018999f, 0.9877796f),
    new Vector3(-2.030958f, 0.03499899f, 12.98798f),
    new Vector3(1.969054f, 0.01599588f, 12.98811f),
    new Vector3(1.969146f, 0.05399674f, -3.012f),
    new Vector3(5.969078f, 0.05400102f, 12.9881f),
    new Vector3(5.969075f, 0.05400009f, 4.988083f),
    new Vector3(0f, 0f, 0f),
    new Vector3(-1.150025f, 0.1638423f, 3.718254f),
    new Vector3(9.753044f, 1.000003f, -11.01181f),
    new Vector3(9.753011f, 1.018997f, 0.9882441f),
    new Vector3(9.753143f, 1.018996f, -3.012075f),
    new Vector3(-2.030858f, 0.05399552f, -3.011994f),
    new Vector3(9.753297f, 5.018997f, -3.01238f),
    new Vector3(0.3230967f, 0.9961958f, -2.063026f),
  ];

  // Convex hull calculator instance
  private ConvexHullCalculator
    convexHullCalculator = new ConvexHullCalculator();

  void GenerateConvexHullMesh()
  {
    if (points == null || points.Count < 3)
    {
      Debug.LogError("Not enough points to generate a convex hull.");
      return;
    }

    // Step 1: Prepare output containers
    var verts = new List<Vector3>();
    var tris = new List<int>();
    var normals = new List<Vector3>();

    // Step 2: Generate convex hull and export the mesh
    convexHullCalculator.GenerateHull(points, true, ref verts, ref tris,
      ref normals);

    // Step 3: Create a Unity Mesh
    var mesh = new Mesh
    {
      vertices = verts.ToArray(),
      triangles = tris.ToArray(),
      normals = normals.ToArray()
    };

    // Step 4: Remove previous preview instance
    if (previewObject != null)
    {
      Destroy(previewObject);
    }

    // Step 5: Create a new GameObject to display the mesh
    previewObject = new GameObject("ConvexHullPreview");
    var meshFilter = previewObject.AddComponent<MeshFilter>();
    var meshRenderer = previewObject.AddComponent<MeshRenderer>();

    meshFilter.mesh = mesh;

    // Step 6: Create and assign the material
    var material = new Material(LoadValheimAssets.CustomPieceShader)
    {
      color = Color.green
    };
    meshRenderer.material = material;

    // Optional: Adjust transform
    previewObject.transform.position = Vector3.zero;
    previewObject.transform.rotation = Quaternion.identity;
  }

  // Test the method with a sample list of points
  [ContextMenu("Generate Mesh")]
  private void TestGenerateMesh()
  {
    GenerateConvexHullMesh();
  }
}