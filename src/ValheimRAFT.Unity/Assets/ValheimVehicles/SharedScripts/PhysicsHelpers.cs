#region

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public static class PhysicsHelpers
  {
    public static void EnableCollisionBetweenLayers(int layerA, int layerB)
    {
      Physics.IgnoreLayerCollision(layerA, layerB, false);
    }

    public static void UpdateRelativeCenterOfMass(Rigidbody rb, float yOffset)
    {
      rb.ResetCenterOfMass();
      var centerOfMass = rb.centerOfMass;
      centerOfMass = new Vector3(centerOfMass.x, yOffset, centerOfMass.z);

      rb.centerOfMass = centerOfMass;
    }

    public static void IgnoreCollidersForLists(List<Collider> targetColliders, List<Collider> collidersToIgnore)
    {
      foreach (var targetCollider in targetColliders)
      foreach (var colliderToIgnore in collidersToIgnore)
      {
        Physics.IgnoreCollision(targetCollider, colliderToIgnore, true);
      }
    }

    public static List<Collider> GetAllChildColliders(Transform colliderParent)
    {
      if (colliderParent) return new List<Collider>();
      var childColliders = colliderParent.GetComponentsInChildren<Collider>().ToList();
      return childColliders;
    }

    public static void IgnoreAllCollisionsBetweenChildren(Transform transform)
    {
      var colliders = transform.GetComponentsInChildren<Collider>(true).ToList();
      var topLevelCollider = transform.GetComponent<Collider>();

      if (topLevelCollider != null)
      {
        colliders.Add(topLevelCollider);
      }
      if (!colliders.Any()) return;

      foreach (var currentCollider in colliders)
      foreach (var targetCollider in colliders)
      {
        Physics.IgnoreCollision(currentCollider, targetCollider, true);
      }
    }


    public static void IgnoreCollisionsWithinRoot(Transform transform, Transform _root, List<Collider> _colliders)
    {
      // Compute bounds of the object
      var bounds = new Bounds(transform.position, Vector3.zero);
      foreach (var col in _colliders)
      {
        bounds.Encapsulate(col.bounds);
      }

      // Perform a sphere cast to find nearby objects
      Collider[] hitColliders = Physics.OverlapSphere(bounds.center, bounds.extents.magnitude);

      foreach (var hitCollider in hitColliders)
      {
        if (hitCollider.transform.root == _root)
        {
          // Ignore collision if the collider belongs to the same root
          foreach (var ownCollider in _colliders)
          {
            Physics.IgnoreCollision(ownCollider, hitCollider, true);
          }
        }
      }
    }
  }
}