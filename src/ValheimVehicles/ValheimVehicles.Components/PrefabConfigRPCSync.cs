using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Components;

[RequireComponent(typeof(ZNetView))]
public class PrefabConfigRPCSync<T, TComponentInterface> : MonoBehaviour, IPrefabCustomConfigRPCSync<T> where T : ISerializableConfig<T, TComponentInterface>, new()
{
  public ZNetView? m_nview { get; set; }
  private T? m_configCache = default;

  public bool hasRegisteredRPCListeners { get; set; }

  public T CustomConfig { get; set; } = new();
  internal SafeRPCHandler? rpcHandler;
  internal RetryGuard retryGuard = null!;
  public TComponentInterface controller;

  public bool HasLoadedInitialCache => m_configCache != null;
  private Coroutine? _prefabSyncRoutine;
  public Stopwatch timer = new();

  public virtual void Awake()
  {
    if (ZNetView.m_forceDisableInit) return;
    retryGuard = new RetryGuard(this);
    m_nview = GetComponent<ZNetView>();
    InitRPCHandler();
  }

  public virtual void OnEnable()
  {
    if (ZNetView.m_forceDisableInit) return;
    RegisterRPCListeners();
    Load();
  }

  public virtual void OnDisable()
  {
    if (ZNetView.m_forceDisableInit) return;
    UnregisterRPCListeners();

    // cancel all Invoke calls from retryGuard.
    CancelInvoke();
  }

  public void SetComponentFromInstance(TComponentInterface instanceComponent)
  {
    if (controller != null) return;
    controller = instanceComponent;
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

  public void RPC_Save(long sender, ZPackage package)
  {
    try
    {
      var localConfig = CustomConfig.Deserialize(package);
      CustomConfig = localConfig;
      Save();
    }
    catch (Exception ex)
    {
      LoggerProvider.LogError($"Failed to deserialize {typeof(T).Name} config: {ex}");
    }
  }

  /// <summary>
  /// Sends an RPC from client.
  /// </summary>
  public void Request_Save()
  {
    if (!this.IsNetViewValid(out var netView)) return;

    // Owners do not have to RPC to self. They can save the data then fire RPC_Load instead.
    if (netView.IsOwner())
    {
      Save();
    }
    else
    {
      var package = new ZPackage();
      CustomConfig.Serialize(package);
      netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_Save), package);
    }
  }

  /// <summary>
  /// Must always trigger Load RPC otherwise Save client is accurate but other clients de-sync
  /// </summary>
  public void Save()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    LoggerProvider.LogDebug($"Received config for {typeof(T).Name}");

    if (!netView.HasOwner() || netView.IsOwner())
    {
      CustomConfig.Save(netView.GetZDO(), CustomConfig);
    }

    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_Load));
  }

  public void Request_Load()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_Load));
  }

  /// <summary>
  /// Tells all clients they need to update their local config as a value has been updated.
  /// </summary>
  /// <param name="sender"></param>
  public void RPC_Load(long sender)
  {
    Load();
  }

  /// <summary>
  /// Useful for lazy initialization without adding a FixedUpdate overhead.
  /// </summary>
  /// <returns></returns>
  private IEnumerator SyncPrefabConfigRoutine()
  {
    timer.Restart();
    while (timer.ElapsedMilliseconds < 10000 && controller == null && !this.IsNetViewValid(out var netView))
    {
      yield return new WaitForFixedUpdate();
    }
    if (timer.ElapsedMilliseconds > 10000)
    {
      timer.Reset();
      yield break;
    }

    yield return new WaitForFixedUpdate();
    timer.Reset();
    _prefabSyncRoutine = null;
    Load();
  }
  /// <summary>
  /// Syncs RPC data from ZDO to local values. If it's not ready it queues up the sync.
  /// </summary>
  /// <param name="forceUpdate"></param>
  public void Load(bool forceUpdate = false)
  {
    if (HasLoadedInitialCache && !forceUpdate) return;
    if (controller == null || !this.IsNetViewValid(out var netView))
    {
      _prefabSyncRoutine ??= StartCoroutine(SyncPrefabConfigRoutine());
      return;
    }
    CustomConfig = CustomConfig.Load(netView.GetZDO(), controller);

    // very important. This sets the values from Config to the actual component.
    CustomConfig.ApplyTo(controller);

    if (_prefabSyncRoutine != null)
    {
      StopCoroutine(_prefabSyncRoutine);
      _prefabSyncRoutine = null;
    }
  }

  public virtual void RegisterRPCListeners()
  {
    rpcHandler?.Register<ZPackage>(nameof(RPC_Save), RPC_Save);
    rpcHandler?.Register(nameof(RPC_Load), RPC_Load);
  }

  public virtual void UnregisterRPCListeners()
  {
    rpcHandler?.UnregisterAll();
  }
}