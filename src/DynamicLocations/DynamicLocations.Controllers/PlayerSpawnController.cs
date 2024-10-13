using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DynamicLocations.Config;
using DynamicLocations.DynamicLocations.Interfaces;
using UnityEngine;
using ZdoWatcher;
using Logger = Jotunn.Logger;

namespace DynamicLocations.Controllers;

/// <summary>
/// This component does the following
/// - Associates the player id with the vehicle id for logout point
/// - Associates the player id with the beds on a vehicle if they die
/// - Unsets / breaks association if player leaves the vehicle for logout point
/// - Unsets / breaks association if player interacts with a bed that is not within the vehicle
///
/// - Syncs all this data on demand to prevent spamming zdo apis.
/// </summary>
public class PlayerSpawnController : MonoBehaviour
{
  // spawn (Beds)
  public static bool CanUpdateLogoutPoint = true;
  public static bool CanRemoveLogoutAfterSync = false;
  public bool IsTeleportingToDynamicLocation = false;

  public static Dictionary<long, PlayerSpawnController> Instances = new();

  public static PlayerSpawnController? Instance;
  private Stopwatch UpdateLocationTimer = new();
  public static Player? player => Player.m_localPlayer;

  private void Awake()
  {
    Instance = this;
    Setup();
  }

  private void OnDisable()
  {
    IsTeleportingToDynamicLocation = false;
    StopAllCoroutines();
  }

  private void Setup()
  {
    // forceDisableInit prevents running awake commands for znetview when it's not ready
    if (ZNetView.m_forceDisableInit) return;
    if (player == null) return;

#if DEBUG
    Logger.LogDebug("listing all player custom keys");
    foreach (var key in player.m_customData.Keys)
    {
      Logger.LogDebug($"key: {key} val: {player.m_customData[key]}");
    }
#endif
  }

  /// <summary>
  /// Persists beds across sessions, requires the bed netview input
  /// </summary>
  /// <param name="netView"></param>
  /// <returns></returns>
  public ZNetView? PersistBedZdo(Bed bed)
  {
    var netView = bed.GetComponent<ZNetView>();
    if (!netView) return null;
    // Beds must be persisted when syncing spawns otherwise they cannot be retrieved directly across sessions / on server shutdown and would require a deep search of all objects.
    var persistentId =
      ZdoWatchManager.Instance.GetOrCreatePersistentID(netView.GetZDO());

    if (persistentId == 0)
    {
      Logger.LogError(
        "No persistent ID returned for bed, this should not be possible. Please report this error");
      LocationController.RemoveSpawnTargetZdo(player);
      return null;
    }

    return netView;
  }

  /// <summary>
  /// Sets or removes the spawnPointZdo to the bed it is associated with a moving zdo
  /// - Only should be called when the bed is interacted with
  /// - This id is used to poke a zone and load it, then teleport the player to their bed like they are spawning
  /// </summary>
  /// <param name="spawnPointObj"></param>
  /// <returns>bool</returns>
  public void SyncBedSpawnPoint(ZNetView spawnPointObj, Bed bed)
  {
    // should sync the zdo just in case it doesn't match player
    if (player == null) return;

    if (!bed.IsMine() && bed.GetOwner() != 0L)
    {
      // exit b/c this is another player's bed, this should not set as a spawn
      return;
    }

    var netView = PersistBedZdo(bed);
    if (netView == null) return;

    if (spawnPointObj.transform.position !=
        spawnPointObj.transform.localPosition &&
        netView.transform.position != spawnPointObj.transform.position)
    {
      var offset = spawnPointObj.transform.localPosition;
      // must be parsed to ZDOID after reading from custom player data
      LocationController.SetSpawnZdoTargetWithOffset(player, netView,
        offset);
    }
    else
    {
      LocationController.SetSpawnZdoTargetWithOffset(player, netView,
        netView.transform.position - spawnPointObj.transform.position);
    }
  }

  /// <summary>
  /// Must be called on logout, but also polled to prevent accidental dsync
  /// </summary>
  /// <param name="nv"></param>
  /// <returns>bool</returns>
  public void SyncLogoutPoint()
  {
    if (!player || player == null || !CanUpdateLogoutPoint) return;
    var netView = player.GetComponentInParent<ZNetView>();
    if (!ZdoWatchManager.GetPersistentID(netView.GetZDO(),
          out var persistentId))
    {
      LocationController.RemoveLogoutZdo(player);
      return;
    }

    if (persistentId == 0)
    {
      Logger.LogDebug("vehicleZdoId is invalid");
      return;
    }

    if (player.transform.localPosition != player.transform.position)
    {
      LocationController.SetLogoutZdoWithOffset(player, netView,
        player.transform.localPosition);
    }
    else
    {
      LocationController.SetLogoutZdo(player, netView);
    }

    Game.instance.m_playerProfile.SavePlayerData(player);
  }

  // public static PlayerSpawnController? GetSpawnController(Player currentPlayer)
  // {
  //   return Instance
  //   // if (player == null) return null;
  //   // if (!currentPlayer) return null;
  //   //
  //   // if (Instances.TryGetValue(currentPlayer.GetPlayerID(), out var instance))
  //   // {
  //   //   instance.transform.position = currentPlayer.transform.position;
  //   //   instance.transform.SetParent(currentPlayer.transform);
  //   //   return instance;
  //   // }
  //   //
  //   // var spawnController = currentPlayer.GetComponent<PlayerSpawnController>();
  //   //
  //   // return spawnController;
  // }

  public bool MovePlayerToLoginPoint()
  {
    IsTeleportingToDynamicLocation = false;
    if (!player)
    {
      Setup();
    }

    if (player == null) return false;
    var loginZdoOffset = LocationController.GetLogoutZdoOffset(player);
    var loginZdoid = LocationController.GetLogoutZdo(player);

    if (loginZdoid == null)
    {
      // when the player is respawning from death this will be null or on first launch with these new keys
      return false;
    }

    StartCoroutine(UpdateLocation(loginZdoid, loginZdoOffset,
      LocationTypes.Logout));

    return true;
  }

  public enum LocationTypes
  {
    Spawn,
    Logout
  }

  public IEnumerator UpdateLocation(ZDO? zdoid, Vector3 offset,
    LocationTypes locationType)
  {
    UpdateLocationTimer.Restart();
    yield return MovePlayerToZdo(zdoid, offset);
    IsTeleportingToDynamicLocation = false;
    UpdateLocationTimer.Reset();
    // must be another coroutine AND only fired after the Move coroutine completes otherwise it WILL break the move coroutine as it deletes the required key.
    // remove logout point after moving the player.
    if (locationType == LocationTypes.Logout)
    {
      if (CanRemoveLogoutAfterSync)
      {
        LocationController.RemoveLogoutZdo(player);
      }
    }

    if (locationType == LocationTypes.Spawn)
    {
    }
  }

  public void MovePlayerToSpawnPoint()
  {
    if (!player) return;

    var spawnZdoOffset = LocationController.GetSpawnTargetZdoOffset(player);
    var spawnZdoid = LocationController.GetSpawnTargetZdo(player);

    StartCoroutine(UpdateLocation(spawnZdoid, spawnZdoOffset,
      LocationTypes.Spawn));
  }

  private void SyncPlayerPosition(Vector3 newPosition)
  {
    Logger.LogDebug("Running PlayerPosition Sync");
    if (ZNetView.m_forceDisableInit || player == null) return;
    var playerZdo = player.m_nview.GetZDO();
    if (playerZdo == null)
    {
      Logger.LogDebug("Player zdo invalid exiting");
      return;
    }

    Logger.LogDebug($"Syncing Player Position and sector, {newPosition}");
    var isLoaded =
      ZoneSystem.instance.IsZoneLoaded(
        ZoneSystem.instance.GetZone(newPosition));

    if (!isLoaded)
    {
      Logger.LogDebug(
        $"zone not loaded, exiting SyncPlayerPosition for position: {newPosition}");
      return;
    }

    ZNet.instance.SetReferencePosition(newPosition);
    playerZdo.SetPosition(newPosition);
    playerZdo.SetSector(ZoneSystem.instance.GetZone(newPosition));
    player.transform.position = newPosition;
  }

  // add a callback that ValheimRAFT/Vehicles can bind to and delegate it's classes to.
  // public Interpolate.Function IsWithinVehicleCallback;

  // private bool IsWithinVehicle()
  // {
  //   return true;
  // }

  public delegate TResult Func<in T, out TResult>(T arg);

  // Meant for being overridden by the ValheimRAFT mod
  public static Func<ZNetView?, IEnumerator> PlayerMoveToVehicleCallback =
    OnPlayerMoveToVehiclePlaceholder;

  private static IEnumerator OnPlayerMoveToVehiclePlaceholder(ZNetView? obj)
  {
    yield return null;
  }

  private static IEnumerator OnPlayerMoveToVehicle(ZNetView? netView)
  {
    var output = PlayerMoveToVehicleCallback(netView);
    yield return output;
  }

  /// <summary>
  /// Does not work, zdoids are not persistent across game and loading content outside a zone does not work well without a reference that persists.
  /// </summary>
  /// <param name="zdoid"></param>
  /// <param name="offset"></param>
  /// <returns></returns>
  public IEnumerator MovePlayerToZdo(ZDO? zdo, Vector3 offset)
  {
    Logger.LogDebug("Running MovePlayerToZdo");
    if (!player) yield break;

    if (zdo == null)
    {
      yield break;
    }

    IsTeleportingToDynamicLocation = true;
    player?.TeleportTo(zdo.GetPosition() + Vector3.up *
      DynamicLocationsConfig.RespawnHeightOffset.Value, zdo.GetRotation(),
      distantTeleport: true);

    // beginning of LoadGameObjectInSector (which cannot be abstracted easily if the return is required)
    // ZDOMan.instance.RequestZDO(zdo.m_uid);
    ZNetView? zdoNetViewInstance = null;
    var isZoneLoaded = false;
    var zoneId = ZoneSystem.instance.GetZone(zdo.GetPosition());

    while (!isZoneLoaded || zdoNetViewInstance == null)
    {
      if (UpdateLocationTimer is { ElapsedMilliseconds: > 10000 })
      {
        Logger.LogWarning(
          $"Timed out: Attempted to spawn player on Boat ZDO expired for {zdo.m_uid}, reason -> spawn zdo was not found");
        yield break;
      }

      zoneId = ZoneSystem.instance.GetZone(zdo.GetPosition());
      ZoneSystem.instance.PokeLocalZone(zoneId);

      var tempInstance = ZNetScene.instance.FindInstance(zdo);

      if (tempInstance == null)
      {
        Logger.LogWarning(
          $"The zdo instance not found ");
      }
      else
      {
        Logger.LogWarning(
          $"The zdo instance named: {tempInstance.name}, -> was found ");
        zdoNetViewInstance = tempInstance;
      }

      if (zdoNetViewInstance) break;
      yield return new WaitForEndOfFrame();
      if (!ZoneSystem.instance.IsZoneLoaded(zoneId))
      {
        yield return null;
      }
      else
      {
        isZoneLoaded = true;
      }
    }

    yield return OnPlayerMoveToVehicle(zdoNetViewInstance);


    var zdoPosition = zdo.GetPosition();
    var zdoRotation = zdo.GetRotation();
    if (!player) yield break;
    // (-2335.91064, 33.813118, -5291.97412)
    // var positionWithOffset = zdoPosition.Value + offset;
    var positionWithOffset =
      zdoNetViewInstance?.transform.position;

    yield return new WaitUntil(() =>
      Player.m_localPlayer.IsTeleporting() == false);

    // SyncPlayerPosition(positionWithOffset);
    // -2275.258 32.3295 -5309.554
    // this might not be needed, but as a backup this is good b/c it avoids teleporting to wrong area especially if the zone suddenly unloads
    if (player != null)
    {
      player.TeleportTo((positionWithOffset ?? zdoPosition) + Vector3.up *
        DynamicLocationsConfig.RespawnHeightOffset.Value, zdo.GetRotation(),
        false);
    }

    IsTeleportingToDynamicLocation = false;
  }
}