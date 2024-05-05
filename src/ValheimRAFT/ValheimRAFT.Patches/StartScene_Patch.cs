using HarmonyLib;
using UnityEngine;

namespace ValheimRAFT.Patches;

public class Start_Patch
{
  [HarmonyPatch(typeof(MenuScene), "Awake")]
  [HarmonyPrefix]
  public static bool MenuScene_Awake(MenuScene __instance)
  {
    Object.Destroy(__instance.gameObject);
    return false;
  }
}