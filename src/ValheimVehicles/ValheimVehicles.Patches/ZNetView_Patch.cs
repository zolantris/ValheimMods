using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;

using Logger = Jotunn.Logger;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class ZNetView_Patch
{
  [HarmonyPatch(typeof(ZNetView), "ResetZDO")]
  [HarmonyPrefix]
  private static bool ZNetView_ResetZDO(ZNetView __instance)
  {
    if (!ZNetView.m_forceDisableInit || __instance == null)
    {
      return true;
    }

    return __instance.m_zdo != null;
  }

  [HarmonyPatch(typeof(ZNetView), "Awake")]
  [HarmonyPostfix]
  private static void ZNetView_Awake(ZNetView __instance)
  {
    if (__instance.m_zdo == null) return;

    VehiclePiecesController.InitPiece(__instance);
    CultivatableComponent.InitPiece(__instance);
  }

  [HarmonyPatch(typeof(ZNetView), "OnDestroy")]
  [HarmonyPrefix]
  private static bool ZNetView_OnDestroy(ZNetView __instance)
  {
    var bv = __instance.GetComponentInParent<VehiclePiecesController>();
    if ((bool)bv)
    {
      bv.RemovePiece(__instance);
    }

    return true;
  }
}