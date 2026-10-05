using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Patches;
using Object = UnityEngine.Object;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.Shared.Constants;
namespace ValheimVehicles.ValheimVehicles.Patches;

public class VehicleShieldGenerator
{
  public Vector3 LastPosition;
  public ShieldGenerator ShieldGenerator;
}

public class ShieldGenerator_Patches
{
  public static Dictionary<ZDOID, VehicleShieldGenerator> VehicleShieldGenerators = new();
  [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.Start))]
  [HarmonyPostfix]
  public static void Start(ShieldGenerator __instance)
  {
    if (Player.IsPlacementGhost(__instance.gameObject)) return;
    InitializeShieldWithConvexHull(__instance);
  }

  [HarmonyPatch(typeof(ShieldDomeImageEffect), nameof(ShieldDomeImageEffect.SetShieldData))]
  [HarmonyPrefix]
  public static void UpdatePostfix(ShieldDomeImageEffect __instance, ShieldGenerator shield,
    Vector3 position,
    float radius,
    float fuelFactor,
    float lastHitTime)
  {
    var vpc = __instance.GetComponentInParent<VehiclePiecesController>();

    if (vpc != null)
    {
      UpdateShieldWithConvexHull(shield);
    }
  }


  [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.Update))]
  [HarmonyPostfix]
  public static void UpdatePostfix(ShieldGenerator __instance)
  {
    // Update the shield using the convex hull mesh
    UpdateShieldWithConvexHull(__instance);
  }

  private static void InitializeShieldWithConvexHull(ShieldGenerator __instance)
  {
    var parentHash = __instance.m_nview.GetZDO().GetInt(VehicleZdoVars.MBPositionHash);

    // no vehicle parent do nothing
    if (parentHash == 0) return;

    var vpc = __instance.GetComponentInParent<VehiclePiecesController>();
    if (vpc == null) return;

    // reliant on first mesh. This might not be entirely safe until the logic in convexhullapi is migrated to a singular convexhull, but that's not accurate if there are multiple split hulls.
    // this might have to generate a collider from each of these and sync it's own shield down instead of using the basegame shield dome.
    if (vpc.convexHullComponent.convexHullMeshes.Count < 1 && vpc.convexHullComponent.convexHullMeshColliders[0] != null) return;

    // Get the vehiclePiecesController component VehiclePiecesController vehiclePiecesController = __instance.GetComponent(); if (vehiclePiecesController == null) { Debug.LogError("VehiclePiecesController not found on the ShieldGenerator."); return; }
// Get the convex hull mesh from the vehiclePiecesController
    var convexHullMesh = vpc.convexHullComponent.convexHullMeshColliders[0].sharedMesh;
    if (convexHullMesh == null)
    {
      Debug.LogError("Convex hull mesh not found.");
      return;
    }

    var meshFilter = __instance.m_shieldDome.GetComponentInChildren<MeshFilter>();
    if (meshFilter == null) return;

// Assign the updated convex hull mesh to the shield dome
    meshFilter.mesh = convexHullMesh;
  }

  private static void UpdateShieldWithConvexHull(ShieldGenerator __instance)
  {
    var vpc = __instance.GetComponentInParent<VehiclePiecesController>();
    if (vpc == null || vpc.MovementController != null) return;
    var movementController = vpc.MovementController;
    if (movementController == null) return;

    // force update position of the barrier.
    var vehicleCenter = movementController.vehicleAutomaticCenterOfMassPoint;
    __instance.m_shieldDome.transform.position = vehicleCenter;

    var vpcOnboardCollider = vpc.OnboardCollider;

    if (vpcOnboardCollider == null) return;

    var vpcOnboardColliderSize = vpcOnboardCollider.size;

    var maxRadius = Mathf.Max(vpcOnboardColliderSize.x, vpcOnboardColliderSize.y, vpcOnboardColliderSize.z) / 2;

    // radius target is set instead of radius to ensure that it expands to this value but not immediately
    __instance.m_radiusTarget = maxRadius * 1.01f; // extra 1% size for vehicle
    __instance.m_shieldDome.transform.position = vehicleCenter;

    // singleton instance that handles the effects
    if (ShieldGenerator.m_shieldDomeEffect)
    {
      ShieldGenerator.m_shieldDomeEffect.SetShieldData(__instance, vehicleCenter, maxRadius, __instance.m_lastFuel, __instance.m_lastHitTime);
    }
  }
}