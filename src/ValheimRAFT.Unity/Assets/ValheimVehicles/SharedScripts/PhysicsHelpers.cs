using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ValheimVehicles.SharedScripts
{
  public static class PhysicsHelpers
  {
    public static void EnableCollisionBetweenLayers(int layerA, int layerB)
    {
      Physics.IgnoreLayerCollision(layerA, layerB, false);
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
  }
}