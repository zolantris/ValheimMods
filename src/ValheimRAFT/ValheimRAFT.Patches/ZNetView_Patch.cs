using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class ZNetView_Patch
{
  [HarmonyPatch(typeof(ZNetView), "ResetZDO")]
  [HarmonyPrefix]
  private static bool ZNetView_ResetZDO(ZNetView __instance)
  {
    if (__instance.m_zdo == null) return false;

    return true;
  }

  [HarmonyPatch(typeof(ZNetView), "Awake")]
  [HarmonyPostfix]
  private static void ZNetView_Awake(ZNetView __instance)
  {
    if (__instance.m_zdo != null)
    {
      MoveableBaseRootComponent.InitPiece(__instance);
      BaseVehicleController.InitPiece(__instance);
      CultivatableComponent.InitPiece(__instance);
    }
  }

  [HarmonyPatch(typeof(ZNetView), "OnDestroy")]
  [HarmonyPrefix]
  private static bool ZNetView_OnDestroy(ZNetView __instance)
  {
    var bv = __instance.GetComponentInParent<BaseVehicleController>();
    if ((bool)bv)
    {
      bv.RemovePiece(__instance);
    }
    else
    {
      var mbr = __instance.GetComponentInParent<MoveableBaseRootComponent>();
      if ((bool)mbr)
      {
        mbr.RemovePiece(__instance);
        if (ValheimRaftPlugin.Instance.DisplacedRaftAutoFix.Value &&
            (bool)Player.m_localPlayer && Player.m_localPlayer.transform.IsChildOf(mbr.transform))
        {
          Logger.LogWarning(
            "DisplacedRaftAutoFix enabled: automatically regenerating broken since the player was attached it the raft");
          MoveRaftConsoleCommand.MoveRaft(Player.m_localPlayer, mbr.m_ship, new Vector3(0, 0, 0));
          // only regenerate if the raft glitches out
          return false;
        }
      }
    }

    return true;
  }
}