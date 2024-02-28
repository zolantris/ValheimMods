using System.Collections.Generic;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Util
{
  public class ZDOPersistentID
  {
    public static readonly int PersistentIDHash =
      "PersistentID".GetStableHashCode();

    public static ZDOPersistentID Instance = new();
    private Dictionary<int, ZDO> m_zdoGuidLookup = new();

    public void Reset() => m_zdoGuidLookup.Clear();

    public static bool GetPersistentID(ZDO zdo, out int id)
    {
      id = zdo.GetInt(PersistentIDHash, 0);
      return id != 0;
    }

    public static int ZDOIDToId(ZDOID zdoid) =>
      (int)zdoid.UserID + (int)zdoid.ID;

    public int GetOrCreatePersistentID(ZDO zdo)
    {
      zdo ??= new ZDO();

      int id = zdo.GetInt(PersistentIDHash, 0);
      if (id == 0)
      {
        id = ZDOIDToId(zdo.m_uid);
        while (m_zdoGuidLookup.ContainsKey(id))
          ++id;
        zdo.Set(PersistentIDHash, id, false);
        m_zdoGuidLookup[id] = zdo;
      }

      return id;
    }

    public void Register(ZDO zdo)
    {
      int id;
      if (!GetPersistentID(zdo, out id))
        return;
      m_zdoGuidLookup[id] = zdo;
    }

    public void Unregister(ZDO zdo)
    {
      int id;
      if (!GetPersistentID(zdo, out id))
        return;
      m_zdoGuidLookup.Remove(id);
    }

    public ZDO GetZDO(int id)
    {
      ZDO zdo;
      return m_zdoGuidLookup.TryGetValue(id, out zdo) ? zdo : null;
    }

    public GameObject GetGameObject(int id)
    {
      ZNetView instance = GetInstance(id);
      return instance
        ? instance.gameObject
        : null;
    }

    public ZNetView GetInstance(int id)
    {
      ZDO zdo = GetZDO(id);
      return zdo != null ? ZNetScene.instance.FindInstance(zdo) : null;
    }
  }
}