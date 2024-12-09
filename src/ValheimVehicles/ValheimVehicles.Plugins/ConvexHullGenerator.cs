using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;

namespace ValheimVehicles.Plugins;

[ExecuteInEditMode]
public class ConvexHullMeshGenerator : MonoBehaviour
{
  public float distanceThreshold = 0.1f;
  public List<GameObject> GeneratedMeshGameObjects = [];

  private void Start()
  {
    ConvexHullMeshGeneratorAPI.GenerateMeshesFromChildColliders(gameObject,
      GeneratedMeshGameObjects, distanceThreshold);
  }

  public void OnEnable()
  {
    ConvexHullMeshGeneratorAPI.GenerateMeshesFromChildColliders(gameObject,
      GeneratedMeshGameObjects, distanceThreshold);
  }

  public void OnDisable()
  {
    ConvexHullMeshGeneratorAPI.DeleteMeshesFromChildColliders(
      GeneratedMeshGameObjects);
  }

  /// <summary>
  /// For seeing the colliders update live. This should not be used in game for performance reasons.
  /// </summary>
  public void FixedUpdate()
  {
    ConvexHullMeshGeneratorAPI.GenerateMeshesFromChildColliders(gameObject,
      GeneratedMeshGameObjects, distanceThreshold);
  }

  [ContextMenu("Generate Mesh")]
  private void TestGenerateMesh()
  {
    ConvexHullMeshGeneratorAPI.GenerateMeshesFromChildColliders(gameObject,
      GeneratedMeshGameObjects, distanceThreshold);
  }
}