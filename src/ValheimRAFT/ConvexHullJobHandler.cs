// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts
{
  using System;
  using System.Collections.Generic;
  using Unity.Collections;
  using Unity.Jobs;
  using UnityEngine;

  public class ConvexHullJobHandler : MonoBehaviour
  {
    private ConvexHullAPI _convexHullAPI;
    private Dictionary<GameObject, JobHandle> _activeJobs = new();

    private void Awake()
    {
      _convexHullAPI = GetComponent<ConvexHullAPI>();

      if (_convexHullAPI == null)
        Debug.LogError("❌ ConvexHullAPI reference missing on ConvexHullJobHandler!");
    }

    /// <summary>
    /// Schedules a job to process `PrefabMeshData`, cluster points, and generate convex hulls.
    /// </summary>
    public void ScheduleConvexHullJob(GameObject prefab, List<PrefabMeshData> meshDataList, float clusterThreshold)
    {
      if (prefab == null || meshDataList.Count == 0) return;

      // ✅ Convert PrefabMeshData into a NativeArray for job processing
      NativeArray<PrefabMeshData> nativeMeshData = new(meshDataList.ToArray(), Allocator.TempJob);

      var maxClusters = meshDataList.Count; // Assume worst case: each PrefabMeshData is its own cluster

      var clusterVertices = new NativeArray<Vector3>[maxClusters];
      var clusterTriangles = new NativeArray<int>[maxClusters];
      var clusterNormals = new NativeArray<Vector3>[maxClusters];

      for (var i = 0; i < maxClusters; i++)
      {
        clusterVertices[i] = new NativeArray<Vector3>(meshDataList.Count * 3, Allocator.TempJob); // Pre-allocate max possible size
        clusterTriangles[i] = new NativeArray<int>(meshDataList.Count * 3, Allocator.TempJob);
        clusterNormals[i] = new NativeArray<Vector3>(meshDataList.Count * 3, Allocator.TempJob);
      }

      ConvexHullJob job = new()
      {
        InputMeshData = nativeMeshData,
        ClusterThreshold = clusterThreshold, // ✅ Passes cluster threshold dynamically
        ClusteredVertices = clusterVertices,
        ClusteredTriangles = clusterTriangles,
        ClusteredNormals = clusterNormals
      };

      var jobHandle = job.Schedule();
      _activeJobs[prefab] = jobHandle;

      jobHandle.Complete(); // ✅ Wait for job to complete (can be optimized later)

      // ✅ Convert results to managed arrays and call the main thread API
      for (var i = 0; i < maxClusters; i++)
      {
        if (clusterVertices[i].Length > 0)
        {
          _convexHullAPI.GenerateMeshFromConvexOutput(
            clusterVertices[i].ToArray(),
            clusterTriangles[i].ToArray(),
            clusterNormals[i].ToArray(),
            i
          );
        }

        // ✅ Dispose after copying to managed memory
        clusterVertices[i].Dispose();
        clusterTriangles[i].Dispose();
        clusterNormals[i].Dispose();
      }

      nativeMeshData.Dispose();
      _activeJobs.Remove(prefab);
    }

    /// <summary>
    /// Job for processing `PrefabMeshData`, clustering points, and generating convex hulls.
    /// </summary>
    private struct ConvexHullJob : IJob
    {
      [ReadOnly] public NativeArray<PrefabMeshData> InputMeshData;
      public float ClusterThreshold; // ✅ Accepts the cluster threshold value
      public NativeArray<Vector3>[] ClusteredVertices;
      public NativeArray<int>[] ClusteredTriangles;
      public NativeArray<Vector3>[] ClusteredNormals;

      public void Execute()
      {
        var calculator = new ConvexHullCalculator();
        var clusteredPoints = ClusterMeshData();

        for (var clusterIndex = 0; clusterIndex < clusteredPoints.Count; clusterIndex++)
        {
          var verts = new List<Vector3>();
          var normals = new List<Vector3>();
          var triangles = new List<int>();

          calculator.GenerateHull(clusteredPoints[clusterIndex], false, ref verts, ref triangles, ref normals);

          // ✅ Store the results in NativeArrays
          NativeArray<Vector3>.Copy(verts.ToArray(), ClusteredVertices[clusterIndex]);
          NativeArray<int>.Copy(triangles.ToArray(), ClusteredTriangles[clusterIndex]);
          NativeArray<Vector3>.Copy(normals.ToArray(), ClusteredNormals[clusterIndex]);
        }
      }

      /// <summary>
      /// Clusters mesh data points into groups for convex hull generation based on `ClusterThreshold`.
      /// </summary>
      private List<List<Vector3>> ClusterMeshData()
      {
        List<List<Vector3>> clusters = new();
        HashSet<int> processed = new();

        for (var i = 0; i < InputMeshData.Length; i++)
        {
          if (processed.Contains(i)) continue;

          List<Vector3> cluster = new(InputMeshData[i].Vertices.ToArray());
          processed.Add(i);

          for (var j = i + 1; j < InputMeshData.Length; j++)
          {
            if (processed.Contains(j)) continue;

            if (Vector3.Distance(InputMeshData[i].LocalPosition, InputMeshData[j].LocalPosition) < ClusterThreshold)
            {
              cluster.AddRange(InputMeshData[j].Vertices.ToArray());
              processed.Add(j);
            }
          }

          clusters.Add(cluster);
        }

        return clusters;
      }
    }
  }
}