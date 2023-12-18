// ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ValheimRAFT.CustomMastComponent

using UnityEngine;
using ValheimRAFT;

public class CustomMastComponent : MastComponent
{
  public GameObject m_upperBeam;

  public GameObject m_lowerBeam;

  public float m_upperBeamLength;

  public float m_lowerBeamLength;

  public float m_upperBeamWidth;

  public float m_lowerBeamWidth;

  public bool m_upperBeamEnabled;

  public bool m_lowerBeamEnabled;

  public RopeAnchorComponent m_upperBeamLeftAnchor;

  public RopeAnchorComponent m_upperBeamRightAnchor;

  public RopeAnchorComponent m_lowerBeamLeftAnchor;

  public RopeAnchorComponent m_lowerBeamRightAnchor;

  public ZNetView m_nview;

  public SailComponent m_sailComponent;

  public void Awake()
  {
    m_sailCloth = GetComponent<Cloth>();
    m_nview = GetComponent<ZNetView>();
    m_sailComponent = GetComponent<SailComponent>();
  }

  public void LoadZDO()
  {
    if ((bool)m_nview && m_nview.GetZDO() != null)
    {
      Vector3 upperBeam = m_nview.GetZDO().GetVec3("MBMast_upperBeam", new Vector3(1f, 1f, 1f));
      m_upperBeamLength = upperBeam.x;
      m_upperBeamWidth = upperBeam.y;
      m_upperBeamEnabled = upperBeam.z == 1f;
      Vector3 lowerBeam = m_nview.GetZDO().GetVec3("MBMast_lowerBeam", new Vector3(1f, 1f, 1f));
      m_lowerBeamLength = lowerBeam.x;
      m_lowerBeamWidth = lowerBeam.y;
      m_lowerBeamEnabled = lowerBeam.z == 1f;
      m_upperBeam.transform.localScale =
        new Vector3(m_upperBeamLength, m_upperBeamWidth, m_upperBeamWidth);
      m_upperBeam.SetActive(m_upperBeamEnabled);
      m_lowerBeam.transform.localScale =
        new Vector3(m_lowerBeamLength, m_lowerBeamWidth, m_lowerBeamWidth);
      m_lowerBeam.SetActive(m_lowerBeamEnabled);
    }
  }
}