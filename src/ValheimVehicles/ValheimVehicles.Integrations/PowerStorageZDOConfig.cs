using UnityEngine;
using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.Interfaces;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations.ZDOConfigs;

public class PowerStorageZDOConfig : INetworkedZDOConfig<PowerStorageBridge>
{
  public void Load(ZDO zdo, PowerStorageBridge component)
  {
    var stored = zdo.GetFloat(VehicleZdoVars.PowerSystem_Energy, component.Logic.ChargeLevel);

    // if (Mathf.Approximately(stored, 0f))
    // {
    //   LoggerProvider.LogWarning("Stored energy is set to Zero somehow");
    // }

    component.Logic.SetStoredEnergy(stored);
  }

  public void Save(ZDO zdo, PowerStorageBridge component)
  {
    zdo.Set(VehicleZdoVars.PowerSystem_Energy, component.Logic.ChargeLevel);
  }
}