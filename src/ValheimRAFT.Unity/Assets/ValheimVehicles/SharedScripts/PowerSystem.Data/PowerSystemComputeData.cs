// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using ValheimVehicles.Shared.Constants;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public abstract partial class PowerSystemComputeData : IPowerSystemEntityData
  {
    // constants
    private bool _isActive = true;
    public const string NetworkIdUnassigned = "UNASSIGNED";

    // static
    public static float PowerRangeDefault = 4f;
    public static float PowerRangePylonDefault = 10f;

    protected HashSet<string> _dirtyFields = new();

    public bool IsTempNetworkId => IsTempNetworkIdStatic(NetworkId);
    public static bool IsTempNetworkIdStatic(string val)
    {
      return val != "" && val != NetworkIdUnassigned;
    }

    public void MarkDirty(string zdoKey)
    {
      _dirtyFields.Add(zdoKey);
    }

    public void ClearDirty()
    {
      _dirtyFields.Clear();
    }

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

    public Action<string> OnNetworkIdChange { get; set; } = (_) => {};

    // methods
    public void Load()
    {
      OnLoad?.Invoke();
    }

    public void Save()
    {
      OnSave?.Invoke();
    }

    public void SetNetworkId(string val)
    {
      if (val == NetworkId) return;
      NetworkId = val;
      MarkDirty(VehicleZdoVars.PowerSystem_NetworkId);
    }

    public void SetActive(bool val)
    {
      if (_isActive == val) return;
      OnActive?.Invoke();
      _isActive = val;
      MarkDirty(VehicleZdoVars.PowerSystem_IsActive);
    }

    public virtual bool IsActive => _isActive;
  }
}