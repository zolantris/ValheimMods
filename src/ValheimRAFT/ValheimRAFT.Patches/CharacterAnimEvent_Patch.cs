using HarmonyLib;
using ValheimVehicles.Propulsion.Rudder;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class CharacterAnimEvent_Patch
{
  [HarmonyPatch(typeof(CharacterAnimEvent), "OnAnimatorIK")]
  [HarmonyPrefix]
  private static bool OnAnimatorIK(CharacterAnimEvent __instance, int layerIndex)
  {
    if (__instance.m_character is Player player && player.IsAttached() &&
        (bool)player.m_attachPoint && (bool)player.m_attachPoint.parent)
    {
      var rudder = player.m_attachPoint.parent.GetComponent<RudderComponent>();
      if ((bool)rudder) rudder.UpdateIK((player).m_animator);
      var ladder = player.m_attachPoint.parent.GetComponent<RopeLadderComponent>();
      if ((bool)ladder)
      {
        ladder.UpdateIK((player).m_animator);
        return false;
      }
    }

    return true;
  }
}