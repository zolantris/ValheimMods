// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public struct PrefabPieceData
  {
    public GameObject Prefab;
    public List<Collider> AllColliders;
    public List<Collider> HullColliders;
    public PrefabColliderPointData PointDataItems;

    public PrefabPieceData(GameObject prefab, Allocator allocator)
    {
      Prefab = prefab;
      AllColliders = new List<Collider>();
      HullColliders = new List<Collider>();
      PointDataItems = default;

      InitializeColliders(prefab.transform.root, allocator);
    }

    public void InitializeColliders(Transform root, Allocator allocator)
    {
      Prefab.GetComponentsInChildren(true, AllColliders);

      foreach (var collider in AllColliders)
      {
        if (!collider.gameObject.activeInHierarchy || !LayerHelpers.IsContainedWithinMask(collider.gameObject.layer, LayerHelpers.PhysicalLayers))
          continue;

        HullColliders.Add(collider); // ✅ Only store relevant colliders

        var points = ConvexHullAPI.GetColliderPointsGlobal(collider)
          .Select(root.InverseTransformPoint)
          .ToArray(); // ✅ Convert to array

        if (points.Length > 0)
        {
          PointDataItems = new PrefabColliderPointData(
            Prefab.transform.localPosition,
            points, // ✅ Pass array directly
            allocator
          );

          break; // ✅ Store only the first valid collider's points
        }
      }
    }

    public void Dispose()
    {
      PointDataItems.Dispose(); // ✅ Dispose local native points
    }
  }
}