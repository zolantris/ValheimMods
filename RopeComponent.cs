// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.RopeComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


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