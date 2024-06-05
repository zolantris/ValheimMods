using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Util
{
  public class ZdoPersistManager
  {
    public static readonly int PersistentUidHash =
      "PersistentID".GetStableHashCode();

    public static Action<ZDO>[] OnRegister = [];
    public static Action<ZDO>[] OnUnRegister = [];

    public static ZdoPersistManager Instance = new();
    private Dictionary<int, ZDO> m_zdoGuidLookup = new();
    private Dictionary<int, int> m_zdoPersistentLookup = new();

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
      id = zdo.GetInt(PersistentUidHash, 0);

      id = zdo.GetInt(PersistentUidHash, 0);
      return id != 0;
    }

    public static int ZDOIDToId(ZDOID zdoid) =>
      (int)zdoid.UserID + (int)zdoid.ID;

    public int GetOrCreatePersistentID(ZDO zdo)
    {
      zdo ??= new ZDO();

      int id = zdo.GetInt(PersistentUidHash, 0);
      if (id == 0)
      {
        id = ZDOIDToId(zdo.m_uid);

        // If the ZDO is not unique/exists in the dictionary, this number must be incremented to prevent a collision
        while (m_zdoGuidLookup.ContainsKey(id))
          ++id;
        zdo.Set(PersistentUidHash, id, false);

        // todo fix this bug
        // This will lead to lookup errors due to the zdo.m_uid not matching when doing a lookup
        m_zdoGuidLookup[id] = zdo;

        var localZdoid = ZDOIDToId(zdo.m_uid);
        if (m_zdoPersistentLookup.ContainsKey(localZdoid))
        {
          Logger.LogWarning($"ZDOID already exists for {localZdoid} and persistentId: {id}");
        }
        else
        {
          m_zdoPersistentLookup[localZdoid] = id;
        }
      }

      return id;
    }

    public void HandlePrefabRegistration(ZDO zdo)
    {
      var playerSpawn = zdo.GetInt(VehicleZdoVars.DynamicLocationSpawn, -1);
      var playerLocation = zdo.GetInt(VehicleZdoVars.DynamicLocationLogout);
    }

    public void Register(ZDO zdo)
    {
      int id;
      if (!GetPersistentID(zdo, out id))
      {
        //
#if DEBUG
        // Experiment to see if zdos collide with only USER_ID and ID combos and items being deleted
        GetOrCreatePersistentID(zdo);
#endif
        return;
      }

      foreach (var action in OnRegister)
      {
        action(zdo);
      }

      m_zdoGuidLookup[id] = zdo;
    }

    public void Unregister(ZDO zdo)
    {
      foreach (var action in OnUnRegister)
      {
        action(zdo);
      }

      int id;
      if (!GetPersistentID(zdo, out id))
        return;
      m_zdoGuidLookup.Remove(id);
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

    /// <summary>
    /// Safe way to use the current ZDO to get the stored ZDO data
    /// </summary>
    /// <returns>ZDO|null</returns>
    public ZDO? GetZDOFromCurrentUid(ZDOID zdoId)
    {
      var uidHash = ZDOIDToId(zdoId);
      if (!m_zdoPersistentLookup.TryGetValue(uidHash, out var zdoPersistentUId))
      {
        return null;
      }

      return GetZDO(zdoPersistentUId);
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