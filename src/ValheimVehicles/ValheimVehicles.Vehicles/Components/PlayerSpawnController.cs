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
      var zdoid = StringToZDOID(player.m_customData[GetLogoutZdoKey()]);
      return zdoid;
    }

    public static Vector3? GetLogoutZdoOffset(Player player)
    {
      if (!player) return null;
      var offset = StringToVector3(player.m_customData[GetLogoutZdoOffsetKey()]);
      return offset;
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
        player.m_customData.Remove(GetSpawnZdoKey());
        return false;
      }

      player.m_customData[GetLogoutZdoKey()] = Vector3ToString(offset);
      return true;
    }

    public static bool SetLogoutZdoWithOffset(Player player, ZNetView dynamicObj,
      Vector3 offset)
    {
      if (!SetLogoutZdo(player, dynamicObj)) return false;
      SetLogoutZdoOffset(player, offset);
      return true;
    }

    /// <summary>
    /// This key may not exist if the spawn does not need an offset. Will default to a Vector3.zero
    /// </summary>
    /// <returns></returns>
    public static string GetSpawnZdoOffsetKey() =>
      !ZNet.instance ? "" : $"{GetFullPrefix()}_{LogoutParentZdoOffset}_{WorldUID}";

    public static string GetSpawnZdoKey() =>
      !ZNet.instance ? "" : $"{GetFullPrefix()}_{SpawnZdo}_{WorldUID}";


    public static ZDOID? GetSpawnZdo(Player player)
    {
      if (!player) return null;
      var zdoid = StringToZDOID(player.m_customData[GetSpawnZdoKey()]);
      return zdoid;
    }

    public static Vector3 GetSpawnZdoOffset(Player player)
    {
      if (!player) return Vector3.zero;
      var offset = StringToVector3(player.m_customData[GetSpawnZdoOffsetKey()]) ?? Vector3.zero;
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
        player.m_customData.Remove(GetSpawnZdoKey());
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
  /// Must sync the position of the 
  /// </summary>
  private void SyncPosition(Vector3 newPosition)
  {
    if (ZNetView.m_forceDisableInit) return;
    zdo?.SetPosition(transform.position);
    transform.position = newPosition;
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
    var bvc = player.transform.root.GetComponent<BaseVehicleController>();
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

  public IEnumerator MovePlayerToSpawnPointWorker()
  {
    if (player == null)
    {
      Setup();
      yield return null;
    }

    if (!player) yield break;

    var spawnZdoOffset = DynamicLocations.GetSpawnZdoOffset(player);
    var spawnZdoid = DynamicLocations.GetSpawnZdo(player);

    if (spawnZdoid == null)
    {
      yield break;
    }

    // for nullable using .Value is required
    var zdoNetviewInstance = LoadGameObjectInSectorFromZdo(spawnZdoid.Value);
    yield return StartCoroutine(zdoNetviewInstance);
    // var zdoInstanceObj = LoadGameObjectInSectorFromZdo(spawnZdoid.Value);
    var isNetView = zdoNetviewInstance.Current?.GetType() == typeof(ZNetView);
    // if (!isNetView) yield break;
    var nv = zdoNetviewInstance.Current as ZNetView;
    if (nv != null)
    {
      var bvc = nv
        .GetComponentInParent<BaseVehicleController>();
      if (bvc)
      {
        bvc?.ForceUpdateAllPiecePositions();
      }
    }

    yield return new WaitForFixedUpdate();

    if (nv != null)
    {
      player.transform.position = nv.transform.position;
    }

    yield return new WaitForSeconds(3);
    if (nv != null)
    {
      player.transform.position = nv.transform.position;
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