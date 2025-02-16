#region

using System;
using Unity.Collections;
using Unity.Jobs;
// using Unity.Burst;
#endregion

namespace ValheimVehicles.SharedScripts
{
  // todo maybe we can figure out how to inject this...
  // [BurstCompile]
  internal struct CollisionIgnoreJob : IJob
  {
    [ReadOnly] public NativeArray<IntPtr> ColliderPointers;
    public NativeArray<IntPtrPair> CollisionPairs; // Fixed-size array

    public void Execute()
    {
      var count = ColliderPointers.Length;
      var pairIndex = 0; // Use a sequential index to prevent race conditions

      for (var i = 0; i < count; i++)
      {
        for (var j = i + 1; j < count; j++)
        {
          if (pairIndex >= CollisionPairs.Length) return; // Prevent out-of-bounds writes
          CollisionPairs[pairIndex] = new IntPtrPair(ColliderPointers[i], ColliderPointers[j]);
          pairIndex++;
        }
      }
    }
  }

  public struct IntPtrPair
  {
    public IntPtr ColliderA;
    public IntPtr ColliderB;

    public IntPtrPair(IntPtr a, IntPtr b)
    {
      ColliderA = a;
      ColliderB = b;
    }
  }
}