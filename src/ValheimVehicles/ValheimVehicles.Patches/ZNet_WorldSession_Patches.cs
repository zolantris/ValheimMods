using HarmonyLib;
using ValheimVehicles.Integrations;
namespace ValheimVehicles.Patches;

public static class ZNet_WorldSession_Patches
{
  [HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
  [HarmonyPostfix]
  private static void SessionStart(ZNet __instance)
  {
    // A joining client has no world yet. Hosts already loaded theirs in Start.
    if (__instance.IsServer()) EnsureWorldScope(__instance);
  }

  [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
  [HarmonyPostfix]
  private static void ClientWorldReceived(ZNet __instance)
  {
    // PeerInfo supplies the client's world, but also returns early on failed
    // handshakes. Only initialize after the connection was accepted.
    if (__instance.IsServer() || ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected) return;
    EnsureWorldScope(__instance);
  }

  private static void EnsureWorldScope(ZNet instance)
  {
    if (!instance || instance != ZNet.instance) return;
    var world = instance.GetWorld();
    if (world == null) return;
    WorldSessionState.EnsureWorldScope(world.m_uid);
  }

  [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
  [HarmonyPostfix]
  private static void SessionTeardown(ZNet __instance)
  {
    // ZNet.OnDestroy clears the singleton for its own instance. An older
    // instance finishing destruction must not clear a newer session's state.
    if (ZNet.instance && ZNet.instance != __instance) return;
    WorldSessionState.OnSessionTeardown();
  }
}
