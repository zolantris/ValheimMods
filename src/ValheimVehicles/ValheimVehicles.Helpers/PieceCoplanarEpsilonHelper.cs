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

  public static float EpsilonDefault = 0.0001f;

  // 1. Classic: Mutate transform (original method, call as before)
  public static void ResolveCoplanarityByTransform(GameObject placedPiece, float? epsilon = null, int maxBands = 16, int layerMask = ~0)
  {
    if (epsilon == null)
    {
      epsilon = EpsilonDefault;
    }
    var renderer = placedPiece.GetComponentInChildren<MeshRenderer>();
    if (!renderer) return;

    for (var band = 0; band < maxBands; ++band)
    {
      var offset = (band - maxBands / 2) * epsilon.Value;
      placedPiece.transform.position = new Vector3(
        placedPiece.transform.position.x,
        placedPiece.transform.position.y + offset,
        placedPiece.transform.position.z);

      if (!HasNearlyCoplanarOverlap(renderer, placedPiece, epsilon.Value, layerMask))
        return;
    }
    Debug.LogWarning($"Unable to resolve coplanarity for {placedPiece.name}");
  }

  // Returns true if any overlapping piece with same prefab name matches in 2 out of 3 axes
  public static bool HasNearlyCoplanarOverlap(
    MeshRenderer thisRenderer,
    GameObject thisObj,
    float epsilon = 0.001f,
    float normalEpsilon = 0.2f, // dot threshold for "parallel"
    int layerMask = ~0)
  {
    if (!thisRenderer) return false;
    var thisPiece = thisObj.GetComponentInParent<Piece>();
    if (!thisPiece) return false;

    var bounds = thisRenderer.bounds;
    var center = bounds.center;
    var normal = thisObj.transform.up; // or change for other face types

    var orientation = thisObj.transform.rotation;
    var extents = bounds.extents;

    var overlaps = Physics.OverlapBox(center, extents, orientation, layerMask, QueryTriggerInteraction.Ignore);

    foreach (var col in overlaps)
    {
      if (col == null) continue;
      var otherPiece = col.GetComponentInParent<Piece>();
      if (otherPiece == null) continue;
      if (otherPiece == thisPiece) continue;
      var otherRenderers = otherPiece.GetComponentsInChildren<MeshRenderer>();
      if (otherRenderers == null) continue;
      foreach (var otherRenderer in otherRenderers)
      {
        if (otherRenderer == null) continue;
        var otherObj = otherRenderer.gameObject;
        if (otherObj == thisObj) continue;

        // Use that piece's up as its face normal
        var otherNormal = otherObj.transform.up;
        var otherCenter = otherRenderer.bounds.center;

        // Check if normals are nearly parallel
        var dot = Mathf.Abs(Vector3.Dot(normal.normalized, otherNormal.normalized));
        if (dot > 1f - normalEpsilon)
        {
          // Point-plane distance
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