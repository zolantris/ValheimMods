using HarmonyLib;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class ZNetScene_Patch
{
  [HarmonyPatch(typeof(ZNetScene), "CreateDestroyObjects")]
  [HarmonyPrefix]
  private static bool CreateDestroyObjects()
  {
    return !PatchSharedData.m_disableCreateDestroy;
  }

#if DEBUG
  // [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
  // [HarmonyPostfix]
  // private static void ZNetScene_Awake_Subscribe()
  // {
  //   LoggerProvider.LogDev("called ZNetScene.Awake.");
  // }
  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OnDestroy))]
  [HarmonyPrefix]
  private static void ZNetScene_OnDestroy_Subscribe()
  {
    LoggerProvider.LogDev("called ZNetScene_OnDestroy");
  }
#endif
}