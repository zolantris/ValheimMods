using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.ValheimVehicles.DynamicLocations;

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