using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Vehicles.Interfaces;

namespace ValheimVehicles.ValheimVehicles.Providers;

public class WearNTearIntegrationProvider : IInitProvider
{
  private static IWearNTearStub? ResolveWearNTear(GameObject? obj)
  {
    if (obj == null) return null;
    var wearNTear = obj.GetComponent<WearNTear>();
    return wearNTear == null ? null : new WearNTearAdapter(wearNTear);
  }

  public void Init()
  {
    WearNTearProviderBase.GetWearNTearComponent = ResolveWearNTear;
  }

  public class WearNTearAdapter : IWearNTearStub
  {
    private static Dictionary<WearNTear, WearNTearAdapter> WearNTearAdapterInstances = new();
    private readonly WearNTear _wearNTear;

    /// <summary>
    /// Rebind methods from WearNTear so we can use this without referencing it directly in Unity projects.
    /// </summary>
    /// <param name="component"></param>
    public WearNTearAdapter(WearNTear component)
    {
      _wearNTear = component;

      if (WearNTearAdapterInstances.ContainsKey(component)) return;
      WearNTearAdapterInstances.Add(component, this);
      _wearNTear.m_onDestroyed += () =>
      {
        WearNTearAdapterInstances.Remove(component);
      };
    }

    /// <summary>
    /// WearNTear extension action to invoke when RPC for on repair is called.
    /// To be invoked in WearNTearPatch
    /// </summary>
    public static void OnHealthVisualChange(WearNTear wnt)
    {
      if (WearNTearAdapterInstances.TryGetValue(wnt, out var adapter))
      {
        adapter.m_onHealthVisualChange.Invoke();
      }
    }

    public Action m_onHealthVisualChange { get; set; } = () => {};

    public Action m_onDestroyed
    {
      get => _wearNTear.m_onDestroyed;
      set => _wearNTear.m_onDestroyed = value;
    }

    public GameObject? m_new => _wearNTear.m_new;

    public GameObject? m_worn => _wearNTear.m_worn;

    public GameObject? m_broken => _wearNTear.m_broken;

    public GameObject? m_wet => _wearNTear.m_wet;
  }
}