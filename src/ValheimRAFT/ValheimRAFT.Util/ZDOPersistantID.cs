using System.Collections.Generic;
using UnityEngine;

namespace ValheimRAFT.Util;

public class ZDOPersistantID
{
	public static readonly int PersistantIDHash = "PersistantID".GetStableHashCode();

	public static ZDOPersistantID Instance = new ZDOPersistantID();

	private Dictionary<int, ZDO> m_zdoGuidLookup = new Dictionary<int, ZDO>();

	public void Reset()
	{
		m_zdoGuidLookup.Clear();
	}

	public bool GetPersistentID(ZDO zdo, out int id)
	{
		id = zdo.GetInt(PersistantIDHash);
		return id != 0;
	}

	public static int ZDOIDToId(ZDOID zdoid)
	{
		return (int)zdoid.UserID + (int)zdoid.ID;
	}

	public int GetOrCreatePersistantID(ZDO zdo)
	{
		int id = zdo.GetInt(PersistantIDHash);
		if (id == 0)
		{
			for (id = ZDOIDToId(zdo.m_uid); m_zdoGuidLookup.ContainsKey(id); id++)
			{
			}
			zdo.Set(PersistantIDHash, id);
			m_zdoGuidLookup[id] = zdo;
		}
		return id;
	}

	public void Register(ZDO zdo)
	{
		if (GetPersistentID(zdo, out var id))
		{
			m_zdoGuidLookup[id] = zdo;
		}
	}

	public void Unregister(ZDO zdo)
	{
		if (GetPersistentID(zdo, out var id))
		{
			m_zdoGuidLookup.Remove(id);
		}
	}

	public ZDO GetZDO(int id)
	{
		if (m_zdoGuidLookup.TryGetValue(id, out var zdo))
		{
			return zdo;
		}
		return null;
	}

	public GameObject GetGameObject(int id)
	{
		ZNetView nv = GetInstance(id);
		if ((bool)nv)
		{
			return nv.gameObject;
		}
		return null;
	}

	public ZNetView GetInstance(int id)
	{
		ZDO zdo = GetZDO(id);
		if (zdo != null)
		{
			return ZNetScene.instance.FindInstance(zdo);
		}
		return null;
	}
}
