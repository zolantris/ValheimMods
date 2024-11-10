using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using DynamicLocations.Config;
using DynamicLocations.Constants;
using JetBrains.Annotations;
using UnityEngine;
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
  /// <param name="zdo"></param>
  /// <param name="locationVariationType"></param>
  /// <returns></returns>
  public bool PersistDynamicPoint(ZDO zdo,
    LocationVariation locationVariationType, out int id)
  {
    id = 0;
    if (ZdoWatchController.Instance == null) return false;
    // Beds must be persisted when syncing spawns otherwise they cannot be retrieved directly across sessions / on server shutdown and would require a deep search of all objects.

    id = ZdoWatchController.Instance.GetOrCreatePersistentID(zdo);

    if (id != 0)
    {
      Logger.LogError(
        "No persistent ID returned for bed, this should not be possible. Please report this error");
      RemoveDynamicPoint(zdo, locationVariationType);
      return false;
    }

    AddDynamicPoint(zdo, locationVariationType);
    return true;
  }

  // required for setting a persistent bed. Without this value set it will persist the ID but the actual item will not be known so if the bed is deleted during session it would not match
  public void AddDynamicPoint(ZDO zdo, LocationVariation locationVariationType)
  {
    zdo.Set(ZdoVarKeys.DynamicLocationsPoint, 1);
  }

  public void RemoveDynamicPoint(ZDO? zdo,
    LocationVariation locationVariationType)
  {
    LocationController.RemoveZdoTarget(locationVariationType,
      player);

#if DEBUG
    if (DynamicLocationsConfig.DEBUG_ShouldNotRemoveTargetKey.Value)
    {
      return;
    }
#endif
    zdo?.RemoveInt(ZdoVarKeys.DynamicLocationsPoint);
  }

  /// <summary>
  /// Sets or removes the spawnPointZdo to the bed it is associated with a moving zdo
  /// - Only should be called when the bed is interacted with
  /// - This id is used to poke a zone and load it, then teleport the player to their bed like they are spawning
  /// </summary>
  /// <param name="zdo"></param>
  /// <param name="bed"></param>
  /// <returns>bool</returns>
  public bool SyncBedSpawnPoint(ZDO zdo, Bed bed)
  {
    // should sync the zdo just in case it doesn't match player
    if (player == null) return false;

    if (!bed.IsMine() && bed.GetOwner() != 0L)
    {
      // exit b/c this is another player's bed, this should not set as a spawn
      return false;
    }

    PersistDynamicPoint(zdo, LocationVariation.Spawn, out _);

    var wasSuccessful = LocationController.SetLocationTypeData(
      LocationVariation.Spawn, player, zdo,
      bed.transform.position - player.transform.position);

    return wasSuccessful;
  }

  /// <summary>
  /// Must be called on logout, and should be fired optimistically to avoid desync if crashes happen.
  /// </summary>
  /// <returns>bool</returns>
  public bool SyncLogoutPoint(ZDO? zdo, bool shouldRemove = false)
  {
    if (ZNet.instance == null) return false;
    if (zdo == null && !shouldRemove)
    {
      Logger.LogError(
        "ZDO not found for netview, this likely means something is wrong with the are it is being called in");
      return false;
    }

    if (shouldRemove)
    {
      RemoveDynamicPoint(zdo, LocationVariation.Logout);
      Game.instance.m_playerProfile.SavePlayerData(player);
      return true;
    }

    var isPersistent =
      PersistDynamicPoint(zdo, LocationVariation.Logout, out var id);
    if (!shouldRemove && !isPersistent)
    {
      Logger.LogDebug("vehicleZdoId is invalid");
      return false;
    }

    var storedPersistentZdo =
      LocationController.GetZdoFromStore(LocationVariation.Logout, player);
    if (storedPersistentZdo == id)
    {
      Logger.LogDebug(
        "Matching ZDOID found already stored, skipping sync/save");
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

  [UsedImplicitly]
  public bool DynamicTeleport(Vector3 position, Quaternion rotation)
  {
    if (player == null) return false;

    player.m_teleportCooldown = 15;
    player.m_teleporting = false;

    return player.TeleportTo(
      position,
      rotation,
      !DynamicLocationsConfig
        .DebugDisableDistancePortal.Value);
  }

  public void MovePlayerToLogoutPoint()
  {
    MoveToLogoutRoutine =
      StartCoroutine(UpdateLocation(LocationVariation.Logout));
  }

  /// <summary>
  /// Looks for the ZDO (mostly performant)
  /// </summary>
  /// <param name="locationVariationType"></param>
  /// <param name="onComplete"></param>
  /// <param name="shouldAdjustReferencePoint"></param>
  /// <returns></returns>
  public IEnumerator FindDynamicZdo(
    LocationVariation locationVariationType, Action<ZDO?> onComplete,
    bool shouldAdjustReferencePoint = false)
  {
    IsRunningFindDynamicZdo = true;

    ZDO? zdoOutput = null;
    yield return LocationController.GetZdoFromStoreAsync(locationVariationType,
      player,
      (output) => { zdoOutput = output; });

    onComplete(zdoOutput);

    if (shouldAdjustReferencePoint && ZNet.instance != null &&
        zdoOutput != null)
    {
      ZNet.instance.SetReferencePosition(zdoOutput.GetPosition());
    }

    IsRunningFindDynamicZdo = false;
  }

  private IEnumerator UpdateLocation(
    LocationVariation locationVariationType)
  {
    var timer = DebugSafeTimer.StartNew(Timers);

    IsTeleportingToDynamicLocation = false;

    var offset = LocationController.GetOffset(locationVariationType, player);
    ZDO? zdoOutput = null;
    yield return FindDynamicZdo(locationVariationType,
      output => { zdoOutput = output; });

    if (
      zdoOutput == null)
    {
      yield break;
    }

    switch (locationVariationType)
    {
      case LocationVariation.Spawn:
        yield return MovePlayerToZdo(zdoOutput, offset);
        break;
      case LocationVariation.Logout:
        yield return LoginAPIController.RunAllIntegrations_OnLoginMoveToZdo(
          zdoOutput,
          offset,
          this);
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
            DynamicLocationsConfig.DEBUG_ShouldNotRemoveTargetKey.Value)
        {
          LocationController.RemoveZdoTarget(
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
        ZoneSystem.GetZone(newPosition));

    if (!isLoaded)
    {
      Logger.LogDebug(
        $"zone not loaded, exiting SyncPlayerPosition for position: {newPosition}");
      return;
    }

    ZNet.instance.SetReferencePosition(newPosition);
    playerZdo.SetPosition(newPosition);
    playerZdo.SetSector(ZoneSystem.GetZone(newPosition));
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

  [UsedImplicitly]
  public bool CanFreezePlayer(bool val)
  {
    return !DynamicLocationsConfig.DebugDisableFreezePlayerTeleportMechanics
      .Value && val;
  }

  /// <summary>
  /// Does not work, zdoids are not persistent across game and loading content outside a zone does not work well without a reference that persists.
  /// </summary>
  /// <remarks>Whenever calling yield break call OnMovePlayerToZdoComplete() otherwise there is no way to check if this has completed its run</remarks>
  /// <param name="zdo"></param>
  /// <param name="offset"></param>
  /// <param name="freezePlayerOnTeleport"></param>
  /// <param name="shouldKeepPlayerFrozen"></param>
  /// <returns></returns>
  public IEnumerator MovePlayerToZdo(ZDO? zdo, Vector3? offset,
    bool freezePlayerOnTeleport = false, bool shouldKeepPlayerFrozen = false)
  {
    if (!player || zdo == null)
    {
      OnMovePlayerToZdoComplete();
      yield break;
    }

    var timer = DebugSafeTimer.StartNew();
    var hasKinematicPlayerFreeze = CanFreezePlayer(freezePlayerOnTeleport);
    var hasKeepPlayerFrozen = CanFreezePlayer(shouldKeepPlayerFrozen);
    if (DynamicLocationsConfig.IsDebug)
    {
      Logger.LogDebug("Running MovePlayerToZdo");
    }

    var teleportHeightOffset = Vector3.up * DynamicLocationsConfig
      .RespawnHeightOffset.Value;
    var teleportPosition = zdo.GetPosition() + teleportHeightOffset;

    // yield return new WaitUntil(() => ZoneSystem.instance.IsZoneLoaded(zoneId));
    // var item = new WaitUntil(() => ZNetScene.instance.FindInstance(zdo));
    // yield return item;
    // TODO add check for item and confirm it has a valid ZDO DynamicLocationPoint var

    IsTeleportingToDynamicLocation =
      DynamicTeleport(teleportPosition, zdo.GetRotation());

    // probably not necessary, but helps with loading some heavier things.
    var zoneId = ZoneSystem.GetZone(zdo.GetPosition());
    ZoneSystem.instance.PokeLocalZone(zoneId);

    var zoneIsNotLoaded = false;
    while (zoneIsNotLoaded == false)
    {
      zoneId = ZoneSystem.GetZone(zdo.GetPosition());
      zoneIsNotLoaded = ZoneSystem.instance.IsZoneLoaded(zoneId);
      yield return new WaitForFixedUpdate();
    }


    if (!IsTeleportingToDynamicLocation)
    {
      OnMovePlayerToZdoComplete();
      Logger.LogError(
        "Teleport command failed for player, exiting dynamic spawn MovePlayerToZdo.");
      yield break;
    }

    if (player != null && CanFreezePlayer(freezePlayerOnTeleport))
    {
      if (player.IsDebugFlying())
      {
        player.ToggleDebugFly();
      }
    }

    ZNetView? zdoNetViewInstance = null;
    var isZoneLoaded = false;

    zoneId = ZoneSystem.GetZone(zdo.GetPosition());
    ZoneSystem.instance.PokeLocalZone(zoneId);

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

    if (player != null && hasKinematicPlayerFreeze && !hasKeepPlayerFrozen)
    {
      if (player.IsDebugFlying())
      {
        player.ToggleDebugFly();
      }
    }

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

    timer.Clear();
    yield return null;
  }
}