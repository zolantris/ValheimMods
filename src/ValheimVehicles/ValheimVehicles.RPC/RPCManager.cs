using System.Collections.Generic;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;

namespace ValheimVehicles.RPC;

public static class RPCManager
{
  public static readonly Dictionary<string, RPCEntity> rpcEntities = new();
  public static bool HasRegistered = false;
  private static ZRoutedRpc _lastRoutedRpcInstance;
  public static readonly HashSet<string> registeredRpcNames = new();
  public static readonly Dictionary<int, string> RPCHashIdsToHashNames = new();

  public static void RegisterAllRPCs()
  {
    var currentInstance = ZRoutedRpc.instance;
    if (currentInstance == null)
    {
      LoggerProvider.LogError("RegisterAllRPCs failed to run due to ZRoutedRpc.instance being null.");
      PowerNetworkControllerIntegration.Instance?.Invoke(nameof(RegisterAllRPCs), 1f);
      return;
    }

    // If ZRoutedRpc.instance changed, clear registrations for safety.
    if (_lastRoutedRpcInstance != null && _lastRoutedRpcInstance != currentInstance)
    {
      registeredRpcNames.Clear();
      LoggerProvider.LogWarning("ZRoutedRpc.instance changed! Cleared previous registered RPCs.");
    }
    _lastRoutedRpcInstance = currentInstance;

    foreach (var rpcEntity in rpcEntities.Values)
    {
      if (registeredRpcNames.Contains(rpcEntity.Name)) continue;
      ZRoutedRpc.instance.Register<ZPackage>(rpcEntity.Name, (long sender, ZPackage pkg) =>
      {
        if (!ZNet.instance) return;
        pkg.SetPos(0);
        ZNet.instance.StartCoroutine(rpcEntity.Action(sender, pkg));
      });
      registeredRpcNames.Add(rpcEntity.Name);
      LoggerProvider.LogDebug($"Registered RPC {rpcEntity.Name}");
    }

    LoggerProvider.LogDebug($"Registered {rpcEntities.Count} RPCs");
    HasRegistered = true;
  }

  public static RPCEntity RegisterRPC(string rpcName, RpcCoroutine action)
  {
    if (!rpcEntities.TryGetValue(RPCEntity.GetRPCName(rpcName), out var rpc))
    {
      var rpcEntity = new RPCEntity(rpcName, action);
      rpcEntities.Add(rpcEntity.Name, rpcEntity);
      RPCHashIdsToHashNames.Add(rpcEntity.Name.GetStableHashCode(), rpcName);
      return rpcEntity;
    }
    return rpc;
  }
}