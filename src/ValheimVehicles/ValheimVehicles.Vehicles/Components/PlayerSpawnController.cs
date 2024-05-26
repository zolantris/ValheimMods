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

  public static class DynamicLocations
  {
    private static string PluginPrefix = "valheim_vehicles";
    private const string DynamicPrefix = "Dynamic";
    private const string SpawnZdo = "SpawnZdo";
    private const string LogoutParentZdoOffset = "LogoutParentZdoOffset";
    private const string LogoutParentZdo = "LogoutParentZdo";

    public static string GetPluginPrefix()
    {
      return PluginPrefix;
    }

    public static string GetFullPrefix()
    {
      return $"{PluginPrefix}_{DynamicPrefix}";
    }

    /// <summary>
    /// must be guarded
    /// </summary>
    private static long WorldUID => ZNet.instance?.GetWorldUID() ?? 0;

    public static bool IsValid(string key) => key.StartsWith(PluginPrefix);

    public static string GetLogoutZdoOffsetKey() =>
      !ZNet.instance ? "" : $"{GetFullPrefix()}_{LogoutParentZdoOffset}_{WorldUID}";

    public static string GetLogoutZdoKey() => !ZNet.instance
      ? ""
      : $"{GetFullPrefix()}_{LogoutParentZdo}_{WorldUID}";

    public static string GetSpawnZdoOffsetKey() =>
      !ZNet.instance ? "" : $"{GetFullPrefix()}_{LogoutParentZdoOffset}_{WorldUID}";

    public static string GetSpawnZdoKey() =>
      !ZNet.instance ? "" : $"{GetFullPrefix()}_{SpawnZdo}_{WorldUID}";

    private static string ZDOIDToString(ZDOID zdoid)
    {
      var userId = zdoid.UserID;
      var id = zdoid.ID;
      return $"{userId},{id}";
    }

    private static ZDOID? StringToZDOID(string zdoidString)
    {
      var zdoIdStringArray = zdoidString.Split(',');
      if (zdoIdStringArray.Length != 2) return null;

      long.TryParse(zdoIdStringArray[0], out var userId);
      uint.TryParse(zdoIdStringArray[1], out var objectId);

      if (userId == 0 || objectId == 0)
      {
        Logger.LogDebug("failed to parse to ZDOID");
        return null;
      }

      var zdoID = new ZDOID(userId, objectId);
      return zdoID;
    }

    private static string Vector3ToString(Vector3 val)
    {
      return $"{val.x},{val.y},{val.z}";
    }

    private static Vector3? StringToVector3(string val)
    {
      var vector3StringArray = val.Split(',');
      if (vector3StringArray.Length != 3) return null;

      float.TryParse(vector3StringArray[0], out var x);
      float.TryParse(vector3StringArray[1], out var y);
      float.TryParse(vector3StringArray[2], out var z);
      var vector = new Vector3(x, y, z);
      return vector;
    }

    public static ZDOID? GetLogoutZdo(Player player)
    {
      if (!player) return null;
      if (!player.m_customData.TryGetValue(GetLogoutZdoKey(), out var logoutZdoString))
      {
        return null;
      }

      var zdoid = StringToZDOID(logoutZdoString);
      return zdoid;
    }

    public static Vector3 GetLogoutZdoOffset(Player player)
    {
      if (!player) return Vector3.zero;
      if (!player.m_customData.TryGetValue(GetLogoutZdoKey(), out var logoutZdoString))
      {
        return Vector3.zero;
      }

      var offset = StringToVector3(logoutZdoString);
      return offset ?? Vector3.zero;
    }

    public static bool SetLogoutZdo(Player player, ZNetView dynamicObj)
    {
      if (!ZNet.instance) return false;
      var spawnPointObjZdo = dynamicObj.GetZDO();
      if (spawnPointObjZdo == null) return false;
      player.m_customData[GetLogoutZdoKey()] = ZDOIDToString(spawnPointObjZdo.m_uid);
      return true;
    }

    public static bool RemoveLogoutZdo(Player player)
    {
      if (!ZNet.instance) return false;
      player.m_customData.Remove(GetLogoutZdoKey());
      player.m_customData.Remove(GetLogoutZdoOffsetKey());
      return true;
    }

    public static bool RemoveSpawnZdo(Player player)
    {
      if (!ZNet.instance) return false;
      player.m_customData.Remove(GetSpawnZdoKey());
      player.m_customData.Remove(GetSpawnZdoOffsetKey());
      return true;
    }

    public static bool SetLogoutZdoOffset(Player player, Vector3 offset)
    {
      if (!player) return false;
      if (Vector3.zero == offset)
      {
        player.m_customData.Remove(GetLogoutZdoOffsetKey());
        return false;
      }

      player.m_customData[GetLogoutZdoOffsetKey()] = Vector3ToString(offset);
      return true;
    }

    public static bool SetLogoutZdoWithOffset(Player player, ZNetView dynamicObj,
      Vector3 offset)
    {
      if (!SetLogoutZdo(player, dynamicObj)) return false;
      SetLogoutZdoOffset(player, offset);
      return true;
    }


    public static ZDOID? GetSpawnZdo(Player player)
    {
      if (!player) return null;
      if (!player.m_customData.TryGetValue(GetSpawnZdoKey(), out var spawnZdoString))
      {
        return null;
      }

      var zdoid = StringToZDOID(spawnZdoString);
      return zdoid;
    }

    public static Vector3 GetSpawnZdoOffset(Player player)
    {
      if (!player) return Vector3.zero;
      if (!player.m_customData.TryGetValue(GetSpawnZdoOffsetKey(), out var offsetString))
      {
        return Vector3.zero;
      }

      var offset = StringToVector3(offsetString) ?? Vector3.zero;
      return offset;
    }

    public static bool SetSpawnZdo(Player player, ZNetView dynamicObj)
    {
      if (!ZNet.instance) return false;
      var spawnPointObjZdo = dynamicObj.GetZDO();
      if (spawnPointObjZdo == null) return false;
      player.m_customData[GetSpawnZdoKey()] = ZDOIDToString(spawnPointObjZdo.m_uid);
      return true;
    }

    /// <summary>
    /// Only sets offset if necessary, otherwise scrubs the data
    /// </summary>
    /// <param name="player"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static bool SetSpawnZdoOffset(Player player, Vector3 offset)
    {
      if (!player) return false;
      if (Vector3.zero == offset)
      {
        if (player.m_customData.TryGetValue(GetSpawnZdoOffsetKey(), out _))
        {
          player.m_customData.Remove(GetSpawnZdoOffsetKey());
        }

        return false;
      }

      player.m_customData[GetSpawnZdoKey()] = Vector3ToString(offset);
      return true;
    }

    public static bool SetSpawnZdoWithOffset(Player player, ZNetView dynamicObj,
      Vector3 offset)
    {
      if (!SetSpawnZdo(player, dynamicObj)) return false;
      SetSpawnZdoOffset(player, offset);
      return true;
    }
  }

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
    InvokeRepeating(nameof(SyncLogoutPoint), 5f, 5f);
    if (MovePlayerToLoginPoint())
    {
      return;
    }

    MovePlayerToSpawnPoint();
  }

  private void OnEnable()
  {
    InvokeRepeating(nameof(SyncLogoutPoint), 5f, 20);
  }

  private void OnDisable()
  {
    CancelInvoke(nameof(SyncLogoutPoint));
  }

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
    if (!player || player == null || zdo == null) return;
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

    // remove logout point after moving the player.
    DynamicLocations.RemoveLogoutZdo(player);
    return true;
  }

  public void MovePlayerToSpawnPoint()
  {
    var spawnZdoOffset = DynamicLocations.GetSpawnZdoOffset(player);
    var spawnZdoid = DynamicLocations.GetSpawnZdo(player);
    StartCoroutine(MovePlayerToZdoWorker(spawnZdoid, spawnZdoOffset));
  }

  public IEnumerator LoadGameObjectInSectorFromZdo(ZDOID zdoid)
  {
    var spawnZdo = ZDOMan.instance.GetZDO(zdoid);
    ZNetView? zdoNetViewInstance = null;
    Vector2i zoneId = new();

    player.transform.position = spawnZdo.GetPosition();
    player.m_nview.GetZDO().SetPosition(spawnZdo.GetPosition());
    player.m_nview.GetZDO().SetSector(spawnZdo.GetSector());
    while (zdoNetViewInstance == null)
    {
      zoneId = ZoneSystem.instance.GetZone(spawnZdo.GetPosition());
      ZoneSystem.instance.PokeLocalZone(zoneId);
      zdoNetViewInstance = ZNetScene.instance.FindInstance(spawnZdo);

      if (zdoNetViewInstance) break;
      yield return new WaitForFixedUpdate();
    }

    yield return new WaitUntil(() => ZoneSystem.instance.IsZoneLoaded(zoneId));
    yield return zdoNetViewInstance;
  }

  private void SyncPlayerPosition(Vector3 newPosition)
  {
    if (ZNetView.m_forceDisableInit || player == null) return;
    player.m_nview.GetZDO()?.SetPosition(newPosition);
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

    // for nullable using .Value is required
    var zdoNetviewInstance = LoadGameObjectInSectorFromZdo(zdoid.Value);
    yield return StartCoroutine(zdoNetviewInstance);
    // var zdoInstanceObj = LoadGameObjectInSectorFromZdo(spawnZdoid.Value);
    var isNetView = zdoNetviewInstance.Current?.GetType() == typeof(ZNetView);
    // if (!isNetView) yield break;
    var nv = zdoNetviewInstance.Current as ZNetView;
    BaseVehicleController? bvc = null;
    if (isNetView && nv != null)
    {
      bvc = nv
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

    var spawnZdo = ZDOMan.instance.GetZDO((ZDOID)zdoid);
    var spawnOffset = spawnZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
    Logger.LogDebug($"SpawnOffset, {spawnOffset}");
    nv = ZNetScene.instance.FindInstance(spawnZdo);

    var relativeZdoPos = spawnZdo.GetPosition() + spawnOffset;
    Logger.LogDebug($"ZDOPos+relative {relativeZdoPos} vs transform.pos {nv?.transform?.position}");
    if (nv != null)
    {
      SyncPlayerPosition(nv.transform.position);
    }

    yield return new WaitForSeconds(1);
    if (nv != null)
    {
      spawnZdo = ZDOMan.instance.GetZDO((ZDOID)zdoid);
      spawnOffset = spawnZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
      relativeZdoPos = spawnZdo.GetPosition() + spawnOffset;
      Logger.LogDebug(
        $"ZDOPos+relative {relativeZdoPos} vs transform.pos {nv?.transform?.position}");
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