using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Shared.Constants;
namespace ValheimVehicles.ValheimVehicles.Patches;

public class VehicleShieldGenerator
{
  public Vector3 LastPosition;
  public ShieldGenerator? ShieldGenerator;
  public VehiclePiecesController? PiecesController;

  public bool IsVehicleShieldValid()
  {
    return ShieldGenerator != null && ShieldGenerator.m_shieldDome != null && PiecesController != null && PiecesController.MovementController != null;
  }

  public Vector3 GetShieldCenter()
  {
    if (ShieldGenerator == null) return Vector3.zero;
    if (PiecesController == null) return ShieldGenerator.m_nview.transform.position;
    if (PiecesController.MovementController == null) return PiecesController.transform.position;
    return PiecesController.MovementController.vehicleAutomaticCenterOfMassPoint;
  }

  public void UpdateShieldCenterPosition()
  {
    LastPosition = GetShieldCenter();
  }
}

public class ShieldGenerator_Patches
{
  public static readonly Dictionary<ZDOID, VehicleShieldGenerator> VehicleShieldGenerators = new();

  public static void Reset()
  {
    VehicleShieldGenerators.Clear();
  }

  [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.Start))]
  [HarmonyPostfix]
  public static void Start(ShieldGenerator __instance)
  {
    if (Player.IsPlacementGhost(__instance.gameObject)) return;
    InitializeShieldWithConvexHull(__instance);
  }

  [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.OnDestroy))]
  [HarmonyPostfix]
  public static void OnDestroy_Cleanup(ShieldGenerator __instance)
  {
    if (__instance.m_isPlacementGhost) return;
    if (__instance.m_nview == null) return;
    if (__instance.m_nview.m_zdo == null) return;
    VehicleShieldGenerators.Remove(__instance.m_nview.m_zdo.m_uid);
  }

  [HarmonyPatch(typeof(ShieldDomeImageEffect), nameof(ShieldDomeImageEffect.SetShieldData))]
  [HarmonyPrefix]
  public static bool SetShieldData_Prefix(ShieldDomeImageEffect __instance, ShieldGenerator shield,
    ref Vector3 position,
    float radius,
    float fuelFactor,
    float lastHitTime)
  {
    if (!IsVehicleShield(shield)) return true;

    // intercepts and updates some properties before the shield generator runs it's normal logic.
    UpdateVehicleShield(shield, true);

    // this must be updated directly otherwise the argument is stale and the original call will not get the new value
    position = shield.m_shieldDome.transform.position;

    return true;
  }


  [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.Update))]
  [HarmonyPostfix]
  public static void UpdatePostfix(ShieldGenerator __instance)
  {
    if (!IsVehicleShield(__instance)) return;
    // Update the shield using the convex hull mesh
    UpdateVehicleShield(__instance);
  }

  private static bool IsVehicleShield(ShieldGenerator shieldGenerator)
  {
    return shieldGenerator.m_nview != null && shieldGenerator.m_nview.IsValid() && VehicleShieldGenerators.TryGetValue(shieldGenerator.m_nview.m_zdo.m_uid, out _);
  }

  private static void InitializeShieldWithConvexHull(ShieldGenerator __instance)
  {
    if (!__instance || !__instance.m_nview || !__instance.m_nview.IsValid()) return;
    var parentId = __instance.m_nview.GetZDO().GetInt(VehicleZdoVars.MBParentId);
    // no vehicle parent do nothing
    if (parentId == 0) return;

    var zdoId = __instance.m_nview.GetZDO().m_uid;
    VehicleShieldGenerators[zdoId] = new VehicleShieldGenerator
    {
      ShieldGenerator = __instance,
      LastPosition = __instance.transform.position,
      PiecesController = __instance.GetComponentInParent<VehiclePiecesController>()
    };
  }

  /// <summary>
  /// Handles all updates for the VehicleShield. This can call twice when the vehicle is doing an update for a shield. But with this approach it ensures that any callsite will always go through here for vehicles
  /// </summary>
  /// <param name="__instance"></param>
  /// <param name="skipSetShieldData">Skip the setShield data. Used in the patch to avoid infinite loop</param>
  private static void UpdateVehicleShield(ShieldGenerator __instance, bool skipSetShieldData = false)
  {
    if (!VehicleShieldGenerators.TryGetValue(__instance.m_nview.m_zdo.m_uid, out var vehicleShieldGenerator))
    {
      return;
    }

    if (!vehicleShieldGenerator.IsVehicleShieldValid())
    {
      return;
    }

    var shieldCenter = vehicleShieldGenerator.GetShieldCenter();
    // no need to update if expected position has not changed.
    if (vehicleShieldGenerator.LastPosition == shieldCenter && __instance.m_shieldDome.transform.position == shieldCenter)
    {
      return;
    }

    if (vehicleShieldGenerator.PiecesController == null) return;

    var onboardCollider = vehicleShieldGenerator.PiecesController.OnboardCollider;
    if (onboardCollider == null) return;
    var vpcOnboardColliderSize = onboardCollider.size;

    // max radius in 3 dimensions
    var maxRadius = Mathf.Max(vpcOnboardColliderSize.x, vpcOnboardColliderSize.y, vpcOnboardColliderSize.z) / 2;

    // radius target is set instead of radius to ensure that it expands to this value but not immediately
    __instance.m_radiusTarget = maxRadius * 1.01f; // extra 1% size for vehicle

    // force update position of the barrier.
    __instance.m_shieldDome.transform.position = shieldCenter;

    // singleton instance that handles the effects
    if (ShieldGenerator.m_shieldDomeEffect && !skipSetShieldData)
    {
      ShieldGenerator.m_shieldDomeEffect.SetShieldData(__instance, shieldCenter, maxRadius, __instance.m_lastFuel, __instance.m_lastHitTime);
    }

    vehicleShieldGenerator.UpdateShieldCenterPosition();
  }
}