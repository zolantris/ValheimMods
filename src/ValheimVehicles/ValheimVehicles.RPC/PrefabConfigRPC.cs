using System;
using System.Collections.Generic;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Constants;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.ValheimVehicles.RPC;

public static class PrefabConfigRPC
{
  public static Dictionary<ZDO, IPrefabSyncRPCSubscribeActions> ZdoToPrefabConfigListeners = new();
  public static bool hasRegistered = false;

  public static void Register()
  {
    if (hasRegistered) return;
    ZRoutedRpc.instance.Register<ZPackage>(ModRPCNames.SyncConfigKeys, RPC_SyncConfigKeys);
    hasRegistered = true;
  }

  public static void AddSubscription(ZDO zdo, IPrefabSyncRPCSubscribeActions config)
  {
    if (!ZdoToPrefabConfigListeners.ContainsKey(zdo))
    {
      ZdoToPrefabConfigListeners.Add(zdo, config);
    }
  }

  public static void RemoveSubscription(ZDO? zdo, IPrefabSyncRPCSubscribeActions config)
  {
    if (zdo != null && ZdoToPrefabConfigListeners.ContainsKey(zdo))
    {
      ZdoToPrefabConfigListeners.Remove(zdo);
    }
  }

  public static void Request_SyncConfigKeys(ZDO zdo, string[] zdoPropertyKeys, long? sender = null)
  {
    if (zdoPropertyKeys.Length == 0)
    {
      LoggerProvider.LogError($"No keys to sync. {zdoPropertyKeys}");
      return;
    }
    var zdoId = zdo.m_uid;

    var pkg = new ZPackage();

    pkg.Write(zdoId);
    pkg.Write(zdoPropertyKeys.Length);
    foreach (var key in zdoPropertyKeys)
      pkg.Write(key);

    if (!sender.HasValue)
    {
      RPCUtils.RunIfNearby(zdo, PowerSystemConfig.PowerSimulationDistanceThreshold.Value, peerId =>
      {
        ZRoutedRpc.instance.InvokeRoutedRPC(peerId, ModRPCNames.SyncConfigKeys, pkg);
      });
    }
    else
    {
      ZRoutedRpc.instance.InvokeRoutedRPC(sender.Value, ModRPCNames.SyncConfigKeys, pkg);
    }
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