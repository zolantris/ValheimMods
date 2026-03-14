using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Controllers;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class ZSyncTransform_Patch
{

  public static HashSet<ZSyncTransform> ZsyncWithPiecesController = new();

  [HarmonyPatch(typeof(ZSyncTransform), nameof(ZSyncTransform.CustomLateUpdate))]
  [HarmonyPostfix]
  private static void CustomLateUpdate(ZSyncTransform __instance)
  {
    if (__instance.m_characterParentSync || !__instance.m_nview) return;
    if (!__instance.m_nview.transform.parent) return;
    if (!__instance.m_nview.transform.GetComponentInParent<VehiclePiecesController>())
    {
      if (ZsyncWithPiecesController.Contains(__instance))
      {
        ZsyncWithPiecesController.Remove(__instance);
        __instance.m_characterParentSync = false;
      }

      return;
    }

    // VehiclePiecesController Exists. Ensure m_characterParentSync is enabled
    ZsyncWithPiecesController.Add(__instance);
    __instance.m_characterParentSync = true;
  }

  [HarmonyPatch(typeof(ZSyncTransform), nameof(ZSyncTransform.GetRelativePosition))]
  [HarmonyPrefix]
  private static bool GetRelativePosition_ForVehicle(ZSyncTransform __instance, ref bool __result, ZDO zdo, out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
  {
    parent = ZDOID.None;
    attachJoint = "";
    relativePos = Vector3.zero;
    relativeRot = Quaternion.identity;
    relativeVel = Vector3.zero;

    var t = __instance.transform;
    if (!t.parent) return true;

    var vehiclePiece = t.GetComponentInParent<VehiclePiecesController>();
    if (vehiclePiece && vehiclePiece.m_nview)
    {
      parent = vehiclePiece.m_nview.GetZDO().m_uid;
      relativePos = t.localPosition;
      relativeRot = t.localRotation;
      __result = true;
      return false;
    }

    return true;
  }
}