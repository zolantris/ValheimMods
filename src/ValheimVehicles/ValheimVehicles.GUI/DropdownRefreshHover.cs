using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ValheimVehicles.ValheimVehicles.API;
namespace ValheimVehicles.ValheimVehicles.GUI;

public class DropdownRefreshOnHover : MonoBehaviour, IPointerEnterHandler
{
  private Dropdown dropdown;

  private void Awake()
  {
    dropdown = GetComponent<Dropdown>();
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    RefreshDropdown();
  }

  private void RefreshDropdown()
  {
    if (dropdown == null) return;

    dropdown.ClearOptions();

    var options = VehicleStorageAPI.GetAllVehicles()
      .Select(x => new Dropdown.OptionData($"{x.Settings.VehicleType.ToString()}"))
      .ToList();

    if (options.Count == 0)
    {
      options.Add(new Dropdown.OptionData("No Vehicles"));
    }

    dropdown.AddOptions(options);
  }
}