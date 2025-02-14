#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{

  public static class RigidbodyUtils
  {
    /// <summary>
    /// Recenters the Rigidbody to its automatic center of mass or a custom override point while maintaining physics stability.
    /// </summary>
    /// <param name="rb">The Rigidbody to recenter.</param>
    /// <param name="overrideCenter">Optional world position to use as the new center. If null, the Rigidbody's center of mass is used.</param>
    public static void RecenterRigidbody(Rigidbody rb, Vector3? overrideCenter = null)
    {
      if (!rb || rb.IsSleeping()) return;

      // Get the target center position (either calculated COM or override)
      var worldCOM = overrideCenter ?? rb.worldCenterOfMass;
      var delta = rb.transform.position - worldCOM;

      // Collect all child transforms and joints
      var rbTransform = rb.transform;
      List<Transform> children = new();
      List<ConfigurableJoint> joints = new();

      foreach (Transform child in rbTransform)
      {
        if (child.TryGetComponent(out ConfigurableJoint joint))
          joints.Add(joint);

        children.Add(child);
      }

      // Move the Rigidbody
      rb.transform.position = worldCOM;

      // Adjust children to maintain local positions
      foreach (var child in children)
      {
        child.position += delta;
      }

      // Update ConfigurableJoints
      foreach (var joint in joints)
      {
        joint.connectedAnchor += delta;
      }

      // Preserve velocity consistency
      rb.velocity += Vector3.Cross(rb.angularVelocity, delta);
    }
  }
}