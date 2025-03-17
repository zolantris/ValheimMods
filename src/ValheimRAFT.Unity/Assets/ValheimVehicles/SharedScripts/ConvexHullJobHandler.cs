// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{

  public class ConvexHullJobHandler : SingletonBehaviour<ConvexHullJobHandler>
  {

    private static readonly Dictionary<ConvexHullAPI, JobData> ActiveJobs = new();


    public void FixedUpdate()
    {
      ProcessCompletedJobs();
    }

    public static void ScheduleConvexHullJob(
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

      // ✅ Extract `PrefabColliderPointData`
      var colliderDataList = new List<PrefabColliderPointData>(prefabDataList.Count);
      foreach (var prefabData in prefabDataList)
      {
        colliderDataList.Add(prefabData.PointDataItems);
      }

      NativeArray<PrefabColliderPointData> nativeColliderData = new(colliderDataList.ToArray(), Allocator.TempJob);

      // ✅ Determine the expected number of output clusters dynamically
      var estimatedClusterCount = Mathf.Max(1, prefabDataList.Count / 2);
      NativeArray<ConvexHullResultData> nativeHullResults = new(estimatedClusterCount, Allocator.TempJob);
      NativeArray<int> validClusterCountArray = new(1, Allocator.Persistent); // ✅ Store cluster count asynchronously

      // ✅ Initialize job
      ConvexHullJob job = new()
      {
        InputColliderData = nativeColliderData,
        ClusterThreshold = clusterThreshold,
        OutputHullData = nativeHullResults,
        ValidClusterCount = validClusterCountArray
      };

      var jobHandle = job.Schedule();

      // ✅ Store the job handle & callback inside a struct
      ActiveJobs[convexHullAPI] = new JobData
      {
        Handle = jobHandle,
        OnComplete = () =>
        {
          Debug.Log($"✅ Convex Hull Job Completed for {convexHullAPI}");

          var validClusters = job.ValidClusterCount[0]; // ✅ Read from the job instance

          Debug.Log($"🔹 {validClusters} valid convex hulls generated.");

          // ✅ Now iterate over only `ValidClusterCount`
          for (var i = 0; i < validClusters; i++)
          {
            var result = nativeHullResults[i];

            convexHullAPI.GenerateMeshFromConvexOutput(
              result.Vertices.ToArray(),
              result.Triangles.ToArray(),
              result.Normals.ToArray(),
              i
            );

            result.Dispose();
          }

          nativeColliderData.Dispose();
          nativeHullResults.Dispose();

          // ✅ Call the user-defined completion callback
          onComplete?.Invoke();
        }
      };

      Debug.Log($"🟢 Scheduled Convex Hull Job for {convexHullAPI}");
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
      if (ActiveJobs.Count == 0) return; // ✅ Exit early if no jobs exist

      var completedJobs = new List<ConvexHullAPI>();

      foreach (var kvp in ActiveJobs)
      {
        var convexHullAPI = kvp.Key;
        var jobData = kvp.Value;

        if (!jobData.Handle.IsCompleted) continue; // ✅ Skip unfinished jobs

        completedJobs.Add(convexHullAPI); // ✅ Mark for removal
      }

      // ✅ Process completed jobs in a second pass
      foreach (var convexHullAPI in completedJobs)
      {
        if (!ActiveJobs.TryGetValue(convexHullAPI, out var jobData)) continue;
        jobData.Handle.Complete(); // ✅ Now complete the job
        jobData.OnComplete?.Invoke(); // ✅ Run callback after job completion
        ActiveJobs.Remove(convexHullAPI); // ✅ Remove cannot be called before complete.
      }
    }

    private struct JobData
    {
      public JobHandle Handle;
      public Action OnComplete;
      public NativeArray<int> ValidClusterCountArray; // ✅ Track valid clusters asynchronously
    }
  }

  /// <summary>
  /// ✅ Job for computing convex hulls from clustered mesh points.
  /// </summary>
  public struct ConvexHullJob : IJob
  {
    [ReadOnly] public NativeArray<PrefabColliderPointData> InputColliderData;
    public float ClusterThreshold;
    public NativeArray<ConvexHullResultData> OutputHullData;
    public NativeArray<int> ValidClusterCount; // ✅ Track the actual number of clusters (async safe)

    public void Execute()
    {
      var calculator = new ConvexHullCalculator();
      var clusteredPoints = ClusterMeshData();
      var validClusters = 0; // ✅ Separate count variable

      var clusterCount = Mathf.Min(clusteredPoints.Count, OutputHullData.Length);

      for (var clusterIndex = 0; clusterIndex < clusterCount; clusterIndex++)
      {
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();

        calculator.GenerateHull(clusteredPoints[clusterIndex], false, ref verts, ref triangles, ref normals);

        if (verts.Count == 0 || triangles.Count == 0) continue; // ✅ Ignore empty hulls

        OutputHullData[validClusters] = new ConvexHullResultData(
          verts.ToArray(),
          triangles.ToArray(),
          normals.ToArray(),
          Allocator.TempJob
        );

        validClusters++; // ✅ Increment the actual valid cluster count
      }

      ValidClusterCount[0] = validClusters; // ✅ Write final count to NativeArray
    }

    public static bool ShouldSkipClusters = true;

    /// <summary>
    /// ✅ Clusters points into meaningful groups based on spatial distance.
    ///
    /// Currently badly implemented. Likely needs to make a local bounds per cluster to optimize this. But we can skip this and just use all the items to make 1 cluster...breaks catamarans.
    /// </summary>
    private List<List<Vector3>> ClusterMeshData()
    {
      var clustersOfPointData = new List<List<PrefabColliderPointData>>();
      var clustersOfPoints = new List<List<Vector3>>();
      if (!ShouldSkipClusters)
      {
        foreach (var inputData in InputColliderData)
        {
          var addedToCluster = false;

          // Check against existing clusters
          foreach (var cluster in clustersOfPointData)
          {
            if (ConvexHullAPI.IsBoundsNearCluster(inputData, cluster, ClusterThreshold))
            {
              addedToCluster = true;
              cluster.Add(inputData);
              break;
            }
          }
          // If not added to an existing cluster, create a new cluster
          if (!addedToCluster)
            clustersOfPointData.Add(new List<PrefabColliderPointData>
            {
              inputData
            });
        }

        clustersOfPoints = clustersOfPointData
          .Select(cluster => cluster.SelectMany(data => data.Points).ToList())
          .ToList();
      }
      else
      {
        var allData = InputColliderData.SelectMany(x => x.Points).ToList();
        var pointCollection = new List<List<Vector3>> { allData };
        clustersOfPoints.AddRange(pointCollection);
      }

      Debug.Log($"🔹 {clustersOfPointData.Count} clusters detected.");
      return clustersOfPoints;
    }
  }
}