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
    private List<GCHandle> _gcHandles = new(); // Track GCHandles for cleanup
    private bool _isVehicleDestroyed;

    private void OnDestroy()
    {
      _isVehicleDestroyed = true; // Mark vehicle for cleanup
      Cleanup(); // Release resources
    }

    public void AddObjectToVehicle(GameObject obj)
    {
      if (_isVehicleDestroyed || obj == null) return; // Avoid processing on destroyed vehicles

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
      if (_isVehicleDestroyed || obj == null) return; // Skip removal on destroyed objects

      var removeColliders = obj.GetComponentsInChildren<Collider>();

      foreach (var collider in removeColliders)
      {
        _colliders.Remove(collider);
      }
    }

    private void ProcessIgnoreCollisions()
    {
      if (_isVehicleDestroyed || _colliders.Count < 2) return; // Prevent processing if the vehicle is gone

      var colliderCount = _colliders.Count;
      var pairCount = colliderCount * (colliderCount - 1) / 2;

      var colliderPtrs = new NativeArray<IntPtr>(colliderCount, Allocator.TempJob);
      var collisionPairs = new NativeArray<IntPtrPair>(pairCount, Allocator.TempJob);

      try
      {
        // Clear GCHandle tracking before creating new ones
        Cleanup();
        _gcHandles.Clear();

        // Convert Colliders to IntPtr & store handles for cleanup
        for (var i = 0; i < colliderCount; i++)
        {
          if (_colliders[i] == null) continue; // Skip null colliders

          var handle = GCHandle.Alloc(_colliders[i], GCHandleType.Weak);
          _gcHandles.Add(handle);
          colliderPtrs[i] = GCHandle.ToIntPtr(handle);
        }

        var job = new CollisionIgnoreJob
        {
          ColliderPointers = colliderPtrs,
          CollisionPairs = collisionPairs
        };

        var handleJob = job.Schedule(colliderCount, 1);
        handleJob.Complete();

        // Apply ignore collisions on the main thread
        for (var i = 0; i < pairCount; i++)
        {
          var pair = collisionPairs[i];

          if (_isVehicleDestroyed || pair.ColliderA == IntPtr.Zero || pair.ColliderB == IntPtr.Zero)
            continue; // Skip invalid pairs

          var handleA = GCHandle.FromIntPtr(pair.ColliderA);
          var handleB = GCHandle.FromIntPtr(pair.ColliderB);

          if (!handleA.IsAllocated || !handleB.IsAllocated)
            continue; // Skip if handle was lost

          var colliderA = handleA.Target as Collider;
          var colliderB = handleB.Target as Collider;

          if (colliderA != null && colliderB != null)
          {
            Physics.IgnoreCollision(colliderA, colliderB, true);
          }
        }
      }
      finally
      {
        // Ensure cleanup
        Cleanup();
        colliderPtrs.Dispose();
        collisionPairs.Dispose();
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
      _colliders.RemoveAll(c => c == null); // Remove null colliders
    }
  }
}