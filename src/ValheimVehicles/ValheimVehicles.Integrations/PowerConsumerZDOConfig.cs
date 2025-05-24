using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.Interfaces;
using ValheimVehicles.Structs;

namespace ValheimVehicles.SharedScripts.ZDOConfigs;

public class PowerConsumerZDOConfig : INetworkedZDOConfig<PowerConsumerBridge>
{
  public void Load(ZDO zdo, PowerConsumerBridge component)
  {
    var isDemanding = zdo.GetBool(VehicleZdoVars.Power_IsDemanding, component.IsDemanding);
    component.Logic.SetDemandState(isDemanding);
  }

  public void Save(ZDO zdo, PowerConsumerBridge component)
  {
    zdo.Set(VehicleZdoVars.Power_IsDemanding, component.IsDemanding);
  }
}