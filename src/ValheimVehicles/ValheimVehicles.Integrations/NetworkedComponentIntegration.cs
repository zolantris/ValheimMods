// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.ZDOConfigs;

namespace ValheimVehicles.Integrations
{
  public abstract class NetworkedComponentIntegration<TComponent, TConfig> : MonoBehaviour, INetView
    where TComponent : MonoBehaviour
    where TConfig : INetworkedZDOConfig<TComponent>, new()
  {
    public ZNetView? m_nview { get; set; }
    public bool hasLoadedInitialData { get; private set; }

    protected TComponent Component => (TComponent)(object)this!;
    protected TConfig Config = new();

    protected virtual void Awake()
    {
      m_nview = GetComponent<ZNetView>();
    }

    protected virtual void Start()
    {
      LoadInitialData();
    }

    protected void LoadInitialData()
    {
      if (hasLoadedInitialData || m_nview?.IsValid() != true) return;
      Config.Load(m_nview.GetZDO(), Component);
      hasLoadedInitialData = true;
    }

    public virtual void UpdateNetworkedData()
    {
      if (m_nview?.IsValid() == true && m_nview.IsOwner())
      {
        Config.Save(m_nview.GetZDO(), Component);
        m_nview.InvokeRPC(ZNetView.Everybody, nameof(RPC_NotifyStateUpdated));
      }
    }

    public void RPC_NotifyStateUpdated(long sender)
    {
      if (m_nview?.IsValid() == true)
      {
        Config.Load(m_nview.GetZDO(), Component);
      }
    }

    public virtual void SyncNetworkedData()
    {
      if (m_nview?.IsValid() == true)
      {
        Config.Load(m_nview.GetZDO(), Component);
      }
    }

    protected void RunIfOwnerOrServer(Action action)
    {
      if (m_nview?.IsValid() != true) return;

      if (m_nview.IsOwner() || ZNet.instance.IsServer())
      {
        if (!m_nview.IsOwner())
          m_nview.ClaimOwnership();

        action();
      }
    }
  }
}