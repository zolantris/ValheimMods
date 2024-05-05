using HarmonyLib;
using UnityEngine;

namespace ValheimRAFT.Patches;

public class StartScene_Patch
{
  [HarmonyPatch(typeof(MenuScene), "Awake")]
  [HarmonyPrefix]
  public static bool MenuScene_Awake(MenuScene __instance)
  {
    Object.Destroy(__instance.gameObject);
    return false;
  }

  // do nothing, this breaks character selection but makes it really easy to just load a empty black scene for valheim.
  [HarmonyPatch(typeof(FejdStartup), "UpdateCamera")]
  [HarmonyPrefix]
  public static bool FejdStartup_UpdateCamera_Patch()
  {
    return false;
  }
}