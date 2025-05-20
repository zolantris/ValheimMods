using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Constants;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Integrations;

public class PowerConduitHover : MonoBehaviour, Hoverable
{
  private PowerConduitPlateComponentIntegration _plateComponent;
  public void Awake()
  {
    _plateComponent = GetComponent<PowerConduitPlateComponentIntegration>();
  }

  public string GetHoverText()
  {
    return _plateComponent.Data.GetHoverText();
  }
  public string GetHoverName()
  {
    return ModTranslations.PowerConduit_DrainPlate_Name;
  }
}