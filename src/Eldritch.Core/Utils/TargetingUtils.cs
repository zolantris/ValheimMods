using UnityEngine;
namespace Eldritch.Core
{
  public static class TargetingUtil
  {
    /// <summary>
    ///   Checks if the "target" is looking at "me" within a certain field of view
    ///   (fov) in degrees.
    /// </summary>
    public static bool IsTargetLookingAtMe(Transform target, Transform me, float fov = 60f)
    {
      var toMe = (me.position - target.position).normalized;
      var dot = Vector3.Dot(target.forward, toMe);
      var angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
      return angle < fov * 0.5f;
    }
  }
}