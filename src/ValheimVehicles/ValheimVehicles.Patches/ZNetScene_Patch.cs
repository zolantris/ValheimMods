using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.Patches;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.ValheimVehicles.RPC;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class ZNetScene_Patch
{
  [HarmonyPatch(typeof(ZNetScene), "CreateDestroyObjects")]
  [HarmonyPrefix]
  private static bool CreateDestroyObjects()
  {
    return !PatchSharedData.m_disableCreateDestroy;
  }
  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
  [HarmonyPostfix]
  private static void ZNetScene_Awake_Subscribe(ZNetScene __instance)
  {
    LoggerProvider.LogDev("called ZNetScene.Awake.");
    PowerSystemRPC.RegisterCustom();
    SwivelPrefabConfigRPC.RegisterCustom();

    LoggerProvider.LogDev($"ZRouteRPC instance, {ZRoutedRpc.instance}");
    // __instance.WithSafeRPCRegister(() =>
    // {
    //  
    // });
  }

  [HarmonyPatch(typeof(Game), nameof(Game.Start))]
  [HarmonyPostfix]
  private static void Game_Start_InjectRPC(ZNetScene __instance)
  {
    LoggerProvider.LogDev($"ZRouteRPC instance, {ZRoutedRpc.instance}");
    PrefabConfigRPC.Register();
    PowerSystemRPC.Register();
    PlayerEitrRPC.Register();
  }

#if DEBUG
  // [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
  // [HarmonyPostfix]
  // private static void ZNetScene_Awake_Subscribe()
  // {
  //   LoggerProvider.LogDev("called ZNetScene.Awake.");
  // }
  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OnDestroy))]
  [HarmonyPrefix]
  private static void ZNetScene_OnDestroy_Subscribe()
  {
    LoggerProvider.LogDev("called ZNetScene_OnDestroy");
  }
#endif
}