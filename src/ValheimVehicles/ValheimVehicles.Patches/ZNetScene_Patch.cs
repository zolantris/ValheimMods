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
  private static void Override_Awake()
  {
    LoggerProvider.LogInfo("ZNetScene is initializing. Prefabs must be registered BEFORE this.");

    LoggerProvider.LogInfo("Checking all registered PieceTables and pieces...");

    foreach (var table in Resources.FindObjectsOfTypeAll<PieceTable>())
    {
      LoggerProvider.LogInfo($"Piece table: {table.name}, pieces: {table.m_pieces.Count}");

      foreach (var piece in table.m_pieces)
      {
        if (piece == null) continue;

        LoggerProvider.LogInfo($"  - {piece.name}");

        // Verify your prefab component exists on the piece
        var hasPowerComponent = piece.GetComponent<PowerStorageComponentIntegration>() != null;
        LoggerProvider.LogInfo($"    - Has PowerStorageComponentIntegration: {hasPowerComponent}");
      }
    }
  }
#endif
}