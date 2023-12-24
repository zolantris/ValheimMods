using System.Collections.Generic;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Util
{
  public class ZDOPersistantID
  {
    public static readonly int PersistantIDHash =
      "PersistantID".GetStableHashCode();

    public static ZDOPersistantID Instance = new ZDOPersistantID();
    private Dictionary<int, ZDO> m_zdoGuidLookup = new Dictionary<int, ZDO>();

    public void Reset() => this.m_zdoGuidLookup.Clear();

    public bool GetPersistentID(ZDO zdo, out int id)
    {
      // Logger.LogInfo($"GetPersistentID called {PersistantIDHash}");
      id = zdo.GetInt(ZDOPersistantID.PersistantIDHash, 0);
      return id != 0;
    }

    public static int ZDOIDToId(ZDOID zdoid) =>
      (int)zdoid.UserID + (int)zdoid.ID;

    public int GetOrCreatePersistantID(ZDO zdo)
    {
      int id = zdo.GetInt(ZDOPersistantID.PersistantIDHash, 0);
      if (id == 0)
      {
        id = ZDOPersistantID.ZDOIDToId(zdo.m_uid);
        // ZLog.LogWarning($"ZDOIDToId(): ZDO ID was 0, now {id}");
        while (this.m_zdoGuidLookup.ContainsKey(id))
          ++id;
        zdo.Set(ZDOPersistantID.PersistantIDHash, id, false);

        ZLog.LogWarning($"ZDOIDToId(): dictionary {id} set to {zdo}");
        this.m_zdoGuidLookup[id] = zdo;
      }

      return id;
    }

    public void Register(ZDO zdo)
    {
      int id;
      if (!this.GetPersistentID(zdo, out id))
        return;
      this.m_zdoGuidLookup[id] = zdo;
    }

    public void Unregister(ZDO zdo)
    {
      int id;
      if (!this.GetPersistentID(zdo, out id))
        return;
      this.m_zdoGuidLookup.Remove(id);
    }

    public ZDO GetZDO(int id)
    {
      ZDO zdo;
      return this.m_zdoGuidLookup.TryGetValue(id, out zdo) ? zdo : (ZDO)null;
    }

    public GameObject GetGameObject(int id)
    {
      ZNetView instance = this.GetInstance(id);
      return instance
        ? ((Component)instance).gameObject
        : (GameObject)null;
    }

    public ZNetView GetInstance(int id)
    {
      ZDO zdo = this.GetZDO(id);
      return zdo != null ? ZNetScene.instance.FindInstance(zdo) : (ZNetView)null;
    }
  }
}