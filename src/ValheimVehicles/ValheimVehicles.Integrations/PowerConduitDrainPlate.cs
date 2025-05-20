using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Integrations;

public class PowerConduitDrainPlate : PowerConduitPlateComponent
{
  protected override void Awake()
  {
    // required for some reason due to serialization not respecting other approaches.
    mode = PowerConduitMode.Drain;
  }
}