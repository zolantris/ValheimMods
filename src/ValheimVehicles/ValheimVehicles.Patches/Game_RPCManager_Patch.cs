using HarmonyLib;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.ValheimVehicles.RPC;
namespace ValheimVehicles.Patches;

/// <summary>
/// For all global RPC methods we call Register.
/// - We do not use jotunn due to it spamming debug logs & not picking up correct bepinex package invoked from. 
/// </summary>
public static class Game_RPCManager_Patch
{
  [HarmonyPatch(typeof(Game), nameof(Game.Start))]
  [HarmonyPostfix]
  private static void Game_Start_InjectRPC(ZNetScene __instance)
  {
    SwivelPrefabConfigRPC.Register();
    PrefabConfigRPC.Register();
    PowerSystemRPC.Register();
    PlayerEitrRPC.Register();
  }
}