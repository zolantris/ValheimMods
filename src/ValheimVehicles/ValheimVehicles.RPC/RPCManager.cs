using System.Collections.Generic;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.RPC;

public static class RPCManager
{
  public static readonly Dictionary<string, RPCEntity> rpcEntities = new();

  public static void RegisterAllRPCs()
  {
    if (ZRoutedRpc.instance == null)
    {
      LoggerProvider.LogError("Register failed to run due to ZRoutedRpc.instance being null.");
      return;
    }
    foreach (var rpcEntity in rpcEntities.Values)
    {
      ZRoutedRpc.instance.Register<ZPackage>(rpcEntity.Name, (long sender, ZPackage pkg) =>
      {
        if (!ZNet.instance) return;
        // prevents silly mistakes. (will always start at 0)
        pkg.SetPos(0);
        ZNet.instance.StartCoroutine(rpcEntity.Action(sender, pkg));
      });
      LoggerProvider.LogDebug($"Registered RPC {rpcEntity.Name}");
    }
  }

  public static RPCEntity RegisterRPC(string rpcName, RpcCoroutine action)
  {
    if (!rpcEntities.TryGetValue(rpcName, out var rpc))
    {
      var rpcEntity = new RPCEntity(rpcName, action);
      rpcEntities.Add(rpcEntity.Name, rpcEntity);
      return rpcEntity;
    }
    return rpc;
  }
}