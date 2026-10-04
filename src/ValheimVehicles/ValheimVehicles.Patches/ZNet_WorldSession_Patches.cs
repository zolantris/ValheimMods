using HarmonyLib;
using ValheimVehicles.Integrations;
namespace ValheimVehicles.Patches;

public static class ZNet_WorldSession_Patches
{
  /// <summary>
  /// ZDOMan is created in ZNet.Awake before the server loads the world and before a client receives any ZDOs. This is the point where all previous ZDOIDs become invalid.
  /// </summary>
  [HarmonyPatch(typeof(ZDOMan), MethodType.Constructor, typeof(int))]
  [HarmonyPostfix]
  private static void ZDOMan_Constructor()
  {
    WorldSessionState.ResetZdoScopedRegistries();
    WorldSessionState.ResetStaticEntries();
  }

  [HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
  [HarmonyPostfix]
  private static void SessionStart()
  {
    if (!ZNet.instance || ZNet.m_world == null) return;
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