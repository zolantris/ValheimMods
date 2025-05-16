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

  public string GetPowerConduitModeName()
  {
    if (!_plateComponent.Logic) return "";
    return _plateComponent.Logic.mode.ToString();
  }

  public string GetHoverText()
  {
    var baseString = $"{ModTranslations.PowerConduit_DrainPlate_Name}\n";

    if (PowerNetworkController.CanShowNetworkData || PrefabConfig.PowerNetwork_ShowAdditionalPowerInformationByDefault.Value)
    {
      var isActive = _plateComponent.Logic.HasPlayerInRange;
      var color = isActive ? "yellow" : "red";
      var text = isActive ? ModTranslations.PowerState_Conduit_Active : ModTranslations.PowerState_Conduit_Inactive;

      var stateText = $"({ModTranslations.WithBoldText(text, color)})";
      baseString += stateText;
    }

    return baseString;
  }

  public string GetHoverName()
  {
    return ModTranslations.PowerConduit_DrainPlate_Name;
  }
}