// ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ValheimRAFT.UI.EditSailComponentPanel

using System.Collections.Generic;
using Jotunn.GUI;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ValheimRAFT;
using ValheimRAFT.UI;

public class EditSailComponentPanel
{
  private GameObject m_editPanel;

  private GameObject m_editSailPanel;

  private GameObject m_editPatternPanel;

  private GameObject m_editLogoPanel;

  private SailComponent m_editSail;

  private Toggle[] m_editLockedSailSides;

  private Toggle[] m_editLockedSailCorners;

  private Toggle m_sailshrinkingToggle;

  private GameObject m_locksTri;

  private GameObject m_locksQuad;

  private Toggle m_disableClothToggle;

  public void ShowPanel(SailComponent sailComponent)
  {
    m_editSail = sailComponent;
    m_editSail.StartEdit();
    bool isTri = m_editSail.m_sailCorners.Count == 3;
    if (!m_editPanel)
    {
      InitPanel();
    }

    GameObject locksArea = (isTri ? m_locksTri : m_locksQuad);
    m_editLockedSailCorners = (Toggle[])(object)new Toggle[isTri ? 3 : 4];
    m_editLockedSailSides = (Toggle[])(object)new Toggle[isTri ? 3 : 4];
    m_locksTri.SetActive(isTri);
    m_locksQuad.SetActive(!isTri);
    m_editLockedSailCorners[0] =
      ((Component)locksArea.transform.Find("CornerA")).GetComponent<Toggle>();
    m_editLockedSailCorners[1] =
      ((Component)locksArea.transform.Find("CornerB")).GetComponent<Toggle>();
    m_editLockedSailCorners[2] =
      ((Component)locksArea.transform.Find("CornerC")).GetComponent<Toggle>();
    if (!isTri)
    {
      m_editLockedSailCorners[3] =
        ((Component)locksArea.transform.Find("CornerD")).GetComponent<Toggle>();
    }

    m_editLockedSailSides[0] =
      ((Component)locksArea.transform.Find("SideA")).GetComponent<Toggle>();
    m_editLockedSailSides[1] =
      ((Component)locksArea.transform.Find("SideB")).GetComponent<Toggle>();
    m_editLockedSailSides[2] =
      ((Component)locksArea.transform.Find("SideC")).GetComponent<Toggle>();
    if (!isTri)
    {
      m_editLockedSailSides[3] =
        ((Component)locksArea.transform.Find("SideD")).GetComponent<Toggle>();
    }

    for (int i = 0; i < m_editLockedSailCorners.Length; i++)
    {
      m_editLockedSailCorners[i]
        .SetIsOnWithoutNotify(
          m_editSail.m_lockedSailCorners.HasFlag((SailComponent.SailLockedSide)(1 << i)));
      m_editLockedSailSides[i]
        .SetIsOnWithoutNotify(
          m_editSail.m_lockedSailSides.HasFlag((SailComponent.SailLockedSide)(1 << i)));
    }

    m_sailshrinkingToggle.SetIsOnWithoutNotify(
      m_editSail.m_sailFlags.HasFlag(SailComponent.SailFlags.AllowSailShrinking));
    m_disableClothToggle.SetIsOnWithoutNotify(
      m_editSail.m_sailFlags.HasFlag(SailComponent.SailFlags.DisableCloth));
    GUIManager.BlockInput(true);
    m_editPanel.SetActive(true);
  }

  private void InitPanel()
  {
    //IL_00e4: Unknown result type (might be due to invalid IL or missing references)
    //IL_00ee: Expected O, but got Unknown
    //IL_0115: Unknown result type (might be due to invalid IL or missing references)
    //IL_011f: Expected O, but got Unknown
    //IL_0146: Unknown result type (might be due to invalid IL or missing references)
    //IL_0150: Expected O, but got Unknown
    //IL_0199: Unknown result type (might be due to invalid IL or missing references)
    //IL_01a3: Expected O, but got Unknown
    //IL_01b6: Unknown result type (might be due to invalid IL or missing references)
    //IL_01c0: Expected O, but got Unknown
    //IL_03a5: Unknown result type (might be due to invalid IL or missing references)
    //IL_03af: Expected O, but got Unknown
    //IL_03be: Unknown result type (might be due to invalid IL or missing references)
    //IL_03c8: Expected O, but got Unknown
    //IL_03d7: Unknown result type (might be due to invalid IL or missing references)
    //IL_03e1: Expected O, but got Unknown
    Transform parent = GUIManager.CustomGUIFront.transform;
    m_editPanel = Object.Instantiate<GameObject>(
      global::ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<GameObject>("edit_sail_panel"),
      parent, false);
    PanelUtil.ApplyPanelStyle(m_editPanel);
    GameObject texture_panel =
      global::ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<GameObject>("edit_texture_panel");
    PanelUtil.ApplyPanelStyle(texture_panel);
    GUIManager.Instance.ApplyDropdownStyle(
      ((Component)texture_panel.transform.Find("TextureName")).GetComponent<Dropdown>(), 15);
    m_editSailPanel = Object.Instantiate<GameObject>(texture_panel, parent, false);
    m_editPatternPanel = Object.Instantiate<GameObject>(texture_panel, parent, false);
    m_editLogoPanel = Object.Instantiate<GameObject>(texture_panel, parent, false);
    m_editSailPanel.SetActive(false);
    m_editPatternPanel.SetActive(false);
    m_editLogoPanel.SetActive(false);
    ((UnityEvent)((Component)m_editSailPanel.transform.Find("Button")).GetComponent<Button>()
      .onClick).AddListener((UnityAction)delegate
    {
      m_editSailPanel.SetActive(false);
      m_editPanel.SetActive(true);
    });
    ((UnityEvent)((Component)m_editPatternPanel.transform.Find("Button")).GetComponent<Button>()
      .onClick).AddListener((UnityAction)delegate
    {
      m_editPatternPanel.SetActive(false);
      m_editPanel.SetActive(true);
    });
    ((UnityEvent)((Component)m_editLogoPanel.transform.Find("Button")).GetComponent<Button>()
      .onClick).AddListener((UnityAction)delegate
    {
      m_editLogoPanel.SetActive(false);
      m_editPanel.SetActive(true);
    });
    GameObject saveObject = ((Component)m_editPanel.transform.Find("SaveButton")).gameObject;
    GameObject cancelObject = ((Component)m_editPanel.transform.Find("CancelButton")).gameObject;
    ((UnityEvent)cancelObject.GetComponent<Button>().onClick).AddListener(
      new UnityAction(CancelEditPanel));
    ((UnityEvent)saveObject.GetComponent<Button>().onClick).AddListener(
      new UnityAction(SaveEditPanel));
    m_sailshrinkingToggle = ((Component)m_editPanel.transform.Find("SailShrinkingToggle"))
      .GetComponent<Toggle>();
    ((UnityEvent<bool>)(object)m_sailshrinkingToggle.onValueChanged).AddListener(
      (UnityAction<bool>)delegate(bool b) { m_editSail.SetAllowSailShrinking(b); });
    m_disableClothToggle =
      ((Component)m_editPanel.transform.Find("ClothToggle")).GetComponent<Toggle>();
    ((UnityEvent<bool>)(object)m_disableClothToggle.onValueChanged).AddListener(
      (UnityAction<bool>)delegate(bool b) { m_editSail.SetDisableCloth(b); });
    m_locksTri = ((Component)m_editPanel.transform.Find("LocksTri")).gameObject;
    m_locksQuad = ((Component)m_editPanel.transform.Find("LocksQuad")).gameObject;
    Toggle[] componentsInChildren = m_locksTri.GetComponentsInChildren<Toggle>();
    foreach (Toggle toggle2 in componentsInChildren)
    {
      ((UnityEvent<bool>)(object)toggle2.onValueChanged).AddListener((UnityAction<bool>)delegate
      {
        UpdateSails();
      });
    }

    Toggle[] componentsInChildren2 = m_locksQuad.GetComponentsInChildren<Toggle>();
    foreach (Toggle toggle in componentsInChildren2)
    {
      ((UnityEvent<bool>)(object)toggle.onValueChanged).AddListener((UnityAction<bool>)delegate
      {
        UpdateSails();
      });
    }

    Button editSailButton =
      ((Component)m_editPanel.transform.Find("EditSailButton")).GetComponent<Button>();
    Button editPatternButton = ((Component)m_editPanel.transform.Find("EditPatternButton"))
      .GetComponent<Button>();
    Button editLogoButton =
      ((Component)m_editPanel.transform.Find("EditLogoButton")).GetComponent<Button>();
    ((Component)m_editSailPanel.transform.Find("Rotation")).gameObject.SetActive(false);
    ((Component)m_editSailPanel.transform.Find("RotationLabel")).gameObject.SetActive(false);
    ((UnityEvent)editSailButton.onClick).AddListener((UnityAction)delegate
    {
      m_editSailPanel.SetActive(true);
      m_editPatternPanel.SetActive(false);
      m_editLogoPanel.SetActive(false);
      m_editPanel.SetActive(false);
      LoadTexturePanel(m_editSailPanel, m_editSail.GetSailMaterial(), "_Main",
        CustomTextureGroup.Get("Sails"));
    });
    ((UnityEvent)editPatternButton.onClick).AddListener((UnityAction)delegate
    {
      m_editSailPanel.SetActive(false);
      m_editPatternPanel.SetActive(true);
      m_editLogoPanel.SetActive(false);
      m_editPanel.SetActive(false);
      LoadTexturePanel(m_editPatternPanel, m_editSail.GetSailMaterial(), "_Pattern",
        CustomTextureGroup.Get("Patterns"));
    });
    ((UnityEvent)editLogoButton.onClick).AddListener((UnityAction)delegate
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
    //IL_013e: Unknown result type (might be due to invalid IL or missing references)
    //IL_0148: Expected O, but got Unknown
    //IL_01a6: Unknown result type (might be due to invalid IL or missing references)
    //IL_01b0: Expected O, but got Unknown
    //IL_0296: Unknown result type (might be due to invalid IL or missing references)
    //IL_02b7: Unknown result type (might be due to invalid IL or missing references)
    //IL_02bc: Unknown result type (might be due to invalid IL or missing references)
    //IL_02fb: Unknown result type (might be due to invalid IL or missing references)
    //IL_0300: Unknown result type (might be due to invalid IL or missing references)
    Button textureColor =
      ((Component)editPanel.transform.Find("TextureColor")).GetComponent<Button>();
    Dropdown textureDropdown =
      ((Component)editPanel.transform.Find("TextureName")).GetComponent<Dropdown>();
    InputField offsetx =
      ((Component)editPanel.transform.Find("OffsetX")).GetComponent<InputField>();
    InputField offsety =
      ((Component)editPanel.transform.Find("OffsetY")).GetComponent<InputField>();
    InputField scalex = ((Component)editPanel.transform.Find("TilingX")).GetComponent<InputField>();
    InputField scaley = ((Component)editPanel.transform.Find("TilingY")).GetComponent<InputField>();
    InputField rot = ((Component)editPanel.transform.Find("Rotation")).GetComponent<InputField>();
    ((UnityEventBase)textureColor.onClick).RemoveAllListeners();
    ((UnityEventBase)textureDropdown.onValueChanged).RemoveAllListeners();
    ((UnityEventBase)offsetx.onValueChanged).RemoveAllListeners();
    ((UnityEventBase)offsety.onValueChanged).RemoveAllListeners();
    ((UnityEventBase)scalex.onValueChanged).RemoveAllListeners();
    ((UnityEventBase)scaley.onValueChanged).RemoveAllListeners();
    ((UnityEventBase)rot.onValueChanged).RemoveAllListeners();
    List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
    for (int i = 0; i < group.Textures.Count; i++)
    {
      options.Add(new Dropdown.OptionData(((Object)group.Textures[i].Texture).name));
    }

    textureDropdown.options = options;
    ((UnityEvent<int>)(object)textureDropdown.onValueChanged).AddListener(
      (UnityAction<int>)delegate(int index)
      {
        mat.SetTexture(parameterName + "Tex", group.Textures[index].Texture);
        if (group.Textures[index].Normal)
        {
          mat.SetTexture(parameterName + "Normal", group.Textures[index].Normal);
        }
      });
    GUIManager.Instance.ApplyDropdownStyle(textureDropdown, 15);
    ColorPicker.ColorEvent val = default(ColorPicker.ColorEvent);
    ColorPicker.ColorEvent val2 = default(ColorPicker.ColorEvent);
    ((UnityEvent)textureColor.onClick).AddListener((UnityAction)delegate
    {
      //IL_0010: Unknown result type (might be due to invalid IL or missing references)
      //IL_001f: Unknown result type (might be due to invalid IL or missing references)
      //IL_002e: Unknown result type (might be due to invalid IL or missing references)
      //IL_0049: Unknown result type (might be due to invalid IL or missing references)
      //IL_0065: Unknown result type (might be due to invalid IL or missing references)
      //IL_006a: Unknown result type (might be due to invalid IL or missing references)
      //IL_006c: Expected O, but got Unknown
      //IL_0071: Expected O, but got Unknown
      //IL_0084: Unknown result type (might be due to invalid IL or missing references)
      //IL_0089: Unknown result type (might be due to invalid IL or missing references)
      //IL_008b: Expected O, but got Unknown
      //IL_0090: Expected O, but got Unknown
      GUIManager instance = GUIManager.Instance;
      Vector2 val3 = new Vector2(0.5f, 0.5f);
      Vector2 val4 = new Vector2(0.5f, 0.5f);
      Vector2 val5 = new Vector2(0.5f, 0.5f);
      Color color2 = mat.GetColor(parameterName + "Color");
      ColorPicker.ColorEvent obj = val;
      if (obj == null)
      {
        ColorPicker.ColorEvent val6 = delegate(Color color)
        {
          //IL_0017: Unknown result type (might be due to invalid IL or missing references)
          mat.SetColor(parameterName + "Color", color);
        };
        ColorPicker.ColorEvent val7 = val6;
        val = val6;
        obj = val7;
      }

      ColorPicker.ColorEvent obj2 = val2;
      if (obj2 == null)
      {
        ColorPicker.ColorEvent val8 = delegate(Color color)
        {
          //IL_0017: Unknown result type (might be due to invalid IL or missing references)
          mat.SetColor(parameterName + "Color", color);
        };
        ColorPicker.ColorEvent val7 = val8;
        val2 = val8;
        obj2 = val7;
      }

      instance.CreateColorPicker(val3, val4, val5, color2, "Color", obj, obj2, true);
    });
    ((UnityEvent<string>)(object)offsetx.onValueChanged).AddListener(
      (UnityAction<string>)delegate(string str)
      {
        //IL_0024: Unknown result type (might be due to invalid IL or missing references)
        //IL_0029: Unknown result type (might be due to invalid IL or missing references)
        //IL_0048: Unknown result type (might be due to invalid IL or missing references)
        if (float.TryParse(str, out var result5))
        {
          Vector2 textureOffset2 = mat.GetTextureOffset(parameterName + "Tex");
          textureOffset2.x = result5;
          mat.SetTextureOffset(parameterName + "Tex", textureOffset2);
        }
      });
    ((UnityEvent<string>)(object)offsety.onValueChanged).AddListener(
      (UnityAction<string>)delegate(string str)
      {
        //IL_0024: Unknown result type (might be due to invalid IL or missing references)
        //IL_0029: Unknown result type (might be due to invalid IL or missing references)
        //IL_0048: Unknown result type (might be due to invalid IL or missing references)
        if (float.TryParse(str, out var result4))
        {
          Vector2 textureOffset = mat.GetTextureOffset(parameterName + "Tex");
          textureOffset.y = result4;
          mat.SetTextureOffset(parameterName + "Tex", textureOffset);
        }
      });
    ((UnityEvent<string>)(object)scalex.onValueChanged).AddListener(
      (UnityAction<string>)delegate(string str)
      {
        //IL_0024: Unknown result type (might be due to invalid IL or missing references)
        //IL_0029: Unknown result type (might be due to invalid IL or missing references)
        //IL_0048: Unknown result type (might be due to invalid IL or missing references)
        if (float.TryParse(str, out var result3))
        {
          Vector2 textureScale2 = mat.GetTextureScale(parameterName + "Tex");
          textureScale2.x = result3;
          mat.SetTextureScale(parameterName + "Tex", textureScale2);
        }
      });
    ((UnityEvent<string>)(object)scaley.onValueChanged).AddListener(
      (UnityAction<string>)delegate(string str)
      {
        //IL_0024: Unknown result type (might be due to invalid IL or missing references)
        //IL_0029: Unknown result type (might be due to invalid IL or missing references)
        //IL_0048: Unknown result type (might be due to invalid IL or missing references)
        if (float.TryParse(str, out var result2))
        {
          Vector2 textureScale = mat.GetTextureScale(parameterName + "Tex");
          textureScale.y = result2;
          mat.SetTextureScale(parameterName + "Tex", textureScale);
        }
      });
    ((UnityEvent<string>)(object)rot.onValueChanged).AddListener(
      (UnityAction<string>)delegate(string str)
      {
        if (float.TryParse(str, out var result))
        {
          mat.SetFloat(parameterName + "Rotation", result);
        }
      });
    Texture tex = mat.GetTexture(parameterName + "Tex");
    CustomTextureGroup.CustomTexture customtex =
      group.GetTextureByHash(StringExtensionMethods.GetStableHashCode(((Object)tex).name));
    if (customtex != null)
    {
      textureDropdown.SetValueWithoutNotify(customtex.Index);
    }

    UpdateColorButton(textureColor, mat.GetColor(parameterName + "Color"));
    Vector2 offset = mat.GetTextureOffset(parameterName + "Tex");
    offsetx.SetTextWithoutNotify(offset.x.ToString());
    offsety.SetTextWithoutNotify(offset.y.ToString());
    Vector2 scale = mat.GetTextureScale(parameterName + "Tex");
    scalex.SetTextWithoutNotify(scale.x.ToString());
    scaley.SetTextWithoutNotify(scale.y.ToString());
    if (((Component)rot).gameObject.activeSelf)
    {
      rot.SetTextWithoutNotify(mat.GetFloat(parameterName + "Rotation").ToString());
    }
  }

  private void UpdateColorButton(Button button, Color color)
  {
    //IL_0010: Unknown result type (might be due to invalid IL or missing references)
    //IL_0018: Unknown result type (might be due to invalid IL or missing references)
    //IL_0021: Unknown result type (might be due to invalid IL or missing references)
    //IL_002a: Unknown result type (might be due to invalid IL or missing references)
    //IL_0040: Unknown result type (might be due to invalid IL or missing references)
    //IL_0049: Unknown result type (might be due to invalid IL or missing references)
    //IL_005d: Unknown result type (might be due to invalid IL or missing references)
    color.a = 1f;
    ColorBlock colors = default(ColorBlock);
    colors.normalColor = color;
    colors.highlightedColor = color;
    colors.disabledColor = color;
    colors.fadeDuration = 0.1f;
    colors.pressedColor = color;
    colors.selectedColor = color;
    colors.colorMultiplier = 1f;
    ((Selectable)button).colors = colors;
  }

  private void UpdateSails()
  {
    if (m_editSail)
    {
      m_editSail.m_lockedSailCorners = SailComponent.SailLockedSide.None;
      for (int i = 0; i < m_editLockedSailCorners.Length; i++)
      {
        m_editSail.m_lockedSailCorners |=
          (SailComponent.SailLockedSide)(m_editLockedSailCorners[i].isOn ? (1 << i) : 0);
      }

      m_editSail.m_lockedSailSides = SailComponent.SailLockedSide.None;
      for (int j = 0; j < m_editLockedSailSides.Length; j++)
      {
        m_editSail.m_lockedSailSides |=
          (SailComponent.SailLockedSide)(m_editLockedSailSides[j].isOn ? (1 << j) : 0);
      }

      m_editSail.UpdateCoefficients();
    }
  }

  public void CloseEditPanel()
  {
    if (m_editPanel)
    {
      m_editPanel.SetActive(false);
    }

    if (m_editSail)
    {
      m_editSail.EndEdit();
    }

    m_editSail = null;
    GUIManager.BlockInput(false);
  }

  public void CancelEditPanel()
  {
    if (m_editSail)
    {
      m_editSail.LoadZDO();
    }

    CloseEditPanel();
  }

  public void SaveEditPanel()
  {
    if (m_editSail)
    {
      m_editSail.LoadFromMaterial();
      m_editSail.SaveZDO();
    }

    CloseEditPanel();
  }
}