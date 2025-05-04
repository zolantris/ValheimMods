using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Components;

[RequireComponent(typeof(ZNetView))]
public class PrefabConfigRPCSync<T> : MonoBehaviour, IPrefabCustomConfigRPCSync<T> where T : ISerializableConfig<T>, new()
{
  public ZNetView? m_nview { get; set; }
  private T? m_configCache = default;

  public bool hasRegisteredRPCListeners { get; set; }

  public T CustomConfig { get; set; } = new();
  internal SafeRPCHandler rpcHandler;

  public virtual void Awake()
  {
    m_nview = GetComponent<ZNetView>();
  }

  public void RPC_SetPrefabConfig(long sender, ZPackage package)
  {
    if (!NetworkValidation.IsNetViewValid(m_nview, out var netView)) return;

    try
    {
      var localConfig = CustomConfig.Deserialize(package);
      CustomConfig = localConfig;
      LoggerProvider.LogDebug($"Received config for {typeof(T).Name}");

      if (!netView.HasOwner() || netView.IsOwner())
      {
        CustomConfig.Save(netView.GetZDO(), CustomConfig);
      }

      netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SyncPrefabConfig), package);
    }
    catch (Exception ex)
    {
      LoggerProvider.LogError($"Failed to deserialize {typeof(T).Name} config: {ex}");
    }
  }

  public void RPC_SyncPrefabConfig(long sender)
  {
    SyncPrefabConfig();
  }

  public void SyncPrefabConfig(bool forceUpdate = false)
  {
    if (m_configCache != null && !forceUpdate) return;
    if (!NetworkValidation.IsNetViewValid(m_nview, out var netView)) return;

    CustomConfig = CustomConfig.Load(netView.GetZDO());
  }

  public void SendPrefabConfig()
  {
    if (!NetworkValidation.IsNetViewValid(m_nview, out var netView)) return;

    var package = new ZPackage();
    CustomConfig.Serialize(package);
    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SetPrefabConfig), package);
  }

  public virtual void RegisterRPCListeners()
  {
    if (!NetworkValidation.IsNetViewValid(m_nview, out var netView)) return;

    _rpcHandler = new SafeRPCHandler(netView);
    _rpcHandler.Register<ZPackage>(nameof(RPC_SetPrefabConfig), RPC_SetPrefabConfig);
    _rpcHandler.Register(nameof(RPC_SyncPrefabConfig), RPC_SyncPrefabConfig);
    _rpcHandler.Register(nameof(RPC_SyncBounds), RPC_SyncBounds);

    netView.Register<ZPackage>(nameof(RPC_SetPrefabConfig), RPC_SetPrefabConfig);
    netView.Register(nameof(RPC_SyncPrefabConfig), RPC_SyncPrefabConfig);
    hasRegisteredRPCListeners = true;
  }

  public virtual void UnregisterRPCListeners()
  {
    if (!hasRegisteredRPCListeners) return;
    if (!IsValid(out var netView))
    {
      hasRegisteredRPCListeners = false;
      return;
    }

    netView.Unregister(nameof(RPC_SetPrefabConfig));
    netView.Unregister(nameof(RPC_SyncPrefabConfig));
    hasRegisteredRPCListeners = false;
  }
}