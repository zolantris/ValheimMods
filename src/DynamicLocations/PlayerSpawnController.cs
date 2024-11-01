using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using ZdoWatcher;
using Logger = Jotunn.Logger;

namespace DynamicLocations;

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

  public static Dictionary<long, PlayerSpawnController> Instances = new();

  public static PlayerSpawnController? Instance;
  private Stopwatch UpdateLocationTimer = new();
  public static Player? player;

  private void Awake()
  {
    Instance = this;
    Setup();
  }

  private void OnDisable()
  {
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

    var netView = player.GetComponentInParent<ZNetView>();
    if (!ZdoWatchManager.GetPersistentID(netView.GetZDO(),
          out var persistentId))
    {
      LocationController.RemoveSpawnTargetZdo(player);
      return;
    }

    if (persistentId == 0)
    {
      Logger.LogDebug("No persistent ID returned");
      return;
    }

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
    if (player == null)
    {
      player = Player.m_localPlayer;
    }

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
      ZoneSystem.instance.IsZoneLoaded(ZoneSystem.GetZone(newPosition));

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

    // beginning of LoadGameObjectInSector (which cannot be abstracted easily if the return is required)
    // ZDOMan.instance.RequestZDO(zdo.m_uid);
    ZNetView? zdoNetViewInstance = null;
    var isZoneLoaded = false;
    while (!isZoneLoaded || zdoNetViewInstance == null)
    {
      if (UpdateLocationTimer is { ElapsedMilliseconds: > 10000 })
      {
        Logger.LogWarning(
          $"Timed out: Attempted to spawn player on Boat ZDO expired for {zdo.m_uid}, reason -> spawn zdo was not found");
        yield break;
      }

      var zoneId = ZoneSystem.GetZone(zdo.GetPosition());
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

    var zdoPosition = zdo?.GetPosition();
    var zdoRotation = zdo?.GetRotation();
    if (!player || zdoPosition == null || zdoRotation == null) yield break;

    var positionWithOffset = zdoPosition.Value + offset;
    // SyncPlayerPosition(positionWithOffset);

    player?.TeleportTo(positionWithOffset, zdoRotation.Value,
      distantTeleport: false);

    // var spawnZdoInstance = ZNetScene.instance.FindInstance(spawnZdo);
    // while (!spawnZdoInstance && UpdateLocationTimer.ElapsedMilliseconds < 10000)
    // {
    //   var tempInstance = ZNetScene.instance.FindInstance(spawnZdo);
    //   if (tempInstance != null)
    //   {
    //     if (tempInstance.GetComponent<Bed>())
    //     {
    //       spawnZdoInstance = tempInstance;
    //     }
    //     else
    //     {
    //       Logger.LogWarning(
    //         $"The temp instance named: {tempInstance.name}, -> was not a bed instance gameobject ");
    //     }
    //   }
    //
    //   yield return null;
    // }

    // Logger.LogDebug("Exited the spawnZdoInstance Loop");
    // if (spawnZdoInstance)
    // {
    //   SyncPlayerPosition(spawnZdoInstance.transform.position);
    // }
    // BaseVehicleController? bvc = null;
    // if (zdoNetViewInstance)
    // {
    //   bvc = zdoNetViewInstance
    //     .GetComponentInParent<BaseVehicleController>();
    //   if (bvc)
    //   {
    //     bvc?.ForceUpdateAllPiecePositions();
    //     bvc?.SyncAllBeds();
    //     Logger.LogDebug(
    //       "Called BaseVehicleController.ForceUpdateAllPiecePositions and BaseVehicleController.SyncAllBeds from PlayerSpawnController");
    //   }
    // }
    //
    // yield return new WaitForFixedUpdate();
    //
    // if (zdoid == ZDOID.None) yield break;
    //
    // spawnZdo = ZDOMan.instance.GetZDO((ZDOID)zdoid);
    //
    // if (spawnZdo == null) yield break;
    //
    // var spawnOffset =
    //   spawnZdo?.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero) ?? Vector3.zero;
    // Logger.LogDebug($"SpawnOffset, {spawnOffset}");
    // zdoNetViewInstance = ZNetScene.instance.FindInstance(spawnZdo);
    //
    // var globalZdoPositionWithOffset = spawnZdo.GetPosition() + spawnOffset;
    // Logger.LogDebug(
    //   $"ZDOPos+relative {globalZdoPositionWithOffset} vs transform.pos {zdoNetViewInstance?.transform?.position}");
    // if (zdoNetViewInstance != null)
    // {
    //   SyncPlayerPosition(zdoNetViewInstance.transform.position);
    // }
    //
    // yield return new WaitForSeconds(1);
    // if (zdoNetViewInstance != null)
    // {
    //   spawnZdo = ZDOMan.instance.GetZDO((ZDOID)zdoid);
    //   if (spawnZdo == null) yield break;
    //
    //   spawnOffset = spawnZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
    //   globalZdoPositionWithOffset = spawnZdo.GetPosition() + spawnOffset;
    //   Logger.LogDebug(
    //     $"ZDOPos+relative {globalZdoPositionWithOffset} vs transform.pos {zdoNetViewInstance?.transform?.position}");
    //   SyncPlayerPosition(globalZdoPositionWithOffset);
    // }
  }

  /// <summary>
  /// Initializes the SpawnController to delegate logout and bed spawning behavior
  /// Meant to be used in Player.Awake to match existing or add a new component
  /// </summary>
  /// <param name="player"></param>
  // public static void HandleDynamicRespawnLocation(Player player)
  // {
  //   // don't need this logic
  //   var playerId = player.GetPlayerID();
  //   Logger.LogDebug($"PlayerID {playerId}");
  //
  //   if (playerId == 0L)
  //   {
  //     Logger.LogDebug("PlayerId is 0L skipping");
  //     return;
  //   }
  //
  //   var controller = GetSpawnController(player);
  //
  //   // should be for first time spawns
  //   if (!controller)
  //   {
  //     var playerSpawnPrefab =
  //       PrefabManager.Instance.GetPrefab(PrefabNames.PlayerSpawnControllerObj);
  //     if (!playerSpawnPrefab) return;
  //     Instantiate(playerSpawnPrefab, player.transform);
  //     return;
  //   }
  // }
}