using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Zolantris.Shared.Debug;
using Logger = Jotunn.Logger;

namespace ZdoWatcher;

public class ZdoWatchController : MonoBehaviour
{
  public static Action<ZDO>? OnDeserialize = null;
  public static Action<ZDO>? OnLoad = null;
  public static Action<ZDO>? OnReset = null;

  public static ZdoWatchController Instance;
  private readonly Dictionary<int, ZDO> _zdoGuidLookup = new();
  private readonly List<DebugSafeTimer> _timers = new();

  private CustomRPC RPC_RequestPersistentIdInstance;

  // In theory, we only need to hit the server then iterate and force send the zdos directly to the peer instead of the other way around.
  // We call SyncToPeer when which does this.
  public void Awake()
  {
    RPC_RequestPersistentIdInstance = NetworkManager.Instance.AddRPC(
      "RPC_RequestSync",
      RequestPersistentIdRPCServerReceive, null);
  }

  /// <summary>
  /// This will not allow mutation. This should be locked down if there is risk of the original source being destroyed.
  /// </summary>
  public Dictionary<int, ZDO> GetAllZdoGuids()
  {
    return _zdoGuidLookup.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
  }

  public void Update()
  {
    DebugSafeTimer.UpdateTimersFromList(_timers);
  }

  public void SyncToPeer(ZDOMan.ZDOPeer? zdoPeer)
  {
    if (!ZNet.instance.IsServer() || zdoPeer == null)
    {
      // Non-servers will not send data to players.
      return;
    }

    foreach (var zdoValue in _zdoGuidLookup.Values)
    {
      zdoPeer?.ForceSendZDO(zdoValue.m_uid);
    }
  }

  // React to the RPC call on a server
  private IEnumerator RequestPersistentIdRPCServerReceive(long sender,
    ZPackage package)
  {
    var persistentId = package.ReadInt();
    var zdoPeer = ZDOMan.instance.GetPeer(sender);
    Logger.LogMessage($"Sending first id across to peer {persistentId}");
    var zdoId = GetZdo(persistentId);
    if (zdoId != null && zdoPeer != null)
    {
      zdoPeer.ForceSendZDO(zdoId.m_uid);
    }
    else
    {
      Logger.LogMessage(
        "RequestPersistentIdRPCServerReceive called but zdoid not found, attempting to sync all ids to peer");
      SyncToPeer(zdoPeer);
    }

    yield return null;
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
  /// For requesting ZDO that matches the persistentId from server. This will do nothing besides start the request. Use WaitForZDO to wait for the request or GetZdoFromServerAsync which will combine both.
  /// </summary>
  /// <param name="persistentId">The persistent ZDOID int which is the int return of ZdoWatchManager.ZdoIdToId()</param>
  /// <returns>bool</returns>
  public bool RequestZdoFromServer(int persistentId)
  {
    if (ZNet.instance.IsServer())
    {
      return false;
    }

    // request only the single persistentZDO key needed. This is for efficiency especially for large servers that could have many persistent zdos.
    var package = new ZPackage();
    package.Write(persistentId);

    var serverPeer = ZRoutedRpc.instance.GetServerPeerID();
    RPC_RequestPersistentIdInstance.SendPackage(serverPeer, package);
    return true;
  }

  /// <summary>
  /// Combines the WaitForZdo and RequestZdoFromServer
  /// </summary>
  /// <param name="persistentId"></param>
  /// <param name="onComplete"></param>
  /// <returns></returns>
  public IEnumerator GetZdoFromServerAsync(int persistentId,
    Action<ZDO?> onComplete)
  {
    if (ZNet.instance.IsServer())
    {
      var zdo = GetZdo(persistentId);
      Logger.LogWarning(
        $"Called GetZdoFromServer for {persistentId} on the server. This should not happen");
      onComplete(zdo);
      yield break;
    }

    var localZdo = GetZdo(persistentId);
    if (localZdo != null)
    {
      onComplete(localZdo);
      yield break;
    }

    if (PendingPersistentIdQueries.ContainsKey(persistentId))
    {
      Logger.LogWarning(
        $"RequestPersistentID called for ongoing operation on id: {persistentId}, requests cannot be called during a specific timelimit");
      onComplete(null);
      yield break;
    }

    RequestZdoFromServer(persistentId);
    yield return WaitForZdo(persistentId, onComplete);
    Logger.LogDebug("GetZdoFromServerAsync finished");
  }

  private IEnumerator WaitForZdo(int persistentId, Action<ZDO?> onComplete,
    int timeoutInMs = 2000)
  {
    var timer = DebugSafeTimer.StartNew(_timers);
    ZDO? targetZdo = null;

    // A bit more efficient than the WaitUntil predicate
    while (timer.ElapsedMilliseconds < timeoutInMs && targetZdo == null)
    {
      if (_zdoGuidLookup.TryGetValue(persistentId, out var maybeZdo))
      {
        targetZdo = maybeZdo;
        break;
      }

      yield return new WaitForFixedUpdate();
    }

    if (timer.ElapsedMilliseconds >= timeoutInMs)
    {
      Logger.LogWarning(
        "Timeout for WaitForZdo reached, exiting the WaitForZdo call with a failure.");
    }
    else
    {
      Logger.LogDebug(
        $"Completed timer in: {timer.ElapsedMilliseconds} milliseconds");
    }


    if (PendingPersistentIdQueries.ContainsKey(persistentId))
    {
      PendingPersistentIdQueries.Remove(persistentId);
    }

    onComplete(targetZdo);
  }


  public void Reset() => _zdoGuidLookup.Clear();

  public static bool GetPersistentID(ZDO zdo, out int id)
  {
    id = zdo.GetInt(ZdoVarController.PersistentUidHash, 0);
    return id != 0;
  }

  public static int ZdoIdToId(ZDOID zdoid) =>
    (int)zdoid.UserID + (int)zdoid.ID;

  public int GetOrCreatePersistentID(ZDO? zdo)
  {
    if (zdo == null)
    {
      Logger.LogWarning(
        "GetOrCreatePersistentID called with a null ZDO, this will be disabled in the future.");
    }

    zdo ??= new ZDO();

    var id = zdo.GetInt(ZdoVarController.PersistentUidHash, 0);
    if (id != 0) return id;
    id = ZdoIdToId(zdo.m_uid);

    // If the ZDO is not unique/exists in the dictionary, this number must be incremented to prevent a collision
    while (_zdoGuidLookup.ContainsKey(id))
      ++id;
    zdo.Set(ZdoVarController.PersistentUidHash, id, false);

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