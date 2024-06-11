using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ZdoWatcher;

public class ZdoWatchManager
{
  public static Action<ZDO> OnDeserialize;
  public static Action<ZDO> OnLoad;
  public static Action<ZDO> OnReset;

  public static ZdoWatchManager Instance = new();
  private Dictionary<int, ZDO> m_zdoGuidLookup = new();

  public void Reset() => m_zdoGuidLookup.Clear();

  /// <summary>
  /// PersistentIds have migrated to a safer structure so players cannot potentially break their dictionaries with duplicate ZDOIDs
  /// </summary>
  /// <returns></returns>
  // public static int ParsePersistentIdString(string persistentString)
  // {
  //   // if (id == 0)
  //   // {
  //   //   // var outputString = zdo.GetString(PersistentUidHash, "");
  //   //   // if (outputString != "")
  //   //   // {
  //   //   //   id = ParsePersistentIdString(outputString);
  //   //   // }
  //   // }
  // }
  public static bool GetPersistentID(ZDO zdo, out int id)
  {
    id = zdo.GetInt(ZdoVarManager.PersistentUidHash, 0);

    id = zdo.GetInt(ZdoVarManager.PersistentUidHash, 0);
    return id != 0;
  }

  public static int ZdoIdToId(ZDOID zdoid) =>
    (int)zdoid.UserID + (int)zdoid.ID;

  public int GetOrCreatePersistentID(ZDO zdo)
  {
    zdo ??= new ZDO();

    var id = zdo.GetInt(ZdoVarManager.PersistentUidHash, 0);
    if (id != 0) return id;
    id = ZdoIdToId(zdo.m_uid);

    // If the ZDO is not unique/exists in the dictionary, this number must be incremented to prevent a collision
    while (m_zdoGuidLookup.ContainsKey(id))
      ++id;
    zdo.Set(ZdoVarManager.PersistentUidHash, id, false);

    m_zdoGuidLookup[id] = zdo;

    return id;
  }

  public void HandleRegisterPersistentId(ZDO zdo)
  {
    if (!GetPersistentID(zdo, out var id))
    {
      return;
    }

    m_zdoGuidLookup[id] = zdo;
  }

  private void HandleDeregisterPersistentId(ZDO zdo)
  {
    if (!GetPersistentID(zdo, out var id))
      return;

    m_zdoGuidLookup.Remove(id);
  }

  public void Deserialize(ZDO zdo)
  {
    OnDeserialize(zdo);
    HandleRegisterPersistentId(zdo);
  }

  public void Load(ZDO zdo)
  {
    OnLoad(zdo);
    HandleRegisterPersistentId(zdo);
  }

  public void Reset(ZDO zdo)
  {
    OnReset(zdo);
    HandleDeregisterPersistentId(zdo);
  }

  /// <summary>
  /// Gets the ZDO from the persistent ZDOID int
  /// </summary>
  /// <param name="id"></param>
  /// <returns>ZDO|null</returns>
  public ZDO? GetZDO(int id)
  {
    ZDO zdo;
    return m_zdoGuidLookup.TryGetValue(id, out zdo) ? zdo : null;
  }

  public GameObject GetGameObject(int id)
  {
    var instance = GetInstance(id);
    return instance
      ? instance.gameObject
      : null;
  }

  public ZNetView GetInstance(int id)
  {
    var zdo = GetZDO(id);
    return zdo != null ? ZNetScene.instance.FindInstance(zdo) : null;
  }
}