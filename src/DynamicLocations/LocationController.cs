using System.IO;
using UnityEngine;
using ZdoWatcher;
using Logger = Jotunn.Logger;

namespace DynamicLocations;

public static class LocationController
{
  private const string DynamicPrefix = "Dynamic";
  private const string SpawnZdo = "SpawnZdo";
  private const string LogoutParentZdoOffset = "LogoutParentZdoOffset";
  private const string LogoutParentZdo = "LogoutParentZdo";

  public static ZDO? cachedSpawnTarget;
  public static Vector3? cachedSpawnTargetOffset;

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

  public static ZDO? GetLogoutZdo(Player player)
  {
    if (!player) return null;
    if (!player.m_customData.TryGetValue(GetLogoutZdoKey(), out var logoutZdoString))
    {
      return null;
    }

    var zdoid = StringToZDOID(logoutZdoString);
    Logger.LogDebug(
      $"Retreiving spawnTargetZdo {zdoid} for name: {player.GetPlayerName()} id: {player.GetPlayerID()}");
    var output = zdoid == ZDOID.None ? ZdoWatchManager.ZdoIdToId(zdoid.Value) : 0;

    // each game will create a new set of IDs, but the persistent data will allow for looking up the current game's ID.
    return ZdoWatchManager.Instance.GetZdo(output);
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

  public static bool RemoveSpawnTargetZdo(Player player)
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

  /// <summary>
  /// Main getter logic for DynamicLocations, uses the player data to lookup a persistent ID saved to the player data for that world.
  /// </summary>
  /// <param name="targetKey"></param>
  /// <param name="player"></param>
  /// <returns></returns>
  public static ZDO? GetZDOFromTargetKey(string targetKey, Player player)
  {
    if (cachedSpawnTarget != null) return cachedSpawnTarget;
    if (!player) return null;
    if (!player.m_customData.TryGetValue(targetKey, out var zdoString))
    {
      return null;
    }

    if (!int.TryParse(zdoString, out var zdoUid))
    {
      Logger.LogError(
        $"The targetKey <{targetKey}> zdoKey: <{zdoString}> could not be parsed as an int");
      return null;
    }

    Logger.LogDebug(
      $"Retreiving targetKey <{targetKey}> zdoKey: <{zdoString}> for name: {player.GetPlayerName()} id: {player.GetPlayerID()}");

    // each game will create a new set of IDs, but the persistent data will allow for looking up the current game's ID.
    var output = ZdoWatchManager.Instance.GetZdo(zdoUid);

    // Remove the zdo key from player if it no longer exists in the game (IE it was destroyed)
    if (output == null)
    {
      Logger.LogDebug(
        $"Removing targetKey as it's ZDO no longer exists");
      player.m_customData.Remove(targetKey);
    }

    return output;
  }

  public static ZDO? GetSpawnTargetZdo(Player player)
  {
    if (cachedSpawnTarget != null) return cachedSpawnTarget;
    cachedSpawnTarget = GetZDOFromTargetKey(GetSpawnZdoKey(), player);
    return cachedSpawnTarget;
  }

  public static Vector3 GetSpawnTargetZdoOffset(Player player)
  {
    if (cachedSpawnTargetOffset != null) return cachedSpawnTargetOffset.Value;
    if (!player) return Vector3.zero;
    if (!player.m_customData.TryGetValue(GetSpawnZdoOffsetKey(), out var offsetString))
    {
      return Vector3.zero;
    }

    var offset = StringToVector3(offsetString) ?? Vector3.zero;
    return offset;
  }

  public static bool SetSpawnZdoTarget(Player player, ZNetView dynamicObj)
  {
    if (!ZNet.instance) return false;
    cachedSpawnTarget = null;
    var spawnPointObjZdo = dynamicObj.GetZDO();
    if (spawnPointObjZdo == null) return false;
    if (!ZdoWatchManager.GetPersistentID(spawnPointObjZdo, out var id))
    {
      Logger.LogWarning($"No persitent id found for dynamicObj {dynamicObj.gameObject.name}");
      return false;
    }

    player.m_customData[GetSpawnZdoKey()] = id.ToString();
    Logger.LogDebug(
      $"Setting spawnTargetZdo {spawnPointObjZdo.m_uid} for name: {player.GetPlayerName()} id: {player.GetPlayerID()}");
    cachedSpawnTarget = spawnPointObjZdo;

    return true;
  }

  /// <summary>
  /// Only sets offset if necessary, otherwise scrubs the data
  /// </summary>
  /// <param name="player"></param>
  /// <param name="offset"></param>
  /// <returns></returns>
  public static bool SetSpawnZdoTargetOffset(Player player, Vector3 offset)
  {
    cachedSpawnTargetOffset = null;
    if (!player) return false;
    if (Vector3.zero == offset)
    {
      if (player.m_customData.TryGetValue(GetSpawnZdoOffsetKey(), out _))
      {
        player.m_customData.Remove(GetSpawnZdoOffsetKey());
      }

      return false;
    }

    player.m_customData[GetSpawnZdoOffsetKey()] = Vector3ToString(offset);
    cachedSpawnTargetOffset = offset;
    return true;
  }

  public static bool SetSpawnZdoTargetWithOffset(Player player, ZNetView dynamicObj,
    Vector3 offset)
  {
    if (!SetSpawnZdoTarget(player, dynamicObj)) return false;
    SetSpawnZdoTargetOffset(player, offset);
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