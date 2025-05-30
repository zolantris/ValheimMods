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

  public void Send(ZPackage pkg)
  {
    pkg.SetPos(0);

    if (ZNet.IsSinglePlayer || ZNet.instance.IsServer())
    {
      ZNet.instance.StartCoroutine(Action(ZRoutedRpc.instance.GetServerPeerID(), pkg));
      return;
    }
    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), Name, pkg);
  }

  public void Send(long peerId, ZPackage pkg)
  {
    pkg.SetPos(0);
    if (peerId == ZRoutedRpc.instance.GetServerPeerID() && (ZNet.IsSinglePlayer || ZNet.instance.IsServer()))
    {
      ZNet.instance.StartCoroutine(Action(peerId, pkg));
      return;
    }
    ZRoutedRpc.instance.InvokeRoutedRPC(peerId, Name, pkg);
  }
}