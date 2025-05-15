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

    return baseString;
  }

  public string GetHoverName()
  {
    return "Power Conduit";
  }
}