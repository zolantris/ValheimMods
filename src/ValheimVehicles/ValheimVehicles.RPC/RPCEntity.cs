using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.RPC;

public delegate IEnumerator RpcCoroutine(long sender, ZPackage pkg);

public class RPCEntity(
  string name,
  RpcCoroutine action)
{
  internal string Name => GetRPCName(name);
  internal static string RPC_Nearby => $"{Assembly.GetExecutingAssembly().GetName().Name}_NearbyOnlyRPC";

  internal RpcCoroutine Action => action;

  public static string ExecutingAssemblyName = string.Empty;

  public static string GetRPCName(string rpcName)
  {
    if (ExecutingAssemblyName == string.Empty)
    {
      ExecutingAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
    }
    return $"{ExecutingAssemblyName}_{rpcName}";
  }

  public void Send(ZPackage pkg, bool canRunOnClient = true)
  {
    Send(ZRoutedRpc.Everybody, pkg, canRunOnClient);
  }

  // internal void RegisterNearbyOnlyRPC()
  // {
  //   RPCManager.RegisterRPC(nameof(RPC_SendNearby), RPC_SendNearby);
  // }

  private static void Request_SendNearby(ZDO zdo, ZPackage pkg)
  {
    var sendPackage = new ZPackage();
    sendPackage.Write(zdo.m_uid);
    sendPackage.Write(pkg);

    ZRoutedRpc.instance.InvokeRoutedRPC(RPC_Nearby, sendPackage);
  }

  // private IEnumerator RPC_SendNearby(long peer, ZPackage passthroughPkg)
  // {
  //   var zdoId = passthroughPkg.ReadZDOID();
  //   var zdo = ZDOMan.instance.GetZDO(zdoId);
  //   var pkg = passthroughPkg.ReadPackage();
  //
  //   SendNearby(pkg, zdo, 150f);
  // }

  /// <summary>
  /// Send to nearby peer only.
  /// </summary>
  public void SendNearby(ZPackage pkg, ZDO zdo, float distance, bool canRunOnClient = true)
  {
    if (!ZNet.instance.IsServer())
    {
      // todo this need a dedicated global coroutine that does a position diff then fires the coroutine from the string name DB we have locally.
      LoggerProvider.LogWarning("This call is not supported on non server clients due to it not being able to decide which peers to run the request on.");
      return;
    }

    var sentPeerIds = new HashSet<long>();
    RPCUtils.RunIfNearby(zdo, distance, (peerId) =>
    {
      sentPeerIds.Add(peerId);
      Send(peerId, pkg, canRunOnClient);
    });

    if (canRunOnClient && Player.m_localPlayer != null)
    {
      var owner = Player.m_localPlayer.GetOwner();
      if (!sentPeerIds.Contains(owner))
      {
        ZNet.instance.StartCoroutine(Action(Player.m_localPlayer.GetOwner(), new ZPackage(pkg.GetArray())));
      }
      else
      {
        LoggerProvider.LogDev("player peer was already called for local player.");
      }
    }
  }

  /// <summary>
  /// This must run the RPC method directly if not a server or using the target server ZRoutedRPC.instance.m_id.
  ///
  /// Valheim Bails here.
  /// if (!this.m_server || routedRpcData.m_targetPeerID == this.m_id)
  /// return;
  /// </summary>
  public void Send(long peerId, ZPackage pkg, bool canRunOnClient = true)
  {
    pkg.SetPos(0);

    var serverPeerId = ZRoutedRpc.instance.GetServerPeerID();
    var isLocalServer = ZRoutedRpc.instance.m_server && ZRoutedRpc.instance.m_id == serverPeerId;
    var isServerPeerRequest = peerId == serverPeerId;
    var isSelfCall = peerId != 0L && isServerPeerRequest && isLocalServer;
    var isEveryone = peerId == 0L;
    // invoke this is provided everyone peer or the current peer is the host.
    // must duplicate the pkg otherwise it will conflict or mutate by different peers.
    if (canRunOnClient && (isSelfCall || isEveryone))
    {
      ZNet.instance.StartCoroutine(Action(peerId, new ZPackage(pkg.GetArray())));
    }

    // do not invoke to self if sending to self peer this is thrown out in ZRoutedRPC anyways.
    if (!isSelfCall)
    {
      ZRoutedRpc.instance.InvokeRoutedRPC(peerId, Name, pkg);
    }
  }
}