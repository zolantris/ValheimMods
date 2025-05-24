// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public abstract partial class PowerSystemComputeData : IPowerSystemEntityData
  {
    // constants
    private bool _isActive = true;
    protected const string NetworkIdUnassigned = "UNASSIGNED";

    // static
    public static float PowerRangeDefault = 4f;
    public static float PowerRangePylonDefault = 10f;

    // local

    public float ConnectionRange
    {
      get;
      set;
    } = PowerRangeDefault;
    public string NetworkId
    {
      get;
      set;
    } = NetworkIdUnassigned;
    public int PrefabHash
    {
      get;
      set;
    }

    // actions
    public Action? OnLoad
    {
      get;
      set;
    }

    public Action? OnSave
    {
      get;
      set;
    }

    public Action? OnActive
    {
      get;
      set;
    }

    // methods
    public void Load()
    {
      OnLoad?.Invoke();
    }

    public void Save()
    {
      OnSave?.Invoke();
    }

    public void SetActive(bool val)
    {
      OnActive?.Invoke();
      _isActive = val;
    }

    public virtual bool IsActive => _isActive;
  }
}