using System;
using System.Collections;
using UnityEngine;
using ValheimRAFT.Util;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Utils;

public class PlayerSpawnController : MonoBehaviour
{
  // spawn (Beds)
  private static int _playerSpawnIdKey = "playerSpawnKey".GetStableHashCode();
  private static int _playerSpawnPointOffsetKey = "playerSpawnPointOffsetKey".GetStableHashCode();

  // logout
  private static int _playerLogoutKey = "playerLogoutKey".GetStableHashCode();
  private static int _playerLogoutPointOffsetKey = "playerLogoutPointOffsetKey".GetStableHashCode();

  // ids
  private static int _playerIdKey = "playerIdKey".GetStableHashCode();
  private static int _playerVehicleId = "playerVehicleId".GetStableHashCode();

  public ZNetView? NetView;
  public long playerId;
  public Player? player;
  public ZDO? zdo;

  private void Awake()
  {
    Setup();
  }

  private void Start()
  {
    Setup();
    InvokeRepeating(nameof(SyncPosition), 5f, 5f);
  }

  private void OnEnable()
  {
    InvokeRepeating(nameof(SyncPosition), 5f, 5f);
  }

  private void OnDisable()
  {
    CancelInvoke(nameof(SyncPosition));
  }

  private void Setup()
  {
    // forceDisableInit prevents running awake commands for znetview when it's not ready
    if (ZNetView.m_forceDisableInit) return;
    NetView = GetComponent<ZNetView>();
    player = GetComponentInParent<Player>();
    playerId = player.GetPlayerID();
    zdo = NetView.GetZDO();
  }

  /// <summary>
  /// Must sync the position of the 
  /// </summary>
  private void SyncPosition()
  {
    zdo?.SetPosition(transform.position);
  }

  public static GameObject CreateSpawnObject(GameObject player)
  {
    var go = new GameObject()
    {
      name = PrefabNames.PlayerSpawnControllerObj,
    };

    go.AddComponent<PlayerSpawnController>();

    return go;
  }

  /// <summary>
  /// Only should be called when the bed is interacted on a vehicle
  /// </summary>
  /// <param name="spawnPointObj"></param>
  /// <returns>bool</returns>
  public void SyncSpawnPoint(ZNetView spawnPointObj)
  {
    if (zdo == null) return;
    var spawnPointZdo = spawnPointObj.GetZDO();
    zdo.Set(_playerSpawnIdKey, ZDOPersistentID.ZDOIDToId(spawnPointZdo.m_uid));
    zdo.Set(_playerSpawnPointOffsetKey, spawnPointObj.transform.localPosition);
  }

  public void MovePlayerToSpawnPoint()
  {
    StartCoroutine(nameof(MovePlayerToSpawnPointWorker));
  }

  public IEnumerable MovePlayerToSpawnPointWorker()
  {
    if (zdo == null) yield break;
    var playerSpawnId = zdo.GetInt(_playerSpawnIdKey);

    /**
     * Likely do not need this as the ship will reposition this object
     */
    // var playerSpawnPointOffset = zdo.GetVec3(_playerSpawnPointOffsetKey, Vector3.zero);

    // required
    var playerSpawnPointZdoId = ZDOPersistentID.Instance.GetZDO(playerSpawnId);

    if (playerSpawnPointZdoId == null)
    {
      yield break;
    }

    ZNetView? go = null;
    Vector2i zoneId;
    while (go == null)
    {
      go = ZNetScene.instance.FindInstance(playerSpawnPointZdoId);
      if (go) break;
      zoneId = ZoneSystem.instance.GetZone(playerSpawnPointZdoId.m_position);
      ZoneSystem.instance.PokeLocalZone(zoneId);
      yield return new WaitForFixedUpdate();
    }

    zoneId = ZoneSystem.instance.GetZone(playerSpawnPointZdoId.m_position);
    ZoneSystem.instance.PokeLocalZone(zoneId);
    yield return new WaitUntil(() => ZoneSystem.instance.IsZoneLoaded(zoneId));
    yield return null;
  }

  public void AddVehicleData(BaseVehicleController baseVehicleController)
  {
    if (zdo == null) return;

    foreach (var mBedPiece in baseVehicleController.m_bedPieces)
    {
      if (mBedPiece.m_nview.GetZDO().GetOwner() != playerId) continue;
      SyncSpawnPoint(mBedPiece.m_nview);
      break;
    }

    zdo.Set(_playerIdKey, 1);
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

  /// <summary>
  /// Initializes the SpawnController to delegate logout and bed spawning behavior
  /// Meant to be used in Player.Awake to match existing or add a new component
  /// </summary>
  /// <param name="player"></param>
  public static void CreateSpawnDelegate(Player player)
  {
    var playerId = player.GetPlayerID();
    Logger.LogDebug($"PlayerID {playerId}");
    var spawnControllers = FindObjectsOfType<PlayerSpawnController>();

    if (playerId == 0L)
    {
      Logger.LogDebug("PlayerId is 0L skipping");
      return;
    }

    var hasController = false;
    foreach (var playerSpawnController in spawnControllers)
    {
      if (playerSpawnController == null) continue;
      var spawnControllerZdo = playerSpawnController.zdo;
      var spawnControllerPlayerId = spawnControllerZdo?.GetInt(_playerIdKey);
      if (spawnControllerPlayerId == 0 || spawnControllerPlayerId == null ||
          playerId != spawnControllerPlayerId) continue;

      hasController = true;
      playerSpawnController.gameObject.transform.position = player.transform.position;
      playerSpawnController.gameObject.transform.SetParent(player.transform);
    }

    if (!hasController)
    {
      player.gameObject.AddComponent<PlayerSpawnController>();
    }
  }
}