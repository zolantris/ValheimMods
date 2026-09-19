using HarmonyLib;

namespace ZdoWatcher.Patches;

[HarmonyPatch]
public class ZdoPatch
{
  [HarmonyPatch(typeof(ZDO), nameof(ZDO.Deserialize))]
  [HarmonyPostfix]
  private static void ZDO_Deserialize(ZDO __instance, ZPackage pkg)
  {
    ZdoWatchController.Instance.Deserialize(__instance);
  }

  [HarmonyPatch(typeof(ZDO), nameof(ZDO.Load))]
  [HarmonyPostfix]
  private static void ZDO_Load(ZDO __instance, ZPackage pkg, int version)
  {
    ZdoWatchController.Instance.Load(__instance);
  }

  // this prefix must never return false otherwise it interferes with basegame recycling of zdos
  // SaveClone bail for callbacks is to prevent 
  [HarmonyPatch(typeof(ZDO), nameof(ZDO.Reset))]
  [HarmonyPrefix]
  private static void ZDO_Reset(ZDO __instance)
  {
    ZdoWatchController.Instance.Reset(__instance);
  }
}