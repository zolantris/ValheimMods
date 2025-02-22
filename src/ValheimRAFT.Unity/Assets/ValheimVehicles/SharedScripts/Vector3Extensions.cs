#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{

  public static class Vector3Extensions
  {
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
}