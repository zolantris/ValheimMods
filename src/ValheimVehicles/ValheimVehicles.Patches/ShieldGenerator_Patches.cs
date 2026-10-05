using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Controllers;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.ValheimVehicles.Patches;

public class VehicleShieldGenerator
{
  public Vector3 LastPosition;
  public ZDOID ZdoId;
  public ShieldGenerator? ShieldGenerator;
  public VehiclePiecesController? PiecesController;

  public bool IsVehicleShieldValid()
  {
    if (ShieldGenerator == null)
    {
      var zdo = ZDOMan.instance.GetZDO(ZdoId);
      if (zdo == null) return false;

      var shieldObj = ZNetScene.instance.FindInstance(zdo);
      if (shieldObj == null) return false;

      ShieldGenerator = shieldObj.GetComponentInChildren<ShieldGenerator>();
    }

    if (ShieldGenerator == null || ShieldGenerator.m_shieldDome == null)
    {
      return false;
    }

    // PiecesController does not initialize immediately
    if (!PiecesController)
    {
      PiecesController = ShieldGenerator.GetComponentInParent<VehiclePiecesController>();
    }

    return PiecesController != null && PiecesController.MovementController != null;
  }

  public Vector3 GetShieldCenter()
  {
    if (ShieldGenerator == null) return Vector3.zero;
    if (PiecesController == null) return ShieldGenerator.m_nview.transform.position;
    if (PiecesController.OnboardCollider == null) return PiecesController.transform.position;
    return PiecesController.OnboardCollider.bounds.center;
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

  /// <summary>
  /// Prefix updates relate to centering the vehicle.
  /// </summary>
  /// <param name="__instance"></param>
  [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.Update))]
  [HarmonyPrefix]
  public static void ShieldGenerator_Update_Prefix(ShieldGenerator __instance)
  {
    if (!IsVehicleShield(__instance)) return;
    // Update the shield using the convex hull mesh
    UpdateVehicleShieldCenter(__instance);
  }

  /// <summary>
  /// Postfix runs the update effect if it was a VehicleShield. This ensures that the prefix does not cause problems for other mods/gameupdates by doing a full override.
  /// </summary>
  /// <param name="__instance"></param>
  [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.Update))]
  [HarmonyPostfix]
  public static void ShieldGenerator_Update_Postfix(ShieldGenerator __instance)
  {
    if (!IsVehicleShield(__instance)) return;

    if (!VehicleShieldGenerators.TryGetValue(__instance.m_nview.m_zdo.m_uid, out var vehicleShieldGenerator))
    {
      return;
    }

    // uses ValheimVehicles.SharedScripts.Vector3Extensions
    var isPositionNearEqual = vehicleShieldGenerator.LastPosition.IsCloseTo(vehicleShieldGenerator.GetShieldCenter(), 1f);

    // prevents spamming shieldDomeEffect visuals
    if (isPositionNearEqual) return;


    // base-game logic.

    // this is run in the original method to skip running setShieldData (when this happens we must run SetShieldData for vehicles)
    var hasSkippedDomeEffectUpdate = __instance.m_lastFuel == __instance.m_lastFuelSent && __instance.m_radius == __instance.m_radiusSent && __instance.m_lastHitTime == __instance.m_lastHitTimeSent;

    // No need to run the update if it's already been done.
    if (!hasSkippedDomeEffectUpdate)
    {
      return;
    }

    ShieldGenerator.m_shieldDomeEffect.SetShieldData(__instance, __instance.m_shieldDome.transform.position, __instance.m_radius, __instance.m_lastFuel, __instance.m_lastHitTime);

    __instance.m_lastFuelSent = __instance.m_lastFuel;
    __instance.m_radiusSent = __instance.m_radius;
    __instance.m_lastHitTimeSent = __instance.m_lastHitTime;

    // end of base-game logic

    // run this to ensure the position update does not need to run again especially if anchored or flying and not-moving vehicle.
    vehicleShieldGenerator.UpdateShieldCenterPosition();
  }

  private static bool IsVehicleShield(ShieldGenerator shieldGenerator)
  {
    return shieldGenerator.m_nview != null && shieldGenerator.m_nview.IsValid() && VehicleShieldGenerators.TryGetValue(shieldGenerator.m_nview.m_zdo.m_uid, out _);
  }

  public static void InitializeShieldWithConvexHull(ShieldGenerator __instance)
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
      PiecesController = __instance.GetComponentInParent<VehiclePiecesController>() // this will likely miss on first check
    };
  }

  /// <summary>
  /// Handles all updates for the VehicleShield. This can call twice when the vehicle is doing an update for a shield. But with this approach it ensures that any callsite will always go through here for vehicles
  /// </summary>
  /// <param name="__instance"></param>
  private static void UpdateVehicleShieldCenter(ShieldGenerator __instance)
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

    // ensures min shield radius is not too small.
    // TODO add a global variable for this.
    __instance.m_minShieldRadius = Mathf.Clamp(VehicleGlobalConfig.VehicleShieldGeneratorMinRadius.Value, 4f, 30f);

    // ensures shield can always expand to fit vehicle. Also Ensures min radius cannot be above max.
    __instance.m_maxShieldRadius = Mathf.Max(__instance.m_minShieldRadius, Mathf.Clamp(VehicleGlobalConfig.VehicleShieldGeneratorMaxRadius.Value, 30f, 200f)); // caps at 200f so a vehicle shield cannot every exceed this size

    // radius target is set instead of radius to ensure that it expands to this value but not immediately
    __instance.m_radiusTarget = maxRadius + 5f; // extra 2.5f around vehicle

    // force update position of the barrier.
    __instance.m_shieldDome.transform.position = shieldCenter;

  }
}