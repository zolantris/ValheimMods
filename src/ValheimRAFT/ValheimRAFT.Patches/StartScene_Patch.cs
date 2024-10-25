using System.Linq;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.ModSupport;

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


  // AudioMan returns a NRE if the StartScene is nuked
  [HarmonyPatch(typeof(AudioMan), "Update")]
  [HarmonyPrefix]
  public static bool AudioMan_Update(AudioMan __instance)
  {
    return __instance.GetActiveAudioListener() != null;
  }
}