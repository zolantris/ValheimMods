// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using ValheimVehicles.Integrations.Interfaces;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations.ZDOConfigs
{
  public class PowerSourceZDOConfig : INetworkedZDOConfig<PowerSourceBridge>
  {

    public void Load(ZDO zdo, PowerSourceBridge component)
    {
      var fuel = zdo.GetFloat(VehicleZdoVars.Power_StoredFuel, component.GetFuelLevel());
      var running = zdo.GetBool(VehicleZdoVars.Power_IsRunning, component.IsRunning);

      // do not call SetFuelLevel directly from integration otherwise infinite loop will occur as it will trigger an RPC
      component.Logic.SetFuelLevel(fuel);
      component.Logic.SetRunning(running);
    }

    public void Save(ZDO zdo, PowerSourceBridge component)
    {
      if (PowerSystemRegistry.TryGetData<PowerSourceData>(zdo, out var powerSourceData))
      {
        powerSourceData.SetFuel(component.GetFuelLevel());
        // powerSourceData.IsActive = component.GetFuelLevel() > 0f && component.IsRunning;
      }
      zdo.Set(VehicleZdoVars.Power_StoredFuel, component.GetFuelLevel());
      zdo.Set(VehicleZdoVars.Power_IsRunning, component.IsRunning);
    }
  }
}