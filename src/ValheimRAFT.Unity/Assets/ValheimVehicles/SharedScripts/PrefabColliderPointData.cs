// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using Unity.Collections;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{

  public struct PrefabColliderPointData
  {
    public Vector3 LocalPosition;
    public int PointCount;
    public Bounds LocalBounds;
    public NativeArray<Vector3> Points; // ✅ Stores points locally per prefab

    public PrefabColliderPointData(Vector3 localPosition, Vector3[] points, Allocator allocator)
    {
      LocalPosition = localPosition;
      PointCount = points.Length;
      LocalBounds = new Bounds(LocalPosition, Vector3.zero);
      Points = new NativeArray<Vector3>(points, allocator);

      foreach (var point in points)
      {
        LocalBounds.Encapsulate(point);
      }
    }

    public Vector3 GetPointAt(int index)
    {
      if (index < 0 || index >= PointCount)
      {
        Debug.LogError($"❌ Invalid index {index} for PrefabColliderPointData (Max: {PointCount})");
        return Vector3.zero;
      }
      return Points[index]; // ✅ Directly fetch from local storage
    }

    public void Dispose()
    {
      if (Points.IsCreated)
      {
        Points.Dispose();
      }
    }
  }
}