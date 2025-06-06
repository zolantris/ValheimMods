using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.PowerSystem.Interfaces;
using ValheimVehicles.Shared.Constants;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute;

public partial class PowerConsumerData : IPowerComputeZdoSync
{
  public PowerConsumerData(ZDO zdo) : base(zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab; // allows for determining variants based on prefab hash.
    OnLoad += OnLoadZDOSync;
    OnSave += OnSaveZDOSync;
    CanRunConsumerForDeltaTime = HandleCanRunConsumerForDeltaTime;

    Load();
  }

  public bool HandleCanRunConsumerForDeltaTime(float deltaTime)
  {
    var nextRequiredEnergyAllotment = GetWattsForLevel(powerIntensityLevel) * deltaTime;
    if (!PowerNetworkController.TryNetworkPowerData(NetworkId, out var powerSystemDisplayData))
    {
      return true;
    }
    return nextRequiredEnergyAllotment <= powerSystemDisplayData.NetworkPowerSupply;
  }

  public void OnSaveZDOSync()
  {
    WithIsValidCheck((validZdo) =>
    {
      foreach (var key in _dirtyFields)
      {
        switch (key)
        {
          case VehicleZdoVars.PowerSystem_BasePowerConsumption:
            ValheimExtensions.SetDelta(validZdo, key, BasePowerConsumption);
            break;
          case VehicleZdoVars.PowerSystem_Intensity_Level:
            ValheimExtensions.SetDelta(validZdo, key, (int)powerIntensityLevel);
            break;
          case VehicleZdoVars.PowerSystem_IsDemanding:
            ValheimExtensions.SetDelta(validZdo, key, IsDemanding);
            break;
        }
      }
      // shared config
      OnSharedConfigSave();
    });
  }

  public void OnLoadZDOSync()
  {
    WithIsValidCheck((validZdo) =>
    {
      // shared config
      OnSharedConfigSync();

      IsDemanding = validZdo.GetBool(VehicleZdoVars.PowerSystem_IsDemanding, false);
      var intensityInt = validZdo.GetInt(VehicleZdoVars.PowerSystem_Intensity_Level, (int)PowerIntensityLevel.Low);
      powerIntensityLevel = (PowerIntensityLevel)intensityInt;
    });
  }
}