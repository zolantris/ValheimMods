using ValheimVehicles.SharedScripts.PowerSystem;

public class PowerConduitPlateChargeComponentIntegration : PowerConduitPlateComponentIntegration
{
  protected override void Start()
  {
    base.Start();
    Logic.mode = PowerConduitPlateComponent.EnergyPlateMode.Charging;
  }
}