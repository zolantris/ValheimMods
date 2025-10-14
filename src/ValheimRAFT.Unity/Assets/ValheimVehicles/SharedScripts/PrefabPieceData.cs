// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using Zolantris.Shared;

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

      if (CanAddColliders(prefab))
      {
        InitColliders(prefab.transform.root, allocator);
      }

      if (ColliderPointData == null)
      {
        InitDefaultColliderPointData(allocator);
      }
    }

    private bool CanAddColliders(GameObject prefab)
    {
      if (prefab.name.Contains(PrefabNames.VehicleSail)) return false;
      if (prefab.name.Contains(PrefabNames.VehicleSailCloth)) return false;
      if (prefab.name.Contains("fire")) return false;
      if (prefab.name.Contains("smoke")) return false;
      if (prefab.name.Contains("vfx")) return false;
      return true;
    }

    /// <summary>
    ///   called within the constructor in order to mutate the original data before
    ///   it's returned.
    /// </summary>
    /// <param name="prefab"></param>
    private void InitComponentProperties(GameObject prefab)
    {
      IsBed = false;
      var swivelComponentParent = prefab.GetComponentInParent<SwivelComponent>();
      IsSwivelChild = swivelComponentParent != null;
    }

    /// <summary>
    ///   called within the constructor in order to mutate the original data before
    ///   it's returned.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="allocator"></param>
    private void InitColliders(Transform root, Allocator allocator)
    {
      // inactive excluded as inactive might not be syncing position well
      Prefab.GetComponentsInChildren(false, AllColliders);

      var allPoints = new List<Vector3>();

      foreach (var collider in AllColliders)
      {
        if (!collider.gameObject.activeInHierarchy || !LayerHelpers.IsContainedWithinLayerMask(collider.gameObject.layer, LayerHelpers.PhysicalLayerMask))
          continue;

        var isValid = true;
        var currentParent = collider.transform;
        while (isValid && currentParent != null)
        {
          isValid = CanAddColliders(currentParent.gameObject);
          if (currentParent == root) break;
          currentParent = currentParent.parent;
        }

        if (!isValid) continue;

        HullColliders.Add(collider); // ✅ Only store relevant colliders

        var localPoints = ConvexHullAPI.GetColliderPointsGlobal(collider)
          .Select(root.InverseTransformPoint)
          .ToArray(); // ✅ Convert to array

        allPoints.AddRange(localPoints);
      }

      if (allPoints.Count > 0)
      {
        ColliderPointData = new PrefabColliderPointData(
          Prefab.transform.localPosition,
          allPoints.ToArray(), // ✅ Pass array directly
          allocator
        );
      }
    }

    private void InitDefaultColliderPointData(Allocator allocator)
    {
      ColliderPointData = new PrefabColliderPointData(Prefab.transform.localPosition, new Vector3[] {}, allocator);
    }

    // public void Dispose()
    // {
    //   PointDataItems.Dispose(); // ✅ Dispose local native points
    // }
  }
}