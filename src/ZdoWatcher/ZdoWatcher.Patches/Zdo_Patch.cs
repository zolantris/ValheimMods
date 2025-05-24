using HarmonyLib;

namespace ZdoWatcher.Patches;

[HarmonyPatch]
public class ZdoPatch
{
  [HarmonyPatch(typeof(ZDO), "Deserialize")]
  [HarmonyPostfix]
  private static void ZDO_Deserialize(ZDO __instance, ZPackage pkg)
  {
    ZdoWatchController.Instance.Deserialize(__instance);
  }

  [HarmonyPatch(typeof(ZDO), nameof(ZDO.Init))]
  [HarmonyPostfix]
  private static void ZDO_Created(ZDO __instance)
  {
    ZdoWatchController.Instance.Init(__instance);
  }

  [HarmonyPatch(typeof(ZDO), "Load")]
  [HarmonyPostfix]
  private static void ZDO_Load(ZDO __instance, ZPackage pkg, int version)
  {
    ZdoWatchController.Instance.Load(__instance);
  }

  [HarmonyPatch(typeof(ZDO), "Reset")]
  [HarmonyPrefix]
  private static void ZDO_Reset(ZDO __instance)
  {
    ZdoWatchController.Instance.Reset(__instance);
  }
}