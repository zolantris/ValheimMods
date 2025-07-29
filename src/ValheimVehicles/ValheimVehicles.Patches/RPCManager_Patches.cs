using System;
using System.Collections.Generic;
using HarmonyLib;
using ValheimVehicles.RPC;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;
namespace ValheimVehicles.Patches;

/// <summary>
/// For all global RPC methods we call Register.
/// - We do not use jotunn due to it spamming debug logs & not picking up correct bepinex package invoked from. 
/// </summary>
public static class RPCManager_Patches
{
  public static bool hasRegisteredAllRPCs = false;
  /// <summary>
  /// This must be invoked in both Game and ZNet in case there is a dsync or client rapidly re-connects and breaks Game.
  /// </summary>
  public static void RegisterAllRPCs()
  {
    if (hasRegisteredAllRPCs) return;
    LoggerProvider.LogDebug("Registering Global RPC handlers for ValheimVehicles");

    // registers the methods so RPCManager can reference them.
    SwivelPrefabConfigRPC.RegisterAll();
    PrefabConfigRPC.RegisterAll();
    PowerSystemRPC.RegisterAll();
    PlayerEitrRPC.RegisterAll();
    CannonHandHeldController.RegisterCannonControllerRPCs();
    TargetController.RegisterCannonControllerRPCs();

    // finally, call the network registry.
    RPCManager.RegisterAllRPCs();

    hasRegisteredAllRPCs = true;
  }

  [HarmonyPatch(typeof(Game), "Start")]
  [HarmonyPostfix]
  private static void Game_Start_InjectRPC()
  {
    RegisterAllRPCs();
  }

  [HarmonyPatch(typeof(ZNetScene), "Awake")]
  [HarmonyPostfix]
  private static void ZNetScene_Awake_InjectRPC()
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