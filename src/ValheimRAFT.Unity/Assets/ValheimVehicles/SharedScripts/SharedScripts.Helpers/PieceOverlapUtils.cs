// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimVehicles.SharedScripts
{
  public static class PieceOverlapUtils
  {
    /// <summary>
    /// Max colliders to check per overlap query. Increase if you expect >50 overlapping pieces.
    /// </summary>
    private static int MaxOverlapResults = 50;
    private static Collider[] _overlapResults = new Collider[MaxOverlapResults];

    /// <summary>
    /// Update the overlap allocation if you change MaxOverlapResults.
    /// </summary>
    public static void UpdateMaxOverlapAllocations(int maxOverlapResults)
    {
      MaxOverlapResults = maxOverlapResults;
      _overlapResults = new Collider[MaxOverlapResults];
    }

    /// <summary>
    /// Resolves coplanarity for a single prefab instance at placement.
    /// </summary>
    public static bool TryResolveCoplanarityOnPlacement(
      GameObject instance,
      Func<GameObject, GameObject>? getPrefabRoot = null,
      float epsilon = 0.001f,
      int maxBands = 16,
      int layerMask = ~0,
      Action<GameObject, Vector3>? onPrefabMoved = null)
    {
      var prefabRoot = getPrefabRoot?.Invoke(instance) ?? instance;
      var meshRenderers = prefabRoot.GetComponentsInChildren<MeshRenderer>();
      if (meshRenderers == null || meshRenderers.Length == 0) return false;

      var originalPos = prefabRoot.transform.position;
      for (var band = 0; band < maxBands; band++)
      {
        var isCoplanar = false;
        foreach (var meshRenderer in meshRenderers)
        {
          if (HasCoplanarOverlapWithNeighbors(
                prefabRoot, meshRenderer, prefabRoot, epsilon, layerMask, getPrefabRoot))
          {
            isCoplanar = true;
            break;
          }
        }
        if (!isCoplanar)
        {
          onPrefabMoved?.Invoke(prefabRoot, prefabRoot.transform.position);
          return true; // Success: Not coplanar with any intersecting neighbor
        }
        // Nudge Y (banding; could try X/Z too as fallback)
        var yOffset = (band - maxBands / 2) * epsilon;
        prefabRoot.transform.position = new Vector3(
          originalPos.x, originalPos.y + yOffset, originalPos.z
        );
      }
      LoggerProvider.LogWarning(
        $"Could not resolve coplanarity for {prefabRoot.name} after {maxBands} attempts."
      );
      onPrefabMoved?.Invoke(prefabRoot, prefabRoot.transform.position);
      return false;
    }

    /// <summary>
    /// Batch repair: ensures all GameObjects in the list are not coplanar/intersecting.
    /// </summary>
    public static void RepairBatchCoplanarity(
      List<GameObject> allPrefabs,
      Func<GameObject, GameObject>? getPrefabRoot = null,
      float epsilon = 0.001f,
      int maxBands = 32,
      int layerMask = ~0,
      Action<GameObject, Vector3>? onPrefabMoved = null)
    {
      for (var i = 0; i < allPrefabs.Count; i++)
      {
        var prefabRootA = getPrefabRoot?.Invoke(allPrefabs[i]) ?? allPrefabs[i];
        var renderersA = prefabRootA.GetComponentsInChildren<MeshRenderer>();
        var originalPosA = prefabRootA.transform.position;
        var resolved = false;
        for (var band = 0; band < maxBands; band++)
        {
          var isCoplanar = false;
          for (var j = 0; j < allPrefabs.Count; j++)
          {
            if (i == j) continue;
            var prefabRootB = getPrefabRoot?.Invoke(allPrefabs[j]) ?? allPrefabs[j];
            var renderersB = prefabRootB.GetComponentsInChildren<MeshRenderer>();
            foreach (var rA in renderersA)
            foreach (var rB in renderersB)
            {
              if (rA == null || rB == null) continue;
              if (AreMeshesCoplanar(rA, rB, epsilon, 0.2f))
              {
                isCoplanar = true;
                break;
              }
            }
            if (isCoplanar) break;
          }
          if (!isCoplanar)
          {
            resolved = true;
            onPrefabMoved?.Invoke(prefabRootA, prefabRootA.transform.position);
            break;
          }
          var yOffset = (band - maxBands / 2) * epsilon;
          prefabRootA.transform.position = new Vector3(
            originalPosA.x, originalPosA.y + yOffset, originalPosA.z
          );
          onPrefabMoved?.Invoke(prefabRootA, prefabRootA.transform.position);
        }
        if (!resolved)
          LoggerProvider.LogWarning(
            $"Could not resolve {prefabRootA.name} after {maxBands} bands."
          );
      }
    }

    // --- Internal Helpers ---

    private static bool HasCoplanarOverlapWithNeighbors(
      GameObject prefabRoot,
      MeshRenderer thisRenderer,
      GameObject thisObj,
      float epsilon,
      int layerMask,
      Func<GameObject, GameObject>? getPrefabRoot,
      float normalEpsilon = 0.2f)
    {
      var bounds = thisRenderer.bounds;
      var orientation = thisObj.transform.rotation;
      var count = Physics.OverlapBoxNonAlloc(
        bounds.center, bounds.extents, _overlapResults, orientation, layerMask, QueryTriggerInteraction.Ignore);

      for (var index = 0; index < count; index++)
      {
        var col = _overlapResults[index];
        var otherPrefabRoot = getPrefabRoot?.Invoke(col.gameObject) ?? col.gameObject;
        if (otherPrefabRoot == prefabRoot) continue;
        var otherRenderers = otherPrefabRoot.GetComponentsInChildren<MeshRenderer>();
        foreach (var otherRenderer in otherRenderers)
        {
          if (otherRenderer == null || otherRenderer == thisRenderer) continue;
          if (AreMeshesCoplanar(thisRenderer, otherRenderer, epsilon, normalEpsilon))
            return true;
        }
      }
      return false;
    }

    private static bool AreMeshesCoplanar(MeshRenderer a, MeshRenderer b, float epsilon, float normalEpsilon)
    {
      var centerA = a.bounds.center;
      var centerB = b.bounds.center;
      var normalA = a.transform.up;
      var normalB = b.transform.up;

      var dot = Mathf.Abs(Vector3.Dot(normalA.normalized, normalB.normalized));
      if (dot > 1f - normalEpsilon)
      {
        var dist = Mathf.Abs(Vector3.Dot(centerA - centerB, normalA.normalized));
        if (dist < epsilon * 0.5f)
          return true;
      }
      return false;
    }
  }
}