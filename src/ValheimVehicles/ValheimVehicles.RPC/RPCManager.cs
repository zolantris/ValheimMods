using System.Collections.Generic;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;

namespace ValheimVehicles.RPC;

public static class RPCManager
{
  public static readonly Dictionary<string, RPCEntity> rpcEntities = new();
  public static bool HasRegistered = false;

  public static void RegisterAllRPCs()
  {
    if (ZRoutedRpc.instance == null)
    {
      LoggerProvider.LogError("RegisterAllRPCs failed to run due to ZRoutedRpc.instance being null.");
      PowerNetworkControllerIntegration.Instance?.Invoke(nameof(RegisterAllRPCs), 1f);
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

    LoggerProvider.LogDebug($"Registered {rpcEntities.Count} RPCs");
    HasRegistered = true;
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