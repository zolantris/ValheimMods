using UnityEngine;

namespace Components;

public class CustomToggleSwitch : ToggleSwitch
{
  // public bool Interact(Humanoid character, bool hold, bool alt)
  // {
  //   if (hold)
  //     return false;
  //   if (this.m_onUse != null)
  //     this.m_onUse(this, character);
  //   return true;
  // }
  //
  // public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
  //
  // public string GetHoverText() => this.m_hoverText;
  //
  // public string GetHoverName() => this.m_name;
  //
  // public void SetState(bool enabled)
  // {
  //   this.m_state = enabled;
  //   this.m_renderer.material =
  //     this.m_state ? this.m_enableMaterial : this.m_disableMaterial;
  // }
  private void Awake()
  {
    return;
  }
}