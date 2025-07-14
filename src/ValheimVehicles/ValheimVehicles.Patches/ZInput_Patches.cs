using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
namespace ValheimVehicles.Patches;

public static class ZInput_Patches
{
  public static bool ShouldBlockInputForAlpha1234Keys = false;

  public static readonly HashSet<string> BlockedInputs = new()
  {
    // Hotbar keys
    "Hotbar1", "Hotbar2", "Hotbar3", "Hotbar4",
    // Cannon controls (your mappings)
    "JoyDPadUp", "JoyDPadDown", "JoyDPadLeft", "JoyDPadRight",
    // Camera zoom (mouse scroll bindings in Valheim)
    "CamZoomIn", "CamZoomOut", "JoyCamZoomIn", "JoyCamZoomOut"
    // Add more as needed!
  };

  [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButton))]
  [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown))]
  [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonUp))]
  [HarmonyPrefix]
  public static bool ZInput_BlockHotbarInputs(ref bool __result, string name)
  {
    if (ShouldBlockInputForAlpha1234Keys && BlockedInputs.Contains(name))
    {
      __result = false;
      return false; // Block original call
    }
    return true; // Allow default processing
  }

  [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
  [HarmonyPrefix]
  public static bool ZInput_BlockMouseScroll(ref float __result)
  {
    if (ShouldBlockInputForAlpha1234Keys)
    {
      __result = 0f;
      return false; // Block original call
    }
    return true;
  }
}