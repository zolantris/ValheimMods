using HarmonyLib;
using ValheimVehicles.Components;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Propulsion.Rudder;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class CharacterAnimEvent_Patch
{
  [HarmonyPatch(typeof(CharacterAnimEvent), "OnAnimatorIK")]
  [HarmonyPrefix]
  private static bool OnAnimatorIK(CharacterAnimEvent __instance,
    int layerIndex)
  {
    if (MechanismSwitch.m_animatedHumanoids.Count > 0)
    {
      foreach (var mAnimatedHumanoid in MechanismSwitch.m_animatedHumanoids)
      {
        if (mAnimatedHumanoid.Key == null || mAnimatedHumanoid.Key.m_animator == null || mAnimatedHumanoid.Value == null) continue;
        mAnimatedHumanoid.Value.UpdateIK(mAnimatedHumanoid.Key.m_animator);
      }
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