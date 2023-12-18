using UnityEngine;

namespace ValheimRAFT
{
  public class RopeComponent : MonoBehaviour
  {
    public SpringJoint m_spring = (SpringJoint)null;

    public string GetHoverName() => "";

    public string GetHoverText() => "$mb_rope_use";

    public bool Interact(Humanoid user, bool hold, bool alt) => true;

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    internal SpringJoint GetSpring()
    {
      if (!m_spring)
        m_spring = ((Component)this).gameObject.AddComponent<SpringJoint>();
      return this.m_spring;
    }
  }
}