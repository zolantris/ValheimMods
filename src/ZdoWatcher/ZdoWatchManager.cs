using System;
using System.Collections;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ZdoWatcher;

public class ZdoWatchManager : MonoBehaviour
{
  public static Action<ZDO>? OnDeserialize = null;
  public static Action<ZDO>? OnLoad = null;
  public static Action<ZDO>? OnReset = null;

  public static ZdoWatchManager Instance;
  private readonly Dictionary<int, ZDO> _zdoGuidLookup = new();

  private CustomRPC RPC_RequestPersistentIdInstance;
  private CustomRPC RPC_ClientSyncInstance;

  public void Awake()
  {
    // RPC_ClientSyncInstance = RPC_RequestPersistentIdInstance =
    //   NetworkManager.Instance.AddRPC(
    //     "RPC_ClientSync",
    //     null, RPC_ClientSync);
    RPC_RequestPersistentIdInstance = NetworkManager.Instance.AddRPC(
      "RPC_RequestSync",
      RequestPersistentIdRPCServerReceive, RPC_ClientSync);
  }

  // React to the RPC call on a server
  private IEnumerator RequestPersistentIdRPCServerReceive(long sender,
    ZPackage package)
  {
    Logger.LogMessage(
      $"Received request for persistentZDOID, processing");

    var requestedZdoId = package.ReadInt();

    // this zdoid must then be retreived on client with a call of ZDOMan.instance
    var serverZdo = GetZdo(requestedZdoId);
    // var zdoid = serverZdo?.m_uid ?? ZDOID.None;
    //
    var serverZdoIdPackage = GetAllServerZdoIds();

    Logger.LogMessage($"Broadcasting to all clients");
    RPC_RequestPersistentIdInstance.SendPackage(sender,
      serverZdoIdPackage);
    yield return null;
  }

  public void WriteAllZdoIdsToLocalStore(ZPackage package)
  {
    var pos = 0;
    var packageSize = package.Size();
    while (packageSize > pos)
    {
      package.SetPos(pos);
      var zdoid = package.ReadZDOID();
      var zdo = ZDOMan.instance.GetZDO(zdoid);
      if (zdo != null)
      {
        GetOrCreatePersistentID(zdo);
      }

      pos = package.GetPos();
    }
  }

  public Dictionary<int, ZDO?> PendingPersistentIdQueries = new();

  // sends all the persisted zdoid to the client
  // likely better approach then individual sending which is more likely to desync.
  public ZPackage GetAllServerZdoIds()
  {
    var zPackage = new ZPackage();

    Logger.LogMessage(
      $"Writing {_zdoGuidLookup.Values.Count} zdos to a ZPackage");
    foreach (var zdoValue in _zdoGuidLookup.Values)
    {
      zPackage.Write(zdoValue.m_uid);
    }

    return zPackage;
  }

  /// <summary>
  /// todo would be cleaner with promises, but this should work.
  /// </summary>
  /// <param name="persistentId">The persistent ZDOID int which is the int return of ZdoWatchManager.ZdoIdToId()</param>
  /// <returns>ZDO</returns>
  public IEnumerator GetZdoFromServer(int persistentId)
  {
    if (ZNet.instance.IsServer())
    {
      yield return GetZdo(persistentId);
    }
    else
    {
      if (PendingPersistentIdQueries.ContainsKey(persistentId))
      {
        Logger.LogWarning(
          "RequestPersistentID called for ongoing operation, exiting to prevent duplicate side effect issues");
        yield break;
      }

      PendingPersistentIdQueries.Add(persistentId, null);

      var package = new ZPackage();
      package.Write(persistentId);
      var serverPeer = ZRoutedRpc.instance.GetServerPeerID();
      RPC_RequestPersistentIdInstance.SendPackage(serverPeer, package);

      // todo add expiration, this should be quick, but not having a timer would possibly cause huge problems
      yield return new WaitUntil(() =>
        RPC_RequestPersistentIdInstance.IsSending == false);
      yield return new WaitUntil(() =>
        _zdoGuidLookup.TryGetValue(persistentId, out _));
      if (PendingPersistentIdQueries.ContainsKey(persistentId))
      {
        PendingPersistentIdQueries.Remove(persistentId);
      }

      yield return GetZdo(persistentId);
    }
  }

  // React to the RPC call on a client
  private IEnumerator RPC_ClientSync(long sender, ZPackage package)
  {
    if (ZNet.instance.IsServer())
    {
      Logger.LogMessage("Skipping Server call for RPC_ClientSync");
      yield break;
    }

    Logger.LogMessage(
      $"Client received blob from sender: {sender}, processing");
    // var requestedZdoId = package.ReadZDOID();
    WriteAllZdoIdsToLocalStore(package);
    Logger.LogMessage("Synced zdo store -> success");
    // var zdo = ZDOMan.instance.GetZDO(requestedZdoId);
    yield return true;
  }

  public void Reset() => _zdoGuidLookup.Clear();

  /// <summary>
  /// PersistentIds many need to migrate to a safer structure such as using uuid.v4 or similar logic for larger longer lasting games
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
    return id != 0;
  }

  /// <summary>
  /// Requests for a persistent ID, meant for cross server support IE LAN peer or any peer connecting to dedicated which would not have a reference to the ZDOID unless it was loaded in their area. Logoff would clear these so it's necessary to request a zdo from the server which holds the references indefinitely.
  /// </summary>
  public static void RPC_RequestPersistentId()
  {
    if (ZNet.instance.IsDedicated())
    {
      return;
    }
  }

  // sends the persistentID to the peer if on a dedicated server which would not have the ID reference
  public static void RPC_SendPersistentId()
  {
  }

  public static int ZdoIdToId(ZDOID zdoid) =>
    (int)zdoid.UserID + (int)zdoid.ID;

  public int GetOrCreatePersistentID(ZDO? zdo)
  {
    zdo ??= new ZDO();

    var id = zdo.GetInt(ZdoVarManager.PersistentUidHash, 0);
    if (id != 0) return id;
    id = ZdoIdToId(zdo.m_uid);

    // If the ZDO is not unique/exists in the dictionary, this number must be incremented to prevent a collision
    while (_zdoGuidLookup.ContainsKey(id))
      ++id;
    zdo.Set(ZdoVarManager.PersistentUidHash, id, false);

    _zdoGuidLookup[id] = zdo;

    return id;
  }

  public void HandleRegisterPersistentId(ZDO zdo)
  {
    if (!GetPersistentID(zdo, out var id))
    {
      return;
    }

    _zdoGuidLookup[id] = zdo;
  }

  private void HandleDeregisterPersistentId(ZDO zdo)
  {
    if (!GetPersistentID(zdo, out var id))
      return;

    _zdoGuidLookup.Remove(id);
  }

  public void Deserialize(ZDO zdo)
  {
    HandleRegisterPersistentId(zdo);

    if (OnDeserialize == null) return;
    try
    {
      OnDeserialize(zdo);
    }
    catch
    {
      Logger.LogError("OnDeserialize had an error");
    }
  }

  public void Load(ZDO zdo)
  {
    HandleRegisterPersistentId(zdo);
    if (OnLoad == null) return;
    try
    {
      OnLoad(zdo);
    }
    catch
    {
      Logger.LogError("OnLoad had an error");
    }
  }

  public void Reset(ZDO zdo)
  {
    HandleDeregisterPersistentId(zdo);
    if (OnReset == null) return;
    try
    {
      OnReset(zdo);
    }
    catch
    {
      Logger.LogError("OnReset had an error");
    }
  }

  /// <summary>
  /// Gets the ZDO from the persistent ZDOID int
  /// </summary>
  /// <param name="id"></param>
  /// <returns>ZDO|null</returns>
  public ZDO? GetZdo(int id)
  {
    return _zdoGuidLookup.TryGetValue(id, out var zdo) ? zdo : null;
  }

  public GameObject? GetGameObject(int id)
  {
    var instance = GetInstance(id);
    return instance
      ? instance?.gameObject
      : null;
  }

  public ZNetView? GetInstance(int id)
  {
    var zdo = GetZdo(id);
    if (zdo == null) return null;
    var output = ZNetScene.instance.FindInstance(zdo);
    return output;
  }
}