using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Integrations;

public class PowerPylonComponentIntegration : PowerPylon, Hoverable, INetView
{
  protected override void Awake()
  {
    m_nview = GetComponent<ZNetView>();
    // don't do anything when we aren't initialized.
    base.Awake();
  }
  public string GetHoverText()
  {
    return $"Power Pylon on Network: {NetworkId}";
  }
  public string GetHoverName()
  {
    return $"Power Pylon (HoverName) on Network: {NetworkId}";
  }
  public ZNetView? m_nview
  {
    get;
    set;
  }
}