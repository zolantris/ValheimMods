// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts
{
  using System.Collections.Generic;
  using UnityEngine;

  public class VehicleCollisionManager : MonoBehaviour
  {
    private ConvexHullJobHandler _convexHullJobHandler;
    private Dictionary<GameObject, PrefabPieceData> _trackedPrefabs = new();

    private void Awake()
    {
      _convexHullJobHandler = GetComponent<ConvexHullJobHandler>();

      if (_convexHullJobHandler == null)
        Debug.LogError("❌ ConvexHullJobHandler reference missing on VehicleCollisionManager!");
    }

    /// <summary>
    /// Requests convex hull generation for a tracked prefab.
    /// </summary>
    public void GenerateConvexHull(GameObject prefab)
    {
      if (prefab == null || !_trackedPrefabs.ContainsKey(prefab)) return;

      var prefabData = _trackedPrefabs[prefab];
      _convexHullJobHandler.ScheduleConvexHullJob(prefab, prefabData.MeshData);
    }
  }
}