using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Vehicles.Components;

namespace ValheimRAFT.Patches;

public class DynamicSpawnAndLogoutPatches
{
  [HarmonyPatch(typeof(Bed), "Interact")]
  [HarmonyPostfix]
  private static void OnSpawnPointUpdated(Bed __instance)
  {
    var currentSpawnPoint = Game.instance.GetPlayerProfile().GetCustomSpawnPoint();
    // if (prevCustomSpawnPoint == currentSpawnPoint) return;

    var spawnController = PlayerSpawnController.GetSpawnController(Player.m_localPlayer);
    if (!spawnController) return;
    spawnController?.SyncBedSpawnPoint(__instance.m_nview, __instance);
  }

  [HarmonyPatch(typeof(PlayerProfile), "SaveLogoutPoint")]
  [HarmonyPostfix]
  private static void OnSaveLogoutPoint()
  {
    var spawnController = Player.m_localPlayer.GetComponentInChildren<PlayerSpawnController>();
    if (spawnController)
    {
      spawnController.SyncLogoutPoint();
    }
  }

  [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
  [HarmonyPostfix]
  private static void OnSpawned(Player __result)
  {
    if (ZNetView.m_forceDisableInit) return;
    __result.gameObject.AddComponent<PlayerSpawnController>();
  }
}