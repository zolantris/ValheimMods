// ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ValheimRAFT.MastComponent

using UnityEngine;

public class MastComponent : MonoBehaviour
{
  public GameObject m_sailObject;

  public Cloth m_sailCloth;

  internal bool m_allowSailRotation;

  internal bool m_allowSailShrinking;

  internal bool m_disableCloth;
}