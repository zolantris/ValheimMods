using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ValheimVehicles.Prefabs;

namespace ValheimRAFT.UI;

public class EditSailComponentPanel
{
  private GameObject m_editPanel;

  private GameObject m_editSailPanel;

  private GameObject m_editPatternPanel;

  private GameObject m_editLogoPanel;

  private SailComponent m_editSail;

  private Toggle[] m_editLockedSailSides;

  private Toggle[] m_editLockedSailCorners;

  private Toggle m_sailShrinkingToggle;

  private GameObject m_locksTri;

  private GameObject m_locksQuad;

  private Toggle m_disableClothToggle;

  public void ShowPanel(SailComponent sailComponent)
  {
    m_editSail = sailComponent;
    m_editSail.StartEdit();
    var isTri = m_editSail.m_sailCorners.Count == 3;

    if (!m_editPanel) InitPanel();

    var locksArea = isTri ? m_locksTri : m_locksQuad;
    m_editLockedSailCorners = new Toggle[isTri ? 3 : 4];
    m_editLockedSailSides = new Toggle[isTri ? 3 : 4];
    m_locksTri.SetActive(isTri);
    m_locksQuad.SetActive(!isTri);
    m_editLockedSailCorners[0] = locksArea.transform.Find("CornerA").GetComponent<Toggle>();
    m_editLockedSailCorners[1] = locksArea.transform.Find("CornerB").GetComponent<Toggle>();
    m_editLockedSailCorners[2] = locksArea.transform.Find("CornerC").GetComponent<Toggle>();
    if (!isTri)
      m_editLockedSailCorners[3] = locksArea.transform.Find("CornerD").GetComponent<Toggle>();

    m_editLockedSailSides[0] = locksArea.transform.Find("SideA").GetComponent<Toggle>();
    m_editLockedSailSides[1] = locksArea.transform.Find("SideB").GetComponent<Toggle>();
    m_editLockedSailSides[2] = locksArea.transform.Find("SideC").GetComponent<Toggle>();
    if (!isTri) m_editLockedSailSides[3] = locksArea.transform.Find("SideD").GetComponent<Toggle>();

    for (var i = 0; i < m_editLockedSailCorners.Length; i++)
    {
      m_editLockedSailCorners[i]
        .SetIsOnWithoutNotify(
          m_editSail.m_lockedSailCorners.HasFlag((SailComponent.SailLockedSide)(1 << i)));
      m_editLockedSailSides[i]
        .SetIsOnWithoutNotify(
          m_editSail.m_lockedSailSides.HasFlag((SailComponent.SailLockedSide)(1 << i)));
    }

    // toggle buttons
    m_sailShrinkingToggle.SetIsOnWithoutNotify(
      m_editSail.m_sailFlags.HasFlag(SailComponent.SailFlags.AllowSailShrinking));
    m_disableClothToggle.SetIsOnWithoutNotify(
      m_editSail.m_sailFlags.HasFlag(SailComponent.SailFlags.DisableCloth));

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
      LoadValheimRaftAssets.editPanel,
      parent, false);
    PanelUtil.ApplyPanelStyle(m_editPanel);
    var texturePanel = LoadValheimRaftAssets.editTexturePanel;
    PanelUtil.ApplyPanelStyle(texturePanel);

    GUIManager.Instance.ApplyDropdownStyle(
      texturePanel.transform.Find("TextureName").GetComponent<Dropdown>(), 15);
    m_editSailPanel = Object.Instantiate(texturePanel, parent, false);
    m_editPatternPanel = Object.Instantiate(texturePanel, parent, false);
    m_editLogoPanel = Object.Instantiate(texturePanel, parent, false);
    m_editSailPanel.SetActive(false);
    m_editPatternPanel.SetActive(false);
    m_editLogoPanel.SetActive(false);
    m_editSailPanel.transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate
    {
      m_editSailPanel.SetActive(false);
      m_editPanel.SetActive(true);
    });
    m_editPatternPanel.transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate
    {
      m_editPatternPanel.SetActive(false);
      m_editPanel.SetActive(true);
    });
    m_editLogoPanel.transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate
    {
      m_editLogoPanel.SetActive(false);
      m_editPanel.SetActive(true);
    });
    var saveObject = m_editPanel.transform.Find("SaveButton").gameObject;
    var cancelObject = m_editPanel.transform.Find("CancelButton").gameObject;
    cancelObject.GetComponent<Button>().onClick.AddListener(CancelEditPanel);
    saveObject.GetComponent<Button>().onClick.AddListener(SaveEditPanel);
    m_sailShrinkingToggle =
      m_editPanel.transform.Find("SailShrinkingToggle").GetComponent<Toggle>();
    m_sailShrinkingToggle.onValueChanged.AddListener(delegate(bool b)
    {
      m_editSail.SetSailMastSetting(SailComponent.SailFlags.AllowSailShrinking, b);
    });

    m_disableClothToggle = m_editPanel.transform.Find("SailClothToggle").GetComponent<Toggle>();
    m_disableClothToggle.onValueChanged.AddListener(delegate(bool b)
    {
      m_editSail.SetSailMastSetting(SailComponent.SailFlags.DisableCloth, b);
    });
    m_locksTri = m_editPanel.transform.Find("LocksTri").gameObject;
    m_locksQuad = m_editPanel.transform.Find("LocksQuad").gameObject;
    var componentsInChildren = m_locksTri.GetComponentsInChildren<Toggle>();
    foreach (var toggle2 in componentsInChildren)
      toggle2.onValueChanged.AddListener(delegate { UpdateSails(); });

    var componentsInChildren2 = m_locksQuad.GetComponentsInChildren<Toggle>();
    foreach (var toggle in componentsInChildren2)
      toggle.onValueChanged.AddListener(delegate { UpdateSails(); });

    var editSailButton = m_editPanel.transform.Find("EditSailButton").GetComponent<Button>();
    var editPatternButton =
      m_editPanel.transform.Find("EditPatternButton").GetComponent<Button>();
    var editLogoButton = m_editPanel.transform.Find("EditLogoButton").GetComponent<Button>();
    editSailButton.onClick.AddListener(delegate
    {
      m_editSailPanel.SetActive(true);
      m_editPatternPanel.SetActive(false);
      m_editLogoPanel.SetActive(false);
      m_editPanel.SetActive(false);
      LoadTexturePanel(m_editSailPanel, m_editSail.GetSailMaterial(), "_Main",
        CustomTextureGroup.Get("Sails"));
    });
    editPatternButton.onClick.AddListener(delegate
    {
      m_editSailPanel.SetActive(false);
      m_editPatternPanel.SetActive(true);
      m_editLogoPanel.SetActive(false);
      m_editPanel.SetActive(false);
      LoadTexturePanel(m_editPatternPanel, m_editSail.GetSailMaterial(), "_Pattern",
        CustomTextureGroup.Get("Patterns"));
    });
    editLogoButton.onClick.AddListener(delegate
    {
      m_editSailPanel.SetActive(false);
      m_editPatternPanel.SetActive(false);
      m_editLogoPanel.SetActive(true);
      m_editPanel.SetActive(false);
      LoadTexturePanel(m_editLogoPanel, m_editSail.GetSailMaterial(), "_Logo",
        CustomTextureGroup.Get("Logos"));
    });
  }

  private void LoadTexturePanel(GameObject editPanel, Material mat, string parameterName,
    CustomTextureGroup group)
  {
    var textureColor = editPanel.transform.Find("TextureColor").GetComponent<Button>();
    var textureDropdown = editPanel.transform.Find("TextureName").GetComponent<Dropdown>();
    var offsetx = editPanel.transform.Find("OffsetX").GetComponent<InputField>();
    var offsety = editPanel.transform.Find("OffsetY").GetComponent<InputField>();
    var scalex = editPanel.transform.Find("TilingX").GetComponent<InputField>();
    var scaley = editPanel.transform.Find("TilingY").GetComponent<InputField>();
    var rot = editPanel.transform.Find("Rotation").GetComponent<InputField>();
    textureColor.onClick.RemoveAllListeners();
    textureDropdown.onValueChanged.RemoveAllListeners();
    offsetx.onValueChanged.RemoveAllListeners();
    offsety.onValueChanged.RemoveAllListeners();
    scalex.onValueChanged.RemoveAllListeners();
    scaley.onValueChanged.RemoveAllListeners();
    rot.onValueChanged.RemoveAllListeners();
    var options = new List<Dropdown.OptionData>();
    for (var i = 0; i < group.Textures.Count; i++)
      options.Add(new Dropdown.OptionData(group.Textures[i].Texture.name));

    textureDropdown.options = options;
    textureDropdown.onValueChanged.AddListener(delegate(int index)
    {
      mat.SetTexture(parameterName + "Tex", group.Textures[index].Texture);
      if ((bool)group.Textures[index].Normal)
        mat.SetTexture(parameterName + "Normal", group.Textures[index].Normal);
    });
    GUIManager.Instance.ApplyDropdownStyle(textureDropdown, 15);
    textureColor.onClick.AddListener(delegate
    {
      GUIManager.Instance.CreateColorPicker(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
        new Vector2(0.5f, 0.5f), mat.GetColor(parameterName + "Color"), "Color",
        delegate(Color color) { mat.SetColor(parameterName + "Color", color); },
        delegate(Color color) { mat.SetColor(parameterName + "Color", color); }, true);
    });
    offsetx.onValueChanged.AddListener(delegate(string str)
    {
      if (float.TryParse(str, out var result5))
      {
        var textureOffset2 = mat.GetTextureOffset(parameterName + "Tex");
        textureOffset2.x = result5;
        mat.SetTextureOffset(parameterName + "Tex", textureOffset2);
      }
    });
    offsety.onValueChanged.AddListener(delegate(string str)
    {
      if (float.TryParse(str, out var result4))
      {
        var textureOffset = mat.GetTextureOffset(parameterName + "Tex");
        textureOffset.y = result4;
        mat.SetTextureOffset(parameterName + "Tex", textureOffset);
      }
    });
    scalex.onValueChanged.AddListener(delegate(string str)
    {
      if (float.TryParse(str, out var result3))
      {
        var textureScale2 = mat.GetTextureScale(parameterName + "Tex");
        textureScale2.x = result3;
        mat.SetTextureScale(parameterName + "Tex", textureScale2);
      }
    });
    scaley.onValueChanged.AddListener(delegate(string str)
    {
      if (float.TryParse(str, out var result2))
      {
        var textureScale = mat.GetTextureScale(parameterName + "Tex");
        textureScale.y = result2;
        mat.SetTextureScale(parameterName + "Tex", textureScale);
      }
    });
    rot.onValueChanged.AddListener(delegate(string str)
    {
      if (float.TryParse(str, out var result)) mat.SetFloat(parameterName + "Rotation", result);
    });
    var tex = mat.GetTexture(parameterName + "Tex");
    var customtex =
      group.GetTextureByHash(tex.name.GetStableHashCode());
    if (customtex != null) textureDropdown.SetValueWithoutNotify(customtex.Index);

    UpdateColorButton(textureColor, mat.GetColor(parameterName + "Color"));
    var offset = mat.GetTextureOffset(parameterName + "Tex");
    offsetx.SetTextWithoutNotify(offset.x.ToString());
    offsety.SetTextWithoutNotify(offset.y.ToString());
    var scale = mat.GetTextureScale(parameterName + "Tex");
    scalex.SetTextWithoutNotify(scale.x.ToString());
    scaley.SetTextWithoutNotify(scale.y.ToString());
    if (rot.gameObject.activeSelf)
      rot.SetTextWithoutNotify(mat.GetFloat(parameterName + "Rotation").ToString());
  }

  private void UpdateColorButton(Button button, Color color)
  {
    color.a = 1f;
    button.colors = new ColorBlock
    {
      normalColor = color,
      highlightedColor = color,
      disabledColor = color,
      fadeDuration = 0.1f,
      pressedColor = color,
      selectedColor = color,
      colorMultiplier = 1f
    };
  }

  private void UpdateSails()
  {
    if (!(bool)m_editSail) return;

    m_editSail.m_lockedSailCorners = SailComponent.SailLockedSide.None;
    for (var i = 0; i < m_editLockedSailCorners.Length; i++)
      m_editSail.m_lockedSailCorners |=
        (SailComponent.SailLockedSide)(m_editLockedSailCorners[i].isOn ? 1 << i : 0);

    m_editSail.m_lockedSailSides = SailComponent.SailLockedSide.None;
    for (var j = 0; j < m_editLockedSailSides.Length; j++)
      m_editSail.m_lockedSailSides |=
        (SailComponent.SailLockedSide)(m_editLockedSailSides[j].isOn ? 1 << j : 0);

    m_editSail.UpdateCoefficients();
  }

  private void CloseEditPanel()
  {
    if ((bool)m_editPanel) m_editPanel.SetActive(false);

    if ((bool)m_editSail) m_editSail.EndEdit();

    m_editSail = null;
    GUIManager.BlockInput(false);
  }

  private void CancelEditPanel()
  {
    if ((bool)m_editSail) m_editSail.LoadZDO();

    CloseEditPanel();
  }

  /**
   * Loads the current materials, saves the data, then loads the saved data to match what a persistent load does
   */
  private void SaveEditPanel()
  {
    if ((bool)m_editSail)
    {
      m_editSail.LoadFromMaterial();
      m_editSail.SaveZdo();
      m_editSail.LoadZDO();
    }

    CloseEditPanel();
  }
}