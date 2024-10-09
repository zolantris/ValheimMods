using HarmonyLib;

namespace YggdrasilTerrain.Patches;

public class ZNetScene_Patch
{
  [HarmonyPatch(typeof(ZNetScene), "Awake")]
  [HarmonyPostfix]
  private static void ZNetScene_Start()
  {
    YggdrasilBranch.OnSceneReady();
  }
}