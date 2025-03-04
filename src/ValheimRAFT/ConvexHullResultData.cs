// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using Unity.Collections;
using UnityEngine;

namespace ValheimVehicles.SharedScripts
{
  public struct ConvexHullResultData
  {
    public NativeArray<Vector3> Vertices;
    public NativeArray<int> Triangles;
    public NativeArray<Vector3> Normals;

    public ConvexHullResultData(Vector3[] vertices, int[] triangles, Vector3[] normals, Allocator allocator)
    {
      Vertices = new NativeArray<Vector3>(vertices, allocator);
      Triangles = new NativeArray<int>(triangles, allocator);
      Normals = new NativeArray<Vector3>(normals, allocator);
    }

    /// <summary>
    /// Disposes the allocated NativeArrays to prevent memory leaks.
    /// </summary>
    public void Dispose()
    {
      if (Vertices.IsCreated) Vertices.Dispose();
      if (Triangles.IsCreated) Triangles.Dispose();
      if (Normals.IsCreated) Normals.Dispose();
    }
  }
}