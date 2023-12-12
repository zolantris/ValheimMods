// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.UI.EditRampComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ValheimRAFT.UI
{
  public class EditRampComponent
  {
    private BoardingRampComponent m_ramp;
    private GameObject m_editPanel;
    private InputField m_segmentsInput;
    private int m_segmentCount;

    public void ShowPanel(BoardingRampComponent boardingRamp)
    {
      this.m_ramp = boardingRamp;
      if (!this.m_editPanel)
        this.InitPanel();
      this.m_segmentCount = boardingRamp.m_segments;
      this.m_segmentsInput.SetTextWithoutNotify(boardingRamp.m_segments.ToString());
      GUIManager.BlockInput(true);
      this.m_editPanel.SetActive(true);
    }

    private void InitPanel()
    {
      Transform transform = GUIManager.CustomGUIFront.transform;
      this.m_editPanel = Object.Instantiate<GameObject>(ValheimRAFT.m_assetBundle.LoadAsset<GameObject>("edit_ramp_panel"), transform, false);
      PanelUtil.ApplyPanelStyle(this.m_editPanel);
      this.m_segmentsInput = ((Component) this.m_editPanel.transform.Find("SegmentsInput")).GetComponent<InputField>();
      Button component1 = ((Component) this.m_editPanel.transform.Find("SaveButton")).GetComponent<Button>();
      Button component2 = ((Component) this.m_editPanel.transform.Find("CancelButton")).GetComponent<Button>();
      // ISSUE: method pointer
      ((UnityEvent) component1.onClick).AddListener(new UnityAction(SaveEditPanel));
      // ISSUE: method pointer
      ((UnityEvent) component2.onClick).AddListener(new UnityAction(CancelEditPanel));
      // ISSUE: method pointer
      ((UnityEvent<string>) this.m_segmentsInput.onValueChanged).AddListener(new UnityAction<string>((object) this, __methodptr(\u003CInitPanel\u003Eb__5_0)));
    }

    private void ClosePanel()
    {
      GUIManager.BlockInput(false);
      this.m_editPanel.SetActive(false);
    }

    private void CancelEditPanel() => this.ClosePanel();

    private void SaveEditPanel()
    {
      if (this.m_ramp != (Object) null)
        this.m_ramp.SetSegmentCount(this.m_segmentCount);
      this.ClosePanel();
    }
  }
}
