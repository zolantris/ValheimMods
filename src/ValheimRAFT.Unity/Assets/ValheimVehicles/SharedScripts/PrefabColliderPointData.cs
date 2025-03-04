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

    public PrefabColliderPointData(Vector3 localPosition, Vector3[] points, Allocator allocator)
    {
      LocalPosition = localPosition;
      Points = new NativeArray<Vector3>(points, allocator);
    }

    /// <summary>
    /// Properly disposes the `NativeArray` when no longer needed.
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