using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Integrations;

public class PowerConduitHover : MonoBehaviour, Hoverable
{
  private PowerConduitPlateComponent _plateComponent;
  public void Awake()
  {
    _plateComponent = GetComponent<PowerConduitPlateComponent>();
  }

  public string GetHoverText()
  {
    if (_plateComponent.mode == PowerConduitPlateComponent.EnergyPlateMode.Charging)
    {
      return "Eitr Power Charger Rate 4/s";
    }
    return "Eitr Power Drainer Rate 10/s Conversion ratio 10 eitr to 0.1 fuel";
  }

  public string GetHoverName()
  {
    return "Power Conduit";
  }
}