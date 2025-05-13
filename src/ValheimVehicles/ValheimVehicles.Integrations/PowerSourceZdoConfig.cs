// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.Structs;

namespace ValheimVehicles.SharedScripts.ZDOConfigs
{
  public class PowerSourceZDOConfig : INetworkedZDOConfig<PowerSourceComponentIntegration>
  {

    public void Load(ZDO zdo, PowerSourceComponentIntegration component)
    {
      var fuel = zdo.GetFloat(VehicleZdoVars.Power_StoredFuel, component.GetFuelLevel());
      var running = zdo.GetBool(VehicleZdoVars.Power_IsRunning, component.isRunning);

      component.Refuel(fuel - component.GetFuelLevel());
      component.SetRunning(running);
    }

    public void Save(ZDO zdo, PowerSourceComponentIntegration component)
    {
      zdo.Set(VehicleZdoVars.Power_StoredFuel, component.GetFuelLevel());
      zdo.Set(VehicleZdoVars.Power_IsRunning, component.isRunning);
    }
  }
}