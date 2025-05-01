using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Compat;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;

using ValheimVehicles.Controllers;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class Character_Patch
{
  
  [HarmonyPatch(typeof(Character), "ApplyGroundForce")]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> Character_Patch_ApplyGroundForce(
    IEnumerable<CodeInstruction> instructions)
  {
    var list = instructions.ToList();
    for (var i = 0; i < list.Count; i++)
      if (list[i]
          .Calls(AccessTools.Method(typeof(Character), "GetStandingOnShip")))
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(Character_Patch),
            nameof(GetStandingOnShip)));
    return list;
  }

  // on vehicle = not swimming
  [HarmonyPatch(typeof(Character), nameof(Character.InWater))]
  [HarmonyPostfix]
  public static void Character_IsInWater(Character __instance,
    ref bool __result)
  {
    var vpc = WaterZoneUtils.IsOnboard(__instance);
    if (vpc) __result = false;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.OnDisable))]
  [HarmonyPostfix]
  public static void Character_OnDisable(Character __instance)
  {
    SaveLastKnownPlaceOnVehicle(__instance);
  }

  /// <summary>
  /// Todo might have to do this in NetView instead. Though OnDestroy is like worst place to call it.
  /// </summary>
  /// <param name="__instance"></param>
  [HarmonyPatch(typeof(Character), nameof(Character.OnDestroy))]
  [HarmonyPostfix]
  public static void Character_OnDestroy(Character __instance)
  {
    SaveLastKnownPlaceOnVehicle(__instance);
  }

  public static void SaveLastKnownPlaceOnVehicle(Character character)
  {
    if (character.m_nview == null) return;
    var zdo = character.m_nview.GetZDO();
    if (zdo == null) return;
    var vehiclePiecesController = character.GetComponentInParent<VehiclePiecesController>();
    if (vehiclePiecesController == null) return;
    VehiclePiecesController.AddTempPieceProperties(character.m_nview, vehiclePiecesController);
  }

  public static object? GetStandingOnShip(Character __instance)
  {
    if (__instance.InNumShipVolumes == 0 || !__instance.IsOnGround() ||
        __instance.m_lastGroundBody == null)
    {
      return null;
    }

    if (WaterZoneUtils.IsOnboard(__instance, out var data))
    {
      return VehicleControllersCompat.InitFromUnknown(data?.VehicleShip);
    }

    var lastOnShip = __instance.m_lastGroundBody.GetComponent<Ship>();

    if (lastOnShip)
    {
      return lastOnShip;
    }

    return null;
  }

  [HarmonyPatch(typeof(Character), "GetStandingOnShip")]
  [HarmonyPrefix]
  private static bool Character_GetStandingOnShip(Character __instance,
    ref object? __result)
  {
    __result = GetStandingOnShip(__instance);
    return false;
  }

  [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
  [HarmonyPostfix]
  private static void UpdateGroundContact(Character __instance)
  {
    if (__instance is Player { m_debugFly: not false })
    {
      if (__instance.transform.parent != null)
        __instance.transform.SetParent(null);
      return;
    }

    VehiclePiecesController? bvc = null;
    if ((bool)__instance.m_lastGroundBody &&
        VehicleOnboardController.IsCharacterOnboard(__instance))
    {
      bvc = __instance.m_lastGroundBody
        .GetComponentInParent<VehiclePiecesController>();
      if ((bool)bvc && __instance.transform.parent != bvc.transform)
      {
        __instance.transform.SetParent(bvc.transform);
      }

      return;
    }

    if (!bvc && __instance.transform.parent != null)
      __instance.transform.SetParent(null);
  }
  
  /// <summary>
  /// Fixes issue where player is running on a moving boat and then enters flying mode or hover mode if moving near edge and ship is moving forward but player is moving backwards
  /// </summary>
  /// <param name="__instance"></param>
  /// <param name="collision"></param>
  /// <returns></returns>
  [HarmonyPatch(typeof(Character), "OnCollisionStay")]
  [HarmonyPrefix]
  private static bool OnCollisionStay(Character __instance, Collision collision)
  {
    WaterZoneUtils.IgnoreColliderIfAttachedAndOnVehicle(__instance, collision);
    if (!__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner() ||
        __instance.m_jumpTimer < 0.1f) return false;
    var contacts = collision.contacts;
    for (var i = 0; i < contacts.Length; i++)
    {
      var contactPoint = contacts[i];
      var hitnormal = contactPoint.normal;
      var hitpoint = contactPoint.point;
      var hitDistance = Mathf.Abs(hitpoint.y - __instance.transform.position.y);
      if (!__instance.m_groundContact && hitnormal.y < 0f && hitDistance < 0.1f)
      {
        hitnormal *= -1f;
        hitpoint = __instance.transform.position;
      }

      if (!(hitnormal.y > 0.1f) ||
          !(hitDistance < __instance.m_collider.radius)) continue;
      if (hitnormal.y > __instance.m_groundContactNormal.y ||
          !__instance.m_groundContact)
      {
        __instance.m_groundContact = true;
        __instance.m_groundContactNormal = hitnormal;
        __instance.m_groundContactPoint = hitpoint;
        __instance.m_lowestContactCollider = collision.collider;
        continue;
      }

      var groundContactNormal =
        Vector3.Normalize(__instance.m_groundContactNormal + hitnormal);
      if (groundContactNormal.y > __instance.m_groundContactNormal.y)
      {
        __instance.m_groundContactNormal = groundContactNormal;
        __instance.m_groundContactPoint =
          (__instance.m_groundContactPoint + hitpoint) * 0.5f;
      }
    }

    return false;
  }
}