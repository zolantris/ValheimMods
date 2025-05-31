using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.PowerSystem.Interfaces;
using ValheimVehicles.Shared.Constants;

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute;

public partial class PowerSourceData : IPowerComputeZdoSync
{
  public PowerSourceData(ZDO zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab;

    OnNetworkIdChange += HandleNetworkIdUpdate;
    OnLoad += OnLoadZDOSync;
    OnSave += OnSaveZDOSync;
    Load();

    OnPropertiesUpdate();
  }

  public void OnSaveZDOSync()
  {
    WithIsValidCheck(validZdo =>
    {
      foreach (var key in _dirtyFields)
      {
        switch (key)
        {
          case VehicleZdoVars.PowerSystem_IsRunning:
            ValheimExtensions.SetDelta(validZdo, key, IsRunning);
            break;
          case VehicleZdoVars.PowerSystem_Fuel:
            ConsolidateFuel();
            ValheimExtensions.SetDelta(validZdo, key, Fuel);
            break;
          case VehicleZdoVars.PowerSystem_FuelOutputRate:
            ValheimExtensions.SetDelta(validZdo, key, OutputRate);
            break;
        }
      }
      OnSharedConfigSave();
    });
  }

  public void OnLoadZDOSync()
  {
    WithIsValidCheck(validZdo =>
    {
      // shared config
      OnSharedConfigSync();

      Fuel = validZdo.GetFloat(VehicleZdoVars.PowerSystem_Fuel);
      FuelCapacity = validZdo.GetFloat(VehicleZdoVars.PowerSystem_StoredFuelCapacity, FuelCapacityDefault);
      OutputRate = validZdo.GetFloat(VehicleZdoVars.PowerSystem_FuelOutputRate, OutputRateDefault);
    });
  }
}