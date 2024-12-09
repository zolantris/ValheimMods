using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimVehicles.Helpers;

public static class ColliderPointsExtractor
{
  /// <summary>
  /// Gets all local-space points of a collider, adjusted for the object's scale.
  /// </summary>
  /// <param name="collider">The collider to extract points from.</param>
  /// <returns>A list of Vector3 points in local space, scaled correctly.</returns>
  public static List<Vector3> GetColliderPointsLocal(Collider collider)
  {
    var points = new List<Vector3>();
    var scale = collider.transform.localScale; // Object's local scale

    if (collider is MeshCollider meshCollider &&
        meshCollider.sharedMesh != null)
    {
      // MeshCollider: Extract scaled local-space vertices
      foreach (var vertex in meshCollider.sharedMesh.vertices)
      {
        var scaledVertex = Vector3.Scale(vertex, scale);
        points.Add(scaledVertex);
      }
    }
    else if (collider is BoxCollider boxCollider)
    {
      // BoxCollider: Calculate 8 corners in scaled local space
      var center = Vector3.Scale(boxCollider.center, scale);
      var size = Vector3.Scale(boxCollider.size * 0.5f, scale);

      var corners = new Vector3[]
      {
        center + new Vector3(-size.x, -size.y, -size.z),
        center + new Vector3(-size.x, -size.y, size.z),
        center + new Vector3(-size.x, size.y, -size.z),
        center + new Vector3(-size.x, size.y, size.z),
        center + new Vector3(size.x, -size.y, -size.z),
        center + new Vector3(size.x, -size.y, size.z),
        center + new Vector3(size.x, size.y, -size.z),
        center + new Vector3(size.x, size.y, size.z)
      };

      points.AddRange(corners);
    }
    else if (collider is SphereCollider sphereCollider)
    {
      // SphereCollider: Use scaled local center
      var center = Vector3.Scale(sphereCollider.center, scale);
      points.Add(center);
    }
    else if (collider is CapsuleCollider capsuleCollider)
    {
      // CapsuleCollider: Get scaled local top, bottom, and center
      var center = Vector3.Scale(capsuleCollider.center, scale);
      var height = (capsuleCollider.height * 0.5f - capsuleCollider.radius) *
                   scale.y; // Scale height
      var direction = capsuleCollider.direction; // 0 = X, 1 = Y, 2 = Z

      // Top and bottom positions (adjusted for scale and direction)
      var offset = Vector3.zero;
      if (direction == 0) offset = Vector3.right * height;
      else if (direction == 1) offset = Vector3.up * height;
      else if (direction == 2) offset = Vector3.forward * height;

      points.Add(center + offset);
      points.Add(center - offset);
    }
    else
    {
      Debug.LogWarning($"Unsupported collider type: {collider.GetType()}");
    }

    return points; // Local space, scaled
  }

  public static List<Vector3> ConvertToRelativeSpace(Collider collider,
    List<Vector3> localPoints)
  {
    var relativePoints = new List<Vector3>();
    foreach (var localPoint in localPoints)
    {
      relativePoints.Add(localPoint + collider.transform.localPosition);
    }

    return relativePoints;
  }

  public static List<Vector3> ConvertToWorldSpace(Collider collider,
    List<Vector3> localPoints)
  {
    var worldPoints = new List<Vector3>();
    foreach (var localPoint in localPoints)
    {
      worldPoints.Add(collider.transform.TransformPoint(localPoint));
    }

    return worldPoints;
  }

  public static List<Vector3> GetColliderPointsLocal(List<Collider> colliders)
  {
    return colliders
      .SelectMany(GetColliderPointsLocal)
      .Distinct()
      .ToList();
  }

  // Optional: Overload for world space conversion of multiple colliders
  public static List<Vector3> GetColliderPointsWorld(List<Collider> colliders)
  {
    return colliders
      .SelectMany(collider =>
        ConvertToWorldSpace(collider, GetColliderPointsLocal(collider)))
      .Distinct()
      .ToList();
  }

  public static List<Vector3> GetColliderPointsRelative(
    List<Collider> colliders)
  {
    return colliders
      .SelectMany(collider =>
        ConvertToRelativeSpace(collider, GetColliderPointsLocal(collider)))
      .Distinct()
      .ToList();
  }
}