using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Interfaces;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class CharacterAnimEvent_Patch
{
  public static Dictionary<Animator, IAnimatorHandler> m_animatedHumanoids = new();

  [HarmonyPatch(typeof(CharacterAnimEvent), "OnAnimatorIK")]
  [HarmonyPrefix]
  private static bool OnAnimatorIK(CharacterAnimEvent __instance,
    int layerIndex)
  {
    if (m_animatedHumanoids.Count > 0 && __instance.m_animator != null && m_animatedHumanoids.TryGetValue(__instance.m_animator, out var activator))
    {
      activator.UpdateIK(__instance.m_animator);
      return false;
    }
    if (__instance.m_character is Player player && player.IsAttached() &&
        (bool)player.m_attachPoint && (bool)player.m_attachPoint.parent)
    {
      var animator = player.m_attachPoint.GetComponentInParent<IAnimatorHandler>();
      if (animator != null)
      {
        animator.UpdateIK(player.m_animator);
        return false;
      }
    }

    return true;
  }
}