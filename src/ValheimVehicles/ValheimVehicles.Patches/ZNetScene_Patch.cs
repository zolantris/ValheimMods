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
  private static void ZNetScene_Awake_Subscribe()
  {
    LoggerProvider.LogDev("called ZNetScene.Awake.");
    PowerSystemRPC.RegisterCustom();
    SwivelPrefabConfigRPC.Register();
    PrefabConfigRPC.Register();
    PrefabConfigRPC.Register();
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