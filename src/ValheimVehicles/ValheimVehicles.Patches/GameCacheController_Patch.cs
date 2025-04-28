using HarmonyLib;
using ValheimVehicles.Controllers;

namespace ValheimVehicles.Patches;

public class GameCacheController_Patch
{
  private static GameCacheController cacheControlerInstance;

  /// <summary>
  /// Should only need to add. Removal should happen per Destroy of ZNetScene
  /// </summary>
  /// <param name="__instance"></param>
  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
  [HarmonyPostfix]
  private static void ZNetScene_Awake(ZNetScene __instance)
  {
    __instance.gameObject.AddComponent<GameCacheController>();
  }
}