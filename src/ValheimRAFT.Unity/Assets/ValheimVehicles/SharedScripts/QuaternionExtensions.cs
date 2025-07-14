
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public static class QuaternionExtensions
  {
    public static float LookSafeThreshold = 1e-6f;
    public static Quaternion LookRotationSafe(Vector3 forward, Vector3 up)
    {
      if (forward.sqrMagnitude < LookSafeThreshold)
        forward = Vector3.forward; // fallback
      if (up.sqrMagnitude < LookSafeThreshold)
        up = Vector3.up;
      return Quaternion.LookRotation(forward, up);
    }

    public static Quaternion LookRotationSafe(Vector3 forward)
    {
      if (forward.sqrMagnitude < LookSafeThreshold)
        forward = Vector3.forward; // fallback
      return Quaternion.LookRotation(forward, Vector3.up);
    }
  }
}