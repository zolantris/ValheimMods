using UnityEngine;
using ValheimVehicles.BepInExConfig;
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
    if (!_plateComponent || _plateComponent.Data == null) return string.Empty;
    return PowerHoverComponent.GetPowerConduitHoverText(_plateComponent.Data);
  }
  public string GetHoverName()
  {
    return ModTranslations.PowerConduit_DrainPlate_Name;
  }
}