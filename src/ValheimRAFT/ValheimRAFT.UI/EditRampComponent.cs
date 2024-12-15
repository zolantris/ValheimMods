using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ValheimVehicles.Prefabs;

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
    if (!m_editPanel) InitPanel();

    m_segmentCount = boardingRamp.m_segments;
    m_segmentsInput.SetTextWithoutNotify(boardingRamp.m_segments.ToString());
    GUIManager.BlockInput(true);
    m_editPanel.SetActive(true);
  }

  /**
   * Initializes the edit panel if needed. Will not be loaded in memory unless editing is required.
   */
  private void InitPanel()
  {
    var parent = GUIManager.CustomGUIFront.transform;
    m_editPanel = Object.Instantiate(
      PrefabRegistryController.raftAssetBundle.LoadAsset<GameObject>(
        "edit_ramp_panel"),
      parent, false);
    PanelUtil.ApplyPanelStyle(m_editPanel);
    m_segmentsInput = m_editPanel.transform.Find("SegmentsInput")
      .GetComponent<InputField>();
    var saveButton = m_editPanel.transform.Find("SaveButton")
      .GetComponent<Button>();
    var cancelButton = m_editPanel.transform.Find("CancelButton")
      .GetComponent<Button>();
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
    GUIManager.BlockInput(false);
    m_editPanel.SetActive(false);
  }

  private void CancelEditPanel()
  {
    ClosePanel();
  }

  private void SaveEditPanel()
  {
    if (m_ramp != null) m_ramp.SetSegmentCount(m_segmentCount);

    ClosePanel();
  }
}