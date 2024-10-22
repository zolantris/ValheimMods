using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicLocations.Config;
using DynamicLocations.Constants;
using Jotunn.Managers;
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
    Dictionary<LocationVariation, ICachedLocation>
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
    LocationVariation locationVariationType)
  {
    _cachedLocations.TryGetValue(locationVariationType, out var cachedLocation);
    return cachedLocation;
  }

  public static bool SetCachedDynamicLocation(
    LocationVariation locationVariationType,
    CacheLocationItem cachedLocationItem)
  {
    if (GetCachedDynamicLocation(locationVariationType) != null)
    {
      _cachedLocations[locationVariationType] = cachedLocationItem;
    }
    else
    {
      _cachedLocations.Add(locationVariationType, cachedLocationItem);
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
    foreach (var keyValuePair in Player.m_localPlayer.m_customData.ToArray())
    {
      if (keyValuePair.Key.Contains(GetFullPrefix()))
      {
        Logger.LogDebug(
          $"Removing: Key: {keyValuePair.Key} Value: {keyValuePair.Value}");
        Player.m_localPlayer.m_customData.Remove(keyValuePair.Key);
      }

      // this was a local change
      // todo remove this
      if (keyValuePair.Key.Contains("valheim_vehicles") &&
          DynamicLocationsConfig.IsDebug)
      {
        Logger.LogDebug(
          $"Removing: Key: {keyValuePair.Key} Value: {keyValuePair.Value}");
        Player.m_localPlayer.m_customData.Remove(keyValuePair.Key);
      }
    }
  }


  public static bool RemoveZdoTarget(
    LocationVariation locationVariationType, Player? player)
  {
    if (player == null)
      return false;

    var selectedZdoKey = GetZdoStorageKey(locationVariationType);
    var selectedOffsetKey = GetZdoStorageKey(locationVariationType);
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

  /// <summary>
  /// Main getter logic for DynamicLocations, uses the player data to lookup a persistent ID saved to the player data for that world.
  /// </summary>
  /// <param name="targetKey"></param>
  /// <param name="player"></param>
  /// <returns></returns>
  public static IEnumerator GetZdoFromStore(
    LocationVariation locationVariationType,
    Player? player)
  {
    var targetKey = GetZdoStorageKey(locationVariationType);
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
    LocationVariation locationVariationType, Player? player) =>
    GetOffset(GetOffsetStorageKey(locationVariationType), player);

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

  /// <summary>
  /// This method is meant to be called when the ZDO is already loaded.
  /// </summary>
  /// <param name="locationVaration"></param>
  /// <param name="player"></param>
  /// <param name="zdo"></param>
  /// <returns></returns>
  public static ZDO? SetZdo(LocationVariation locationVaration,
    Player? player,
    ZDO? zdo)
  {
    if (player == null) return null;
    if (!ZNet.instance) return null;
    if (zdo == null) return null;
    var saveKey = GetZdoStorageKey(locationVaration);
    if (!ZdoWatchController.GetPersistentID(zdo, out var id))
    {
      Logger.LogWarning(
        $"No persistent id found for dynamicObj {zdo}");
      return null;
    }

    zdo.Set(ZdoVarKeys.DynamicLocationsPoint, 1);

    if (player.m_customData.TryGetValue(saveKey, out var zdoString))
    {
      player.m_customData[saveKey] = id.ToString();
    }
    else
    {
      player.m_customData.Add(saveKey, id.ToString());
    }


    if (!player.m_customData.ContainsKey(saveKey))
    {
      Logger.LogError("Zdo string failed to set on player.customData");
    }

    Logger.LogDebug(
      $"Setting key: {saveKey}, uid: {zdo.m_uid} for name: {player.GetPlayerName()} id: {player.GetPlayerID()}");

    // likely not needed
    Game.instance.m_playerProfile.SavePlayerData(player);

    // required to write to disk unfortunately due to logout not actually triggering a save meaning the player customData is not mutated and logging in resets to the previous player state
    Game.instance.m_playerProfile.Save();

    return zdo;
  }

  public static Vector3? SetOffset(
    LocationVariation locationVariationType, Player player,
    Vector3 offset) =>
    SetOffset(GetOffsetStorageKey(locationVariationType), player, offset);

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
    LocationVariation locationVariationType)
  {
    return locationVariationType switch
    {
      LocationVariation.Spawn => GetSpawnZdoOffsetKey(),
      LocationVariation.Logout => GetLogoutZdoOffsetKey(),
      _ => throw new ArgumentOutOfRangeException(nameof(locationVariationType),
        locationVariationType, null)
    };
  }

  public static string GetZdoStorageKey(
    LocationVariation locationVariationType)
  {
    return locationVariationType switch
    {
      LocationVariation.Spawn => GetSpawnZdoKey(),
      LocationVariation.Logout => GetLogoutZdoKey(),
      _ => throw new ArgumentOutOfRangeException(nameof(locationVariationType),
        locationVariationType, null)
    };
  }

  public static bool SetLocationTypeData(
    LocationVariation locationVariation,
    Player player,
    ZDO zdo,
    Vector3 offset)
  {
    var locationOffset =
      SetOffset(GetOffsetStorageKey(locationVariation), player, offset);
    var locationZdo =
      SetZdo(locationVariation, player, zdo);
    if (locationZdo == null) return false;
    SetCachedDynamicLocation(locationVariation,
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