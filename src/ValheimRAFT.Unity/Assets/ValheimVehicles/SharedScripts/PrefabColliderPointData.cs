// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts
{
  using Unity.Collections;
  using UnityEngine;

  public struct PrefabColliderPointData
  {
    public Vector3 LocalPosition; // ✅ Local offset relative to the prefab root
    public NativeArray<Vector3> Points; // ✅ Collider points in local space
    public Bounds LocalBounds; // ✅ Encapsulated bounds for all points

    public PrefabColliderPointData(Vector3 localPosition, Vector3[] points, Allocator allocator)
    {
      LocalPosition = localPosition;
      Points = new NativeArray<Vector3>(points, allocator);

      if (points.Length > 0)
      {
        LocalBounds = new Bounds(points[0], Vector3.zero); // ✅ Initialize at first point
        foreach (var point in points)
        {
          LocalBounds.Encapsulate(point);
        }
      }
      else
      {
        LocalBounds = new Bounds(localPosition, Vector3.zero); // ✅ Default to local position if no points exist
      }
    }

    /// <summary>
    /// ✅ Returns the center of the bounds in local space.
    /// </summary>
    public Vector3 Center => LocalBounds.center;

    /// <summary>
    /// ✅ Properly disposes of the `NativeArray` when no longer needed.
    /// </summary>
    public void Dispose()
    {
      if (Points.IsCreated)
      {
        Points.Dispose();
      }
    }
  }

}