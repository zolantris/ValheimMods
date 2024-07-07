using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using ValheimVehicles.Prefabs;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Components;

public class VehicleMeshMaskManager : MonoBehaviour
{
  private ZNetView m_nview;
  private VehicleShip vehicleShip;

  private List<Vector3> meshCoordinates = [new Vector3(0, 0, 0), new Vector3(0, 20, 0)];

  private GameObject? currentPanel;
  private List<GameObject> buttonItems = [];
  private GameObject scrollViewObj;
  private GameObject? prevDemoMesh;

  public int numberOfVertices = 10000;
  public Vector3 bounds = new Vector3(200, 50, 200);
  public Material meshMaterial;

  private void Awake()
  {
    m_nview = GetComponent<ZNetView>();
  }

  public void Init(VehicleShip vehicle)
  {
    vehicleShip = vehicle;
  }

  public void AddPoint(Vector3 point)
  {
    meshCoordinates.Add(point);

    if (buttonItems.Count > 0)
    {
      foreach (var item in buttonItems.ToList())
      {
        buttonItems.Remove(item);
        if (item != null)
        {
          Destroy(item);
        }
      }
    }

    AddButtonsForMeshPoints();
  }

  public void OnTogglePanel(bool state)
  {
    if (currentPanel)
    {
      Destroy(currentPanel);
      currentPanel = null;
      return;
    }

    var guiMan = Jotunn.Managers.GUIManager.Instance;
    currentPanel =
      guiMan.CreateWoodpanel(Jotunn.Managers.GUIManager.CustomGUIFront.transform, Vector2.zero,
        new Vector2(0, 0), new Vector2(0, 50), 600, 750);
    scrollViewObj =
      Jotunn.Managers.GUIManager.Instance.CreateScrollView(currentPanel.transform, false, true, 50,
        5, ColorBlock.defaultColorBlock, Color.gray, 500, 500);

    var guiButtonGo = guiMan.CreateButton($"Close", currentPanel.transform, Vector2.zero,
      Vector2.zero, new Vector2(50, 50));
    var guiButton = guiButtonGo.GetComponent<Button>();

    guiButton.onClick.AddListener(() =>
    {
      Logger.LogMessage("ClickedButton");
      Destroy(currentPanel);
      if (prevDemoMesh)
      {
        Destroy(prevDemoMesh);
        prevDemoMesh = null;
      }
      // scrollView.SetActive(!scrollView.activeInHierarchy);
    });

    var guiPreviewButtonGo = guiMan.CreateButton($"Close", currentPanel.transform, Vector2.zero,
      Vector2.zero, new Vector2(50, 50));
    var onPreviewButton = guiPreviewButtonGo.GetComponent<Button>();

    onPreviewButton.onClick.AddListener(() =>
    {
      Logger.LogMessage("ClickedButton");
      OnPreview();
    });

    AddButtonsForMeshPoints();
  }

  public void CreateWaterMaskMesh()
  {
    if (prevDemoMesh)
    {
      Destroy(prevDemoMesh);
      prevDemoMesh = null;
    }

    var go = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));

    go.transform.position = transform.position;
    go.transform.rotation = transform.rotation;
    go.transform.SetParent(transform);

    var meshRenderer = go.GetComponent<MeshRenderer>();
    var meshFilter = go.GetComponent<MeshFilter>();

    var wmMaterial = LoadValheimAssets.waterMask.GetComponent<MeshRenderer>().material;
    var wm_material = LoadValheimAssets.waterMask.GetComponent<Material>();

    var generatedMesh = new Mesh();

    var vertices = new List<Vector3>();
    var uvs = new List<Vector2>();

    // 3 triangles per polygon...so a square of 2 triangles requires 2*3 = 6 triangles
    // triangles must be clockwise in order for them to display on camera...
    // https://www.youtube.com/watch?v=gmuHI_wsOgI
    var triangles = new List<int>();

    meshRenderer.material = wm_material;
    meshFilter.mesh = generatedMesh;

    var wmShader = wmMaterial?.shader;
    meshRenderer.material = wmMaterial;

    var mesh = GenerateMeshesFromPoints(meshCoordinates);
    mesh.SetVertices(vertices);
    mesh.SetTriangles(triangles, 0);
    mesh.SetUVs(0, uvs);
    mesh.Optimize();
    mesh.RecalculateNormals();

    meshFilter.sharedMesh = mesh;
  }

  public Mesh GenerateMeshesFromPoints(List<Vector3> vertices)
  {
    var triangles = PerformDelaunayTriangulation(vertices);
    var delaunayMesh = GenerateUnityMesh(vertices, triangles);
    return delaunayMesh;
  }

  // List<Vector3> GenerateRandomVertices(int count, Vector3 bounds)
  // {
  //   List<Vector3> vertices = new List<Vector3>();
  //   for (int i = 0; i < count; i++)
  //   {
  //     float x = Random.Range(0, bounds.x);
  //     float y = Random.Range(0, bounds.y);
  //     float z = Random.Range(0, bounds.z);
  //     vertices.Add(new Vector3(x, y, z));
  //   }
  //
  //   return vertices;
  // }

  List<Triangle> PerformDelaunayTriangulation(List<Vector3> vertices)
  {
    // Create a super triangle
    float maxX = bounds.x * 10;
    float maxY = bounds.y * 10;
    float maxZ = bounds.z * 10;

    Vector3 v1 = new Vector3(-maxX, -maxY, -maxZ);
    Vector3 v2 = new Vector3(maxX, -maxY, -maxZ);
    Vector3 v3 = new Vector3(0, maxY, maxZ);

    List<Triangle> triangles = new List<Triangle> { new Triangle(v1, v2, v3) };

    // Add each vertex to the triangulation
    foreach (var vertex in vertices)
    {
      List<Triangle> badTriangles = new List<Triangle>();
      List<Edge> polygon = new List<Edge>();

      // Find all triangles that are no longer valid
      foreach (var triangle in triangles)
      {
        if (IsPointInCircumcircle(vertex, triangle))
        {
          badTriangles.Add(triangle);
          polygon.Add(new Edge(triangle.v1, triangle.v2));
          polygon.Add(new Edge(triangle.v2, triangle.v3));
          polygon.Add(new Edge(triangle.v3, triangle.v1));
        }
      }

      // Remove the bad triangles
      triangles.RemoveAll(t => badTriangles.Contains(t));

      // Remove duplicate edges
      polygon = RemoveDuplicateEdges(polygon);

      // Re-triangulate the polygonal hole
      foreach (var edge in polygon)
      {
        triangles.Add(new Triangle(edge.v1, edge.v2, vertex));
      }
    }

    // Remove triangles that share a vertex with the super triangle
    triangles.RemoveAll(t => t.ContainsVertex(v1) || t.ContainsVertex(v2) || t.ContainsVertex(v3));

    return triangles;
  }

  List<Edge> RemoveDuplicateEdges(List<Edge> edges)
  {
    List<Edge> uniqueEdges = new List<Edge>();
    foreach (var edge in edges)
    {
      if (!uniqueEdges.Contains(edge))
      {
        uniqueEdges.Add(edge);
      }
      else
      {
        uniqueEdges.Remove(edge);
      }
    }

    return uniqueEdges;
  }

  bool IsPointInCircumcircle(Vector3 point, Triangle triangle)
  {
    float ax = triangle.v1.x - point.x;
    float ay = triangle.v1.y - point.y;
    float az = triangle.v1.z - point.z;
    float bx = triangle.v2.x - point.x;
    float by = triangle.v2.y - point.y;
    float bz = triangle.v2.z - point.z;
    float cx = triangle.v3.x - point.x;
    float cy = triangle.v3.y - point.y;
    float cz = triangle.v3.z - point.z;

    float det = (ax * ax + ay * ay + az * az) * (bx * cy - by * cx)
                - (bx * bx + by * by + bz * bz) * (ax * cy - ay * cx)
                + (cx * cx + cy * cy + cz * cz) * (ax * by - ay * bx);

    return det > 0;
  }

  Mesh GenerateUnityMesh(List<Vector3> vertices, List<Triangle> triangles)
  {
    Mesh mesh = new Mesh();
    List<Vector3> meshVertices = new List<Vector3>();
    List<int> meshTriangles = new List<int>();

    Dictionary<Vector3, int> vertexIndexMap = new Dictionary<Vector3, int>();
    int index = 0;

    foreach (var vertex in vertices)
    {
      if (!vertexIndexMap.ContainsKey(vertex))
      {
        vertexIndexMap[vertex] = index;
        meshVertices.Add(vertex);
        index++;
      }
    }

    foreach (var triangle in triangles)
    {
      meshTriangles.Add(vertexIndexMap[triangle.v1]);
      meshTriangles.Add(vertexIndexMap[triangle.v2]);
      meshTriangles.Add(vertexIndexMap[triangle.v3]);
    }

    mesh.SetVertices(meshVertices);
    mesh.SetTriangles(meshTriangles, 0);
    mesh.RecalculateNormals();

    return mesh;
  }

  struct Triangle
  {
    public Vector3 v1;
    public Vector3 v2;
    public Vector3 v3;

    public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
      this.v1 = v1;
      this.v2 = v2;
      this.v3 = v3;
    }

    public bool ContainsVertex(Vector3 vertex)
    {
      return v1 == vertex || v2 == vertex || v3 == vertex;
    }
  }

  struct Edge
  {
    public Vector3 v1;
    public Vector3 v2;

    public Edge(Vector3 v1, Vector3 v2)
    {
      this.v1 = v1;
      this.v2 = v2;
    }

    public override bool Equals(object obj)
    {
      if (!(obj is Edge))
        return false;

      Edge other = (Edge)obj;
      return (v1 == other.v1 && v2 == other.v2) || (v1 == other.v2 && v2 == other.v1);
    }

    public override int GetHashCode()
    {
      return v1.GetHashCode() ^ v2.GetHashCode();
    }
  }


  public void GenerateUvs()
  {
  }

  public void GenerateVerts()
  {
  }

  public void GenerateTriangles()
  {
  }

  public void OnPreview()
  {
  }

  private void AddButtonsForMeshPoints()
  {
    var guiMan = Jotunn.Managers.GUIManager.Instance;
    var pos = 0;
    buttonItems = [];
    foreach (var meshCoordinate in meshCoordinates)
    {
      buttonItems.Add(guiMan.CreateButton(
        $"Coordinate: (x,y,z) {meshCoordinate.x}, {meshCoordinate.y}, {meshCoordinate.z}",
        scrollViewObj.transform, Vector2.zero,
        Vector2.zero, new Vector2(0, pos)));
      pos += 50;
    }
  }

  public void UpdateMeshWithVehicleBounds()
  {
  }

  public static bool OnTriggerPanelFromLever(GameObject go, bool state)
  {
    var instance = go.GetComponentInParent<VehicleMeshMaskManager>();
    if (instance != null)
    {
      instance.OnTogglePanel(state);
      return true;
    }

    return false;
  }
}