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

  [HarmonyPatch(typeof(ZDO), nameof(ZDO.Reset))]
  [HarmonyPrefix]
  private static bool ZDO_Reset(ZDO __instance)
  {
    if (__instance.SaveClone)
    {
      return false;
    }

    ZdoWatchController.Instance.Reset(__instance);

    return false;
  }
}