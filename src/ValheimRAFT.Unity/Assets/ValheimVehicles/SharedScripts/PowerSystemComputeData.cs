// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public interface IPowerComputeBase
  {
    float Range { get; set; }
    string NetworkId { get; set; }
    int PrefabHash { get; set; }
    Action? OnLoad { get; set; }
    public void Load();
    public bool IsActive { get; }
  }

  public abstract partial class PowerSystemComputeData : IPowerComputeBase
  {
    // constants
    protected const string NetworkIdUnassigned = "UNASSIGNED";

    // static
    public static float PowerRangeDefault = 4f;
    public static float PowerRangePylonDefault = 10f;

    // local

    public float Range
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

    // methods
    public void Load()
    {
      OnLoad?.Invoke();
    }

    public abstract bool IsActive { get; }
  }
}