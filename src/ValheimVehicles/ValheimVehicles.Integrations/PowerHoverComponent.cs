using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Integrations;

public class PowerHoverComponent : MonoBehaviour, Hoverable, Interactable
{
  private PowerSourceComponentIntegration _powerSourceComponent;
  private PowerStorageComponentIntegration _powerStorageComponent;
  private bool HasPowerStorage = false;
  private bool HasPowerSource = false;
  public void Start()
  {
    _powerSourceComponent = GetComponent<PowerSourceComponentIntegration>();
    _powerStorageComponent = GetComponent<PowerStorageComponentIntegration>();

    HasPowerStorage = _powerStorageComponent != null;
    HasPowerSource = _powerSourceComponent != null;
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (hold)
    {
      if (HasPowerSource)
      {
        _powerSourceComponent.Refuel(2);
        return true;
      }
    }
    if (alt)
    {
      if (HasPowerSource)
      {
        _powerSourceComponent.Refuel(10);
        return true;
      }
    }

    if (HasPowerSource)
    {
      _powerSourceComponent.Refuel(1);
      return true;
    }

    return false;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public string GetHoverText()
  {
    var outString = "";
    if (HasPowerSource)
    {
      outString += $"Power Source: {_powerSourceComponent.GetFuelLevel()}/{_powerSourceComponent.GetFuelCapacity()}\n";
    }
    if (HasPowerStorage)
    {
      outString += $"Power Storage: {_powerStorageComponent.ChargeLevel}/{_powerStorageComponent.Capacity}";
    }
    return outString;
  }
  public string GetHoverName()
  {
    return "Power Source";
  }
}