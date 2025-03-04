// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts
{
  using System;
  using Unity.Collections;
  using UnityEngine;

  /// <summary>
  /// Stores **job-compatible mesh data** for a prefab.
  /// No Unity API references, making it safe for multi-threading.
  /// </summary>
  public struct PrefabMeshData : IDisposable
  {
    public Vector3 LocalPosition; // ✅ Position relative to the root prefab
    public NativeArray<Vector3> Vertices; // ✅ Local-space mesh points
    public NativeArray<int> Triangles; // ✅ Mesh face indices

    public PrefabMeshData(Vector3 localPos, Vector3[] vertices, int[] triangles, Allocator allocator)
    {
      LocalPosition = localPos;
      Vertices = new NativeArray<Vector3>(vertices, allocator);
      Triangles = new NativeArray<int>(triangles, allocator);
    }

    public void Dispose()
    {
      if (Vertices.IsCreated) Vertices.Dispose();
      if (Triangles.IsCreated) Triangles.Dispose();
    }

    public bool IsValid()
    {
      return Vertices.IsCreated && Vertices.Length > 0 &&
             Triangles.IsCreated && Triangles.Length > 0;
    }
  }
}