using System;
using System.Collections;
using System.Collections.Generic;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ValheimRAFT.Util;
using ValheimVehicles.Prefabs;
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
  private static int _playerSpawnIdKey = "playerSpawnKey".GetStableHashCode();

  private const string PlayerSpawnKey = "valheim_vehicles_playerSpawnKey";
  // private static int _playerSpawnPointOffsetKey = "playerSpawnPointOffsetKey".GetStableHashCode();

  // logout
  // private static int _playerLogoutKey = "playerLogoutKey".GetStableHashCode();
  private static int _playerLogoutPointOffsetKey = "playerLogoutPointOffsetKey".GetStableHashCode();

  // ids
  private static int _playerIdKey = "playerIdKey".GetStableHashCode();
  private static int _playerVehicleId = "playerVehicleId".GetStableHashCode();

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
    // InvokeRepeating(nameof(SyncPosition), 5f, 5f);
    MovePlayerToSpawnPoint();
  }
  //
  // private void OnEnable()
  // {
  //   InvokeRepeating(nameof(SyncPosition), 5f, 5f);
  // }
  //
  // private void OnDisable()
  // {
  //   CancelInvoke(nameof(SyncPosition));
  // }

  public void GetSpawnPoint()
  {
    if (zdo == null) return;
    var playerSpawnId = zdo.GetInt(_playerSpawnIdKey, 0);
    if (playerSpawnId == 0) return;
    var playerSpawnObj = ZDOPersistentID.Instance.GetGameObject(playerSpawnId);
    if (playerSpawnObj)
    {
      Logger.LogDebug("go playerspawnobj");
    }
  }

  public static void OnPlayerDeath()
  {
    if (Player.m_localPlayer == null)
    {
      return;
    }

    var playerSpawnController = GetSpawnController(Player.m_localPlayer);
    playerSpawnController?.transform.SetParent(null);
  }

  public struct PlayerSpawnData
  {
    public int WorldId;
    public int x;
    public int y;
    public int z;
  }

  private static string MobileSpawnFromWorldId => $"PlayerSpawnKey_{ZNet.instance.GetWorldUID()}";

  private void Setup()
  {
    // forceDisableInit prevents running awake commands for znetview when it's not ready
    if (ZNetView.m_forceDisableInit) return;
    NetView = GetComponent<ZNetView>();
    zdo = NetView.GetZDO();
    player = GetComponentInParent<Player>();

    var worldUID = ZNet.instance.GetWorldUID();
    Logger.LogDebug(worldUID);
    if (!player.m_customData.TryGetValue(MobileSpawnFromWorldId,
          out var customPlayerSpawnIdKey))
    {
    }

    Logger.LogDebug("listing all player custom keys");
    foreach (var key in player.m_customData.Keys)
    {
      Logger.LogDebug($"key: {key} val: {player.m_customData[key]}");
    }

    if (player == null)
    {
      playerId = zdo.GetInt(_playerIdKey);
    }
    else
    {
      zdo?.Set(_playerIdKey, playerId);
    }

    playerId = player?.GetPlayerID() ?? 0;

    if (player != null)
    {
      zdo?.SetConnection(ZDOExtraData.ConnectionType.Spawned, player.GetZDOID());
    }

    if (playerId != 0 && Instances.TryGetValue(playerId, out var instance))
    {
      if (instance == null)
      {
        Instances.Remove(playerId);
      }
    }
    else if (playerId != 0)
    {
      Instances.Add(playerId, this);
    }

    SyncPosition();
  }

  private void OnDestroy()
  {
    if (playerId != 0)
    {
      Instances.Remove(playerId);
    }
  }

  /// <summary>
  /// Must sync the position of the 
  /// </summary>
  private void SyncPosition()
  {
    if (ZNetView.m_forceDisableInit) return;
    zdo?.SetPosition(transform.position);
  }

  /// <summary>
  /// Sets the spawnPointZdo to the bed it is associated with
  /// - Only should be called when the bed is interacted on a vehicle
  /// - This id is used to poke a zone and load it, then teleport the player to their bed like they are spawning
  /// </summary>
  /// <param name="spawnPointObj"></param>
  /// <returns>bool</returns>
  public void SyncSpawnPoint(ZNetView spawnPointObj)
  {
    // should sync the zdo just in case it doesn't match player
    SyncPosition();

    if (player == null) return;

    var spawnPointObjZdo = spawnPointObj.GetZDO();
    var userId = spawnPointObjZdo.m_uid.UserID;
    var id = spawnPointObjZdo.m_uid.ID;
    // must be parsed to ZDOID after reading from custom player data
    player.m_customData[MobileSpawnFromWorldId] =
      $"{userId},{id}";
  }

  /// <summary>
  /// Must be called on logout, but also polled to prevent accidental dsync
  /// </summary>
  /// <param name="nv"></param>
  /// <returns>bool</returns>
  public void SyncLogoutPoint(GameObject go)
  {
    // should sync the zdo just in case it doesn't match player
    SyncPosition();
    var bvc = go.transform.root.GetComponent<BaseVehicleController>();
    if (zdo == null || bvc == null)
    {
      RemovePlayerFromVehicles();
      return;
    }

    var vehicleZdoId = bvc.PersistentZdoId;
    if (vehicleZdoId == 0)
    {
      Logger.LogDebug("vehicleZdoId is invalid");
      return;
    }

    zdo.Set(_playerVehicleId, vehicleZdoId);
    zdo.Set(_playerLogoutPointOffsetKey, go.transform.localPosition);
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

  public void RemovePlayerFromVehicles()
  {
    zdo?.RemoveVec3(_playerLogoutPointOffsetKey);
    zdo?.RemoveInt(_playerVehicleId);
  }

  public void RemoveSpawnPointFromVehicle()
  {
    zdo?.RemoveInt(_playerSpawnIdKey);
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

  public static void MoveCurrentPlayerToSpawnPoint(Player currentPlayer)
  {
    var spawnController = GetSpawnController(currentPlayer);
    if (!spawnController)
    {
      Logger.LogDebug("No spawncontroller on player");
      return;
    }

    spawnController?.MovePlayerToSpawnPoint();
  }

  public void MovePlayerToSpawnPoint()
  {
    StartCoroutine(nameof(MovePlayerToSpawnPointWorker));
  }

  public IEnumerator MovePlayerToSpawnPointWorker()
  {
    if (player == null)
    {
      Setup();
      yield return null;
    }

    if (!player.m_customData.TryGetValue(MobileSpawnFromWorldId, out var spawnWorldData))
    {
      yield break;
    }

    var zdoIdStringArray = spawnWorldData.Split(',');
    long.TryParse(zdoIdStringArray[0], out var userId);
    uint.TryParse(zdoIdStringArray[1], out var objectId);

    if (userId == 0L || objectId == 0)
    {
      Logger.LogDebug("failed to parse to ZDOID");
      yield break;
    }

    var zdoID = new ZDOID(userId, objectId);

    var spawnZdo = ZDOMan.instance.GetZDO(zdoID);
    ZNetView? go = null;
    Vector2i zoneId = new();

    player.transform.position = spawnZdo.GetPosition();
    player.m_nview.GetZDO().SetPosition(spawnZdo.GetPosition());
    player.m_nview.GetZDO().SetSector(spawnZdo.GetSector());
    while (go == null)
    {
      zoneId = ZoneSystem.instance.GetZone(spawnZdo.GetPosition());
      ZoneSystem.instance.PokeLocalZone(zoneId);
      go = ZNetScene.instance.FindInstance(spawnZdo);

      if (go) break;
      yield return new WaitForFixedUpdate();
    }

    yield return new WaitUntil(() => ZoneSystem.instance.IsZoneLoaded(zoneId));

    if (go)
    {
      var bvc = go.GetComponentInParent<BaseVehicleController>();
      if (bvc)
      {
        bvc?.ForceUpdateAllPiecePositions();
      }
    }

    yield return new WaitForFixedUpdate();
    player.transform.position = go.transform.position;
  }

  private void SyncPlayerId()
  {
    if (playerId == 0 && player != null)
    {
      playerId = player.GetPlayerID();
    }

    if (playerId != 0)
    {
      zdo?.Set(_playerIdKey, playerId);
    }

    var owner = player.GetOwner();
    zdo.SetOwner(owner);
  }

  public void AddVehicleData(BaseVehicleController baseVehicleController)
  {
    if (zdo == null) return;

    foreach (var mBedPiece in baseVehicleController.GetBedPieces())
    {
      if (mBedPiece.m_nview.GetZDO().GetOwner() != playerId) continue;
      SyncSpawnPoint(mBedPiece.m_nview);
      break;
    }

    zdo.Set(_playerVehicleId, 1);
  }

  /// <summary>
  /// Should be called if the Player is outside the vehicle bounds
  /// </summary>
  public void RemoveVehicleData()
  {
    if (zdo == null) return;
    zdo.RemoveInt(_playerSpawnIdKey);
    zdo.RemoveInt(_playerIdKey);
    zdo.RemoveInt(_playerVehicleId);
  }

  public static void InitZdo(ZDO zdo)
  {
    if (zdo.m_prefab == PrefabNames.PlayerSpawnControllerObj.GetStableHashCode())
    {
      AttachSpawnControllerToPlayer(zdo);
    }
  }

  public static void AttachSpawnControllerToPlayer(ZDO zdo)
  {
    var prefabInstance = ZNetScene.instance.FindInstance(zdo);
    if (!prefabInstance) return;

    var playerSpawnController = prefabInstance.GetComponent<PlayerSpawnController>();
    var spawnControllerZdo = playerSpawnController?.zdo;

    if (spawnControllerZdo == null) return;

    var spawnControllerPlayerId = spawnControllerZdo?.GetInt(_playerIdKey);
    var playerFromId = Player.GetPlayer(_playerIdKey);
    if (spawnControllerPlayerId == 0 || spawnControllerPlayerId == null || !playerFromId) return;

    prefabInstance.transform.position = playerFromId.transform.position;
    prefabInstance.gameObject.transform.SetParent(playerFromId.transform);
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