using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DynamicLocations.Config;
using DynamicLocations.Constants;
using DynamicLocations.Controllers;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using Logger = Jotunn.Logger;

namespace DynamicLocations.Patches;

public class DynamicLocationsPatches
{
  [HarmonyPatch(typeof(Bed), "Interact")]
  [HarmonyPostfix]
  private static void OnSpawnPointUpdated(Bed __instance)
  {
    // todo compare if the current bed zdo is the players otherwise update it.
    var currentSpawnPoint =
      Game.instance.GetPlayerProfile().GetCustomSpawnPoint();

    var character = Player.m_localPlayer as Character;
    if (character.InInterior())
    {
      if (DynamicLocationsConfig.IsDebug)
      {
        Logger.LogDebug(
          "Cannot dynamic spawn inside dungeon or building. InIniterior returned true, must skip.");
      }

      return;
    }

    var spawnController = PlayerSpawnController.Instance;
    if (!spawnController) return;
    spawnController?.SyncBedSpawnPoint(__instance.m_nview.GetZDO(), __instance);
  }

  // [HarmonyPatch(typeof(PlayerProfile), "SetLogoutPoint")]
  // [HarmonyPostfix]
  // private static void OnSaveLogoutPoint(bool __result)
  // {
  //   // PlayerSpawnController.Instance.SyncLogoutPoint();
  // }


  [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
  [HarmonyPostfix]
  private static void OnDeathDestroyLogoutPoint(Player __instance)
  {
    if (!__instance.m_nview.IsOwner())
    {
      return;
    }

    try
    {
      LocationController.RemoveZdoTarget(
        LocationVariation.Logout, __instance);
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error occurred while removing a zdotarget. \n{e}");
    }
  }

  [HarmonyPatch(typeof(Player), "ShowTeleportAnimation")]
  [HarmonyPostfix]
  private static void ShowTeleportAnimation(bool __result)
  {
    if (PlayerSpawnController.Instance == null) return;

    var isRespawnTeleporting =
      PlayerSpawnController.Instance.IsTeleportingToDynamicLocation;

    if (isRespawnTeleporting)
    {
      __result = false;
    }
  }

  [HarmonyPatch(typeof(Game), nameof(Game.Awake))]
  [HarmonyPostfix]
  private static void AddSpawnController(Game __instance)
  {
    Logger.LogDebug(
      "Game Awake called and added PlayerSpawnController and LocationController");
    __instance.gameObject.AddComponent<LocationController>();
    __instance.gameObject.AddComponent<PlayerSpawnController>();
  }

  [HarmonyPatch(typeof(Game), nameof(Game.OnDestroy))]
  [HarmonyPostfix]
  private static void ResetSpawnController(Game __instance)
  {
    try
    {

      LocationController.ResetCachedValues();
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error with ResetSpawnController \n{e}");
    }
  }


  [HarmonyPatch(typeof(Game), nameof(Game.RequestRespawn))]
  [HarmonyPrefix]
  public static bool RequestRespawnListener(Game __instance)
  {
#if DEBUG || BETA
    if (DynamicLocationsConfig.PlayerRespawnImmediately.Value)
    {
      __instance._RequestRespawn();
      return false;
    }
#endif
    return true;
  }


  public static void SetupPlayerDebugValues()
  {
#if DEBUG || BETA
    if (Game.instance != null)
    {
      Game.instance.m_fadeTimeDeath = DynamicLocationsConfig.PlayerRespawnFadeTime.Value;
    }
    Character.m_debugFlySpeed = Mathf.RoundToInt(DynamicLocationsConfig.FastDebugFlySpeed.Value);
#endif
  }

  /// <summary>
  /// Noting that I could override Game.FindSpawnPoint and PlayerProfile.HaveLogoutPoint to return the updated data. Updating this would then force the game to load in the correct location.
  ///
  /// Limitations
  /// - requested point would have to be accurate but since the check runs ever FixedUpdate, it could not have asynchronous task data, meaning it still needs a coroutine for loading the ZDO + setting the area to load   
  /// </summary>
  /// <param name="__instance"></param>
  /// <param name="__result"></param>
  [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
  [HarmonyPostfix]
  private static void OnSpawned(Game __instance, Player __result)
  {

    if (ZNetView.m_forceDisableInit) return;
    if (!DynamicLocationsConfig.EnableDynamicLogoutPoint.Value && !DynamicLocationsConfig.EnableDynamicLogoutPoint.Value)
      if (__result == null || Game.instance == null)
        return;

    SetupPlayerDebugValues();

    var character = __result;
    var profile = Game.instance.GetPlayerProfile();

    if (PlayerSpawnController.Instance == null)
    {
      LoggerProvider.LogWarning("No spawn controller. This means dynamic locations is not working.");
      return;
    }

    // Always prefer logout point if valid
    if (profile?.HaveLogoutPoint() == true)
    {
      if (DynamicLocationsConfig.EnableDynamicLogoutPoint.Value &&
          !character.InInterior() &&
          !character.InIntro())
      {
        PlayerSpawnController.Instance.MovePlayerToLogoutPoint();
      }
    }
    // Only use spawn point if no logout point and death respawn
    else if (__instance.m_respawnAfterDeath &&
             DynamicLocationsConfig.EnableDynamicSpawnPoint.Value &&
             !character.InIntro())
    {
      PlayerSpawnController.Instance.MovePlayerToSpawnPoint();
    }
  }

}