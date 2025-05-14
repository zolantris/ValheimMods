// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using ValheimVehicles.Integrations.Interfaces;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations.ZDOConfigs
{
  public class PowerSourceZDOConfig : INetworkedZDOConfig<PowerSourceComponentIntegration>
  {

    public void Load(ZDO zdo, PowerSourceComponentIntegration component)
    {
      var fuel = zdo.GetFloat(VehicleZdoVars.Power_StoredFuel, component.GetFuelLevel());
      var running = zdo.GetBool(VehicleZdoVars.Power_IsRunning, component.IsRunning);

      component.SetFuelLevel(fuel);
      component.SetRunning(running);
    }

    public void Save(ZDO zdo, PowerSourceComponentIntegration component)
    {
      zdo.Set(VehicleZdoVars.Power_StoredFuel, component.GetFuelLevel());
      zdo.Set(VehicleZdoVars.Power_IsRunning, component.IsRunning);
    }
  }
}