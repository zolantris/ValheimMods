using UnityEngine;

namespace ValheimRAFT;

public class MastComponent : MonoBehaviour
{
  public GameObject m_sailObject;

  public Cloth m_sailCloth;

  public bool m_allowSailRotation;

  public bool m_allowSailShrinking = true;

  public bool m_disableCloth;
}