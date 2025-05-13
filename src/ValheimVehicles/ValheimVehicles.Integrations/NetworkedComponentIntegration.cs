// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.Interfaces;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

namespace ValheimVehicles.Integrations
{
  public abstract class NetworkedComponentIntegration<TComponent, TDelegateComponent, TConfig> : MonoBehaviour, INetView, INetworkedComponent
    where TComponent : MonoBehaviour
    where TConfig : INetworkedZDOConfig<TComponent>, new()
    where TDelegateComponent : Component
  {
    private TDelegateComponent _logic;
    public TDelegateComponent Logic => _logic;
    public ZNetView? m_nview { get; set; }
    public bool hasLoadedInitialData { get; private set; }
    protected SafeRPCHandler RpcHandler { get; private set; }

    protected TComponent Component => (TComponent)(object)this!;
    protected TConfig Config = new();

    protected virtual void Awake()
    {
      m_nview = GetComponent<ZNetView>();
      _logic = gameObject.AddComponent<TDelegateComponent>();
      RpcHandler = new SafeRPCHandler(m_nview);
      RegisterDefaultRPCs();
    }

    protected virtual void Start()
    {
      LoadInitialData();
    }

    protected abstract void RegisterDefaultRPCs();

    protected void LoadInitialData()
    {
      if (hasLoadedInitialData || !this.IsNetViewValid(out var netView)) return;
      Config.Load(netView.GetZDO(), Component);
      hasLoadedInitialData = true;
    }

    public virtual void UpdateNetworkedData()
    {
      if (this.IsNetViewValid(out var netView) && netView.IsOwner())
      {
        Config.Save(netView.GetZDO(), Component);
        netView.InvokeRPC(ZNetView.Everybody, nameof(RPC_NotifyStateUpdated));
      }
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
        Config.Load(netView.GetZDO(), Component);
      }
    }

    public virtual void SyncNetworkedData()
    {
      if (this.IsNetViewValid(out var netView))
      {
        Config.Load(netView.GetZDO(), Component);
      }
    }

    protected void RunIfOwnerOrServer(Action action)
    {
      if (!this.IsNetViewValid(out var netView)) return;

      if (netView.IsOwner() || ZNet.instance.IsServer())
      {
        if (!netView.IsOwner())
          netView.ClaimOwnership();

        action();
      }
    }
  }
}