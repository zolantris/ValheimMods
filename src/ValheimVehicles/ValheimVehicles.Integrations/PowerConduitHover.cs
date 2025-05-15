using UnityEngine;
using ValheimVehicles.Constants;
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
    var baseString = $"Power Conduit: Type {GetPowerConduitModeName()}";

    if (_plateComponent.Logic.HasPlayer)
    {
      var activeText = ModTranslations.WithBoldText(ModTranslations.PowerState_Conduit_Active, "yellow");
      baseString += $"\n({activeText})";
    }
    // if (_plateComponent.mode == PowerConduitPlateComponent.EnergyPlateMode.Charging)
    // {
    //   return "Eitr Power Charger Rate 4/s";
    // }
    // return "Eitr Power Drainer Rate 10/s Conversion ratio 10 eitr to 0.1 fuel";
    return baseString;
  }

  public string GetHoverName()
  {
    return "Power Conduit";
  }
}