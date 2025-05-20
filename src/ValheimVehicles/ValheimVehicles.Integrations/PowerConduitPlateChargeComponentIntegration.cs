using ValheimVehicles.SharedScripts;

public class PowerConduitPlateChargeComponentIntegration : PowerConduitPlateComponentIntegration
{
  protected override void Start()
  {
    base.Start();
    Logic.mode = PowerConduitMode.Charge;
  }
}