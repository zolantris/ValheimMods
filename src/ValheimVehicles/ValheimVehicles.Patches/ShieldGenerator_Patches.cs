using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using ValheimVehicles.Controllers;
namespace ValheimVehicles.ValheimVehicles.Patches;

public class ShieldGenerator_Patches
{
  // [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.Start))]
  // [HarmonyPostfix]
  // public static void Start(ShieldGenerator __instance)
  // {
  //   if (Player.IsPlacementGhost(__instance.gameObject)) return;
  //   InitializeShieldWithConvexHull(__instance);
  // }

  [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.Update))]
  [HarmonyPrefix]
  public static bool UpdatePrefix(ShieldGenerator __instance)
  {
    // skip prefix if not in vehicle
    if (__instance.GetComponentInParent<VehiclePiecesController>() == null) return true;

    // run full prefix. This is required to avoid weird interactions of the update when running only half of it.
    UpdateShieldWithConvexHull(__instance);

    if ((bool)(Object)__instance.m_shieldDome)
    {
      var num = __instance.m_shieldDome.transform.localScale.x + (__instance.m_radius - __instance.m_shieldDome.transform.localScale.x) * __instance.m_decreaseInertia;
      __instance.m_shieldDome.transform.localScale = new Vector3(num, num, num);
    }
    if ((double)__instance.m_radiusTarget != (double)__instance.m_radius)
    {
      if (!__instance.m_firstCheck)
      {
        __instance.m_firstCheck = true;
        __instance.m_radius = __instance.m_radiusTarget;
      }
      var f = __instance.m_radiusTarget - __instance.m_radius;
      __instance.m_radius += Mathf.Min(__instance.m_startStopSpeed * Time.deltaTime, Mathf.Abs(f)) * ((double)f > 0.0 ? 1f : -1f);
    }

    // TODO this needs a positional check then it can skip running.
    // if ((double)__instance.m_lastFuel == (double)__instance.m_lastFuelSent && (double)__instance.m_radius == (double)__instance.m_radiusSent && (double)__instance.m_lastHitTime == (double)__instance.m_lastHitTimeSent)
    //   return false;

    ShieldGenerator.m_shieldDomeEffect.SetShieldData(__instance, __instance.m_shieldDome.transform.position, __instance.m_radius, __instance.m_lastFuel, __instance.m_lastHitTime);
    __instance.m_lastFuelSent = __instance.m_lastFuel;
    __instance.m_radiusSent = __instance.m_radius;
    __instance.m_lastHitTimeSent = __instance.m_lastHitTime;
    // Update the shield using the convex hull mesh

    return false;
  }

  private static void UpdateShieldWithConvexHull(ShieldGenerator __instance)
  {
    var vpc = __instance.GetComponentInParent<VehiclePiecesController>();
    if (vpc == null || vpc.MovementController == null) return;
    var movementController = vpc.MovementController;
    if (movementController == null) return;

    // force update position of the barrier.
    var vehicleCenter = movementController.OnboardCollider.bounds.center;
    __instance.m_shieldDome.transform.position = vehicleCenter;

    var vpcOnboardCollider = vpc.OnboardCollider;

    if (vpcOnboardCollider == null) return;

    var vpcOnboardColliderSize = vpcOnboardCollider.size;

    var maxRadius = Mathf.Max(vpcOnboardColliderSize.x, vpcOnboardColliderSize.y, vpcOnboardColliderSize.z) / 2;

    // __instance.m_radius = maxRadius * 1.05f; // extra 5% size for vehicle
    __instance.m_radiusTarget = maxRadius * 1.05f; // extra 5% size for vehicle
    __instance.m_shieldDome.transform.position = vehicleCenter;
  }
}