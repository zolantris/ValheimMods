#region

  using System.Collections.Generic;
  using UnityEngine;

#endregion

  namespace ValheimVehicles.SharedScripts;

  public static class Vector3Extensions
  {
    // Extends Vector3 to allow: point1.IsCloseTo(point2, 0.1f)
    public static bool IsCloseTo(this Vector3 origin, Vector3 target, float tolerance = 0.01f)
    {
      return (origin - target).sqrMagnitude < tolerance * tolerance;
    }

    public static Vector3 Average(this List<Vector3> points)
    {
      if (points == null || points.Count == 0) return Vector3.zero;

      var sum = Vector3.zero;
      foreach (var point in points)
      {
        sum += point;
      }
      return sum / points.Count;
    }
  }