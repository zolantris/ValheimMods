using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class WearNTear_Patch
{
  [HarmonyPatch(typeof(WearNTear), "Start")]
  [HarmonyPrefix]
  private static bool WearNTear_Start(WearNTear __instance)
  {
    // we could check to see if the object is within a Controller, but this is unnecessary. Start just needs a protector.
    // this is a patch for basegame to prevent WNT from calling on objects without heightmaps which will return a NRE
    var hInstance = Heightmap.FindHeightmap(__instance.transform.position);

    if (hInstance != null) return true;

    Logger.LogWarning(
      $"WearNTear heightmap not found, this could be a problem with a prefab layer type not being a piece, netview name: {__instance.m_nview.name}");

    __instance.m_connectedHeightMap = hInstance;
    return false;
  }

  [HarmonyPatch(typeof(WearNTear), "Destroy")]
  [HarmonyPrefix]
  private static bool WearNTear_Destroy(WearNTear __instance)
  {
    // todo to find a better way to omit hull damage on item creation, most likely it's a collider problem triggering extreme damage.
    if (__instance.gameObject.name.Contains(PrefabNames.WaterVehiclePrefabName))
    {
      return false;
    }

    var mbr = __instance.GetComponentInParent<MoveableBaseRootComponent>();
    var bv = __instance.GetComponentInParent<BaseVehicleController>();


    if ((bool)mbr) mbr.DestroyPiece(__instance);
    if ((bool)bv) bv.DestroyPiece(__instance);

    return true;
  }

  [HarmonyPatch(typeof(WearNTear), "ApplyDamage")]
  [HarmonyPrefix]
  private static bool WearNTear_ApplyDamage(WearNTear __instance, float damage)
  {
    var mbr = __instance.GetComponent<MoveableBaseShipComponent>();
    var bv = __instance.GetComponent<BaseVehicleController>();

    // todo to find a better way to omit hull damage on item creation, most likely it's a collider problem triggering extreme damage.
    if (__instance.gameObject.name.Contains(PrefabNames.WaterVehiclePrefabName))
    {
      return false;
    }

    // vehicles ignore WNT for now...
    if ((bool)mbr || (bool)bv)
    {
      return false;
    }

    return true;
  }

  [HarmonyPatch(typeof(WearNTear), "UpdateSupport")]
  [HarmonyPatch(typeof(WearNTear), "SetupColliders")]
  [HarmonyPatch(typeof(Player), "PieceRayTest")]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> WearNTear_AttachShip(
    IEnumerable<CodeInstruction> instructions)
  {
    var list = instructions.ToList();
    for (var i = 0; i < list.Count; i++)
      if (list[i].Calls(AccessTools.PropertyGetter(typeof(Collider), "attachedRigidbody")))
      {
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(WearNTear_Patch),
            nameof(AttachRigidbodyMovableBase)));
        break;
      }

    return list;
  }

  [HarmonyPatch(typeof(WearNTear), "UpdateSupport")]
  [HarmonyPrefix]
  private static bool UpdateSupport(WearNTear __instance)
  {
    if (!__instance.isActiveAndEnabled) return false;
    var mbr = __instance.GetComponentInParent<MoveableBaseRootComponent>();
    var baseVehicle = __instance.GetComponentInParent<BaseVehicleController>();
    if (!(bool)mbr && !(bool)baseVehicle) return true;
    // if (__instance.transform.localPosition.y > 1f) return true;

    // makes all support values below 1f very high
    __instance.m_nview.GetZDO().Set(ZDOVars.s_support, 1500f);
    __instance.m_support = 1500f;
    __instance.m_supports = true;
    __instance.m_noSupportWear = true;
    return false;
  }

  private static Rigidbody? AttachRigidbodyMovableBase(Collider collider)
  {
    var rb = collider.attachedRigidbody;
    if (!rb) return null;
    var mbr = rb.GetComponent<MoveableBaseRootComponent>();
    var bvc = rb.GetComponent<BaseVehicleController>();
    if ((bool)mbr || bvc) return null;
    return rb;
  }
}