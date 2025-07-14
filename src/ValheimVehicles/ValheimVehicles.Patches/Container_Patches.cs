using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.ModSupport;


namespace ValheimVehicles.Patches;

/// <summary>
/// Patches for all Valheim Containers which are not easy to track without a patch.
/// </summary>
public static class Container_Patches
{
  [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
  [HarmonyPostfix]
  public static void Container_OnAwake(Container __instance)
  {
    ValheimContainerTracker.AddContainer(__instance);
  }

  [HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
  [HarmonyPostfix]
  public static void Container_OnDestroyed(Container __instance)
  {
    ValheimContainerTracker.RemoveContainer(__instance);
  }
}