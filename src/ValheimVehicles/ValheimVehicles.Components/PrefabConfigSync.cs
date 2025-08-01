using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.RPC;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Components;

public class PrefabConfigSync<T, TComponentInterface> : MonoBehaviour, IPrefabCustomConfigRPCSync<T> where T : ISerializableConfig<T, TComponentInterface>, new()
{
  public ZNetView? m_nview { get; set; }

  public bool HasInitLoaded;
  public bool _suppressMotionStateBroadcast = false;
  public bool hasRegisteredRPCListeners { get; set; }
  public bool IsBroadcastSuppressed => _suppressMotionStateBroadcast;

  private T CustomConfig { get; set; } = new();
  public T Config => CustomConfig;
  internal SafeRPCHandler? rpcHandler;
  internal RetryGuard? retryGuard = null!;
  public TComponentInterface? controller;

  private Coroutine? _prefabSyncRoutine;
  public Stopwatch timer = new();

  public static Dictionary<ZDO, ISerializableConfig<T, TComponentInterface>> s_zdoToConfig = new();

  public virtual void Awake()
  {
    if (ZNetView.m_forceDisableInit) return;
    retryGuard = new RetryGuard(this);
    controller = GetComponent<TComponentInterface>();
    if (controller is INetView nvController)
    {
      m_nview = nvController.m_nview;
    }
    else
    {
      m_nview = GetComponent<ZNetView>();
    }
  }

  public virtual void OnEnable()
  {
    if (ZNetView.m_forceDisableInit) return;
    this.WaitForZNetView((nv) =>
    {
      InitRPCHandler();
      RegisterRPCListeners();
      PrefabConfigRPC.AddSubscription(nv.m_zdo, this);
      Load();
    }, 10f, true);
  }

  public virtual void OnDisable()
  {
    if (ZNetView.m_forceDisableInit) return;
    UnregisterRPCListeners();

    if (m_nview != null && m_nview.m_zdo != null)
    {
      PrefabConfigRPC.RemoveSubscription(m_nview.m_zdo, this);
    }

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
    if (!netView.IsOwner())
    {
      netView.ClaimOwnership();
    }

    LoggerProvider.LogDebug($"Received config for {typeof(T).Name}");

    CustomConfig = newConfig;
    CustomConfig.Save(netView.GetZDO(), CustomConfig);

    // load for self
    Load();

    // load for everyone else
    Request_Load();
  }

  public void Request_Load()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    var pkg = new ZPackage();
    CustomConfig.Serialize(pkg);
    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_Load), pkg);
  }

  /// <summary>
  /// Tells all clients they need to update their local config as a value has been updated.
  /// </summary>
  public void RPC_Load(long sender, ZPackage pkg)
  {
    if (pkg.Size() == 0)
    {
      Load();
      return;
    }

    pkg.SetPos(0);

    try
    {
      var config = Config.Deserialize(pkg);
      CustomConfig = config;
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error with deserialization {e}");
      Load();
    }
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
  public void Load(string[]? filterKeys = null, bool shouldSkipEvent = false)
  {
    if (controller == null || !this.IsNetViewValid(out var netView))
    {
      _prefabSyncRoutine ??= StartCoroutine(SyncPrefabConfigRoutine());
      return;
    }
    CustomConfig = CustomConfig.Load(netView.GetZDO(), controller, filterKeys);

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

    if (!shouldSkipEvent)
    {
      OnLoad();
    }
  }
  public void Load(ZDO zdo, string[]? filterKeys = null)
  {
    if (controller == null) return;
    CustomConfig = Config.Load(zdo, controller, filterKeys);
  }

  public void Save(ZDO zdo, string[]? filterKeys = null)
  {
    CustomConfig.Save(zdo, Config, filterKeys);
  }

  /// <summary>
  /// Todo consider calling request load after. This might need to be guarded by a update check to avoid infinite loops.
  /// </summary>
  /// <param name="filterKeys"></param>
  public void Save(string[]? filterKeys = null)
  {
    if (!this.IsNetViewValid(out var nv)) return;
    Save(nv.GetZDO(), filterKeys);
  }

  public virtual void OnLoad() {}

  public void Request_CommitConfigChange()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (netView.IsOwner())
    {
      CommitConfigChange(Config);
    }
    else
    {
      var pkg = new ZPackage();
      Config.Serialize(pkg);
      netView.InvokeRPC(netView.GetZDO().GetOwner(), nameof(RPC_CommitConfigChange), pkg);
    }
  }

  public void Request_CommitConfigChange(T newConfig)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (netView.IsOwner() || !netView.HasOwner())
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
    if (!netView.IsOwner()) netView.ClaimOwnership();

    var newConfig = new T().Deserialize(pkg);
    CommitConfigChange(newConfig);
  }

  public virtual void RegisterRPCListeners()
  {
    rpcHandler?.Register<ZPackage>(nameof(RPC_Load), RPC_Load);
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