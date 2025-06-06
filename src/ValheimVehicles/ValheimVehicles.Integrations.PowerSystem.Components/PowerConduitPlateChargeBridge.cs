using ValheimVehicles.SharedScripts;

public class PowerConduitPlateChargeBridge : PowerConduitPlateBridge
{
  protected override void Start()
  {
    base.Start();
    Logic.mode = PowerConduitMode.Charge;
  }
}