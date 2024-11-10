using System;
using HarmonyLib;

namespace ValheimVehicles.Patches;

public class Minimap_VehicleIcons
{
  // [HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
  // [HarmonyPostfix]
  // public static void ForceUpdateIconSpriteSize(Minimap __instance)
  // {
  //   __instance.m_visibleIconTypes =
  //     new bool[Enum.GetValues(typeof(Minimap.PinType)).Length];
  //
  //   // adds additional indexes for icons we want.
  //   __instance.m_visibleIconTypes[__instance.m_visibleIconTypes.Length] = true;
  //
  //   return;
  // }
}