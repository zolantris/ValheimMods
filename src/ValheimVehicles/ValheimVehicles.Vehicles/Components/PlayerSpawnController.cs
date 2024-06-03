using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ValheimRAFT.Util;
using ValheimVehicles.Prefabs;
using ValheimVehicles.ValheimVehicles.DynamicLocations;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Components;

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

  public static PlayerSpawnController Instance;

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
    if (Player.m_localPlayer == null) return;

#if DEBUG
    Logger.LogDebug("listing all player custom keys");
    foreach (var key in Player.m_localPlayer.m_customData.Keys)
    {
      Logger.LogDebug($"key: {key} val: {Player.m_localPlayer.m_customData[key]}");
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
    if (Player.m_localPlayer == null) return;

    if (!bed.IsMine() && bed.GetOwner() != 0L)
    {
      // exit b/c this is another player's bed, this should not set as a spawn
      return;
    }

    var bvc = bed.GetComponentInParent<BaseVehicleController>();
    if (!bvc)
    {
      DynamicLocations.RemoveSpawnZdo(Player.m_localPlayer);
      return;
    }

    if (spawnPointObj.transform.position != spawnPointObj.transform.localPosition)
    {
      var offset = spawnPointObj.transform.localPosition;
      // must be parsed to ZDOID after reading from custom player data
      DynamicLocations.SetSpawnZdoWithOffset(Player.m_localPlayer, spawnPointObj, offset);
    }
    else
    {
      DynamicLocations.SetSpawnZdo(Player.m_localPlayer, spawnPointObj);
    }
  }

  /// <summary>
  /// Must be called on logout, but also polled to prevent accidental dsync
  /// </summary>
  /// <param name="nv"></param>
  /// <returns>bool</returns>
  public void SyncLogoutPoint()
  {
    if (!Player.m_localPlayer || Player.m_localPlayer == null || !CanUpdateLogoutPoint) return;
    var bvc = Player.m_localPlayer.GetComponentInParent<BaseVehicleController>();
    if (!bvc)
    {
      DynamicLocations.RemoveLogoutZdo(Player.m_localPlayer);
      return;
    }

    var vehicleZdoId = bvc.PersistentZdoId;
    if (vehicleZdoId == 0)
    {
      Logger.LogDebug("vehicleZdoId is invalid");
      return;
    }

    if (Player.m_localPlayer.transform.localPosition != Player.m_localPlayer.transform.position)
    {
      DynamicLocations.SetLogoutZdoWithOffset(Player.m_localPlayer, bvc.m_nview,
        Player.m_localPlayer.transform.localPosition);
    }
    else
    {
      DynamicLocations.SetLogoutZdo(Player.m_localPlayer, bvc.m_nview);
    }

    Game.instance.m_playerProfile.SavePlayerData(Player.m_localPlayer);
  }

  /// <summary>
  /// Mostly for debug, but could be used to fix potential issues with player spawn controllers by nuking them all.
  /// </summary>
  public static void DestroyAllDynamicSpawnControllers()
  {
    if (Player.m_localPlayer == null) return;

    var spawnControllers = FindObjectsOfType<PlayerSpawnController>();
    foreach (var spawnController in spawnControllers)
    {
      var controllerZdo = spawnController?.GetComponent<ZNetView>()?.GetZDO();
      // todo this may need to force load a zone to get the zdo
      if (controllerZdo == null) continue;
      var go = ZNetScene.instance.FindInstance(controllerZdo);
      if (go?.gameObject == null) continue;
      ZNetScene.instance.Destroy(go.gameObject);
    }
  }

  // public static PlayerSpawnController? GetSpawnController(Player currentPlayer)
  // {
  //   return Instance
  //   // if (Player.m_localPlayer == null) return null;
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
    if (!Player.m_localPlayer)
    {
      Setup();
    }

    if (Player.m_localPlayer == null) return false;

    var loginZdoOffset = DynamicLocations.GetLogoutZdoOffset(Player.m_localPlayer);
    var loginZdoid = DynamicLocations.GetLogoutZdo(Player.m_localPlayer);

    if (loginZdoid == null)
    {
      // when the player is respawning from death this will be null or on first launch with these new keys
      return false;
    }

    StartCoroutine(UpdateLocation(loginZdoid, loginZdoOffset, LocationTypes.Logout));

    return true;
  }

  public enum LocationTypes
  {
    Spawn,
    Logout
  }

  public IEnumerator UpdateLocation(ZDOID? zdoid, Vector3 offset, LocationTypes locationType)
  {
    yield return MovePlayerToZdo(zdoid, offset);

    // must be another coroutine AND only fired after the Move coroutine completes otherwise it WILL break the move coroutine as it deletes the required key.
    // remove logout point after moving the player.
    if (locationType == LocationTypes.Logout)
    {
      if (CanRemoveLogoutAfterSync)
      {
        DynamicLocations.RemoveLogoutZdo(Player.m_localPlayer);
      }
    }

    if (locationType == LocationTypes.Spawn)
    {
    }
  }

  public void MovePlayerToSpawnPoint()
  {
    var spawnZdoOffset = DynamicLocations.GetSpawnTargetZdoOffset(Player.m_localPlayer);
    var spawnZdoid = DynamicLocations.GetSpawnTargetZdo(Player.m_localPlayer);
    StartCoroutine(UpdateLocation(spawnZdoid, spawnZdoOffset, LocationTypes.Spawn));
  }

  private void SyncPlayerPosition(Vector3 newPosition)
  {
    if (ZNetView.m_forceDisableInit || Player.m_localPlayer == null) return;
    var playerZdo = Player.m_localPlayer.m_nview.GetZDO();
    if (playerZdo == null) return;
    Logger.LogDebug($"Syncing Player Position and sector, {newPosition}");
    var isLoaded = ZoneSystem.instance.IsZoneLoaded(ZoneSystem.instance.GetZone(newPosition));

    if (!isLoaded)
    {
      Logger.LogDebug($"zone not loaded, exiting SyncPlayerPosition for position: {newPosition}");
      return;
    }

    playerZdo.SetPosition(newPosition);
    playerZdo.SetSector(ZoneSystem.instance.GetZone(newPosition));
    Player.m_localPlayer.transform.position = newPosition;
  }

  public IEnumerator MovePlayerToZdo(ZDOID? zdoid, Vector3 offset)
  {
    if (Player.m_localPlayer == null)
    {
      Setup();
      yield return null;
    }

    if (!Player.m_localPlayer) yield break;

    if (zdoid == null)
    {
      yield break;
    }

    // beginning of LoadGameObjectInSector (which cannot be abstracted easily if the return is required)
    ZDOMan.instance.RequestZDO((ZDOID)zdoid);
    var spawnZdo = ZDOMan.instance.GetZDO((ZDOID)zdoid);
    ZNetView? zdoNetViewInstance = null;
    Vector2i zoneId = new();
    var isZoneLoaded = false;
    while (!isZoneLoaded)
    {
      ZDOMan.instance.RequestZDO((ZDOID)zdoid);
      spawnZdo = ZDOMan.instance.GetZDO((ZDOID)zdoid);
      if (spawnZdo == null)
      {
        yield return new WaitForFixedUpdate();
        continue;
      }

      zoneId = ZoneSystem.instance.GetZone(spawnZdo.GetPosition());
      ZoneSystem.instance.PokeLocalZone(zoneId);

      zdoNetViewInstance = ZNetScene.instance.FindInstance(spawnZdo);

      if (zdoNetViewInstance) break;
      yield return new WaitForFixedUpdate();
      if (!ZoneSystem.instance.IsZoneLoaded(zoneId))
      {
        yield return null;
      }
      else
      {
        isZoneLoaded = true;
      }
    }

    var zdoPosition = spawnZdo?.GetPosition();
    var zdoRotation = spawnZdo?.GetRotation();
    if (!Player.m_localPlayer || zdoPosition == null || zdoRotation == null) yield break;

    Player.m_localPlayer?.TeleportTo(zdoPosition.Value + offset, zdoRotation.Value,
      distantTeleport: true);

    yield return new WaitUntil(() => ZNetScene.instance.FindInstance(spawnZdo));
    var spawnZdoInstance = ZNetScene.instance.FindInstance(spawnZdo);
    SyncPlayerPosition(spawnZdoInstance.transform.position);
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