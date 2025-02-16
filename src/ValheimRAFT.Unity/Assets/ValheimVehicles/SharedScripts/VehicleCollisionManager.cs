#region

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{

  public class VehicleCollisionManager : MonoBehaviour
  {
    private List<Collider> _colliders = new();
    private JobHandle _currentJob;
    private List<GCHandle> _gcHandles = new(); // Track GCHandles for cleanup
    private bool _isProcessing;
    private bool _isVehicleDestroyed;

    private void OnDestroy()
    {
      _isVehicleDestroyed = true;
      Cleanup();
    }

    public void AddObjectToVehicle(GameObject obj)
    {
      if (_isVehicleDestroyed || obj == null) return;

      var newColliders = obj.GetComponentsInChildren<Collider>();
      foreach (var collider in newColliders)
      {
        if (collider != null && !_colliders.Contains(collider))
        {
          _colliders.Add(collider);
        }
      }

      ProcessIgnoreCollisions();
    }

    public void RemoveObjectFromVehicle(GameObject obj)
    {
      if (_isVehicleDestroyed || obj == null) return;

      var removeColliders = obj.GetComponentsInChildren<Collider>();
      foreach (var collider in removeColliders)
      {
        _colliders.Remove(collider);
      }

      Cleanup();
    }

    private void ProcessIgnoreCollisions()
    {
      if (_isVehicleDestroyed || _colliders.Count < 2 || _isProcessing) return;

      _isProcessing = true;

      var colliderCount = _colliders.Count;
      var pairCount = colliderCount * (colliderCount - 1) / 2;

      var colliderPtrs = new NativeArray<IntPtr>(colliderCount, Allocator.TempJob);
      var collisionPairs = new NativeArray<IntPtrPair>(pairCount, Allocator.TempJob);

      try
      {
        Cleanup();
        _gcHandles.Clear();

        for (var i = 0; i < colliderCount; i++)
        {
          if (_colliders[i] == null)
          {
            _colliders.RemoveAt(i--);
            continue;
          }

          var handle = GCHandle.Alloc(_colliders[i], GCHandleType.Weak);
          _gcHandles.Add(handle);
          colliderPtrs[i] = GCHandle.ToIntPtr(handle);
        }

        var job = new CollisionIgnoreJob
        {
          ColliderPointers = colliderPtrs,
          CollisionPairs = collisionPairs
        };

        _currentJob = job.Schedule();
        _currentJob.Complete(); // Ensure job is done before processing results

        for (var i = 0; i < pairCount; i++)
        {
          var pair = collisionPairs[i];

          if (_isVehicleDestroyed || pair.ColliderA == IntPtr.Zero || pair.ColliderB == IntPtr.Zero)
            continue;

          var handleA = GCHandle.FromIntPtr(pair.ColliderA);
          var handleB = GCHandle.FromIntPtr(pair.ColliderB);

          if (!handleA.IsAllocated || !handleB.IsAllocated)
            continue;

          var colliderA = handleA.Target as Collider;
          var colliderB = handleB.Target as Collider;

          if (colliderA != null && colliderB != null)
          {
            Physics.IgnoreCollision(colliderA, colliderB, true);
          }
        }
      }
      catch (Exception e)
      {
        Debug.LogError($"VehicleCollisionManager encountered an error: {e.Message}");
      }
      finally
      {
        Cleanup();
        colliderPtrs.Dispose();
        collisionPairs.Dispose();
        _isProcessing = false;
      }
    }

    private void Cleanup()
    {
      foreach (var handle in _gcHandles)
      {
        if (handle.IsAllocated)
        {
          handle.Free();
        }
      }
      _gcHandles.Clear();
      _colliders.RemoveAll(c => c == null);
    }
  }

}