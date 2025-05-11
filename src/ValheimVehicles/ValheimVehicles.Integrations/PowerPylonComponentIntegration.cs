using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Integrations;

public class PowerPylonComponentIntegration : PowerPylon, Hoverable
{
  public string GetHoverText()
  {
    return $"Power Pylon on Network: {NetworkId}";
  }
  public string GetHoverName()
  {
    return $"Power Pylon (HoverName) on Network: {NetworkId}";
  }
}