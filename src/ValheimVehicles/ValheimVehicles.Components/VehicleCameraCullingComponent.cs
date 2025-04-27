using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Components;

namespace ValheimVehicles.Components;

public class VehicleCameraCullingComponent : MonoBehaviour
{
  public static void AddOrRemoveCameraCulling()
  {
    if (Camera.main == null) return;
    var cullingCamera =
      Camera.main.GetComponent<VehicleCameraCullingComponent>();

    if (CameraConfig.CameraOcclusionEnabled.Value != true)
    {
      if (cullingCamera != null) Destroy(cullingCamera);
      return;
    }

    if (Camera.main == null || cullingCamera != null) return;
    Camera.main.gameObject.AddComponent<VehicleCameraCullingComponent>();
  }

  // Dictionary to track disabled MeshRenderers
  private Dictionary<GameObject, List<Renderer>> disabledRenderers = new();
  private Dictionary<GameObject, float> occlusionTimeTracker = new();

  // Camera to check occlusion (can be set to main camera or another camera)
  private Camera? gameCamera;

  private void Start()
  {
    // Cache the main camera
    gameCamera = Camera.main;
  }

  private void DisableRenderersForVehicleInstances(float timeSinceLastUpdate)
  {
    if (ZNetScene.instance == null || Camera.main == null) return;
    if (!ZNetScene.instance.IsAreaReady(Camera.main.transform.position)) return;

    foreach (var vehicle in VehicleShip.VehicleInstances.Values)
    {
      var piecesController = vehicle.PiecesController;
      if (piecesController == null) continue;
      foreach (var piece in piecesController.m_nviewPieces)
      {
        var obj = piece.gameObject;
        var isObjectOccluded = IsObjectOccluded(obj);

        if (isObjectOccluded)
        {
          if (!occlusionTimeTracker.ContainsKey(obj))
            occlusionTimeTracker[obj] = 0f;

          // Increase occlusion time for objects that are occluded
          occlusionTimeTracker[obj] += timeSinceLastUpdate;

          // Disable object after a certain threshold of occlusion
          if (occlusionTimeTracker[obj] >
              CameraConfig.CameraOcclusionInterval.Value * 3)
            DisableMeshComponents(obj);
        }
        else
        {
          // Reset the occlusion timer for the object
          if (occlusionTimeTracker.ContainsKey(obj))
            occlusionTimeTracker[obj] = 0f;

          EnableMeshComponents(obj);
        }
      }
    }
  }

  private float delayDebounce;

  private void FixedUpdate()
  {
    if (delayDebounce < CameraConfig.CameraOcclusionInterval.Value)
    {
      delayDebounce += Time.fixedDeltaTime;
      return;
    }

    DisableRenderersForVehicleInstances(delayDebounce);
    delayDebounce = 0f;
  }

  // Disable MeshRenderer for all children of the target GameObject
  private void DisableMeshComponents(GameObject target)
  {
    // Get all the children of the target GameObject
    var allRenderers = target.GetComponentsInChildren<MeshRenderer>();
    foreach (var renderer in allRenderers)
      if (renderer != null)
      {
        // Add the Renderer to the dictionary to track it
        if (!disabledRenderers.ContainsKey(target.gameObject))
          disabledRenderers[target.gameObject] = new List<Renderer>();

        if (!disabledRenderers[target.gameObject].Contains(renderer))
          disabledRenderers[target.gameObject].Add(renderer);

        // Disable the Renderer component
        renderer.enabled = false;
      }
  }

  // Enable MeshRenderer for all children of the target GameObject
  private void EnableMeshComponents(GameObject target)
  {
    // Enable MeshRenderer components
    if (disabledRenderers.ContainsKey(target.gameObject))
    {
      foreach (var renderer in disabledRenderers[target.gameObject])
        if (renderer != null)
          renderer.enabled = true;

      disabledRenderers[target.gameObject].Clear();
    }
  }

  // Check if the object is visible to the camera using frustum culling
  private bool IsObjectVisibleToCamera(GameObject obj)
  {
    var colliders = obj.GetComponentsInChildren<Collider>();
    if (colliders.Length == 0) return true;
    foreach (var collider in colliders)
    {
      // Get the camera's frustum planes
      var planes = GeometryUtility.CalculateFrustumPlanes(gameCamera);

      // Check if the collider's bounding box is inside the camera's frustum
      return GeometryUtility.TestPlanesAABB(planes, collider.bounds);
    }

    return true;
  }

  private bool IsObjectOccluded(GameObject obj)
  {
    if (Player.m_localPlayer == null) return false;
    if (obj.name.StartsWith(PrefabNames.ShipAnchorWood) ||
        obj.name.StartsWith(PrefabNames.ShipSteeringWheel)) return false;
    if (Vector3.Distance(obj.transform.position,
          Player.m_localPlayer.transform.position) <
        CameraConfig.DistanceToKeepObjects.Value)
      return false;

    if (gameCamera == null) return false;
    // First check if the object is inside the camera's frustum
    if (!IsObjectVisibleToCamera(obj))
      return true; // If it's outside the camera's view, it's occluded

    var cameraTransform = gameCamera.transform;
    var cameraPosition = cameraTransform.position;

    var ray = new Ray(cameraPosition,
      obj.transform.position - cameraPosition);

    var results = new RaycastHit[3];
    var size =
      Physics.RaycastNonAlloc(ray, results, 80f,
        LayerMask.GetMask("Default_small", "default", "piece"));

    var isOccluded = true;
    // Iterate through all hits and check if any of them occlude the object
    for (var index = 0; index < size; index++)
    {
      var hit = results[index];
      if (hit.collider == null) continue;
      // If the ray hits something other than the obj object, parent, or netview at top, it false's occluded
      if (hit.collider.gameObject == obj) isOccluded = false;

      if (hit.collider.transform.parent == obj.transform) isOccluded = false;

      var nv = hit.collider.GetComponentInParent<ZNetView>();
      if (nv != null && nv.gameObject == obj) isOccluded = false;

      if (isOccluded) return false;
    }

    return isOccluded; // The object is not occluded
  }
}