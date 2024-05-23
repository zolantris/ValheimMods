using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Vehicles;

namespace ValheimRAFT.Patches;

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
      if (list[i].Calls(AccessTools.Method(typeof(Character), "GetStandingOnShip")))
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(Character_Patch), nameof(GetStandingOnShip)));
    return list;
  }

  public static object? GetStandingOnShip(Character __instance)
  {
    if (__instance.InNumShipVolumes == 0 || !__instance.IsOnGround() ||
        !(bool)__instance.m_lastGroundBody)
    {
      return null;
    }

    var lastOnShip = __instance.m_lastGroundBody.GetComponent<Ship>();

    if (lastOnShip)
    {
      return lastOnShip;
    }

    var bvc = __instance.m_lastGroundBody.GetComponentInParent<BaseVehicleController>();
    if ((bool)bvc)
    {
      return VehicleShipCompat.InitFromUnknown(bvc?.VehicleInstance);
    }

    /*
     * @deprecated old ship logic
     */
    var mb = __instance.m_lastGroundBody.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mb && (bool)mb.m_ship)
    {
      return VehicleShipCompat.InitFromUnknown(mb.m_ship);
    }

    return null;
  }

  [HarmonyPatch(typeof(Character), "GetStandingOnShip")]
  [HarmonyPrefix]
  private static bool Character_GetStandingOnShip(Character __instance, ref object? __result)
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
      if (__instance.transform.parent != null) __instance.transform.SetParent(null);
      return;
    }

    BaseVehicleController? bvc = null;
    if ((bool)__instance.m_lastGroundBody)
    {
      bvc = __instance.m_lastGroundBody.GetComponentInParent<BaseVehicleController>();
      if ((bool)bvc && __instance.transform.parent != bvc.transform)
      {
        __instance.transform.SetParent(bvc.transform);
        return;
      }
    }

    MoveableBaseRootComponent? mbr = null;
    if ((bool)__instance.m_lastGroundBody)
    {
      mbr = __instance.m_lastGroundBody.GetComponentInParent<MoveableBaseRootComponent>();
      if ((bool)mbr && __instance.transform.parent != mbr.transform)
        __instance.transform.SetParent(mbr.transform);
    }

    if (!mbr && !bvc && __instance.transform.parent != null) __instance.transform.SetParent(null);
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

      if (!(hitnormal.y > 0.1f) || !(hitDistance < __instance.m_collider.radius)) continue;
      if (hitnormal.y > __instance.m_groundContactNormal.y || !__instance.m_groundContact)
      {
        __instance.m_groundContact = true;
        __instance.m_groundContactNormal = hitnormal;
        __instance.m_groundContactPoint = hitpoint;
        __instance.m_lowestContactCollider = collision.collider;
        continue;
      }

      var groundContactNormal = Vector3.Normalize(__instance.m_groundContactNormal + hitnormal);
      if (groundContactNormal.y > __instance.m_groundContactNormal.y)
      {
        __instance.m_groundContactNormal = groundContactNormal;
        __instance.m_groundContactPoint = (__instance.m_groundContactPoint + hitpoint) * 0.5f;
      }
    }

    return false;
  }
}