using HarmonyLib;
using ValheimVehicles.RPC;
namespace ValheimVehicles.Patches;

/// <summary>
/// For all global RPC methods we call Register.
/// - We do not use jotunn due to it spamming debug logs & not picking up correct bepinex package invoked from. 
/// </summary>
public static class RPCManager_Patches
{
  /// <summary>
  /// This must be invoked in both Game and ZNet in case there is a dsync or client rapidly re-connects and breaks Game.
  /// </summary>
  public static void RegisterAllRPCs()
  {
    // registers the methods so RPCManager can reference them.
    SwivelPrefabConfigRPC.RegisterAll();
    PrefabConfigRPC.RegisterAll();
    PowerSystemRPC.RegisterAll();
    PlayerEitrRPC.RegisterAll();

    // finally, call the network registry.
    RPCManager.RegisterAllRPCs();
  }

  [HarmonyPatch(typeof(Game), "Start")]
  [HarmonyPostfix]
  private static void Game_Start_InjectRPC()
  {
    RegisterAllRPCs();
  }

  [HarmonyPatch(typeof(ZNet), "Start")]
  [HarmonyPostfix]
  private static void ZNet_Start_InjectRPC()
  {
    RegisterAllRPCs();
  }
}