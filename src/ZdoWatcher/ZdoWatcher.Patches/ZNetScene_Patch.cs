using HarmonyLib;
using ZdoWatcher;

namespace ZdoWatcher.Patches;

public class ZNetScene_Patch
{
  [HarmonyPatch(typeof(ZNetScene), "Shutdown")]
  [HarmonyPostfix]
  private static void ZNetScene_Shutdown()
  {
    ZdoWatchController.Instance.Reset();
  }
}