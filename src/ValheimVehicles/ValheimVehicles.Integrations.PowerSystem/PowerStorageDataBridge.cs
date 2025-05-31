using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.PowerSystem.Interfaces;
using ValheimVehicles.Shared.Constants;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute;

/// <summary>
/// Integration level class.
/// </summary>
public partial class PowerStorageData : IPowerComputeZdoSync
{
  public PowerStorageData(ZDO zdo) : base(zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab;
    OnLoad += OnLoadZDOSync;
    OnSave += OnSaveZDOSync;
    Load();
  }

  public void OnSaveZDOSync()
  {
    WithIsValidCheck(validZdo =>
    {
      foreach (var key in _dirtyFields)
      {
        switch (key)
        {
          case VehicleZdoVars.PowerSystem_Energy:
            ValheimExtensions.SetDelta(validZdo, key, Energy);
            break;
          case VehicleZdoVars.PowerSystem_EnergyCapacity:
            ValheimExtensions.SetDelta(validZdo, key, EnergyCapacity);
            break;
        }
      }
      // shared config
      OnSharedConfigSave();
    });
  }

  // This is meant for integration
  public void OnLoadZDOSync()
  {
    WithIsValidCheck(validZdo =>
    {
      // shared config
      OnSharedConfigSync();
      Energy = validZdo.GetFloat(VehicleZdoVars.PowerSystem_Energy, 0);
      EnergyCapacity = validZdo.GetFloat(VehicleZdoVars.PowerSystem_EnergyCapacity, EnergyCapacityDefault);
    });
  }
}