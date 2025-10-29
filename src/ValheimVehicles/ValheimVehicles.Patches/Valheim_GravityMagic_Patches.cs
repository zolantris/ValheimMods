using HarmonyLib;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts.Magic;
namespace ValheimVehicles.ValheimVehicles.Patches;

[HarmonyPatch]
public class Valheim_GravityMagic_Patches
{
#if DEBUG
  [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
  [HarmonyPostfix]
  public static void PlayerAwake_InjectGravityMagic(Player __instance)
  {
    if (__instance)
    {
      __instance.gameObject.AddComponent<GravityMagicControllerBridge>();
      __instance.gameObject.AddComponent<GravityForceSpells>();
    }
  }
#endif
}