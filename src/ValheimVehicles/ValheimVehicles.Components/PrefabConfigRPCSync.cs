using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using ValheimVehicles.Controllers;
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
  internal SafeRPCHandler? rpcHandler;
  internal RetryGuard retryGuard = null!;

  public virtual void Awake()
  {
    retryGuard = new RetryGuard(this);
    m_nview = GetComponent<ZNetView>();
    InitRPCHandler();
  }

  public virtual void OnEnable()
  {
    RegisterRPCListeners();
  }


  public virtual void OnDisable()
  {
    UnregisterRPCListeners();

    // cancel all Invoke calls from retryGuard.
    CancelInvoke();
  }

  public void InitRPCHandler()
  {
    if (rpcHandler != null) return;
    if (m_nview == null)
    {
      LoggerProvider.LogError("InitRPCHandler failed to invoke");
      return;
    }
    rpcHandler = new SafeRPCHandler(m_nview);
  }

  public void RPC_SetPrefabConfig(long sender, ZPackage package)
  {
    if (!this.IsNetViewValid(out var netView)) return;

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

  /// <summary>
  /// Tells all clients they need to update their local config as a value has been updated.
  /// </summary>
  /// <param name="sender"></param>
  public void RPC_SyncPrefabConfig(long sender)
  {
    SyncPrefabConfig();
  }

  /// <summary>
  /// Syncs RPC data from ZDO to local values.
  /// </summary>
  /// <param name="forceUpdate"></param>
  public void SyncPrefabConfig(bool forceUpdate = false)
  {
    if (m_configCache != null && !forceUpdate) return;
    if (!this.IsNetViewValid(out var netView)) return;
    CustomConfig = CustomConfig.Load(netView.GetZDO());
  }

  /// <summary>
  /// Sends an RPC from client.
  /// </summary>
  public void SendPrefabConfig()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    var package = new ZPackage();
    CustomConfig.Serialize(package);
    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SetPrefabConfig), package);
  }

  public virtual void RegisterRPCListeners()
  {
    rpcHandler?.Register<ZPackage>(nameof(RPC_SetPrefabConfig), RPC_SetPrefabConfig);
  }

  public virtual void UnregisterRPCListeners()
  {
    rpcHandler?.UnregisterAll();
  }
}