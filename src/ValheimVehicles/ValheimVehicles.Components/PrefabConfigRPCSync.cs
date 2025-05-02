using System;
using System.Diagnostics.CodeAnalysis;
using Jotunn;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Components;

[RequireComponent(typeof(ZNetView))]
public class PrefabConfigRPCSync<T> : MonoBehaviour, IPrefabCustomConfigRPCSync<T> where T : ISerializableConfig<T>, new()
{
  public ZNetView m_nview
  {
    get;
    set;
  } = null!;

  private T? m_configCache = default;

  public bool hasRegisteredRPCListeners
  {
    get;
    set;
  }

  public T CustomConfig
  {
    get;
    set;
  } = new();

  public virtual void Awake()
  {
    m_nview = GetComponent<ZNetView>();
  }

  public bool IsValid()
  {
    if (!isActiveAndEnabled || m_nview == null || m_nview.GetZDO() == null) return false;
    return true;
  }

  /// <summary>
  /// Guards on ZNetView and ZDO
  /// </summary>
  /// <param name="netView"></param>
  /// <returns></returns>
  public bool IsValid([NotNullWhen(true)] out ZNetView? netView)
  {
    netView = null;
    if (IsValid()) return false;
    netView = m_nview;
    return true;
  }

  /// <summary>
  /// The main function to sync config. Only applies for the NetView owner.
  /// </summary>
  /// <param name="sender"></param>
  /// <param name="package"></param>
  public void RPC_SetPrefabConfig(long sender, ZPackage package)
  {
    if (!ZNetViewExtensions.IsValid(m_nview, out var netView)) return;
    var localVehicleConfig = CustomConfig.Deserialize(package);
    LoggerProvider.LogDebug($"Received config: {CustomConfig}");
    CustomConfig = localVehicleConfig;

    if (!netView.HasOwner() || netView.IsOwner())
    {
      CustomConfig.Save(netView.GetZDO(), CustomConfig);
    }

    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SyncPrefabConfig), package);
  }

  public void RPC_SyncPrefabConfig(long sender)
  {
    SyncPrefabConfig();
  }

  public void SyncPrefabConfig(bool forceUpdate = false)
  {
    if (m_configCache != null && !forceUpdate)
    {
      return;
    }
    if (!ZNetViewExtensions.IsValid(m_nview, out var netView)) return;
    CustomConfig = CustomConfig.Load(netView.GetZDO());
  }
  public void SendPrefabConfig()
  {
    if (!ZNetViewExtensions.IsValid(m_nview, out var netView)) return;
    var package = new ZPackage();
    CustomConfig.Serialize(package);
    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SetPrefabConfig), package);
  }

  public virtual void UnregisterRPCListeners()
  {
    if (!ZNetViewExtensions.IsValid(m_nview, out var netView)) return;
    netView.Unregister(nameof(RPC_SetPrefabConfig));
    netView.Unregister(nameof(RPC_SyncPrefabConfig));
  }

  public virtual void RegisterRPCListeners()
  {
    if (!ZNetViewExtensions.IsValid(m_nview, out var netView)) return;
    netView.Register<ZPackage>(nameof(RPC_SetPrefabConfig), RPC_SetPrefabConfig);
    netView.Register(nameof(RPC_SyncPrefabConfig), RPC_SyncPrefabConfig);
  }
}