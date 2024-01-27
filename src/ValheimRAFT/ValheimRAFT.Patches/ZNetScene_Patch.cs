using HarmonyLib;
using ValheimRAFT.Util;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class ZNetScene_Patch
{
  [HarmonyPatch(typeof(ZNetScene), "CreateDestroyObjects")]
  [HarmonyPrefix]
  private static bool CreateDestroyObjects()
  {
    return !PatchSharedData.m_disableCreateDestroy;
  }

  [HarmonyPatch(typeof(ZNetScene), "Shutdown")]
  [HarmonyPostfix]
  private static void ZNetScene_Shutdown()
  {
    ZDOPersistentID.Instance.Reset();
  }
}