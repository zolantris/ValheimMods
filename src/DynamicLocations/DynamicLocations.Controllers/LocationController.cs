using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DynamicLocations.Constants;
using UnityEngine;
using ZdoWatcher;
using Logger = Jotunn.Logger;

namespace DynamicLocations.Controllers;

public interface ICachedLocation
{
  Vector3? Offset { get; set; }
  ZDO Zdo { get; set; }
}

public class CacheLocationItem : ICachedLocation
{
  public Vector3? Offset { get; set; }
  public ZDO Zdo { get; set; }
}

public class LocationController : MonoBehaviour
{
  private const string DynamicPrefix = "Dynamic";
  private const string SpawnZdo = "SpawnZdo";
  private const string LogoutParentZdoOffset = "LogoutParentZdoOffset";
  private const string LogoutParentZdo = "LogoutParentZdo";

  // todo determine if having to re-request is a heavy performance hit when it already exists
  private static
    Dictionary<PlayerSpawnController.LocationTypes, ICachedLocation>
    _cachedLocations = new();

  public static LocationController Instance;

  public void Awake()
  {
    Instance = this;
  }

  public void OnDestroy()
  {
    _cachedLocations.Clear();
  }

  public static ICachedLocation? GetCachedDynamicLocation(
    PlayerSpawnController.LocationTypes locationType)
  {
    _cachedLocations.TryGetValue(locationType, out var cachedLocation);
    return cachedLocation;
  }

  public static bool SetCachedDynamicLocation(
    PlayerSpawnController.LocationTypes locationType,
    CacheLocationItem cachedLocationItem)
  {
    if (GetCachedDynamicLocation(locationType) != null)
    {
      _cachedLocations[locationType] = cachedLocationItem;
    }
    else
    {
      _cachedLocations.Add(locationType, cachedLocationItem);
    }

    return true;
  }

  public static string GetPluginPrefix()
  {
    return DynamicLocationsPlugin.ModName;
  }

  public static string GetFullPrefix()
  {
    return $"{GetPluginPrefix()}_{DynamicPrefix}";
  }

  /// <summary>
  /// must be guarded
  /// </summary>
  private static long WorldUID => ZNet.instance?.GetWorldUID() ?? 0;


  public static string GetLogoutZdoOffsetKey() =>
    !ZNet.instance
      ? ""
      : $"{GetFullPrefix()}_{LogoutParentZdoOffset}_{WorldUID}";

  public static string GetLogoutZdoKey() => !ZNet.instance
    ? ""
    : $"{GetFullPrefix()}_{LogoutParentZdo}_{WorldUID}";

  public static string GetSpawnZdoOffsetKey() =>
    !ZNet.instance
      ? ""
      : $"{GetFullPrefix()}_{LogoutParentZdoOffset}_{WorldUID}";

  public static string GetSpawnZdoKey() =>
    !ZNet.instance ? "" : $"{GetFullPrefix()}_{SpawnZdo}_{WorldUID}";

  /// <summary>
  /// Todo see if this is needed
  /// </summary>
  /// <param name="zdoid"></param>
  /// <returns></returns>
  private static string ZDOIDToString(ZDOID zdoid)
  {
    var userId = zdoid.UserID;
    var id = zdoid.ID;
    return $"{userId},{id}";
  }

  /// <summary>
  /// Todo see if this is needed
  /// </summary>
  /// <param name="zdoidString"></param>
  /// <returns></returns>
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

  /// <summary>
  /// For debugging and scripts
  /// </summary>
  internal static void DEBUG_RemoveAllDynamicLocationKeys()
  {
    foreach (var keyValuePair in Player.m_localPlayer.m_customData)
    {
      if (keyValuePair.Key.Contains(GetFullPrefix()))
      {
        Logger.LogDebug(
          $"Removing: Key: {keyValuePair.Key} Value: {keyValuePair.Value}");
        Player.m_localPlayer.m_customData.Remove(keyValuePair.Key);
      }
    }
  }


  public static bool RemoveZdoTarget(
    PlayerSpawnController.LocationTypes locationType, Player? player)
  {
    if (player == null)
      return false;

    var selectedZdoKey = GetZdoStorageKey(locationType);
    var selectedOffsetKey = GetZdoStorageKey(locationType);
    if (!ZNet.instance) return false;

    if (player.m_customData.ContainsKey(selectedOffsetKey))
    {
      player.m_customData.Remove(selectedOffsetKey);
    }

    if (player.m_customData.ContainsKey(selectedZdoKey))
    {
      player.m_customData.Remove(selectedZdoKey);
    }

    return true;
  }

  public static IEnumerator GetZdoFromStore(
    PlayerSpawnController.LocationTypes locationType, Player? player)
  {
    var targetKey = GetZdoStorageKey(locationType);
    yield return GetZdoFromStore(targetKey, player);
  }

  /// <summary>
  /// Main getter logic for DynamicLocations, uses the player data to lookup a persistent ID saved to the player data for that world.
  /// </summary>
  /// <param name="targetKey"></param>
  /// <param name="player"></param>
  /// <returns></returns>
  public static IEnumerator GetZdoFromStore(string targetKey,
    Player? player)
  {
    if (player == null)
    {
      yield break;
    }

    if (!player.m_customData.TryGetValue(targetKey, out var zdoString))
    {
      yield break;
    }

    if (!int.TryParse(zdoString, out var zdoUid))
    {
      Logger.LogError(
        $"The targetKey <{targetKey}> zdoKey: <{zdoString}> could not be parsed as an int");
      yield break;
    }

    Logger.LogDebug(
      $"Retreiving targetKey <{targetKey}> zdoKey: <{zdoString}> for name: {player.GetPlayerName()} id: {player.GetPlayerID()}");

    // each game will create a new set of IDs, but the persistent data will allow for looking up the current game's ID.
    var output = ZdoWatchController.Instance.GetZdoFromServer(zdoUid);
    yield return output;
    yield return new WaitUntil(() => output.Current is ZDO);

    var zdoOutput = output.Current as ZDO;
    // ZDOs are not truely unique with ZdoWatcher and therefore must have a unique key for spawn / login objects so we know the ZDO returned is of this type instead of any ZDO matching especially if a ZDO was deleted.
    if (zdoOutput != null)
    {
      var dynamicLocationsObject =
        (zdoOutput).GetInt(ZdoVarKeys.DynamicLocationsPoint);
      if (dynamicLocationsObject == 0)
      {
        output = null;
      }
    }

    // Remove the zdo key from player if it no longer exists in the game (IE it was destroyed)
    if (zdoOutput == null)
    {
      Logger.LogDebug(
        $"Removing targetKey as it's ZDO no longer exists");
      player.m_customData.Remove(targetKey);
    }

    yield return zdoOutput;
  }

  public static void ResetCachedValues()
  {
    _cachedLocations.Clear();
  }

  public static Vector3 GetOffset(
    PlayerSpawnController.LocationTypes locationType, Player? player) =>
    GetOffset(GetOffsetStorageKey(locationType), player);

  public static Vector3 GetOffset(string key, Player? player)
  {
    if (player == null) return Vector3.zero;
    if (!player.m_customData.TryGetValue(key,
          out var offsetString))
    {
      return Vector3.zero;
    }

    var offset = StringToVector3(offsetString) ?? Vector3.zero;
    return offset;
  }


  public static ZDO? SetZdo(PlayerSpawnController.LocationTypes locationType,
    Player? player,
    ZNetView dynamicObj) =>
    SetZdo(GetZdoStorageKey(locationType), player, dynamicObj);

  /// <summary>
  /// This method is meant to be called when the ZDO is already loaded.
  /// </summary>
  /// <param name="saveKey"></param>
  /// <param name="player"></param>
  /// <param name="dynamicObj"></param>
  /// <returns></returns>
  public static ZDO? SetZdo(string saveKey, Player? player,
    ZNetView dynamicObj)
  {
    if (player == null) return null;
    if (!ZNet.instance) return null;
    var spawnPointObjZdo = dynamicObj.GetZDO();
    if (spawnPointObjZdo == null) return null;
    if (!ZdoWatchController.GetPersistentID(spawnPointObjZdo, out var id))
    {
      Logger.LogWarning(
        $"No persistent id found for dynamicObj {dynamicObj.gameObject.name}");
      return null;
    }

    spawnPointObjZdo.Set(ZdoVarKeys.DynamicLocationsPoint, 1);

    if (player.m_customData.TryGetValue(saveKey, out var zdoString))
    {
      player.m_customData[saveKey] = id.ToString();
    }
    else
    {
      player.m_customData.Add(saveKey, id.ToString());
    }


    if (zdoString == null)
    {
      Logger.LogError("Zdo string failed to set on player.customData");
    }

    Logger.LogDebug(
      $"Setting key: {saveKey}, uid: {spawnPointObjZdo.m_uid} for name: {player.GetPlayerName()} id: {player.GetPlayerID()}");

    // likely not needed
    Game.instance.m_playerProfile.SavePlayerData(player);

    // required to write to disk unfortunately due to logout not actually triggering a save meaning the player customData is not mutated and logging in resets to the previous player state
    Game.instance.m_playerProfile.Save();

    return spawnPointObjZdo;
  }

  public static Vector3? SetOffset(
    PlayerSpawnController.LocationTypes locationType, Player player,
    Vector3 offset) =>
    SetOffset(GetOffsetStorageKey(locationType), player, offset);

  /// <summary>
  /// Only sets offset if necessary, otherwise scrubs the data
  /// </summary>
  /// <param name="key"></param>
  /// <param name="player"></param>
  /// <param name="offset"></param>
  /// <returns></returns>
  public static Vector3? SetOffset(string key, Player player, Vector3 offset)
  {
    if (!player) return null;
    if (Vector3.zero == offset)
    {
      if (player.m_customData.TryGetValue(key, out _))
      {
        player.m_customData.Remove(key);
      }

      return null;
    }

    player.m_customData[key] = Vector3ToString(offset);
    return offset;
  }

  public static string GetOffsetStorageKey(
    PlayerSpawnController.LocationTypes locationType)
  {
    return locationType switch
    {
      PlayerSpawnController.LocationTypes.Spawn => GetSpawnZdoOffsetKey(),
      PlayerSpawnController.LocationTypes.Logout => GetLogoutZdoOffsetKey(),
      _ => throw new ArgumentOutOfRangeException(nameof(locationType),
        locationType, null)
    };
  }

  public static string GetZdoStorageKey(
    PlayerSpawnController.LocationTypes locationType)
  {
    return locationType switch
    {
      PlayerSpawnController.LocationTypes.Spawn => GetSpawnZdoKey(),
      PlayerSpawnController.LocationTypes.Logout => GetLogoutZdoKey(),
      _ => throw new ArgumentOutOfRangeException(nameof(locationType),
        locationType, null)
    };
  }

  public static bool SetLocationTypeData(
    PlayerSpawnController.LocationTypes locationType,
    Player player,
    ZNetView dynamicObj,
    Vector3 offset)
  {
    var locationOffset =
      SetOffset(GetOffsetStorageKey(locationType), player, offset);
    var locationZdo =
      SetZdo(GetZdoStorageKey(locationType), player, dynamicObj);
    if (locationZdo == null) return false;
    SetCachedDynamicLocation(locationType,
      new CacheLocationItem() { Zdo = locationZdo, Offset = locationOffset });
    return true;
  }

  public static void SaveWorldData()
  {
    var WorldSavePath = World.GetWorldSavePath();
    var fileName = $"{Game.instance.m_devWorldName}_mod_dynamic_locations.json";

    // File.WriteAllText(
    //   Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
    //     $"{ModName}_AutoDoc.md"),
    //   sb.ToString());
  }
}