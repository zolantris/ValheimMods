using System.Collections;
using System.Reflection;

namespace ValheimVehicles.RPC;

public delegate IEnumerator RpcCoroutine(long sender, ZPackage pkg);

public class RPCEntity(
  string name,
  RpcCoroutine action)
{
  internal string Name { get; } = GetRPCName(name);
  internal RpcCoroutine Action { get; } = action;

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