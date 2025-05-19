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
    var baseString = $"{ModTranslations.PowerConduit_DrainPlate_Name}\n";

    if (PowerNetworkController.CanShowNetworkData || PrefabConfig.PowerNetwork_ShowAdditionalPowerInformationByDefault.Value)
    {
      var isActive = _plateComponent.Logic.HasPlayerInRange;
      var stateText = PowerNetworkController.GetMechanismPowerSourceStatus(isActive);
      baseString += stateText;
    }

    return baseString;
  }

  public string GetHoverName()
  {
    return ModTranslations.PowerConduit_DrainPlate_Name;
  }
}