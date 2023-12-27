using UnityEngine;

namespace ValheimRAFT;

public class MastComponent : MonoBehaviour
{
  public GameObject m_sailObject;

  public Cloth m_sailCloth;

  internal bool m_allowSailRotation = true;

  internal bool m_allowSailShrinking = true;

  internal bool m_disableCloth;
}