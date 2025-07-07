
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public static class ColliderUtil
  {
    public static IEnumerable<Vector3> GetSamplePoints(Collider collider)
    {
      var bounds = collider.bounds;
      // Center + 6 faces
      yield return bounds.center;
      yield return bounds.center + Vector3.right * bounds.extents.x;
      yield return bounds.center - Vector3.right * bounds.extents.x;
      yield return bounds.center + Vector3.up * bounds.extents.y;
      yield return bounds.center - Vector3.up * bounds.extents.y;
      yield return bounds.center + Vector3.forward * bounds.extents.z;
      yield return bounds.center - Vector3.forward * bounds.extents.z;
    }
  }
}