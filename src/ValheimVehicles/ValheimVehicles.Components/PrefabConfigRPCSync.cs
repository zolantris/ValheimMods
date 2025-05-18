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

  public bool HasInitLoaded;
  public bool _suppressMotionStateBroadcast = false;
  public bool hasRegisteredRPCListeners { get; set; }
  public bool IsBroadcastSuppressed => _suppressMotionStateBroadcast;

  private T CustomConfig { get; set; } = new();
  public T Config => CustomConfig;
  internal SafeRPCHandler? rpcHandler;
  internal RetryGuard retryGuard = null!;
  public TComponentInterface? controller;

  public bool HasLoadedInitialCache => m_configCache != null;
  private Coroutine? _prefabSyncRoutine;
  public Stopwatch timer = new();

  public virtual void Awake()
  {
    if (ZNetView.m_forceDisableInit) return;
    retryGuard = new RetryGuard(this);
    m_nview = GetComponent<ZNetView>();
    controller = GetComponent<TComponentInterface>();
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
    HasInitLoaded = false;
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

  /// Only allowed to be called by the owner/server
  public void CommitConfigChange(T newConfig)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (!netView.IsOwner() && !ZNet.instance.IsServer()) return;
    LoggerProvider.LogDebug($"Received config for {typeof(T).Name}");

    CustomConfig = newConfig;
    CustomConfig.Save(netView.GetZDO(), CustomConfig);
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
    SuppressConfigSync(() =>
    {
      CustomConfig.ApplyTo(controller);
    });

    LoggerProvider.LogDebug($"Loaded config for {typeof(T).Name}");

    if (_prefabSyncRoutine != null)
    {
      StopCoroutine(_prefabSyncRoutine);
      _prefabSyncRoutine = null;
    }

    if (!HasInitLoaded)
    {
      HasInitLoaded = true;
    }

    OnLoad();
  }

  public virtual void OnLoad() {}

  public void RequestCommitConfigChange(T newConfig)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (netView.IsOwner())
    {
      CommitConfigChange(newConfig);
    }
    else
    {
      var pkg = new ZPackage();
      newConfig.Serialize(pkg);
      netView.InvokeRPC(netView.GetZDO().GetOwner(), nameof(RPC_CommitConfigChange), pkg);
    }
  }

  private void RPC_CommitConfigChange(long sender, ZPackage pkg)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (!netView.IsOwner()) return;
    var newConfig = new T();
    newConfig.Deserialize(pkg);
    CommitConfigChange(newConfig);
  }

  public virtual void RegisterRPCListeners()
  {
    rpcHandler?.Register(nameof(RPC_Load), RPC_Load);
    rpcHandler?.Register<ZPackage>(nameof(RPC_CommitConfigChange), RPC_CommitConfigChange);
  }

  public virtual void UnregisterRPCListeners()
  {
    rpcHandler?.UnregisterAll();
  }

  public void SuppressConfigSync(Action apply)
  {
    _suppressMotionStateBroadcast = true;
    try { apply(); }
    finally { _suppressMotionStateBroadcast = false; }
  }
}