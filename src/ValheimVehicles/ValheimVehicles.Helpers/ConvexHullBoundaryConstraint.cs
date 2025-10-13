using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.ValheimVehicles.Structs;
using Zolantris.Shared;

namespace ValheimVehicles.Helpers;

/// <summary>
/// Handles boundary constraints for convex hull generation.
/// Uses ship chunk boundary pieces to create a constraint mesh that limits where collider points can be placed.
/// </summary>
public class ConvexHullBoundaryConstraint
{
  private Mesh boundaryMesh;
  private MeshCollider boundaryCollider;
  private GameObject boundaryObject;
  private bool isInitialized;

  public HashSet<VehicleChunkSizeData> cachedChunkSizeDataItems = new();

  /// <summary>
  /// List of all boundary cube vertices collected from placed ship chunk boundary pieces
  /// </summary>
  public List<Vector3> boundaryVertices = new();

  public int GetVerticesCount => boundaryVertices.Count;
  public int GetObjectsCount => cachedChunkSizeDataItems.Count;

  public bool IsInitialized => isInitialized && boundaryCollider != null;

  public void UpdateAllBoundaryPoints()
  {
    // always clear these to avoid duplicates
    boundaryVertices.Clear();

    // Add boundary points from each boundary piece in the chunk size data
    foreach (var dataItem in cachedChunkSizeDataItems)
    {
      AddBoundaryPiecePoints(dataItem.position.ToVector3(), dataItem.chunkSize * Vector3.one);
    }
  }

  public void SetChunkSizeDataItems(HashSet<VehicleChunkSizeData> items)
  {
    cachedChunkSizeDataItems = items;
  }

  public static Vector3[] GetBoundaryVerticesArray(Vector3 localPosition, Vector3 scale)
  {
    var halfScale = scale * 0.5f;
    var cubeVertices = new Vector3[]
    {
      new(-halfScale.x, -halfScale.y, -halfScale.z),
      new(halfScale.x, -halfScale.y, -halfScale.z),
      new(-halfScale.x, halfScale.y, -halfScale.z),
      new(halfScale.x, halfScale.y, -halfScale.z),
      new(-halfScale.x, -halfScale.y, halfScale.z),
      new(halfScale.x, -halfScale.y, halfScale.z),
      new(-halfScale.x, halfScale.y, halfScale.z),
      new(halfScale.x, halfScale.y, halfScale.z)
    };
    var relativeVertices = cubeVertices.Select(x => localPosition + x);

    return relativeVertices.ToArray();
  }

  public static Bounds GetChunkBounds(Vector3 localPosition, Vector3 scale)
  {
    var vertices = GetBoundaryVerticesArray(localPosition, scale);
    var bounds = new Bounds();
    var first = true;

    foreach (var vertice in vertices)
    {
      // always use first index otherwise it's inaccurate if coordinates are not near localposition.
      if (first)
      {
        bounds = new Bounds(vertice, Vector3.zero);
        first = false;
      }
      else
      {
        bounds.Encapsulate(vertice);
      }
    }

    return bounds;
  }

  /// <summary>
  /// Adds boundary points from a ship chunk boundary piece (cube with 8 vertices)
  /// </summary>
  public void AddBoundaryPiecePoints(Vector3 localPosition, Vector3 scale)
  {
    // Get the 8 vertices of a cube in local space
    var vertices = GetBoundaryVerticesArray(localPosition, scale);
    foreach (var localVertex in vertices)
    {
      boundaryVertices.Add(localVertex);
    }
  }

  /// <summary>
  /// Clears all boundary points
  /// </summary>
  public void Clear()
  {
    boundaryVertices.Clear();
    cachedChunkSizeDataItems.Clear();

    isInitialized = false;

    if (boundaryObject != null)
    {
      Object.Destroy(boundaryObject);
      boundaryObject = null;
    }

    boundaryCollider = null;
    boundaryMesh = null;
  }

  /// <summary>
  /// Generates the boundary constraint mesh from collected boundary points using ConvexHullCalculator
  /// </summary>
  public bool GenerateBoundaryMesh(Transform parentTransform)
  {
    if (boundaryVertices.Count < 4)
    {
      LoggerProvider.LogWarning($"Not enough boundary points to generate boundary mesh: {boundaryVertices.Count}");
      return false;
    }

    try
    {
      var verts = new List<Vector3>();
      var tris = new List<int>();
      var normals = new List<Vector3>();

      var calculator = new ConvexHullCalculator();
      if (!calculator.GenerateHull(boundaryVertices, false, ref verts, ref tris, ref normals, out _))
      {
        LoggerProvider.LogWarning("Failed to generate boundary hull");
        return false;
      }

      // Create mesh
      boundaryMesh = new Mesh
      {
        vertices = verts.ToArray(),
        triangles = tris.ToArray(),
        normals = normals.ToArray()
      };
      boundaryMesh.RecalculateBounds();

      // Create GameObject with MeshCollider for efficient point-in-mesh and closest-point queries
      boundaryObject = new GameObject("VehicleBoundaryCollider");
      boundaryObject.transform.SetParent(parentTransform);
      boundaryObject.transform.localPosition = Vector3.zero;
      boundaryObject.transform.localRotation = Quaternion.identity;
      boundaryObject.transform.localScale = Vector3.one;
      boundaryObject.layer = LayerHelpers.PieceNonSolidLayer;

      boundaryCollider = boundaryObject.AddComponent<MeshCollider>();
      boundaryCollider.sharedMesh = boundaryMesh;
      boundaryCollider.convex = true;
      boundaryCollider.isTrigger = true;
      boundaryCollider.enabled = true;

      isInitialized = true;
      LoggerProvider.LogInfo($"✅ Boundary constraint mesh generated with {verts.Count} vertices");
      return true;
    }
    catch (System.Exception e)
    {
      LoggerProvider.LogError($"Error generating boundary mesh: {e}");
      return false;
    }
  }

  /// <summary>
  /// Constrains a point to be within the boundary mesh.
  /// If the point is outside, returns the closest point on the boundary surface.
  /// This is highly performant using Unity's MeshCollider.ClosestPoint.
  /// Points inside the boundary remain unchanged (e.g., center of 16x16x16 stays at center).
  /// Points outside are pushed to the boundary surface.
  /// </summary>
  public Vector3 ConstrainPoint(Vector3 localPoint)
  {
    if (!IsInitialized) return localPoint;

    // boundaryCollider is in the same local space as the points we're constraining
    // Convert localPoint to world space for ClosestPoint query
    var parentTransform = boundaryCollider.transform.parent;
    var worldPoint = parentTransform.TransformPoint(localPoint);

    // Get closest point on the boundary collider surface (in world space)
    var closestPoint = boundaryCollider.ClosestPoint(worldPoint);

    // Calculate distance in 3D space to determine if point is inside or outside
    var distance = Vector3.Distance(worldPoint, closestPoint);

    // If point is inside (distance is near zero), return original point unchanged
    // This allows points at the center of a 16x16x16 cube to stay at center
    if (distance < 0.001f)
    {
      return localPoint;
    }

    // Point is outside boundary, return closest point on boundary surface in local space
    return parentTransform.InverseTransformPoint(closestPoint);
  }

  /// <summary>
  /// Batch constrains multiple points. More efficient for large point lists.
  /// </summary>
  public void ConstrainPoints(List<Vector3> points)
  {
    if (!IsInitialized) return;

    for (var i = 0; i < points.Count; i++)
    {
      points[i] = ConstrainPoint(points[i]);
    }
  }
}