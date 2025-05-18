using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.Interfaces;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations.ZDOConfigs;

public class PowerStorageZDOConfig : INetworkedZDOConfig<PowerStorageComponentIntegration>
{
  public void Load(ZDO zdo, PowerStorageComponentIntegration component)
  {
    var stored = zdo.GetFloat(VehicleZdoVars.Power_StoredEnergy, component.Logic.ChargeLevel);
    component.Logic.SetStoredEnergy(stored);
  }

  public void Save(ZDO zdo, PowerStorageComponentIntegration component)
  {
    zdo.Set(VehicleZdoVars.Power_StoredEnergy, component.Logic.ChargeLevel);
  }
}