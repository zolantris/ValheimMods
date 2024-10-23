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
using Zolantris.Shared.Debug;
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
  // mostly for debugging, this should not be kept other it will retain the logout point even when it's possible the point could be inaccurate.
  internal bool CanUpdateLogoutPoint = true;
  internal bool CanRemoveLogoutAfterSync = true;
  internal bool IsTeleportingToDynamicLocation = false;
  private bool IsRunningFindDynamicZdo = false;

  public static Dictionary<long, PlayerSpawnController> Instances = new();

  public static PlayerSpawnController? Instance;

  // internal Stopwatch UpdateLocationTimer = new();
  private static Player? player => Player.m_localPlayer;
  public static Coroutine? MoveToLogoutRoutine;
  public static Coroutine? MoveToSpawnRoutine;

  internal static List<DebugSafeTimer> Timers = [];

  private void Awake()
  {
    Setup();
  }

  private void Update()
  {
    DebugSafeTimer.UpdateTimersFromList(Timers);
  }

  public void DEBUG_MoveTo(LocationVariation locationVariationType)
  {
    CanUpdateLogoutPoint = true;
    CanRemoveLogoutAfterSync = false;
    switch (locationVariationType)
    {
      case LocationVariation.Spawn:
        Instance?.MovePlayerToSpawnPoint();
        break;
      case LocationVariation.Logout:
        Instance?.MovePlayerToLogoutPoint();
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(locationVariationType),
          locationVariationType, null);
    }
  }

  internal void Reset()
  {
    IsRunningFindDynamicZdo = false;
    IsTeleportingToDynamicLocation = false;
    CanUpdateLogoutPoint = true;
    CanRemoveLogoutAfterSync = true;
    MovePlayerToZdoComplete = true;

    ResetRoutine(ref MoveToLogoutRoutine);
    ResetRoutine(ref MoveToSpawnRoutine);
  }

  private bool MovePlayerToZdoComplete = false;

  // Events
  internal void OnMovePlayerToZdoComplete(bool success = false,
    string errorMessage = "OnMovePlayerToZdo exited but failed")
  {
    Reset();
    Timers.Clear();
    IsTeleportingToDynamicLocation = false;
    MovePlayerToZdoComplete = true;

    if (!success)
    {
      Logger.LogError(errorMessage);
    }
  }

  internal static void ResetRoutine(ref Coroutine? routine)
  {
    if (routine == null) return;
    Instance?.StopCoroutine(routine);
    routine = null;
  }

  private void OnDestroy()
  {
    Reset();
    Logger.LogDebug("Called onDestroy");
  }

  private void OnDisable()
  {
    Reset();
    StopAllCoroutines();
  }

  private void Setup()
  {
    Instance = this;
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
      LocationController.RemoveZdoTarget(LocationVariation.Spawn,
        player);
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
  /// <param name="bed"></param>
  /// <returns>bool</returns>
  public bool SyncBedSpawnPoint(ZNetView spawnPointObj, Bed bed)
  {
    // should sync the zdo just in case it doesn't match player
    if (player == null) return false;

    if (!bed.IsMine() && bed.GetOwner() != 0L)
    {
      // exit b/c this is another player's bed, this should not set as a spawn
      return false;
    }

    var netView = PersistBedZdo(bed);
    if (netView == null) return false;
    var zdo = netView.GetZDO();

    LocationController.SetZdo(LocationVariation.Spawn, player,
      zdo);

    var wasSuccessful = LocationController.SetLocationTypeData(
      LocationVariation.Spawn, player, zdo,
      spawnPointObj.transform.position - player.transform.position);


    return wasSuccessful;
  }

  /// <summary>
  /// Must be called on logout, and should be fired optimistically to avoid desync if crashes happen.
  /// </summary>
  /// <returns>bool</returns>
  public bool SyncLogoutPoint(ZDO zdo, bool shouldRemove = false)
  {
    var canRemove = shouldRemove &&
                    DynamicLocationsConfig.ShouldRemoveLoginPoint.Value;

    if (canRemove || !ZdoWatchController.GetPersistentID(zdo,
          out var persistentId))
    {
      LocationController.RemoveZdoTarget(LocationVariation.Logout,
        player);
      return false;
    }

    if (persistentId == 0)
    {
      Logger.LogDebug("vehicleZdoId is invalid");
      return false;
    }

    if (player == null) return false;

    if (player.transform.localPosition != player.transform.position)
    {
      LocationController.SetOffset(LocationVariation.Logout, player,
        player.transform.localPosition);
    }

    LocationController.SetZdo(LocationVariation.Logout, player, zdo);


    Game.instance.m_playerProfile.SavePlayerData(player);
    return true;
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

  public void MovePlayerToLogoutPoint()
  {
    MoveToLogoutRoutine =
      StartCoroutine(UpdateLocation(LocationVariation.Logout));
  }

  // /// <summary>
  // /// This method might not be necessary. It is written to prevent cachine issues
  // /// todo determine if this is needed.
  // /// </summary>
  // /// <returns></returns>
  // public IEnumerator GetDynamicZdo()
  // {
  //   var spawnZdo = PlayerSpawnPointZDO;
  //   if (spawnZdo != null)
  //   {
  //     yield return spawnZdo;
  //     yield break;
  //   }
  //
  //   if (spawnZdo == null)
  //   {
  //     var pendingZdo = FindDynamicZdo(
  //       PlayerSpawnController
  //         .DynamicLocationVariant.Logout, true);
  //     yield return pendingZdo;
  //     spawnZdo = pendingZdo.Current;
  //   }
  //
  //   yield return spawnZdo;
  // }

  /// <summary>
  /// Looks for the ZDO (mostly performant)
  /// </summary>
  /// <param name="locationVariationType"></param>
  /// <param name="shouldAdjustReferencePoint"></param>
  /// <returns></returns>
  public IEnumerator FindDynamicZdo(
    LocationVariation locationVariationType,
    bool shouldAdjustReferencePoint = false)
  {
    var timer = DebugSafeTimer.StartNew(Timers);
    IsRunningFindDynamicZdo = true;
    var maybeZdo =
      LocationController.GetZdoFromStore(locationVariationType, player);
    yield return maybeZdo;
    yield return new WaitUntil(() =>
      maybeZdo.Current is ZDO || HasExpiredTimer(timer));

    var zdo = maybeZdo.Current as ZDO;
    yield return zdo;
    if (shouldAdjustReferencePoint && ZNet.instance != null && zdo != null)
    {
      ZNet.instance.SetReferencePosition(zdo.GetPosition());
    }

    IsRunningFindDynamicZdo = false;
    timer.Clear();
  }


  /// <summary>
  /// Returns a vector3 or null. Null if the position is invalid for the zdo too. This way the bed position can be used as a fallback.
  /// </summary>
  /// <param name="locationVariationType"></param>
  /// <returns></returns>
  // public Vector3? OnFindSpawnPoint(DynamicLocationVariant locationType)
  // {
  //   if (IsRunningFindDynamicZdo || PlayerSpawnPointZDO != null)
  //   {
  //     var pos = PlayerSpawnPointZDO?.GetPosition();
  //     return pos == Vector3.zero ? null : pos;
  //   }
  //
  //   StartCoroutine(FindDynamicZdo(locationType));
  //   return null;
  // }
  internal IEnumerator UpdateLocation(
    LocationVariation locationVariationType)
  {
    var timer = DebugSafeTimer.StartNew(Timers);

    IsTeleportingToDynamicLocation = false;

    var offset = LocationController.GetOffset(locationVariationType, player);
    var maybeZdo = FindDynamicZdo(locationVariationType);
    yield return maybeZdo;
    yield return new WaitUntil(() => maybeZdo.Current is ZDO || HasExpiredTimer(
      timer,
      DynamicLocationsConfig.LocationControlsTimeoutInMs.Value));
    var zdo = maybeZdo.Current as ZDO;
    if (zdo == null)
    {
      throw new NullReferenceException("zdo, should be ZDO");
    }

    if (
      maybeZdo.Current == null)
    {
      OnMovePlayerToZdoComplete(false, "Zdo is null");
      yield break;
    }

    switch (locationVariationType)
    {
      case LocationVariation.Spawn:
        yield return MovePlayerToZdo(zdo, offset);
        break;
      case LocationVariation.Logout:
        var handled = LoginAPIController.RunAllIntegrations_OnLoginMoveToZdo(
          zdo,
          offset,
          this);
        yield return handled;
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(locationVariationType),
          locationVariationType, null);
    }


    IsTeleportingToDynamicLocation = false;
    switch (locationVariationType)
    {
      // must be another coroutine AND only fired after the Move coroutine completes otherwise it WILL break the move coroutine as it deletes the required key.
      // remove logout point after moving the player.
      case LocationVariation.Logout when player != null:
      {
        if (CanRemoveLogoutAfterSync &&
            DynamicLocationsConfig.ShouldRemoveLoginPoint.Value)
        {
          yield return LocationController.RemoveZdoTarget(
            LocationVariation.Logout,
            player);
        }

        break;
      }
      case LocationVariation.Spawn:
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(locationVariationType),
          locationVariationType, null);
    }

    timer.Clear();
    yield return true;
  }

  public Coroutine MovePlayerToSpawnPoint()
  {
    ResetRoutine(ref MoveToSpawnRoutine);
    // logout routine if activated must be cancelled as respawn takes priority
    ResetRoutine(ref MoveToLogoutRoutine);
    MoveToSpawnRoutine =
      StartCoroutine(UpdateLocation(LocationVariation.Spawn));
    return MoveToSpawnRoutine;
  }

  public static LocationVariation GetLocationType(Game game)
  {
    return game.m_respawnAfterDeath
      ? LocationVariation.Spawn
      : LocationVariation.Logout;
  }

  private static IEnumerator GetZdo(
    LocationVariation locationVariationType)
  {
    yield return LocationController.GetZdoFromStore(locationVariationType,
      player);
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
  // public static Func<ZNetView?, IEnumerator> PlayerMoveToVehicleCallback =
  // OnPlayerMoveToVehiclePlaceholder;

  // private static IEnumerator OnPlayerMoveToVehiclePlaceholder(ZNetView? obj)
  // {
  //   yield return null;
  // }
  //
  // private static IEnumerator OnPlayerMoveToVehicle(ZNetView? netView)
  // {
  //   var output = PlayerMoveToVehicleCallback(netView);
  //   yield return output;
  // }

  public static bool HasExpiredTimer(Stopwatch timer, int timeInMs = 1000)
  {
    var time = timeInMs > 1000
      ? timeInMs
      : DynamicLocationsConfig.LocationControlsTimeoutInMs.Value;

    var hasExpiredTimer = timer.ElapsedMilliseconds > time;

    return hasExpiredTimer;
  }

  public static bool HasExpiredTimer(DebugSafeTimer timer, int timeInMs = 1000)
  {
    var time = timeInMs > 1000
      ? timeInMs
      : DynamicLocationsConfig.LocationControlsTimeoutInMs.Value;

    var hasExpiredTimer = timer.ElapsedMilliseconds > time;

    return hasExpiredTimer;
  }

  /// <summary>
  /// Does not work, zdoids are not persistent across game and loading content outside a zone does not work well without a reference that persists.
  /// </summary>
  /// <remarks>Whenever calling yield break call OnMovePlayerToZdoComplete() otherwise there is no way to check if this has completed its run</remarks>
  /// <param name="zdo"></param>
  /// <param name="offset"></param>
  /// <returns></returns>
  public IEnumerator MovePlayerToZdo(ZDO? zdo, Vector3? offset)
  {
    var timer = DebugSafeTimer.StartNew();
    if (DynamicLocationsConfig.IsDebug)
    {
      Logger.LogDebug("Running MovePlayerToZdo");
    }

    if (!player || zdo == null)
    {
      OnMovePlayerToZdoComplete();
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
      OnMovePlayerToZdoComplete();
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
      Player.m_localPlayer.IsTeleporting() == false || HasExpiredTimer(timer,
        DynamicLocationsConfig.LocationControlsTimeoutInMs.Value));

    zdoNetViewInstance = ZNetScene.instance.FindInstance(zdo);

    yield return new WaitUntil(() =>
    {
      zdoNetViewInstance = ZNetScene.instance.FindInstance(zdo);
      return zdoNetViewInstance != null || HasExpiredTimer(timer,
        DynamicLocationsConfig.LocationControlsTimeoutInMs.Value);
    });

    if (HasExpiredTimer(timer,
          DynamicLocationsConfig.LocationControlsTimeoutInMs.Value))
    {
      Logger.LogError("Error attempting to find NetView instance of the ZDO");
      yield break;
    }

    if (DynamicLocationsConfig.FreezePlayerPosition.Value && character != null)
    {
      character.m_body.isKinematic = false;
    }

    // yield return OnPlayerMoveToVehicle(zdoNetViewInstance);

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
        zdoNetViewInstance?.transform.position + offset;
      teleportPosition = (positionWithOffset ?? zdo.GetPosition()) +
                         Vector3.up *
                         DynamicLocationsConfig.RespawnHeightOffset.Value;
      player.transform.position = teleportPosition;
    }
  }
}