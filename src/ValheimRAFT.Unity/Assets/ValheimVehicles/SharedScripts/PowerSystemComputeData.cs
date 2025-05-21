// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  internal interface IPowerComputeIntegration
  {
    public void OnLoadZDO();
  }

  public abstract partial class PowerSystemComputeData
  {
    // constants
    protected const string NetworkIdUnassigned = "UNASSIGNED";

    // static
    public static float PowerRangeDefault = 4f;
    public static float PowerRangePylonDefault = 10f;

    // local
    public float Range = PowerRangeDefault;
    public string NetworkId = NetworkIdUnassigned;
    protected int PrefabHash;

    // actions
    protected Action? OnLoad;

    public void Load()
    {
      OnLoad?.Invoke();
    }
  }
}