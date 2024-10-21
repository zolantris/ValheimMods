using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DynamicLocations.Config;
using DynamicLocations.Constants;
using DynamicLocations.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;
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
      ZdoWatchController.Instance.GetOrCreatePersistentID(netView.GetZDO());

    if (persistentId == 0)
    {
      Logger.LogError(
        "No persistent ID returned for bed, this should not be possible. Please report this error");
      LocationController.RemoveSpawnTargetZdo(player);
      return null;
    }

    // required for setting a persistent bed. Without this value set it will persist the ID but the actual item will not be known so if the bed is deleted during session it would not match
    netView.GetZDO().Set(ZdoVarKeys.DynamicLocationsPoint, 1);


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
    if (!ZdoWatchController.GetPersistentID(netView.GetZDO(),
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

  private bool SpawnTeleport(Vector3 position, Quaternion rotation)
  {
    if (player == null) return false;

    player.m_teleportCooldown = 15;
    player.m_teleporting = false;

    return player.TeleportTo(
      position,
      rotation,
      DynamicLocationsConfig
        .DebugDistancePortal.Value);
  }

  public Coroutine MovePlayerToLogoutPoint()
  {
    return StartCoroutine(UpdateLocation(LocationTypes.Logout));
  }

  public enum LocationTypes
  {
    Spawn,
    Logout
  }

  private IEnumerator UpdateLocation(LocationTypes locationType)
  {
    var offset = GetZdoOffsetForType(locationType);
    var zdoid = GetZdoidForType(locationType);
    UpdateLocationTimer.Restart();
    IsTeleportingToDynamicLocation = false;
    yield return MovePlayerToZdo(zdoid, offset);
    IsTeleportingToDynamicLocation = false;
    UpdateLocationTimer.Reset();

    switch (locationType)
    {
      // must be another coroutine AND only fired after the Move coroutine completes otherwise it WILL break the move coroutine as it deletes the required key.
      // remove logout point after moving the player.
      case LocationTypes.Logout when player != null:
      {
        if (CanRemoveLogoutAfterSync)
        {
          yield return LocationController.RemoveLogoutZdo(player);
        }

        break;
      }
      case LocationTypes.Spawn:
        break;
    }

    yield return true;
  }

  public void MovePlayerToSpawnPoint()
  {
    StartCoroutine(UpdateLocation(LocationTypes.Spawn));
  }

  private static Vector3 GetZdoOffsetForType(LocationTypes locationTypes)
  {
    return locationTypes switch
    {
      LocationTypes.Spawn => LocationController.GetSpawnTargetZdoOffset(player),
      LocationTypes.Logout => LocationController.GetLogoutZdoOffset(player),
      _ => throw new ArgumentOutOfRangeException(nameof(locationTypes),
        locationTypes, null)
    };
  }

  private static IEnumerator GetZdoidForType(LocationTypes locationTypes)
  {
    yield return locationTypes switch
    {
      LocationTypes.Spawn => LocationController.GetSpawnTargetZdo(player),
      LocationTypes.Logout => LocationController.GetLogoutZdo(player),
      _ => throw new ArgumentOutOfRangeException(nameof(locationTypes),
        locationTypes, null)
    };
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

  private bool HasExpiredTimer => UpdateLocationTimer is
    { ElapsedMilliseconds: > 10000 };

  /// <summary>
  /// Does not work, zdoids are not persistent across game and loading content outside a zone does not work well without a reference that persists.
  /// </summary>
  /// <param name="zdoid"></param>
  /// <param name="zdo"></param>
  /// <param name="offset"></param>
  /// <returns></returns>
  private IEnumerator MovePlayerToZdo(ZDO? zdo, Vector3 offset)
  {
    if (DynamicLocationsConfig.IsDebug)
    {
      Logger.LogDebug("Running MovePlayerToZdo");
    }

    if (!player) yield break;

    if (zdo == null)
    {
      yield break;
    }

    var teleportHeightOffset = Vector3.up * DynamicLocationsConfig
      .RespawnHeightOffset.Value;
    var teleportPosition = zdo.GetPosition() + teleportHeightOffset;

    var zoneId = ZoneSystem.instance.GetZone(zdo.GetPosition());
    ZoneSystem.instance.PokeLocalZone(zoneId);

    // yield return new WaitUntil(() => ZoneSystem.instance.IsZoneLoaded(zoneId));
    // var item = new WaitUntil(() => ZNetScene.instance.FindInstance(zdo));
    // yield return item;
    // TODO add check for item and confirm it has a valid ZDO DynamicLocationPoint var

    IsTeleportingToDynamicLocation =
      SpawnTeleport(teleportPosition, zdo.GetRotation());

    if (!IsTeleportingToDynamicLocation)
    {
      Logger.LogError(
        "Teleport command failed for player, exiting dynamic spawn MovePlayerToZdo.");
      yield break;
    }

    var character = player?.GetComponent<Character>();
    if (DynamicLocationsConfig.FreezePlayerPosition.Value && character != null)
    {
      character.m_body.isKinematic = true;
    }

    ZNetView? zdoNetViewInstance = null;
    var isZoneLoaded = false;

    zoneId = ZoneSystem.instance.GetZone(zdo.GetPosition());
    // while (!isZoneLoaded || zdoNetViewInstance == null)
    // {
    //   if (UpdateLocationTimer is { ElapsedMilliseconds: > 10000 })
    //   {
    //     if (DynamicLocationsConfig.IsDebug)
    //     {
    //       Logger.LogWarning(
    //         $"Timed out: Attempted to spawn player on Boat ZDO expired for {zdo.m_uid}, reason -> spawn zdo was not found");
    //     }
    //
    //     yield break;
    //   }
    //
    //   zoneId = ZoneSystem.instance.GetZone(zdo.GetPosition());
    //   ZoneSystem.instance.PokeLocalZone(zoneId);
    //
    //   var tempInstance = ZNetScene.instance.FindInstance(zdo);
    //
    //   if (tempInstance == null)
    //   {
    //     if (DynamicLocationsConfig.IsDebug)
    //     {
    //       Logger.LogInfo(
    //         $"The zdo instance not found ");
    //     }
    //   }
    //   else
    //   {
    //     if (DynamicLocationsConfig.IsDebug)
    //     {
    //       Logger.LogInfo(
    //         $"The zdo instance named: {tempInstance.name}, -> was found ");
    //     }
    //
    //     zdoNetViewInstance = tempInstance;
    //   }
    //
    //   if (zdoNetViewInstance) break;
    //   yield return new WaitForEndOfFrame();
    //   if (!ZoneSystem.instance.IsZoneLoaded(zoneId))
    //   {
    //     yield return null;
    //   }
    //   else
    //   {
    //     isZoneLoaded = true;
    //   }
    // }
    yield return new WaitUntil(() =>
      Player.m_localPlayer.IsTeleporting() == false || HasExpiredTimer);
    zdoNetViewInstance = ZNetScene.instance.FindInstance(zdo);

    if (DynamicLocationsConfig.FreezePlayerPosition.Value && character != null)
    {
      character.m_body.isKinematic = false;
    }

    yield return OnPlayerMoveToVehicle(zdoNetViewInstance);

    if (DynamicLocationsConfig.DebugForceUpdatePositionAfterTeleport.Value &&
        DynamicLocationsConfig.DebugForceUpdatePositionDelay.Value > 0f)
    {
      yield return new WaitForSeconds(DynamicLocationsConfig
        .DebugForceUpdatePositionDelay.Value);
    }

    if (player != null && DynamicLocationsConfig
          .DebugForceUpdatePositionAfterTeleport.Value)
    {
      var positionWithOffset =
        zdoNetViewInstance?.transform.position;
      teleportPosition = (positionWithOffset ?? zdo.GetPosition()) +
                         Vector3.up *
                         DynamicLocationsConfig.RespawnHeightOffset.Value;
      player.transform.position = teleportPosition;
    }
  }
}