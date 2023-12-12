// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.UI.EditRampComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


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
      Transform parent = GUIManager.CustomGUIFront.transform;
      m_editPanel = Object.Instantiate<GameObject>(
        ValheimRaftEntrypoint.m_assetBundle.LoadAsset<GameObject>("edit_ramp_panel"),
        parent, false);
      PanelUtil.ApplyPanelStyle(m_editPanel);
      m_segmentsInput = ((Component)m_editPanel.transform.Find("SegmentsInput"))
        .GetComponent<InputField>();
      Button saveButton =
        ((Component)m_editPanel.transform.Find("SaveButton")).GetComponent<Button>();
      Button cancelButton =
        ((Component)m_editPanel.transform.Find("CancelButton")).GetComponent<Button>();
      ((UnityEvent)saveButton.onClick).AddListener(new UnityAction(SaveEditPanel));
      ((UnityEvent)cancelButton.onClick).AddListener(new UnityAction(CancelEditPanel));
      ((UnityEvent<string>)(object)m_segmentsInput.onValueChanged).AddListener(
        (UnityAction<string>)delegate(string val)
        {
          int.TryParse(val, out m_segmentCount);
          m_segmentCount = Mathf.Clamp(m_segmentCount, 2, 32);
        });
    }

    private void ClosePanel()
    {
      GUIManager.BlockInput(false);
      this.m_editPanel.SetActive(false);
    }

    private void CancelEditPanel() => this.ClosePanel();

    private void SaveEditPanel()
    {
      if (this.m_ramp != (Object)null)
        this.m_ramp.SetSegmentCount(this.m_segmentCount);
      this.ClosePanel();
    }
  }
}