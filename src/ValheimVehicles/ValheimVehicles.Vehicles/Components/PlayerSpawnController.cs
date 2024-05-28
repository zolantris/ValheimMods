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

  public ZNetView? NetView;
  public long playerId;
  public Player? player;
  public ZDO? zdo;
  internal Vector3 spawnPoint;
  internal Vector3 logoutPoint;
  public static Dictionary<long, PlayerSpawnController> Instances = new();

  private void Awake()
  {
    Setup();
  }

  private void Start()
  {
    // InvokeRepeating(nameof(SyncLogoutPoint), 5f, 5f);
    if (MovePlayerToLoginPoint())
    {
      return;
    }

    MovePlayerToSpawnPoint();
  }

  // private void OnEnable()
  // {
  //   // InvokeRepeating(nameof(SyncLogoutPoint), 5f, 20);
  // }
  //
  // private void OnDisable()
  // {
  //   // CancelInvoke(nameof(SyncLogoutPoint));
  // }

  private void Setup()
  {
    // forceDisableInit prevents running awake commands for znetview when it's not ready
    if (ZNetView.m_forceDisableInit) return;
    NetView = GetComponent<ZNetView>();
    zdo = NetView.GetZDO();
    player = GetComponentInParent<Player>();

#if DEBUG
    Logger.LogDebug("listing all player custom keys");
    foreach (var key in player.m_customData.Keys)
    {
      Logger.LogDebug($"key: {key} val: {player.m_customData[key]}");
    }
#endif

    playerId = player?.GetPlayerID() ?? 0;
  }

  private void OnDestroy()
  {
    if (playerId != 0)
    {
      Instances.Remove(playerId);
    }
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

    var bvc = bed.GetComponentInParent<BaseVehicleController>();
    if (!bvc)
    {
      DynamicLocations.RemoveSpawnZdo(player);
      return;
    }

    // must be parsed to ZDOID after reading from custom player data
    DynamicLocations.SetSpawnZdo(player, spawnPointObj);
  }

  /// <summary>
  /// Must be called on logout, but also polled to prevent accidental dsync
  /// </summary>
  /// <param name="nv"></param>
  /// <returns>bool</returns>
  public void SyncLogoutPoint()
  {
    if (!player || player == null || zdo == null || !CanUpdateLogoutPoint) return;
    var bvc = player.GetComponentInParent<BaseVehicleController>();
    if (!bvc)
    {
      DynamicLocations.RemoveLogoutZdo(player);
      return;
    }

    var vehicleZdoId = bvc.PersistentZdoId;
    if (vehicleZdoId == 0)
    {
      Logger.LogDebug("vehicleZdoId is invalid");
      return;
    }

    DynamicLocations.SetLogoutZdoWithOffset(player, bvc.m_nview, player.transform.localPosition);
    Game.instance.m_playerProfile.SavePlayerData(player);
  }

  /// <summary>
  /// Mostly for debug, but could be used to fix potential issues with player spawn controllers by nuking them all.
  /// </summary>
  public static void DestroyAllDynamicSpawnControllers()
  {
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

  public static PlayerSpawnController? GetSpawnController(Player currentPlayer)
  {
    if (!currentPlayer) return null;

    if (Instances.TryGetValue(currentPlayer.GetPlayerID(), out var instance))
    {
      instance.transform.position = currentPlayer.transform.position;
      instance.transform.SetParent(currentPlayer.transform);
      return instance;
    }

    var controllerObj =
      currentPlayer.transform.FindDeepChild($"{PrefabNames.PlayerSpawnControllerObj}(Clone)");
    var spawnController = controllerObj?.GetComponent<PlayerSpawnController>();

    return spawnController;
  }

  public bool MovePlayerToLoginPoint()
  {
    if (!player)
    {
      Setup();
    }

    if (player == null) return false;

    var loginZdoOffset = DynamicLocations.GetLogoutZdoOffset(player);
    var loginZdoid = DynamicLocations.GetLogoutZdo(player);

    if (loginZdoid == null)
    {
      // when the player is respawning from death this will be null or on first launch with these new keys
      return false;
    }

    StartCoroutine(MovePlayerToZdoWorker(loginZdoid, loginZdoOffset));

    // TODO This must be another coroutine AND only fired after the Move coroutine completes otherwise it WILL break the move coroutine as it deletes the required key.
    // remove logout point after moving the player.
    if (CanRemoveLogoutAfterSync)
    {
      DynamicLocations.RemoveLogoutZdo(player);
    }

    return true;
  }

  public void MovePlayerToSpawnPoint()
  {
    var spawnZdoOffset = DynamicLocations.GetSpawnZdoOffset(player);
    var spawnZdoid = DynamicLocations.GetSpawnZdo(player);
    StartCoroutine(MovePlayerToZdoWorker(spawnZdoid, spawnZdoOffset));
  }

  // public IEnumerator LoadGameObjectInSectorFromZdo(ZDOID zdoid)
  // {
  //   var spawnZdo = ZDOMan.instance.GetZDO(zdoid);
  //   ZNetView? zdoNetViewInstance = null;
  //   Vector2i zoneId = new();
  //
  //   player.transform.position = spawnZdo.GetPosition();
  //   player.m_nview.GetZDO().SetPosition(spawnZdo.GetPosition());
  //   player.m_nview.GetZDO().SetSector(spawnZdo.GetSector());
  //   while (zdoNetViewInstance == null)
  //   {
  //     zoneId = ZoneSystem.instance.GetZone(spawnZdo.GetPosition());
  //     ZoneSystem.instance.PokeLocalZone(zoneId);
  //     zdoNetViewInstance = ZNetScene.instance.FindInstance(spawnZdo);
  //
  //     if (zdoNetViewInstance) break;
  //     yield return new WaitForFixedUpdate();
  //   }
  //
  //   yield return new WaitUntil(() => ZoneSystem.instance.IsZoneLoaded(zoneId));
  //   yield return zdoNetViewInstance;
  // }

  private void SyncPlayerPosition(Vector3 newPosition)
  {
    if (ZNetView.m_forceDisableInit || player == null) return;
    var playerZdo = player.m_nview.GetZDO();
    if (playerZdo == null) return;
    playerZdo.SetPosition(newPosition);
    playerZdo.SetSector(ZoneSystem.instance.GetZone(newPosition));
    player.transform.position = newPosition;
  }

  public IEnumerator MovePlayerToZdoWorker(ZDOID? zdoid, Vector3 offset)
  {
    if (player == null)
    {
      Setup();
      yield return null;
    }

    if (!player) yield break;

    if (zdoid == null)
    {
      yield break;
    }

    // beginning of LoadGameObjectInSector (which cannot be abstracted easily if the return is required)
    var spawnZdo = ZDOMan.instance.GetZDO((ZDOID)zdoid);
    ZNetView? zdoNetViewInstance = null;
    Vector2i zoneId = new();

    while (zdoNetViewInstance == null)
    {
      zoneId = ZoneSystem.instance.GetZone(spawnZdo.GetPosition());
      ZoneSystem.instance.PokeLocalZone(zoneId);
      zdoNetViewInstance = ZNetScene.instance.FindInstance(spawnZdo);

      if (zdoNetViewInstance) break;
      yield return new WaitForFixedUpdate();
    }

    yield return new WaitUntil(() => ZoneSystem.instance.IsZoneLoaded(zoneId));
    // yield return zdoNetViewInstance;
    // end 


    BaseVehicleController? bvc = null;
    if (zdoNetViewInstance)
    {
      bvc = zdoNetViewInstance
        .GetComponentInParent<BaseVehicleController>();
      if (bvc)
      {
        bvc?.ForceUpdateAllPiecePositions();
        bvc?.SyncAllBeds();
        Logger.LogDebug(
          "Called BaseVehicleController.ForceUpdateAllPiecePositions and BaseVehicleController.SyncAllBeds from PlayerSpawnController");
      }
    }

    yield return new WaitForFixedUpdate();

    if (zdoid == ZDOID.None) yield break;

    spawnZdo = ZDOMan.instance.GetZDO((ZDOID)zdoid);

    if (spawnZdo == null) yield break;

    var spawnOffset =
      spawnZdo?.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero) ?? Vector3.zero;
    Logger.LogDebug($"SpawnOffset, {spawnOffset}");
    zdoNetViewInstance = ZNetScene.instance.FindInstance(spawnZdo);

    var relativeZdoPos = spawnZdo.GetPosition() + spawnOffset;
    Logger.LogDebug(
      $"ZDOPos+relative {relativeZdoPos} vs transform.pos {zdoNetViewInstance?.transform?.position}");
    if (zdoNetViewInstance != null)
    {
      SyncPlayerPosition(zdoNetViewInstance.transform.position);
    }

    yield return new WaitForSeconds(1);
    if (zdoNetViewInstance != null)
    {
      spawnZdo = ZDOMan.instance.GetZDO((ZDOID)zdoid);
      spawnOffset = spawnZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
      relativeZdoPos = spawnZdo.GetPosition() + spawnOffset;
      Logger.LogDebug(
        $"ZDOPos+relative {relativeZdoPos} vs transform.pos {zdoNetViewInstance?.transform?.position}");
      SyncPlayerPosition(relativeZdoPos);
    }
  }

  /// <summary>
  /// Initializes the SpawnController to delegate logout and bed spawning behavior
  /// Meant to be used in Player.Awake to match existing or add a new component
  /// </summary>
  /// <param name="player"></param>
  public static void HandleDynamicRespawnLocation(Player player)
  {
    // don't need this logic
    var playerId = player.GetPlayerID();
    Logger.LogDebug($"PlayerID {playerId}");

    if (playerId == 0L)
    {
      Logger.LogDebug("PlayerId is 0L skipping");
      return;
    }

    var controller = GetSpawnController(player);

    // should be for first time spawns
    if (!controller)
    {
      var playerSpawnPrefab =
        PrefabManager.Instance.GetPrefab(PrefabNames.PlayerSpawnControllerObj);
      if (!playerSpawnPrefab) return;
      Instantiate(playerSpawnPrefab, player.transform);
      return;
    }
  }
}