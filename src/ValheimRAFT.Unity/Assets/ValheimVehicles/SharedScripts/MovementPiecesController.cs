#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  /// <summary>
  /// This is meant to be extended for VehiclePiecesController, allowing non-Valheim asset replication of movement without all other piece logic.
  /// </summary>
  public class MovementPiecesController : MonoBehaviour
  {
    public static GameObject m_convexHullJobHandlerObj;
    public static float RebuildPieceMinDelay = 4f;
    public static float RebuildPieceMaxDelay = 60f;
    internal static float RebuildBoundsDelayPerPiece = 0.02f;
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

    public float clusterThreshold = 2f;

    public readonly Dictionary<GameObject, PrefabPieceData> prefabPieceDataItems = new();

    internal bool _isInitialPieceActivationComplete;
    internal int _lastPieceRevision = 1;

    // for bounds and piece building revisions.
    internal int _lastRebuildItemCount;
    // revisions must be different
    internal int _lastRebuildPieceRevision = -1;

    internal float _lastRebuildTime;
    internal Coroutine? _rebuildBoundsTimer;

    // collision logic
    internal bool _shouldUpdatePieceColliders;
    internal bool _shouldUpdateVehicleColliders;
    [CanBeNull] internal VehicleWheelController WheelController;

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
      if (_rebuildBoundsTimer != null)
      {
        StopCoroutine(_rebuildBoundsTimer);
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
        m_convexHullJobHandlerObj = new GameObject("ConvexHullJobHandler");
      }
#endif
      if (m_convexHullJobHandler != null || m_convexHullJobHandlerObj == null) return;

      if (ConvexHullJobHandler.Instance != null)
      {
        m_convexHullJobHandler = ConvexHullJobHandler.Instance;
        return;
      }

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
      var prefabPieceData = new PrefabPieceData(piece);

      // ReSharper disable once CanSimplifyDictionaryLookupWithTryAdd
      if (!prefabPieceDataItems.ContainsKey(piece))
      {
        prefabPieceDataItems.Add(piece, prefabPieceData);
      }

      pieceDataChangeIndex++;


#if UNITY_EDITOR
      if (!_isInitialPieceActivationComplete)
      {
        _isInitialPieceActivationComplete = true;
      }
#endif

    }

    public void OnPieceRemoved(GameObject piece)
    {
#if UNITY_EDITOR
      piece.transform.SetParent(null);
#endif
      if (!prefabPieceDataItems.TryGetValue(piece, out var prefabPieceData)) return;

      // ✅ Avoid LINQ allocation by using `foreach`
      var shouldRebuild = true;

      if (convexHullMeshes.Count > 0)
      {
        foreach (var meshCollider in m_convexHullAPI.convexHullMeshColliders)
        {
          if (prefabPieceData.AreAllPointsValid(meshCollider, transform, convexAPIThresholdDistance))
          {
            shouldRebuild = false;
            break;
          }
        }
      }

      if (shouldRebuild)
      {
        RequestBoundsRebuild();
      }

      prefabPieceDataItems.Remove(piece);
      pieceDataChangeIndex++;
    }

    /// <summary>
    /// Requests a delayed convex hull rebuild.
    /// </summary>
    public virtual void RequestBoundsRebuild()
    {
      // if we are already queuing up an update, we can skip any additional requests.
      if (_rebuildBoundsTimer != null)
      {
        return;
      }
      // No need to run the revision
      if (_lastPieceRevision == _lastRebuildPieceRevision)
      {
        return;
      }

      // we do not rebuild bounds until this generation is completed
      if (!_isInitialPieceActivationComplete)
      {
        return;
      }

      _rebuildBoundsTimer = StartCoroutine(RebuildBoundsThrottleRoutine(() => RebuildBounds()));
    }

    /// <summary>
    /// Additional logic is implemented in the VehiclePiecesController
    /// Local method is important for
    /// - updating hashes
    /// Some Code here will be used for testing within unity editor environment
    /// </summary>
    /// <param name="isForced"></param>
    public virtual void RebuildBounds(bool isForced = false)
    {
      if (!isActiveAndEnabled) return;

      _shouldUpdateVehicleColliders = true;
      _lastRebuildTime = Time.fixedTime;
      _lastRebuildItemCount = prefabPieceDataItems.Count;

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
      var items = prefabPieceDataItems.Keys.Where(x => x != null).ToArray();
      m_meshClusterComponent.GenerateCombinedMeshes(items);

      if (WheelController != null)
      {
        var bounds = m_convexHullAPI.GetConvexHullBounds(true);
        WheelController.Initialize(bounds);
      }
    }

    internal virtual int GetPieceCount()
    {
      return prefabPieceDataItems.Count;
    }

    /// <summary>
    /// This is virtual in case there needs to be an override to this routine.
    /// </summary>
    /// <param name="onRebuildReadyCallback"></param>
    /// <returns></returns>
    internal virtual IEnumerator RebuildBoundsThrottleRoutine(Action onRebuildReadyCallback)
    {
      var hasMinimumWait = _lastRebuildTime + 40 < Time.fixedTime && Mathf.Abs(prefabPieceDataItems.Count - _lastRebuildItemCount) > 20f;
      if (hasMinimumWait)
      {
        yield return new WaitForSeconds(RebuildPieceMinDelay);
        _rebuildBoundsTimer = null;
        onRebuildReadyCallback.Invoke();
        yield break;
      }

      var pieceCount = GetPieceCount();
      if (pieceCount <= 0)
      {
        _rebuildBoundsTimer = null;
        yield break;
      }

      // in case the local m_nviewPieces are somehow larger we check.
      var allItems = Math.Max(prefabPieceDataItems.Count, pieceCount);

      // these calcs ensure we rebuild if there is a significant difference between currentItems
      var allVsCurrentRebuildItems = allItems - _lastRebuildItemCount;
      var newItemsDiff = Mathf.Abs(allVsCurrentRebuildItems);
      var itemDiffRatio = newItemsDiff / Mathf.Min(allItems, 1);

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

      _rebuildBoundsTimer = null;
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
      foreach (var prefabPieceData in prefabPieceDataItems.Values)
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

    /// <summary>
    /// Requests convex hull generation for a tracked prefab.
    /// </summary>
    public void GenerateConvexHull(float clusterThreshold, Action? callback)
    {
      // ✅ Avoid extra allocations by passing `IEnumerable` instead of `.ToList()`
      var prefabDataItems = prefabPieceDataItems.Values;
      ConvexHullJobHandler.ScheduleConvexHullJob(m_convexHullAPI, new List<PrefabPieceData>(prefabDataItems), clusterThreshold, () =>
      {
        m_convexHullAPI.PostGenerateConvexMeshes();
        Debug.Log("✅ Convex Hull Generation Complete!");
        IgnoreAllCollisionsFromConvexColliders();
        callback?.Invoke();
      });
    }
  }
}