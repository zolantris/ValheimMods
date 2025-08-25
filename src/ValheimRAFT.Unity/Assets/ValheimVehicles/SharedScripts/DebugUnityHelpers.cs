#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public static class DebugUnityHelpers
  {
    public static void AdaptiveDestroy(Object gameObject)
    {
#if !VALHEIM
      Object.DestroyImmediate(gameObject);
#else
      Object.Destroy(gameObject);
#endif
    }

    public static bool Vector3ArrayEqualWithTolerance(Vector3[] array1,
      Vector3[] array2,
      float tolerance = 0.0001f)
    {
      if (array1 == null || array2 == null) return false;
      if (array1.Length != array2.Length) return false;

      for (var i = 0; i < array1.Length; i++)
        if (Vector3.Distance(array1[i], array2[i]) > tolerance)
          return false;

      return true;
    }
  }
}