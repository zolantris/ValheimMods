using System;
using UnityEngine;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Integrations;

public class CannonControllerInteractions : MonoBehaviour, Hoverable, Interactable
{
  public CannonController controller;
  public string currentAmmo => controller == null ? "None" : controller.AmmoType.ToString();
  public void Awake()
  {
    controller = GetComponent<CannonController>();
  }

  public string GetHoverText()
  {
    return $"Toggle ammo (current) {currentAmmo}";
  }
  public string GetHoverName()
  {
    return "HOVER NAME";
  }
  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (hold) return false;
    if (controller == null) return false;

    controller.AmmoType = controller.AmmoType == Cannonball.CannonballType.Explosive ? Cannonball.CannonballType.Solid : Cannonball.CannonballType.Explosive;

    return true;
  }
  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }
}