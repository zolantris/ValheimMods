#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts
  {
    /// <summary>
    /// This is meant to be extended for VehiclePiecesController, allowing non-Valheim asset replication of movement without all other piece logic.
    /// </summary>
    public class BasePiecesController : MonoBehaviour
    {

      public enum HullGenerationMode
      {
        Basic, // for a simple bounds based mesh. This will not create any additional colliders besides a CapsuleCollider for the vehicle.
        ConvexHullForeground, // works but a bit Heavy. Very accurate collisions.
        ConvexHullBackground // not supported yet
      }

      public static GameObject m_convexHullJobHandlerObj;
      public static float RebuildPieceMinDelay = 0.1f;
      public static float RebuildPieceMaxDelay = 60f;
      internal static float RebuildBoundsDelayPerPiece = 0.02f;

      public static bool isBasicHullCalculation = false;

      public static int clusterThreshold = 500;
      public Rigidbody m_syncRigidbody;
      public Rigidbody m_localRigidbody;

      public bool m_shouldSync = true;
      public Vector3 convexAPIThresholdDistance = Vector3.one;

      public List<Collider> vehicleCollidersToIgnore = new();
      public List<Collider> convexHullColliders = new();
      public List<MeshCollider> convexHullMeshColliders = new();
      public ConvexHullAPI m_convexHullAPI;
      public MeshClusterController m_meshClusterComponent;
      public int pieceDataChangeIndex;

      // convexJobHandler is used for scheduling all piece updates.
      public ConvexHullJobHandler m_convexHullJobHandler;

      public HullGenerationMode selectedHullGenerationMode = HullGenerationMode.ConvexHullForeground;

      public readonly Dictionary<GameObject, PrefabPieceData> m_prefabPieceDataItems = new();
      internal int _lastPieceRevision = 1;

      // for bounds and piece building revisions.
      internal int _lastRebuildItemCount;
      // revisions must be different
      internal int _lastRebuildPieceRevision = -1;

      internal float _lastRebuildTime;
      internal Coroutine? _rebuildBoundsRoutineInstance;

      // collision logic
      internal bool _shouldUpdatePieceColliders;
      internal bool _shouldUpdateVehicleColliders;

      internal bool isInitialPieceActivationComplete;

      private readonly ConvexHullCalculator m_convexHullCalculator = new();
      private List<Vector3> normals = new();
      private List<int> tris = new();

      private List<Vector3> verts = new();
      public VehicleWheelController? WheelController { get; set; }

      public List<GameObject> convexHullMeshes =>
        m_convexHullAPI.convexHullMeshes;
      public List<GameObject> convexHullTriggerMeshes =>
        m_convexHullAPI.convexHullTriggerMeshes;

      public virtual void Awake()
      {
        m_convexHullAPI = GetComponent<ConvexHullAPI>();
        if (!m_convexHullAPI)
        {
          m_convexHullAPI = gameObject.AddComponent<ConvexHullAPI>();
        }
        if (!m_meshClusterComponent)
        {
          m_meshClusterComponent = gameObject.AddComponent<MeshClusterController>();
        }

        m_localRigidbody = GetComponent<Rigidbody>();
        TryAddConvexHullJobHandler();
      }

#if UNITY_EDITOR
      public void FixedUpdate()
    {
      CustomFixedUpdate(Time.fixedDeltaTime);
    }
#endif

      public virtual void OnDisable()
      {
        if (_rebuildBoundsRoutineInstance != null)
        {
          StopCoroutine(_rebuildBoundsRoutineInstance);
          _rebuildBoundsRoutineInstance = null;
        }
      }

      public virtual void CustomFixedUpdate(float deltaTime)
      {
        if (m_shouldSync)
        {
          m_localRigidbody.Move(m_syncRigidbody.position, m_syncRigidbody.rotation);
        }
      }

      /// <summary>
      /// Adds the jobhandler to any gameobject supplied. In the VehiclePiecesController this is the ZNetScene but in Unity.Editor this can be any object.
      /// </summary>
      public void TryAddConvexHullJobHandler()
      {
#if UNITY_EDITOR
      if (m_convexHullJobHandlerObj == null)
      {
        m_convexHullJobHandlerObj = gameObject;
      }
#endif
        if (m_convexHullJobHandler != null || m_convexHullJobHandlerObj == null) return;

        m_convexHullJobHandler = m_convexHullJobHandlerObj.GetComponent<ConvexHullJobHandler>();
        if (m_convexHullJobHandler == null)
        {
          m_convexHullJobHandler = m_convexHullJobHandlerObj.AddComponent<ConvexHullJobHandler>();
        }
      }

      public void OnPieceAdded(GameObject piece)
      {
#if UNITY_EDITOR
      piece.transform.SetParent(transform, false);
#endif
        var prefabPieceData = new PrefabPieceData(piece, Allocator.Persistent);

        // ReSharper disable once CanSimplifyDictionaryLookupWithTryAdd
        if (!m_prefabPieceDataItems.ContainsKey(piece))
        {
          m_prefabPieceDataItems.Add(piece, prefabPieceData);
        }
        pieceDataChangeIndex++;


#if UNITY_EDITOR
      if (!isInitialPieceActivationComplete)
      {
        isInitialPieceActivationComplete = true;
      }
#endif

      }

      public void OnPieceRemoved(GameObject piece)
      {
#if UNITY_EDITOR
      piece.transform.SetParent(null);
#endif
        if (!m_prefabPieceDataItems.TryGetValue(piece, out var prefabPieceData)) return;

        // ✅ Avoid LINQ allocation by using `foreach`
        var shouldRebuild = true;

        // if (convexHullMeshes.Count > 0)
        // {
        //   foreach (var meshCollider in m_convexHullAPI.convexHullMeshColliders)
        //   {
        //     // if (prefabPieceData.AreAllPointsValid(meshCollider, transform, convexAPIThresholdDistance))
        //     // {
        //     // shouldRebuild = false;
        //     // break;
        //     // }
        //   }
        // }

        if (shouldRebuild)
        {
          RequestBoundsRebuild();
        }

        m_prefabPieceDataItems.Remove(piece);
        pieceDataChangeIndex++;
      }

      /// <summary>
      /// Requests a delayed convex hull rebuild.
      /// </summary>
      public virtual void RequestBoundsRebuild()
      {
        // if we are already queuing up an update, we can skip any additional requests.
        if (_rebuildBoundsRoutineInstance != null)
        {
          return;
        }
        // No need to run the revision
        if (_lastPieceRevision == _lastRebuildPieceRevision)
        {
          return;
        }

        // we do not rebuild bounds until this generation is completed
        if (!isInitialPieceActivationComplete)
        {
          return;
        }

        _rebuildBoundsRoutineInstance = StartCoroutine(RebuildBoundsThrottleRoutine(() => RebuildBounds()));
      }

      /// <summary>
      /// - This RebuildBounds must be called within the override if overridden. 
      /// - Additional logic is implemented in the VehiclePiecesController
      /// </summary>
      /// <param name="isForced"></param>
      public virtual void RebuildBounds(bool isForced = false)
      {
        _rebuildBoundsRoutineInstance = null;
        if (!isActiveAndEnabled) return;

        _shouldUpdateVehicleColliders = true;
        _lastRebuildTime = Time.fixedTime;
        _lastRebuildItemCount = m_prefabPieceDataItems.Count;

        if (_lastRebuildPieceRevision != _lastPieceRevision || isForced)
        {
          _shouldUpdatePieceColliders = true;
        }

        // always update them for now until we can get smarter with convex collider detecting which ones have been added if any.
        _shouldUpdateVehicleColliders = true;

        _lastRebuildPieceRevision = _lastPieceRevision;

        GenerateConvexHull(clusterThreshold, OnConvexHullGenerated);
      }

      public virtual void OnConvexHullGenerated()
      {
        var items = m_prefabPieceDataItems.Keys.Where(x => x != null).ToArray();
        m_meshClusterComponent.GenerateCombinedMeshes(items);

        if (WheelController != null)
        {
          var bounds = m_convexHullAPI.GetConvexHullBounds(true);
          WheelController.Initialize(bounds);
        }
      }

      public virtual int GetPieceCount()
      {
        return m_prefabPieceDataItems.Count;
      }

      /// <summary>
      /// This is virtual in case there needs to be an override to this routine.
      /// </summary>
      /// <param name="onRebuildReadyCallback"></param>
      /// <returns></returns>
      internal virtual IEnumerator RebuildBoundsThrottleRoutine(Action onRebuildReadyCallback)
      {
        var hasMinimumWait = _lastRebuildTime + 40 < Time.fixedTime && Mathf.Abs(m_prefabPieceDataItems.Count - _lastRebuildItemCount) > 20f;
        if (hasMinimumWait)
        {
          yield return new WaitForSeconds(RebuildPieceMinDelay);
          _rebuildBoundsRoutineInstance = null;
          onRebuildReadyCallback.Invoke();
          yield break;
        }

        var pieceCount = GetPieceCount();
        if (pieceCount <= 0)
        {
          _rebuildBoundsRoutineInstance = null;
          yield break;
        }

        // in case the local m_nviewPieces are somehow larger we check.
        var allItems = Math.Max(m_prefabPieceDataItems.Count, pieceCount);

        // these calcs ensure we rebuild if there is a significant difference between currentItems
        var allVsCurrentRebuildItems = allItems - _lastRebuildItemCount;
        var newItemsDiff = Mathf.Abs(allVsCurrentRebuildItems);
        var itemDiffRatio = newItemsDiff / Mathf.Max(allItems, 1);

        // if more than 20 items are added or the vehicle grows in item size by 10% we need to update bounds sooner.
        if (newItemsDiff > 20 || itemDiffRatio > 0.1f)
        {
          yield return new WaitForSeconds(RebuildPieceMinDelay);
        }
        else
        {
          var additionalWaitTimeFromItems = allItems * RebuildBoundsDelayPerPiece;
          var timeToWait = Mathf.Clamp(additionalWaitTimeFromItems, RebuildPieceMinDelay, RebuildPieceMaxDelay);
          yield return new WaitForSeconds(timeToWait);
        }

        _rebuildBoundsRoutineInstance = null;
        onRebuildReadyCallback.Invoke();
      }

      /// <summary>
      /// Updates collision ignores for a specific `PrefabPieceData` instance.
      /// </summary>
      public void UpdateCollidersIgnoresOnChange(PrefabPieceData prefabPieceData)
      {
        // foreach (var prefabCollider in prefabPieceData.AllColliders)
        // {
        //     if (prefabCollider == null) continue;
        //
        //     foreach (var vehicleCollider in vehicleCollidersToIgnore)
        //     {
        //         if (vehicleCollider == null) continue;
        //         Physics.IgnoreCollision(prefabCollider, vehicleCollider, true);
        //     }
        //
        //     foreach (var convexCollider in m_convexHullAPI.convexHullMeshColliders)
        //     {
        //         if (convexCollider == null) continue;
        //         Physics.IgnoreCollision(prefabCollider, convexCollider, true);
        //     }
        // }
      }

      /// <summary>
      /// Ignores all collisions between convex colliders and tracked vehicle colliders.
      /// </summary>
      public void IgnoreAllCollisionsFromConvexColliders()
      {
        // ✅ Precompute `AllColliders` to avoid reallocation
        var allColliders = new HashSet<Collider>();
        foreach (var prefabPieceData in m_prefabPieceDataItems.Values)
        {
          allColliders.UnionWith(prefabPieceData.AllColliders);
        }

        foreach (var convexCollider in m_convexHullAPI.convexHullMeshColliders)
        {
          if (convexCollider == null) continue;

          foreach (var prefabCollider in allColliders)
          {
            if (prefabCollider == null) continue;
            Physics.IgnoreCollision(convexCollider, prefabCollider, true);
          }

          foreach (var vehicleCollider in vehicleCollidersToIgnore)
          {
            if (vehicleCollider == null) continue;
            Physics.IgnoreCollision(convexCollider, vehicleCollider, true);
          }
        }
      }

      public void GenerateConvexHull(float maxClusters, Action? callback)
      {
        switch (selectedHullGenerationMode)
        {

          case HullGenerationMode.Basic:
            break;
          case HullGenerationMode.ConvexHullForeground:
            GenerateConvexHullOnMainThread(clusterThreshold, callback);
            break;
          case HullGenerationMode.ConvexHullBackground:
            GenerateConvexHullOnBackgroundThread(clusterThreshold, callback);
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
        // todo get this background thread working
        // GenerateConvexHullOnBackgroundThread(clusterThreshold, callback);
      }
      /// <summary>
      /// </summary>
      /// TODO this method should be removed once BackgroundThread is optimized.
      /// 
      /// <param name="maxClusters"></param>
      /// <param name="callback"></param>
      public void GenerateConvexHullOnMainThread(float maxClusters, Action? callback)
      {
        verts.Clear();
        tris.Clear();
        normals.Clear();

        var points = new List<Vector3>();
        if (isBasicHullCalculation)
        {
          points = m_prefabPieceDataItems.Values.Where(x => x.ColliderPointData != null).Select(x => x.ColliderPointData!.Value.LocalBounds.center).ToList();
        }
        else
        {
          foreach (var item in m_prefabPieceDataItems.Values)
          {
            var itemCollectionData = item.ColliderPointData;
            if (item.IsSwivelChild) continue;
            if (item.Prefab == null || item.Prefab.name.StartsWith(PrefabNames.SwivelPrefabName)) continue;
            if (itemCollectionData == null) continue;
            if (itemCollectionData.Value.Points.Length == 0)
            {
              LoggerProvider.LogDebug($"Unexpected Points list has zero points for item: {item}");
              continue;
            }

            try
            {
              foreach (var point in itemCollectionData.Value.Points)
              {
                // It's possible to get a NRE when adding collections so adding this to guard it.
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (point == null)
                {
                  LoggerProvider.LogDebug($"ValheimRAFT: Point is null for item: {item}");
                  continue;
                }
                points.Add(point);
              }
            }
            catch (Exception e)
            {
              LoggerProvider.LogError($"ValheimRAFT: Error while adding points for item: {item}. Error \n {e}");
            }
          }
        }

        // We cannot generate a convex collider with so view points. This is not a good situation to be in.
        // todo add a fallback that uses a simple box collider in this case. (IE THE BASIC RENDER)
        if (points.Count <= 4 || !m_convexHullCalculator.GenerateHull(points, false, ref verts, ref tris, ref normals, out var hasBailed))
        {
          LoggerProvider.LogError("Points cannot be less than 4. This is likely an error with the mod or the vehicle only contains a piece without collider points.");
          callback?.Invoke();
          return;
        }

        m_convexHullAPI.GenerateMeshFromConvexOutput(verts.ToArray(), tris.ToArray(), normals.ToArray(), 0);

        Debug.Log("✅ Convex Hull Generation Complete!");
        m_convexHullAPI.PostGenerateConvexMeshes();
        IgnoreAllCollisionsFromConvexColliders();
        callback?.Invoke();
      }

      /// <summary>
      /// Requests convex hull generation for a tracked prefab.
      /// TODO must be optimized so that native arrays are not nested and pass references to those native array points so we do not allocate
      /// </summary>
      public void GenerateConvexHullOnBackgroundThread(float maxClusters, Action? callback)
      {
        // ✅ Avoid extra allocations by passing `IEnumerable` instead of `.ToList()`
        var prefabDataItems = m_prefabPieceDataItems.Values.ToArray();
        m_convexHullJobHandler.ScheduleConvexHullJob(m_convexHullAPI, prefabDataItems, maxClusters, () =>
        {
          m_convexHullAPI.PostGenerateConvexMeshes();
          Debug.Log("✅ Convex Hull Generation Complete!");
          IgnoreAllCollisionsFromConvexColliders();
          callback?.Invoke();
        });
      }
    }
  }