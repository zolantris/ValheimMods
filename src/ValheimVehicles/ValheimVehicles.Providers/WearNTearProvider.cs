using System;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Interfaces;

namespace ValheimVehicles.Providers;

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

  private class WearNTearAdapter : IWearNTearStub
  {
    private readonly WearNTear _wearNTear;

    /// <summary>
    /// Rebind methods from WearNTear so we can use this without referencing it directly in Unity projects.
    /// </summary>
    /// <param name="component"></param>
    public WearNTearAdapter(WearNTear component)
    {
      _wearNTear = component;
      m_onDestroyed = component.m_onDestroyed;
      m_new = component.m_new;
      m_worn = component.m_worn;
      m_broken = component.m_broken;
      m_wet = component.m_wet;
    }

    public Action m_onDestroyed
    {
      get;
      set;
    }
    public GameObject? m_new
    {
      get;
      set;
    }
    public GameObject? m_worn
    {
      get;
      set;
    }
    public GameObject? m_broken
    {
      get;
      set;
    }
    public GameObject? m_wet
    {
      get;
      set;
    }
  }
}