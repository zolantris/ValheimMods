// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;

namespace ValheimVehicles.Integrations;

public class PowerNetworkControllerIntegration : PowerNetworkController
{
  private readonly List<string> _networksToRemove = new();

  public override void Awake()
  {
    LoggerProvider.LogDebug("Called Awake with debug");
    LoggerProvider.LogMessage("Called Awake with Message");
    base.Awake();
    LoggerProvider.LogMessage("Called post awake with message");
    StartCoroutine(DelayedRegister());
  }

  public IEnumerator DelayedRegister()
  {
    while (ZNet.instance == null || ZRoutedRpc.instance == null)
    {
      yield return null;
    }
    ZDOClaimUtility.RegisterClaimZdoRpc();
    RegisterRebuildRpc();
  }

  private static bool _registeredRpc = false;
  public void EnsureZDOClaimRPCRegistered()
  {
    if (!_registeredRpc)
    {
      ZRoutedRpc.instance.Register<ZDOID>(nameof(ValheimVehicles_Server_ClaimZDO), ValheimVehicles_Server_ClaimZDO);
      _registeredRpc = true;
    }
  }

  // DO nothing for fixed update. Hosts cannot run FixedUpdate on server I think...
  protected override void FixedUpdate() {}
  protected override void Update()
  {
    base.Update();
    SimulateOnClientAndServer();
  }

  public static List<ZDO> GetAllZDOsWithPrefab(string prefabName)
  {
    var results = new List<ZDO>();
    var index = 0;
    while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefabName, results, ref index)) ;
    return results;
  }

  // Client-side: call this after spawning the prefab
  public static void RequestServerToClaimZDO(ZDOID id)
  {
    ZRoutedRpc.instance.InvokeRoutedRPC("ValheimVehicles_ClaimZDO", id);
  }

  public void ValheimVehicles_Server_ClaimZDO(long sender, ZDOID id)
  {
    if (!ZNet.instance || !ZNet.instance.IsServer())
    {
      ZLog.LogWarning("[ZDOClaim] Attempted to handle ClaimZDO on a non-server instance.");
      return;
    }

    var zdo = ZDOMan.instance.GetZDO(id);
    if (zdo == null || !zdo.Persistent)
    {
      ZLog.LogWarning($"[ZDOClaim] Invalid or non-persistent ZDO {id} from peer {sender}");
      return;
    }

    // Take ownership
    if (zdo.GetOwner() != ZDOMan.GetSessionID())
    {
      zdo.SetOwner(ZDOMan.GetSessionID());
    }

    // Instantiate if not already present
    if (ZNetScene.instance.FindInstance(id) == null)
    {
      var prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
      if (prefab == null)
      {
        ZLog.LogError($"[ZDOClaim] Missing prefab for hash {zdo.GetPrefab()}");
        return;
      }

      Instantiate(prefab, zdo.GetPosition(), Quaternion.identity);
      ZLog.Log($"[ZDOClaim] Server claimed and instantiated {prefab.name} at {zdo.GetPosition()}");
    }
  }


  public void ValidateServerProblems()
  {
    foreach (var player in Player.s_players)
    {
      if (!player) continue;

      var pos = player.transform.position;

      var prefabName = PrefabNames.Mechanism_Power_Storage_Eitr;
      var list = GetAllZDOsWithPrefab(prefabName);

      if (list.Count > 0)
      {
        var totalCount = 0;
        foreach (var zdo in list)
        {
          if (Vector3.Distance(zdo.GetPosition(), pos) < 50f)
          {
            totalCount++;
            var comp = ZNetScene.instance.FindInstance(zdo.m_uid);
            if (comp)
            {
              var powerStorageComponent = comp.GetComponent<PowerStorageComponentIntegration>();
              if (powerStorageComponent)
              {
                LoggerProvider.LogInfoDebounced("Found copy of powerStorageComponentIntegration...force registering");
                RegisterPowerComponent(powerStorageComponent);
              }
            }
          }
        }
        LoggerProvider.LogInfoDebounced($"Found ({totalCount}) counts of  ZDO for prefab {prefabName}");
      }
      else
      {
        LoggerProvider.LogInfoDebounced($"Found no ZDO for prefab {prefabName}");
      }
    }
  }

  // foreach (var pieceTable in Resources.FindObjectsOfTypeAll<PieceTable>())
  // {
  //   foreach (var pieceTableMPiece in pieceTable.m_pieces)
  //   {
  //     if (pieceTableMPiece.name.Contains(PrefabNames.Mechanism_Power_Source_Eitr))
  //     {
  //       var powerSourceComponent = pieceTableMPiece.GetComponent<PowerSourceComponentIntegration>();
  //       LoggerProvider.LogInfoDebounced("Found in prefab powerSourceComponent");
  //     }
  //     if (pieceTableMPiece.name.Contains(PrefabNames.Mechanism_Power_Storage_Eitr))
  //     {
  //       var powerStorageComponent = pieceTableMPiece.GetComponent<PowerStorageComponentIntegration>();
  //       LoggerProvider.LogInfoDebounced("Found in prefab powerStorageComponent");
  //     }
  //   }
  //   LoggerProvider.LogInfo($" - {pieceTable.name}, has {pieceTable.m_pieces.Count} pieces");
  // }
  // ValidateServerProblems();

  public void SimulateOnClientAndServer()
  {
    if (!isActiveAndEnabled || !ZNet.instance) return;
    if (Time.time < _nextUpdate) return;
    _nextUpdate = Time.time + _updateInterval;

    // if (ZNet.instance.IsServer())
    // {
    //   if (_networks.Count == 0 && (Consumers.Count > 0 || Conduits.Count > 0 || Storages.Count > 0 || Sources.Count > 0))
    //   {
    //     RequestRebuildNetwork();
    //   }
    // }
    LoggerProvider.LogInfoDebounced($"_networks, {_networks.Count}, Consumers, {Consumers.Count}, Conduits, {Conduits.Count}, Storages, {Storages.Count}, Sources, {Sources.Count}");

    foreach (var pair in _networks)
    {
      var nodes = pair.Value;

      LoggerProvider.LogInfoDebounced($"Pair Key: {pair.Key}, nodes: {nodes.Count}");

      if (nodes == null || nodes.Count == 0)
      {
        _networksToRemove.Add(pair.Key);
        continue;
      }

      nodes.RemoveAll(n => n == null);

      if (nodes.Count == 0)
      {
        _networksToRemove.Add(pair.Key);
        continue;
      }

      var currentZone = ZoneSystem.GetZone(nodes[0].Position);
      if (!ZoneSystem.instance.IsZoneLoaded(currentZone))
        continue;

      if (ZNet.instance.IsServer())
      {
        Host_SimulateNetwork(nodes, pair.Key);
      }

      if (!ZNet.instance.IsDedicated())
      {
        Client_SimulateNetwork(nodes, pair.Key);
      }

      if (ZNet.instance.IsServer())
      {
        SyncNetworkState(nodes);
      }
      else
      {
        SyncNetworkStateClient(nodes);
      }
    }

    foreach (var key in _networksToRemove)
    {
      _networks.Remove(key);
    }

    _networksToRemove.Clear();
  }


}