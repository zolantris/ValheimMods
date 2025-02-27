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
      private const int BatchSize = 5000; // Optimal for 100K+ colliders

      private List<Collider> _colliders = new();
      private List<GCHandle> _gcHandles = new();
      private bool _isProcessing;
      private bool _isVehicleDestroyed;
      private NativeArray<IntPtr> _colliderPtrs;
      private NativeArray<IntPtrPair> _collisionPairs;
      private JobHandle _currentJob;

      private void OnDestroy()
      {
          _isVehicleDestroyed = true;
          Cleanup();
      }

      public void AddColliderToVehicle(Collider collider)
      {
          AddColliderToVehicle(collider, false);
      }

      public void AddColliderToVehicle(Collider collider, bool shouldUpdate)
      {
          if (collider != null && !_colliders.Contains(collider))
          {
              _colliders.Add(collider);
          }
          if (shouldUpdate)
          {
              ProcessIgnoreCollisions();
          }
      }

      public void AddListOfColliders(List<Collider> colliders, bool shouldUpdate = false)
      {
          colliders.ForEach(AddColliderToVehicle);
          if (shouldUpdate)
          {
              ProcessIgnoreCollisions();
          }
      }

      public void AddListOfColliders(List<WheelCollider> colliders, bool shouldUpdate = false)
      {
          colliders.ForEach(AddColliderToVehicle);
          if (shouldUpdate)
          {
              ProcessIgnoreCollisions();
          }
      }

      public void AddObjectToVehicle(GameObject obj, bool shouldUpdate = false)
      {
          if (_isVehicleDestroyed || obj == null) return;

          var newColliders = obj.GetComponentsInChildren<Collider>();
          foreach (var collider in newColliders)
          {
              AddColliderToVehicle(collider);
          }
          if (shouldUpdate)
          {
              ProcessIgnoreCollisions();
          }  
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

          Cleanup();

          var colliderCount = _colliders.Count;
          var pairCount = colliderCount * (colliderCount - 1) / 2;

          if (colliderCount < 2 || pairCount <= 0)
          {
              Debug.LogWarning("⚠ Skipping collision processing: Not enough colliders.");
              _isProcessing = false;
              return;
          }

          Debug.Log($"🚀 Processing Ignore Collisions: colliderCount={colliderCount}, pairCount={pairCount}");

          _colliderPtrs = new NativeArray<IntPtr>(colliderCount, Allocator.Persistent);
          _collisionPairs = new NativeArray<IntPtrPair>(pairCount, Allocator.Persistent);

          _gcHandles.Clear();
          var validIndex = 0;

          // Ensure valid collider storage
          for (var i = 0; i < _colliders.Count; i++)
          {
              if (_colliders[i] == null)
                  continue;

              var handle = GCHandle.Alloc(_colliders[i], GCHandleType.Weak);
              _gcHandles.Add(handle);
              _colliderPtrs[validIndex++] = GCHandle.ToIntPtr(handle);
          }

          // 🔹 Schedule batched processing
          var batchCount = Mathf.CeilToInt((float)colliderCount / BatchSize);
          JobHandle previousJob = default;

          for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
          {
              var start = batchIndex * BatchSize;
              var end = Mathf.Min(start + BatchSize, colliderCount);
              var length = end - start;

              if (start >= colliderCount || length <= 0)
              {
                  Debug.LogWarning($"⚠ Skipping batch {batchIndex}: start={start}, length={length}, colliderCount={colliderCount}");
                  continue; // 🚨 Prevents invalid subarray calls
              }

              var pairsInBatch = length * (length - 1) / 2;
              if (pairsInBatch <= 0) continue; // 🚨 Prevents empty subarray

              Debug.Log($"🔍 Scheduling job {batchIndex}: start={start}, length={length}, pairsInBatch={pairsInBatch}");

              var job = new CollisionIgnoreJob
              {
                  ColliderPointers = _colliderPtrs.GetSubArray(start, length),
                  CollisionPairs = _collisionPairs.GetSubArray(start, pairsInBatch)
              };

              previousJob = batchIndex == 0 ? job.Schedule() : job.Schedule(previousJob);
          }

          _currentJob = previousJob;
          JobHandle.ScheduleBatchedJobs();
      }

      private void LateUpdate()
      {
          if (_isProcessing && _currentJob.IsCompleted)
          {
              _currentJob.Complete();
              ApplyCollisionIgnores();
          }
      }

      private void ApplyCollisionIgnores()
      {
          var pairCount = _collisionPairs.Length;

          for (var i = 0; i < pairCount; i++)
          {
              var pair = _collisionPairs[i];

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

          Cleanup();
          _isProcessing = false;
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

          if (_colliderPtrs.IsCreated) _colliderPtrs.Dispose();
          if (_collisionPairs.IsCreated) _collisionPairs.Dispose();
      }
  }
}