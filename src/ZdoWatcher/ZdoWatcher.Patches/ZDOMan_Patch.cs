using HarmonyLib;

namespace ZdoWatcher.Patches;

public class ZDOMan_Patch
{
  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.AddPeer))]
  [HarmonyPostfix]
  private static void OnAddPeer(ZDOMan __instance)
  {
    var listCount = __instance.m_peers.Count - 1;
    if (listCount < 1)
    {
      return;
    }

    var currentPeer = __instance.m_peers[listCount];
    ZdoWatchController.Instance.SyncToPeer(
      currentPeer);
  }
}