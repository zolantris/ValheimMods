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
    public bool IsSwivelChild;
    public bool IsBed;
    public PrefabColliderPointData? ColliderPointData;

    public PrefabPieceData(GameObject prefab, Allocator allocator)
    {
      Prefab = prefab;
      AllColliders = new List<Collider>();
      HullColliders = new List<Collider>();
      ColliderPointData = null;
      IsBed = false;
      IsSwivelChild = false;

      InitComponentProperties(prefab);
      InitColliders(prefab.transform.root, allocator);
    }

    /// <summary>
    /// called within the constructor in order to mutate the original data before it's returned.
    /// </summary>
    /// <param name="prefab"></param>
    private void InitComponentProperties(GameObject prefab)
    {
      IsBed = false;
      // IsBed = prefab.GetComponent<Bed>();
      IsSwivelChild = prefab.GetComponentInParent<SwivelComponent>() != null;
    }

    /// <summary>
    /// called within the constructor in order to mutate the original data before it's returned.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="allocator"></param>
    private void InitColliders(Transform root, Allocator allocator)
    {
      Prefab.GetComponentsInChildren(true, AllColliders);

      foreach (var collider in AllColliders)
      {
        if (!collider.gameObject.activeInHierarchy || !LayerHelpers.IsContainedWithinLayerMask(collider.gameObject.layer, LayerHelpers.PhysicalLayers))
          continue;

        HullColliders.Add(collider); // ✅ Only store relevant colliders

        var points = ConvexHullAPI.GetColliderPointsGlobal(collider)
          .Select(root.InverseTransformPoint)
          .ToArray(); // ✅ Convert to array

        if (points.Length > 0)
        {
          ColliderPointData = new PrefabColliderPointData(
            Prefab.transform.localPosition,
            points, // ✅ Pass array directly
            allocator
          );

          break; // ✅ Store only the first valid collider's points
        }
      }
    }

    // public void Dispose()
    // {
    //   PointDataItems.Dispose(); // ✅ Dispose local native points
    // }
  }
}