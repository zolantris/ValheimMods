using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.Interfaces;
using ValheimVehicles.Structs;

namespace ValheimVehicles.SharedScripts.ZDOConfigs;

public class PowerConsumerZDOConfig : INetworkedZDOConfig<PowerConsumerComponentIntegration>
{
  public void Load(ZDO zdo, PowerConsumerComponentIntegration component)
  {
    var isDemanding = zdo.GetBool(VehicleZdoVars.Power_IsDemanding, component.IsDemanding);
    component.GetLogic().SetDemandState(isDemanding);
  }

  public void Save(ZDO zdo, PowerConsumerComponentIntegration component)
  {
    zdo.Set(VehicleZdoVars.Power_IsDemanding, component.IsDemanding);
  }
}