using System;
using System.Collections;
using Jotunn.Managers;
using UnityEngine;
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

  private void Awake()
  {
    Setup();
  }

  private void Start()
  {
    Setup();
    SyncPlayerId();
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

    if (zdo == null) return;
    var spawnPointZdo = spawnPointObj.GetZDO();
    zdo.Set(_playerSpawnIdKey, ZDOPersistentID.ZDOIDToId(spawnPointZdo.m_uid));
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

  public static void MoveCurrentPlayerToSpawnPoint()
  {
    var spawnController = Player.m_localPlayer?.GetComponent<PlayerSpawnController>();
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

  public IEnumerable MovePlayerToSpawnPointWorker()
  {
    yield return new WaitUntil(() => !ZNetView.m_forceDisableInit);
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

  /// <summary>
  /// Initializes the SpawnController to delegate logout and bed spawning behavior
  /// Meant to be used in Player.Awake to match existing or add a new component
  /// </summary>
  /// <param name="player"></param>
  public static void CreateSpawnDelegate(Player player)
  {
    var playerId = player.GetPlayerID();
    Logger.LogDebug($"PlayerID {playerId}");
    var spawnControllers = Resources.FindObjectsOfTypeAll<PlayerSpawnController>();

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
      MoveCurrentPlayerToSpawnPoint();
      break;
    }

    if (!hasController)
    {
      var playerSpawnPrefab =
        PrefabManager.Instance.GetPrefab(PrefabNames.PlayerSpawnControllerObj);
      if (!playerSpawnPrefab) return;
      var spawnPrefab = Instantiate(playerSpawnPrefab, player.transform);
      // likely not needed...but could need it
      // spawnPrefab.transform.position = player.transform.position;
      // spawnPrefab.transform.localPosition = Vector3.zero;
    }
  }
}