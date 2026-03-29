using HarmonyLib;
using ValheimVehicles.Integrations;
namespace ValheimVehicles.Patches;

public static class ZNet_WorldSession_Patches
{
  [HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
  [HarmonyPostfix]
  private static void SessionStart()
  {
    if (!ZNet.instance) return;
    var currentWorldId = ZNet.instance.GetWorldUID();
    WorldSessionState.EnsureWorldScope(currentWorldId);
  }

  [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
  [HarmonyPostfix]
  private static void SessionTeardown()
  {
    WorldSessionState.OnSessionTeardown();
  }
}