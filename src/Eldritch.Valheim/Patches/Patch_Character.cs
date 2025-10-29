// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using Eldritch.Core;
using HarmonyLib;
using UnityEngine;

namespace Eldritch.Valheim
{
  public class Patch_Character
  {
    private static bool IsXeno(Character ch)
    {
      // Robust: check for some unique component you already have
      if (ch.GetComponent<XenoAnimationController>()) return true;

      // Fallback: prefab name or instantiated name match
      var n = ch.name; // instantiated name often has "(Clone)"
      return n.Contains("Eldritch_Xeno", System.StringComparison.OrdinalIgnoreCase)
             || n.Contains("xenomorph-drone-v1", System.StringComparison.OrdinalIgnoreCase);
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    [HarmonyPostfix]
    internal static void Patch_Character_SetLevel_NoScaleForXeno_Awake(Character __instance)
    {
      if (!__instance) return;
      if (!IsXeno(__instance)) return;
      __instance.transform.localScale = Vector3.one; // or your known authored scale
    }


    /// <summary>
    /// Valheim sets scale of Character with more stars messing up most of the balance/hitboxes of eldritch enemies
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(Character), nameof(Character.SetLevel))]
    [HarmonyPostfix]
    internal static void Patch_Character_SetLevel_NoScaleForXeno(Character __instance)
    {
      if (!__instance) return;
      if (!IsXeno(__instance)) return;
      __instance.transform.localScale = Vector3.one; // or your known authored scale
    }
  }
}