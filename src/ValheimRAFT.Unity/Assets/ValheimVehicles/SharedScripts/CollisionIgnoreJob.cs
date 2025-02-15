#region

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

#endregion

namespace ValheimVehicles.SharedScripts
{

  [BurstCompile]
  internal struct CollisionIgnoreJob : IJobParallelFor
  {
    [ReadOnly] public NativeArray<IntPtr> ColliderPointers;
    public NativeArray<IntPtrPair> CollisionPairs;

    public void Execute(int index)
    {
      var count = ColliderPointers.Length;
      var pairIndex = index * (count - 1) - index * (index - 1) / 2; // Optimized index calculation

      for (var j = index + 1; j < count; j++)
      {
        if (pairIndex < CollisionPairs.Length)
        {
          CollisionPairs[pairIndex] = new IntPtrPair(ColliderPointers[index], ColliderPointers[j]);
          pairIndex++;
        }
      }
    }
  }

// Struct to store collider pointers
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