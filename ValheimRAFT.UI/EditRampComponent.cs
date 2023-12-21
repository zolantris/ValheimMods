using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRAFT.UI;

public class EditRampComponent
{
  private BoardingRampComponent m_ramp;

  private GameObject m_editPanel;

  private InputField m_segmentsInput;

  private int m_segmentCount;

  public void ShowPanel(BoardingRampComponent boardingRamp)
  {
    m_ramp = boardingRamp;
    if (!m_editPanel)
    {
      InitPanel();
    }

    m_segmentCount = boardingRamp.m_segments;
    m_segmentsInput.SetTextWithoutNotify(boardingRamp.m_segments.ToString());
    GUIManager.BlockInput(state: true);
    m_editPanel.SetActive(value: true);
  }

  private void InitPanel()
  {
    Transform parent = GUIManager.CustomGUIFront.transform;
    m_editPanel = Object.Instantiate(
      ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("edit_ramp_panel"),
      parent, worldPositionStays: false);
    PanelUtil.ApplyPanelStyle(m_editPanel);
    m_segmentsInput = m_editPanel.transform.Find("SegmentsInput").GetComponent<InputField>();
    Button saveButton = m_editPanel.transform.Find("SaveButton").GetComponent<Button>();
    Button cancelButton = m_editPanel.transform.Find("CancelButton").GetComponent<Button>();
    saveButton.onClick.AddListener(SaveEditPanel);
    cancelButton.onClick.AddListener(CancelEditPanel);
    m_segmentsInput.onValueChanged.AddListener(delegate(string val)
    {
      int.TryParse(val, out m_segmentCount);
      m_segmentCount = Mathf.Clamp(m_segmentCount, 2, 32);
    });
  }

  private void ClosePanel()
  {
    GUIManager.BlockInput(state: false);
    m_editPanel.SetActive(value: false);
  }

  private void CancelEditPanel()
  {
    ClosePanel();
  }

  private void SaveEditPanel()
  {
    if (m_ramp != null)
    {
      m_ramp.SetSegmentCount(m_segmentCount);
    }

    ClosePanel();
  }
}