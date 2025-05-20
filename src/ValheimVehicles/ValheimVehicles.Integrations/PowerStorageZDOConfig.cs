using UnityEngine;
using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.Interfaces;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations.ZDOConfigs;

public class PowerStorageZDOConfig : INetworkedZDOConfig<PowerStorageComponentIntegration>
{
  public void Load(ZDO zdo, PowerStorageComponentIntegration component)
  {
    var stored = zdo.GetFloat(VehicleZdoVars.Power_StoredEnergy, component.Logic.ChargeLevel);

    // if (Mathf.Approximately(stored, 0f))
    // {
    //   LoggerProvider.LogWarning("Stored energy is set to Zero somehow");
    // }

    component.Logic.SetStoredEnergy(stored);
  }

  public void Save(ZDO zdo, PowerStorageComponentIntegration component)
  {
    zdo.Set(VehicleZdoVars.Power_StoredEnergy, component.Logic.ChargeLevel);
  }
}