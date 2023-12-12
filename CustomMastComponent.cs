// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.CustomMastComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using UnityEngine;

namespace ValheimRAFT
{
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
      this.m_sailCloth = ((Component)this).GetComponent<Cloth>();
      this.m_nview = ((Component)this).GetComponent<ZNetView>();
      this.m_sailComponent = ((Component)this).GetComponent<SailComponent>();
    }

    public void LoadZDO()
    {
      if (!Object.op_Implicit((Object)this.m_nview) || this.m_nview.m_zdo == null)
        return;
      Vector3 vec3_1 = this.m_nview.m_zdo.GetVec3("MBMast_upperBeam", new Vector3(1f, 1f, 1f));
      this.m_upperBeamLength = vec3_1.x;
      this.m_upperBeamWidth = vec3_1.y;
      this.m_upperBeamEnabled = (double)vec3_1.z == 1.0;
      Vector3 vec3_2 = this.m_nview.m_zdo.GetVec3("MBMast_lowerBeam", new Vector3(1f, 1f, 1f));
      this.m_lowerBeamLength = vec3_2.x;
      this.m_lowerBeamWidth = vec3_2.y;
      this.m_lowerBeamEnabled = (double)vec3_2.z == 1.0;
      this.m_upperBeam.transform.localScale = new Vector3(this.m_upperBeamLength,
        this.m_upperBeamWidth, this.m_upperBeamWidth);
      this.m_upperBeam.SetActive(this.m_upperBeamEnabled);
      this.m_lowerBeam.transform.localScale = new Vector3(this.m_lowerBeamLength,
        this.m_lowerBeamWidth, this.m_lowerBeamWidth);
      this.m_lowerBeam.SetActive(this.m_lowerBeamEnabled);
    }
  }
}