// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.MastComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using UnityEngine;

namespace ValheimRAFT
{
  public class MastComponent : MonoBehaviour
  {
    public GameObject m_sailObject;
    public Cloth m_sailCloth;
    internal bool m_allowSailRotation;
    internal bool m_allowSailShrinking;
    internal bool m_disableCloth;
  }
}