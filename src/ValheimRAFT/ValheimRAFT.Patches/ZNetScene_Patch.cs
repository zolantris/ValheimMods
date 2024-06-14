using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimRAFT.Util;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class ZNetScene_Patch
{
  [HarmonyPatch(typeof(ZNetScene), "CreateDestroyObjects")]
  [HarmonyPrefix]
  private static bool CreateDestroyObjects()
  {
    return !PatchSharedData.m_disableCreateDestroy;
  }
}