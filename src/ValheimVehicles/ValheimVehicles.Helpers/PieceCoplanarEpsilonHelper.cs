using UnityEngine;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Helpers;

using UnityEngine;
using System.Collections.Generic;

public static class PieceCoplanarEpsilonHelper
{
  // Main check: finds any overlapping pieces by MeshRenderer bounds (not colliders)
  public static bool HasCoplanarOverlap(
    MeshRenderer thisRenderer,
    GameObject thisObj,
    out List<MeshRenderer> overlappingRenderers,
    float epsilon = 0.0001f,
    int layerMask = ~0)
  {
    overlappingRenderers = new List<MeshRenderer>();
    if (!thisRenderer) return false;
    var bounds = thisRenderer.bounds;
    var center = bounds.center;
    var extents = bounds.extents;
    var orientation = thisObj.transform.rotation;

    var overlaps = Physics.OverlapBox(center, extents, orientation, layerMask, QueryTriggerInteraction.Ignore);
    var any = false;

    foreach (var col in overlaps)
    {
      if (col == null) continue;
      var otherRenderer = col.GetComponentInParent<MeshRenderer>();
      if (otherRenderer == null || otherRenderer == thisRenderer) continue;
      var otherObj = otherRenderer.gameObject;
      if (otherObj == thisObj) continue;

      // Check if mesh bounds intersect and centers are coplanar within epsilon
      var otherBounds = otherRenderer.bounds;
      if (bounds.Intersects(otherBounds) &&
          Mathf.Abs(bounds.center.y - otherBounds.center.y) < epsilon * 0.5f)
      {
        overlappingRenderers.Add(otherRenderer);
        any = true;
      }
    }
    return any;
  }

  // 1. Classic: Mutate transform (original method, call as before)
  public static void ResolveCoplanarityByTransform(
    GameObject placedPiece,
    float epsilon = 0.0001f,
    int maxBands = 16,
    int layerMask = ~0,
    float normalEpsilon = 0.01f,
    int maxChecks = 50)
  {
    var renderer = placedPiece.GetComponentInChildren<MeshRenderer>();
    if (!renderer) return;

    // Cache original position!
    var basePos = placedPiece.transform.position;

    for (var band = 0; band < maxBands; ++band)
    {
      var offset = (band - maxBands / 2) * epsilon;
      placedPiece.transform.position = new Vector3(
        basePos.x,
        basePos.y + offset,
        basePos.z);

      if (!HasNearlyCoplanarOverlapWithBox(
            renderer,
            placedPiece,
            epsilon,
            normalEpsilon,
            layerMask,
            maxChecks))
      {
        // No overlap found; we're done!
        return;
      }
    }

    // If we get here, all bands failed
    Debug.LogWarning($"Unable to resolve coplanarity for {placedPiece.name} after {maxBands} attempts.");
  }

  public static void ApplyDeterministicCoplanarNudge(GameObject go, float epsilon = 0.0001f, int bandCount = 16)
  {
    // Combine world position, rotation, prefab name for a unique per-piece hash
    var pos = go.transform.position;
    var rot = go.transform.rotation.eulerAngles;
    var hash = Mathf.RoundToInt(pos.x * 100f) ^ Mathf.RoundToInt(pos.y * 100f) ^ Mathf.RoundToInt(pos.z * 100f) ^
               Mathf.RoundToInt(rot.y * 10f) ^ go.name.GetHashCode();

    var band = Mathf.Abs(hash) % bandCount;
    var offset = (band - bandCount / 2) * epsilon;

    go.transform.position += go.transform.up * offset;
  }

  public static void ResolveCoplanarityWithHashNudge(
    GameObject placedPiece,
    float epsilon = 0.0001f,
    int maxBands = 16,
    int layerMask = ~0,
    float normalEpsilon = 0.01f,
    int maxChecks = 50)
  {
    // var renderer = placedPiece.GetComponentInChildren<MeshRenderer>();
    // if (!renderer) return;
    // var basePos = placedPiece.transform.position;
    // var resolved = false;
    //
    // for (var band = 0; band < maxBands; ++band)
    // {
    //   var offset = (band - maxBands / 2) * epsilon;
    //   placedPiece.transform.position = new Vector3(
    //     basePos.x,
    //     basePos.y + offset,
    //     basePos.z);
    //
    //   if (!HasNearlyCoplanarOverlapWithBox(
    //         renderer,
    //         placedPiece,
    //         epsilon,
    //         normalEpsilon,
    //         layerMask,
    //         maxChecks))
    //   {
    //     resolved = true;
    //     break;
    //   }
    // }

    // Even after bands, apply a deterministic epsilon (hash nudge)
    ApplyDeterministicCoplanarNudge(placedPiece, epsilon, 16);

    // if (!resolved)
    // {
    //   Debug.LogWarning($"Unable to resolve coplanarity for {placedPiece.name} after {maxBands} attempts, but hash nudge was applied.");
    // }
  }

  public static void ApplyDeterministicEpsilon(GameObject go, float epsilon = 0.0001f, int bandCount = 16)
  {
    var pos = go.transform.position;
    var rot = go.transform.rotation.eulerAngles;
    var hash = Mathf.RoundToInt(pos.x * 10f) ^ Mathf.RoundToInt(pos.y * 10f) ^ Mathf.RoundToInt(pos.z * 10f) ^
               Mathf.RoundToInt(rot.y * 100f) ^
               go.name.GetHashCode();
    var band = Mathf.Abs(hash) % bandCount;
    var offset = (band - bandCount / 2) * epsilon;

    go.transform.position += go.transform.up * offset;
  }

  public static bool HasNearlyCoplanarOverlapWithBox(
    MeshRenderer thisRenderer,
    GameObject thisObj,
    float epsilon = 0.0001f,
    float normalEpsilon = 0.01f,
    int layerMask = ~0,
    int maxChecks = 50) // Failsafe
  {
    if (!thisRenderer) return false;
    var thisPiece = thisObj.GetComponentInParent<Piece>();
    if (!thisPiece) return false;

    var bounds = thisRenderer.bounds;
    var center = bounds.center;
    var normal = thisObj.transform.up;

    var orientation = thisObj.transform.rotation;
    var extents = bounds.extents;

    var overlaps = Physics.OverlapBox(center, extents, orientation, layerMask, QueryTriggerInteraction.Ignore);
    var checkedPieces = new HashSet<Piece>();
    var checkCount = 0;

    foreach (var col in overlaps)
    {
      if (col == null) continue;
      var otherPiece = col.GetComponentInParent<Piece>();
      if (otherPiece == null || otherPiece == thisPiece) continue;
      if (!checkedPieces.Add(otherPiece)) continue;
      if (++checkCount > maxChecks) break; // Safety: never check more than N neighbors

      var otherRenderers = otherPiece.GetComponentsInChildren<MeshRenderer>();
      foreach (var otherRenderer in otherRenderers)
      {
        if (otherRenderer == null || otherRenderer == thisRenderer) continue;
        var otherObj = otherRenderer.gameObject;
        if (otherObj == thisObj) continue;

        var otherNormal = otherObj.transform.up;
        var otherCenter = otherRenderer.bounds.center;

        var dot = Mathf.Abs(Vector3.Dot(normal.normalized, otherNormal.normalized));
        if (dot > 1f - normalEpsilon)
        {
          var dist = Mathf.Abs(Vector3.Dot(center - otherCenter, normal.normalized));
          if (dist < epsilon * 0.5f)
          {
            // Nearly coplanar in world, even if rotated
            return true;
          }
        }
      }
    }
    return false;
  }
}