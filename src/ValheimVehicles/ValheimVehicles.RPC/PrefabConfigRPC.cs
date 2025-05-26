using System;
using System.Collections.Generic;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Constants;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.ValheimVehicles.RPC;

public static class PrefabConfigRPC
{
  public static Dictionary<ZDO, IPrefabSyncRPCSubscribers> ZdoToPrefabConfigListeners = new();

  public static void Register()
  {
    ZRoutedRpc.instance.Register<ZPackage>(ModRPCNames.SyncConfigKeys, RPC_SyncConfigKeys);
  }

  public static void AddSubscription(ZDO zdo, IPrefabSyncRPCSubscribers config)
  {
    if (!ZdoToPrefabConfigListeners.ContainsKey(zdo))
    {
      ZdoToPrefabConfigListeners.Add(zdo, config);
    }
  }

  public static void RemoveSubscription(ZDO? zdo, IPrefabSyncRPCSubscribers config)
  {
    if (zdo != null && ZdoToPrefabConfigListeners.ContainsKey(zdo))
    {
      ZdoToPrefabConfigListeners.Remove(zdo);
    }
  }

  public static void Request_SyncConfigKeys(ZDO zdo, List<string> zdoPropertyKeys)
  {
    if (zdoPropertyKeys.Count == 0)
    {
      LoggerProvider.LogError($"No keys to sync. {zdoPropertyKeys}");
      return;
    }

    RPCUtils.RunIfNearby(zdo, PowerSystemConfig.PowerSimulationDistanceThreshold.Value, peerId =>
    {
      ZRoutedRpc.instance.InvokeRoutedRPC(peerId, ModRPCNames.SyncConfigKeys, zdoPropertyKeys);
    });
  }

  public static void RPC_SyncConfigKeys(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);
    var zdoId = pkg.ReadZDOID();
    var zdo = ZDOMan.instance.GetZDO(zdoId);

    if (zdo == null)
    {
      LoggerProvider.LogError($"Could not find ZDO related to \n {zdoId}");
      return;
    }
    // read the rest of the data if valid
    var length = pkg.ReadInt();
    var zdoPropertyKeys = new string[length];

    try
    {
      for (var i = 0; i < length; i++)
        zdoPropertyKeys[i] = pkg.ReadString();
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error while reading pkg strings \n {e}");
    }

    ConfigLoadByKeys(zdo, zdoPropertyKeys);
  }

  public static void ConfigLoadByKeys(ZDO zdo, string[] zdoPropertyKeys)
  {
    if (!ZdoToPrefabConfigListeners.TryGetValue(zdo, out var config))
    {
      LoggerProvider.LogError($"Could not find config for ZDO \n {zdo}. With keys {zdoPropertyKeys}");
      return;
    }
    config.Load(zdo, zdoPropertyKeys);
  }
}