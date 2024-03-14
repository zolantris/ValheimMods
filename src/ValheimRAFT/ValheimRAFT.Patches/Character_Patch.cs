using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Vehicles;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class Character_Patch
{
  [HarmonyPatch(typeof(Character), "GetStandingOnShip")]
  [HarmonyPrefix]
  private static bool Character_GetStandingOnShip(Character __instance, ref object? __result)
  {
    if (!__instance.IsOnGround()) return false;
    if ((bool)__instance.m_lastGroundBody)
    {
      var lastOnShip = __instance.m_lastGroundBody.GetComponent<Ship>();
      var lastOnWaterVehicle = __instance.m_lastGroundBody.GetComponent<VVShip>();

      if (lastOnShip)
      {
        __result = lastOnShip;
      }

      if (lastOnWaterVehicle)
      {
        __result = lastOnWaterVehicle;
        // lastOnWaterVehicle.m_players.Add(__instance.get);
      }

      if (!lastOnShip && !lastOnWaterVehicle)
      {
        __result = null;
      }


      if (__result == null)
      {
        // todo deprecate this logic
        var mb = __instance.m_lastGroundBody.GetComponentInParent<MoveableBaseRootComponent>();
        if ((bool)mb && (bool)mb.shipController)
          __result = mb.shipController.GetComponent<Ship>();

        // new logic
        var bvc = __instance.m_lastGroundBody.GetComponentInParent<WaterVehicleController>();
        if ((bool)bvc && (bool)bvc.VehicleInstance.Instance)
          __result = bvc.VehicleInstance;
      }

      return false;
    }

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

    MoveableBaseRootComponent? mbr = null;
    if ((bool)__instance.m_lastGroundBody)
    {
      mbr = __instance.m_lastGroundBody.GetComponentInParent<MoveableBaseRootComponent>();
      if ((bool)mbr && __instance.transform.parent != mbr.transform)
        __instance.transform.SetParent(mbr.transform);
    }

    BaseVehicleController? bvc = null;
    if ((bool)__instance.m_lastGroundBody)
    {
      bvc = __instance.m_lastGroundBody.GetComponentInParent<BaseVehicleController>();
      if ((bool)bvc && __instance.transform.parent != bvc.transform)
        __instance.transform.SetParent(bvc.transform);
    }

    if (!mbr && !bvc && __instance.transform.parent != null) __instance.transform.SetParent(null);
  }

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