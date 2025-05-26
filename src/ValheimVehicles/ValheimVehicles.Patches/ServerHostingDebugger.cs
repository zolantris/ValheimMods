using HarmonyLib;
using UnityEngine;
namespace ValheimVehicles.Patches;

#if DEBUG
/// <summary>
/// To only be included in debug modes to allow infinite connect time when debugging host/zdo server problems.
/// </summary>
public static class NoAutoDisconnectPatch
{
  [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
  [HarmonyPrefix]
  private static bool HostOverride_Prefix()
  {
    // Prevent auto-disconnect (client-side)
    // if (ZNet.instance.IsClient() && YourDebugFlag)
    // {
    //   Debug.LogWarning("Auto-disconnect suppressed for debugging");
    //   return false; // Skip original Disconnect()
    // }
    return true;
  }
}

#endif