// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{

  public class ConvexHullJobHandler : MonoBehaviour
  {
    private const int MAX_BUFFER_SIZE = 10000000; // ✅ Initial buffer size

    private static readonly Dictionary<ConvexHullAPI, JobData> ActiveJobs = new();
    private Coroutine _jobMonitorCoroutine;

    /// <summary>
    /// ✅ Coroutine that runs **only when jobs are active** and stops when all jobs are done.
    /// </summary>
    private IEnumerator JobMonitorCoroutine()
    {
      while (ActiveJobs.Count > 0)
      {
        ProcessCompletedJobs();
        yield return new WaitForSeconds(0.1f);
      }

      // ✅ All jobs completed, stop coroutine
      _jobMonitorCoroutine = null;
      Debug.Log("🛑 Job monitor stopped. No more active jobs.");
    }


    public void ScheduleConvexHullJob(
      ConvexHullAPI convexHullAPI,
      List<PrefabPieceData> prefabDataList,
      float clusterThreshold,
      Action onComplete = null)
    {
      if (convexHullAPI == null)
      {
        Debug.LogError("❌ ConvexHullAPI must be provided.");
        return;
      }

      if (prefabDataList.Count == 0) return;

      if (ActiveJobs.ContainsKey(convexHullAPI))
      {
        Debug.LogWarning($"⚠️ ConvexHullJob is already running for {convexHullAPI}. Skipping duplicate request.");
        return;
      }

      var colliderDataList = new List<PrefabColliderPointData>(prefabDataList.Count);
      foreach (var prefabData in prefabDataList)
      {
        colliderDataList.Add(prefabData.PointDataItems);
      }

      NativeArray<PrefabColliderPointData> nativeColliderData = new(colliderDataList.ToArray(), Allocator.TempJob);
      NativeArray<int> validClusterCountArray = new(1, Allocator.Persistent);
      NativeArray<ConvexHullResultData> nativeHullResults = new(prefabDataList.Count, Allocator.TempJob);

      var maxVertices = 10000;
      var maxTriangles = 10000;
      var maxNormals = 10000;

      NativeArray<Vector3> nativeVertices = new(maxVertices, Allocator.TempJob);
      NativeArray<int> nativeTriangles = new(maxTriangles, Allocator.TempJob);
      NativeArray<Vector3> nativeNormals = new(maxNormals, Allocator.TempJob);

      ConvexHullJob job = new()
      {
        InputColliderData = nativeColliderData,
        ClusterThreshold = clusterThreshold,
        OutputHullData = nativeHullResults,
        OutputVertices = nativeVertices,
        OutputTriangles = nativeTriangles,
        OutputNormals = nativeNormals,
        ValidClusterCount = validClusterCountArray
      };

      var jobHandle = job.Schedule();

      ActiveJobs[convexHullAPI] = new JobData
      {
        Handle = jobHandle,
        ValidClusterCountArray = validClusterCountArray,
        OnComplete = () =>
        {
          Debug.Log($"✅ Convex Hull Job Completed for {convexHullAPI}");

          var validClusters = validClusterCountArray[0];

          Debug.Log($"🔹 {validClusters} valid convex hulls generated.");

          for (var i = 0; i < validClusters; i++)
          {
            var result = nativeHullResults[i];

            convexHullAPI.GenerateMeshFromConvexOutput(
              nativeVertices.GetSubArray(result.VertexStartIndex, result.VertexCount).ToArray(),
              nativeTriangles.GetSubArray(result.TriangleStartIndex, result.TriangleCount).ToArray(),
              nativeNormals.GetSubArray(result.NormalStartIndex, result.NormalCount).ToArray(),
              i
            );
          }

          nativeColliderData.Dispose();
          nativeHullResults.Dispose();
          nativeVertices.Dispose();
          nativeTriangles.Dispose();
          nativeNormals.Dispose();

          if (validClusterCountArray.IsCreated)
          {
            validClusterCountArray.Dispose();
          }

          onComplete?.Invoke();
        }
      };

      Debug.Log($"🟢 Scheduled Convex Hull Job for {convexHullAPI}");

      if (_jobMonitorCoroutine == null)
      {
        StartCoroutine(JobMonitorCoroutine());
      }
    }

    // public void OnEnable()
    // {
    //     InvokeRepeating(nameof(ProcessCompletedJobs), 1f, 5f);
    // }
    //
    // public void OnDisable()
    // {
    //     CancelInvoke(nameof(ProcessCompletedJobs));
    // }

    public static void ProcessCompletedJobs()
    {
      if (ActiveJobs.Count == 0) return;

      var completedJobs = new List<ConvexHullAPI>();

      foreach (var kvp in ActiveJobs)
      {
        var convexHullAPI = kvp.Key;
        var jobData = kvp.Value;

        if (!jobData.Handle.IsCompleted) continue;
        completedJobs.Add(convexHullAPI);
      }

      foreach (var convexHullAPI in completedJobs)
      {
        if (!ActiveJobs.TryGetValue(convexHullAPI, out var jobData)) continue;

        ActiveJobs.Remove(convexHullAPI);

        jobData.Handle.Complete();
        jobData.OnComplete?.Invoke();

        jobData.Dispose(); // ✅ Cleanup per-job allocations
      }
    }


    // public static void ProcessCompletedJobs()
    // {
    //   if (ActiveJobs.Count == 0) return; // ✅ Exit early if no jobs exist
    //
    //   var completedJobs = new List<ConvexHullAPI>();
    //
    //   foreach (var kvp in ActiveJobs)
    //   {
    //     var convexHullAPI = kvp.Key;
    //     var jobData = kvp.Value;
    //
    //     if (!jobData.Handle.IsCompleted) continue; // ✅ Skip unfinished jobs
    //
    //     completedJobs.Add(convexHullAPI); // ✅ Mark for removal
    //   }
    //
    //   // ✅ Process completed jobs in a second pass
    //   foreach (var convexHullAPI in completedJobs)
    //   {
    //     if (!ActiveJobs.TryGetValue(convexHullAPI, out var jobData)) continue;
    //     jobData.Handle.Complete(); // ✅ Now complete the job
    //     jobData.OnComplete?.Invoke(); // ✅ Run callback after job completion
    //     ActiveJobs.Remove(convexHullAPI); // ✅ Remove cannot be called before complete.
    //   }
    // }

    private struct JobData
    {
      public ConvexHullJob job;
      public JobHandle Handle;
      public Action OnComplete;

      public NativeArray<PrefabColliderPointData> ColliderData;
      public NativeArray<ConvexHullResultData> HullResults;
      public NativeArray<int> ValidClusterCountArray;

      public NativeArray<Vector3> PointsBuffer; // ✅ Unique per job
      public NativeArray<Vector3> VerticesBuffer;
      public NativeArray<int> TrianglesBuffer;
      public NativeArray<Vector3> NormalsBuffer;

      public void Dispose()
      {
        if (ColliderData.IsCreated) ColliderData.Dispose();
        if (HullResults.IsCreated) HullResults.Dispose();
        if (ValidClusterCountArray.IsCreated) ValidClusterCountArray.Dispose();

        if (PointsBuffer.IsCreated) PointsBuffer.Dispose();
        if (VerticesBuffer.IsCreated) VerticesBuffer.Dispose();
        if (TrianglesBuffer.IsCreated) TrianglesBuffer.Dispose();
        if (NormalsBuffer.IsCreated) NormalsBuffer.Dispose();
      }
    }


    /// <summary>
    /// ✅ Job for computing convex hulls from clustered mesh points.
    /// </summary>
    public struct ConvexHullJob : IJob
    {
      [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<PrefabColliderPointData> InputColliderData;
      public float ClusterThreshold;
      [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Vector3> AllPointsBuffer; // ✅ Store all points here

      [DeallocateOnJobCompletion] public NativeArray<ConvexHullResultData> OutputHullData;
      [DeallocateOnJobCompletion] public NativeArray<Vector3> OutputVertices;
      [DeallocateOnJobCompletion] public NativeArray<int> OutputTriangles;
      [DeallocateOnJobCompletion] public NativeArray<Vector3> OutputNormals;

      [DeallocateOnJobCompletion] public NativeArray<int> ValidClusterCount; // ✅ Track cluster count separately

      public void Execute()
      {
        var calculator = new ConvexHullCalculator();
        var clusteredPoints = ClusterMeshData();
        var validClusters = 0;

        var vertexOffset = 0;
        var triangleOffset = 0;
        var normalOffset = 0;

        for (var clusterIndex = 0; clusterIndex < clusteredPoints.Count; clusterIndex++)
        {
          var verts = new List<Vector3>();
          var normals = new List<Vector3>();
          var triangles = new List<int>();

          calculator.GenerateHull(clusteredPoints[clusterIndex], false, ref verts, ref triangles, ref normals);

          if (verts.Count == 0 || triangles.Count == 0) continue;

          OutputHullData[validClusters] = new ConvexHullResultData(
            vertexOffset, verts.Count,
            triangleOffset, triangles.Count,
            normalOffset, normals.Count,
            validClusters
          );

          NativeArray<Vector3>.Copy(verts.ToArray(), 0, OutputVertices, vertexOffset, verts.Count);
          NativeArray<int>.Copy(triangles.ToArray(), 0, OutputTriangles, triangleOffset, triangles.Count);
          NativeArray<Vector3>.Copy(normals.ToArray(), 0, OutputNormals, normalOffset, normals.Count);

          vertexOffset += verts.Count;
          triangleOffset += triangles.Count;
          normalOffset += normals.Count;

          validClusters++;

          verts.Clear();
          normals.Clear();
          triangles.Clear();
        }

        ValidClusterCount[0] = validClusters; // ✅ Store final count safely
      }

      private List<List<Vector3>> ClusterMeshData()
      {
        var clusters = new List<List<Vector3>>();
        var processed = new HashSet<int>();

        for (var i = 0; i < InputColliderData.Length; i++)
        {
          if (processed.Contains(i)) continue;

          var cluster = new List<Vector3>(InputColliderData[i].Points.ToArray());
          processed.Add(i);

          for (var j = i + 1; j < InputColliderData.Length; j++)
          {
            if (processed.Contains(j)) continue;

            if (Vector3.Distance(InputColliderData[i].LocalPosition, InputColliderData[j].LocalPosition) < ClusterThreshold)
            {
              cluster.AddRange(InputColliderData[j].Points.ToArray());
              processed.Add(j);
            }
          }

          clusters.Add(cluster);
        }

        Debug.Log($"🔹 {clusters.Count} clusters detected.");
        return clusters;
      }
    }
  }
}