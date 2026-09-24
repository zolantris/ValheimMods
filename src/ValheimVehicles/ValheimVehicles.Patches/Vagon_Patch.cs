using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Patches;

/// <summary>
///   Keeps carts off a vehicle's convex hull collider.
/// </summary>
/// <remarks>
///   A vehicle's convex hull collider is a solid, filled volume spanning the whole ship, so a
///   cart parked on the deck sits about two metres inside it. Excluding the vanilla "vehicle"
///   layer on that collider covers stock carts, but not carts from other mods: OdinHorse's
///   rae_HorseCart, for instance, adds solid colliders on the "piece" and "Default" layers,
///   and the hull has to keep colliding with those layers to hit terrain and buildings at all.
///   Ignore the pairing per collider instead, for every Vagon, whatever layers it happens to use.
///
///   This runs every fixed step rather than once at spawn because both halves of the pairing
///   move. A cart restored on world load exists before the hull has been regenerated, so there
///   is nothing to ignore it against yet; and each regeneration destroys and rebuilds the hull
///   collider, which silently drops every per-pair ignore previously set against it. Watching
///   for the collider identity to change covers both, and costs a handful of integer compares
///   per cart per step in the steady state.
/// </remarks>
public class Vagon_Patch
{
  [HarmonyPatch(typeof(Vagon), "Awake")]
  [HarmonyPostfix]
  private static void Vagon_Awake_IgnoreVehicleHullCollisions(Vagon __instance)
  {
    if (__instance == null) return;
    if (__instance.GetComponent<VagonHullCollisionIgnorer>() != null) return;
    __instance.gameObject.AddComponent<VagonHullCollisionIgnorer>();
  }
}

public class VagonHullCollisionIgnorer : MonoBehaviour
{
  private Collider[] _cartColliders = Array.Empty<Collider>();
  private readonly HashSet<long> _appliedPairs = new();
  private int _hullColliderSignature;

  private void Awake()
  {
    // Includes inactive colliders: a cart's load-visualisation props are toggled by
    // Vagon.UpdateLoadVisualization as its container fills, and they carry colliders too.
    _cartColliders = GetComponentsInChildren<Collider>(true);
  }

  private void FixedUpdate()
  {
    var signature = 17;
    var sawAnyHull = false;

    foreach (var convexHullApi in ConvexHullAPI.Instances)
    {
      if (convexHullApi == null) continue;
      var hullColliders = convexHullApi.convexHullMeshColliders;
      for (var index = 0; index < hullColliders.Count; index++)
      {
        var hullCollider = hullColliders[index];
        if (hullCollider == null) continue;
        sawAnyHull = true;
        unchecked
        {
          signature = signature * 31 + hullCollider.GetInstanceID();
        }
      }
    }

    if (!sawAnyHull) return;

    // A rebuilt hull invalidates every ignore recorded against the old collider instances.
    if (signature != _hullColliderSignature)
    {
      _appliedPairs.Clear();
      _hullColliderSignature = signature;
    }

    foreach (var convexHullApi in ConvexHullAPI.Instances)
    {
      if (convexHullApi == null) continue;
      var hullColliders = convexHullApi.convexHullMeshColliders;
      for (var index = 0; index < hullColliders.Count; index++)
      {
        var hullCollider = hullColliders[index];
        if (!IsUsable(hullCollider)) continue;
        IgnoreCartCollidersAgainst(hullCollider);
      }
    }
  }

  private void IgnoreCartCollidersAgainst(Collider hullCollider)
  {
    var hullId = hullCollider.GetInstanceID();

    foreach (var cartCollider in _cartColliders)
    {
      // Physics.IgnoreCollision errors on colliders that are not currently active, so leave
      // those out of the applied set and pick them up on a later step instead.
      if (!IsUsable(cartCollider)) continue;

      var pair = ((long)hullId << 32) ^ (uint)cartCollider.GetInstanceID();
      if (!_appliedPairs.Add(pair)) continue;

      Physics.IgnoreCollision(hullCollider, cartCollider, true);
    }
  }

  private static bool IsUsable(Collider collider)
  {
    return collider != null && collider.enabled && collider.gameObject.activeInHierarchy;
  }
}
