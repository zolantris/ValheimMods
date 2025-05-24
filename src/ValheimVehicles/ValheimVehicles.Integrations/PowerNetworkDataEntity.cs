// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

namespace ValheimVehicles.Integrations
{
  public abstract class PowerNetworkDataEntity<TComponent, TDelegateComponent, TData> : MonoBehaviour, INetView, INetworkedComponent
    where TComponent : MonoBehaviour
    where TData : PowerSystemComputeData, new()
    where TDelegateComponent : Component
  {
    private TDelegateComponent _logic;

    public TDelegateComponent Logic
    {
      get
      {
        EnsureInitialized(); // this check is O(1)
        return _logic;
      }
    }

    public ZNetView? m_nview { get; set; }
    public bool hasLoadedInitialData { get; private set; }
    protected SafeRPCHandler RpcHandler { get; private set; }

    protected TComponent Component => (TComponent)(object)this!;
    public TData Data = new();

    public string instanced_RpcNotifyStateUpdate = null!;
    private bool _isInitialized;
    private Coroutine? registerCoroutine;

    protected virtual void Awake()
    {
      EnsureInitialized();
    }

    public virtual void EnsureInitialized()
    {
      if (_isInitialized) return;
      _isInitialized = true;
      m_nview = GetComponent<ZNetView>();

      if (!_logic)
      {
        _logic = gameObject.AddComponent<TDelegateComponent>();
      }

      instanced_RpcNotifyStateUpdate = $"{GetType().Name}_{nameof(RPC_NotifyStateUpdated)}";
      RpcHandler = new SafeRPCHandler(m_nview);
    }

    protected virtual void Start()
    {
      registerCoroutine = this.WaitForPowerSystemNodeData<TData>((data) =>
      {
        LoadInitialData();
        RpcHandler.Register(instanced_RpcNotifyStateUpdate, RPC_NotifyStateUpdated);
        RegisterDefaultRPCs();
        Data = data;
        Data.Load();
        registerCoroutine = null;
      });
    }

    protected virtual void OnDestroy()
    {
      RpcHandler.UnregisterAll();
      if (registerCoroutine != null)
      {
        StopCoroutine(registerCoroutine);
        registerCoroutine = null;
      }
    }

    protected abstract void RegisterDefaultRPCs();

    protected void LoadInitialData()
    {
      if (hasLoadedInitialData || !this.IsNetViewValid(out var netView)) return;
      Data.Load();
      hasLoadedInitialData = true;
    }

    public virtual void UpdateNetworkedData()
    {
      this.RunIfOwnerOrServerOrNoOwner((nv) =>
      {
        Data.Save();
        nv.InvokeRPC(ZNetView.Everybody, instanced_RpcNotifyStateUpdate);
      });
    }

    protected void RegisterRPC<T>(string name, Action<long, T> method)
    {
      RpcHandler.Register(name, method);
    }

    protected void RegisterRPC(string name, Action<long> method)
    {
      RpcHandler.Register(name, method);
    }

    protected void InvokeRPC(string name, params object[] args)
    {
      RpcHandler.InvokeRPC(name, args);
    }

    public void RPC_NotifyStateUpdated(long sender)
    {
      if (this.IsNetViewValid(out var netView))
      {
        Data.Load();
      }
    }

    public virtual void SyncNetworkedData()
    {
      if (this.IsNetViewValid(out var netView))
      {
        Data.Load();
      }
    }
  }
}