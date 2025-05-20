using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Integrations;
using ValheimVehicles.Patches;
using ValheimVehicles.SharedScripts;

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
#if DEBUG
  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
  [HarmonyPrefix]
  private static void ZNetScene_Awake_Subscribe()
  {
    LoggerProvider.LogDebug("called ZNetScene.Awake.");
  }
  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OnDestroy))]
  [HarmonyPrefix]
  private static void ZNetScene_OnDestroy_Subscribe()
  {
    LoggerProvider.LogDebug("called ZNetScene_OnDestroy");
  }
#endif
}