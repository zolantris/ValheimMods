// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.UI.EditSailComponentPanel
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ValheimRAFT.UI
{
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
      this.m_editSail = sailComponent;
      this.m_editSail.StartEdit();
      bool flag = this.m_editSail.m_sailCorners.Count == 3;
      if (!Object.op_Implicit((Object) this.m_editPanel))
        this.InitPanel();
      GameObject gameObject = flag ? this.m_locksTri : this.m_locksQuad;
      this.m_editLockedSailCorners = new Toggle[flag ? 3 : 4];
      this.m_editLockedSailSides = new Toggle[flag ? 3 : 4];
      this.m_locksTri.SetActive(flag);
      this.m_locksQuad.SetActive(!flag);
      this.m_editLockedSailCorners[0] = ((Component) gameObject.transform.Find("CornerA")).GetComponent<Toggle>();
      this.m_editLockedSailCorners[1] = ((Component) gameObject.transform.Find("CornerB")).GetComponent<Toggle>();
      this.m_editLockedSailCorners[2] = ((Component) gameObject.transform.Find("CornerC")).GetComponent<Toggle>();
      if (!flag)
        this.m_editLockedSailCorners[3] = ((Component) gameObject.transform.Find("CornerD")).GetComponent<Toggle>();
      this.m_editLockedSailSides[0] = ((Component) gameObject.transform.Find("SideA")).GetComponent<Toggle>();
      this.m_editLockedSailSides[1] = ((Component) gameObject.transform.Find("SideB")).GetComponent<Toggle>();
      this.m_editLockedSailSides[2] = ((Component) gameObject.transform.Find("SideC")).GetComponent<Toggle>();
      if (!flag)
        this.m_editLockedSailSides[3] = ((Component) gameObject.transform.Find("SideD")).GetComponent<Toggle>();
      for (int index = 0; index < this.m_editLockedSailCorners.Length; ++index)
      {
        this.m_editLockedSailCorners[index].SetIsOnWithoutNotify(this.m_editSail.m_lockedSailCorners.HasFlag((Enum) (SailComponent.SailLockedSide) (1 << index)));
        this.m_editLockedSailSides[index].SetIsOnWithoutNotify(this.m_editSail.m_lockedSailSides.HasFlag((Enum) (SailComponent.SailLockedSide) (1 << index)));
      }
      this.m_sailshrinkingToggle.SetIsOnWithoutNotify(this.m_editSail.m_sailFlags.HasFlag((Enum) SailComponent.SailFlags.AllowSailShrinking));
      this.m_disableClothToggle.SetIsOnWithoutNotify(this.m_editSail.m_sailFlags.HasFlag((Enum) SailComponent.SailFlags.DisableCloth));
      GUIManager.BlockInput(true);
      this.m_editPanel.SetActive(true);
    }

    private void InitPanel()
    {
      Transform transform = GUIManager.CustomGUIFront.transform;
      this.m_editPanel = Object.Instantiate<GameObject>(ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<GameObject>("edit_sail_panel"), transform, false);
      PanelUtil.ApplyPanelStyle(this.m_editPanel);
      GameObject editPanel = ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<GameObject>("edit_texture_panel");
      PanelUtil.ApplyPanelStyle(editPanel);
      GUIManager.Instance.ApplyDropdownStyle(((Component) editPanel.transform.Find("TextureName")).GetComponent<Dropdown>(), 15);
      this.m_editSailPanel = Object.Instantiate<GameObject>(editPanel, transform, false);
      this.m_editPatternPanel = Object.Instantiate<GameObject>(editPanel, transform, false);
      this.m_editLogoPanel = Object.Instantiate<GameObject>(editPanel, transform, false);
      this.m_editSailPanel.SetActive(false);
      this.m_editPatternPanel.SetActive(false);
      this.m_editLogoPanel.SetActive(false);
      // ISSUE: method pointer
      ((UnityEvent) ((Component) this.m_editSailPanel.transform.Find("Button")).GetComponent<Button>().onClick).AddListener(new UnityAction((object) this, __methodptr(\u003CInitPanel\u003Eb__12_0)));
      // ISSUE: method pointer
      ((UnityEvent) ((Component) this.m_editPatternPanel.transform.Find("Button")).GetComponent<Button>().onClick).AddListener(new UnityAction((object) this, __methodptr(\u003CInitPanel\u003Eb__12_1)));
      // ISSUE: method pointer
      ((UnityEvent) ((Component) this.m_editLogoPanel.transform.Find("Button")).GetComponent<Button>().onClick).AddListener(new UnityAction((object) this, __methodptr(\u003CInitPanel\u003Eb__12_2)));
      GameObject gameObject = ((Component) this.m_editPanel.transform.Find("SaveButton")).gameObject;
      // ISSUE: method pointer
      ((UnityEvent) ((Component) this.m_editPanel.transform.Find("CancelButton")).gameObject.GetComponent<Button>().onClick).AddListener(new UnityAction((object) this, __methodptr(CancelEditPanel)));
      // ISSUE: method pointer
      ((UnityEvent) gameObject.GetComponent<Button>().onClick).AddListener(new UnityAction((object) this, __methodptr(SaveEditPanel)));
      this.m_sailshrinkingToggle = ((Component) this.m_editPanel.transform.Find("SailShrinkingToggle")).GetComponent<Toggle>();
      // ISSUE: method pointer
      ((UnityEvent<bool>) this.m_sailshrinkingToggle.onValueChanged).AddListener(new UnityAction<bool>((object) this, __methodptr(\u003CInitPanel\u003Eb__12_3)));
      this.m_disableClothToggle = ((Component) this.m_editPanel.transform.Find("ClothToggle")).GetComponent<Toggle>();
      // ISSUE: method pointer
      ((UnityEvent<bool>) this.m_disableClothToggle.onValueChanged).AddListener(new UnityAction<bool>((object) this, __methodptr(\u003CInitPanel\u003Eb__12_4)));
      this.m_locksTri = ((Component) this.m_editPanel.transform.Find("LocksTri")).gameObject;
      this.m_locksQuad = ((Component) this.m_editPanel.transform.Find("LocksQuad")).gameObject;
      foreach (Toggle componentsInChild in this.m_locksTri.GetComponentsInChildren<Toggle>())
      {
        // ISSUE: method pointer
        ((UnityEvent<bool>) componentsInChild.onValueChanged).AddListener(new UnityAction<bool>((object) this, __methodptr(\u003CInitPanel\u003Eb__12_8)));
      }
      foreach (Toggle componentsInChild in this.m_locksQuad.GetComponentsInChildren<Toggle>())
      {
        // ISSUE: method pointer
        ((UnityEvent<bool>) componentsInChild.onValueChanged).AddListener(new UnityAction<bool>((object) this, __methodptr(\u003CInitPanel\u003Eb__12_9)));
      }
      Button component1 = ((Component) this.m_editPanel.transform.Find("EditSailButton")).GetComponent<Button>();
      Button component2 = ((Component) this.m_editPanel.transform.Find("EditPatternButton")).GetComponent<Button>();
      Button component3 = ((Component) this.m_editPanel.transform.Find("EditLogoButton")).GetComponent<Button>();
      ((Component) this.m_editSailPanel.transform.Find("Rotation")).gameObject.SetActive(false);
      ((Component) this.m_editSailPanel.transform.Find("RotationLabel")).gameObject.SetActive(false);
      // ISSUE: method pointer
      ((UnityEvent) component1.onClick).AddListener(new UnityAction((object) this, __methodptr(\u003CInitPanel\u003Eb__12_5)));
      // ISSUE: method pointer
      ((UnityEvent) component2.onClick).AddListener(new UnityAction((object) this, __methodptr(\u003CInitPanel\u003Eb__12_6)));
      // ISSUE: method pointer
      ((UnityEvent) component3.onClick).AddListener(new UnityAction((object) this, __methodptr(\u003CInitPanel\u003Eb__12_7)));
    }

    private void LoadTexturePanel(
      GameObject editPanel,
      Material mat,
      string parameterName,
      CustomTextureGroup group)
    {
      // ISSUE: object of a compiler-generated type is created
      // ISSUE: variable of a compiler-generated type
      EditSailComponentPanel.\u003C\u003Ec__DisplayClass13_0 cDisplayClass130 = new EditSailComponentPanel.\u003C\u003Ec__DisplayClass13_0();
      // ISSUE: reference to a compiler-generated field
      cDisplayClass130.mat = mat;
      // ISSUE: reference to a compiler-generated field
      cDisplayClass130.parameterName = parameterName;
      // ISSUE: reference to a compiler-generated field
      cDisplayClass130.group = group;
      Button component1 = ((Component) editPanel.transform.Find("TextureColor")).GetComponent<Button>();
      Dropdown component2 = ((Component) editPanel.transform.Find("TextureName")).GetComponent<Dropdown>();
      InputField component3 = ((Component) editPanel.transform.Find("OffsetX")).GetComponent<InputField>();
      InputField component4 = ((Component) editPanel.transform.Find("OffsetY")).GetComponent<InputField>();
      InputField component5 = ((Component) editPanel.transform.Find("TilingX")).GetComponent<InputField>();
      InputField component6 = ((Component) editPanel.transform.Find("TilingY")).GetComponent<InputField>();
      InputField component7 = ((Component) editPanel.transform.Find("Rotation")).GetComponent<InputField>();
      ((UnityEventBase) component1.onClick).RemoveAllListeners();
      ((UnityEventBase) component2.onValueChanged).RemoveAllListeners();
      ((UnityEventBase) component3.onValueChanged).RemoveAllListeners();
      ((UnityEventBase) component4.onValueChanged).RemoveAllListeners();
      ((UnityEventBase) component5.onValueChanged).RemoveAllListeners();
      ((UnityEventBase) component6.onValueChanged).RemoveAllListeners();
      ((UnityEventBase) component7.onValueChanged).RemoveAllListeners();
      List<Dropdown.OptionData> optionDataList = new List<Dropdown.OptionData>();
      // ISSUE: reference to a compiler-generated field
      for (int index = 0; index < cDisplayClass130.group.Textures.Count; ++index)
      {
        // ISSUE: reference to a compiler-generated field
        optionDataList.Add(new Dropdown.OptionData(((Object) cDisplayClass130.group.Textures[index].Texture).name));
      }
      component2.options = optionDataList;
      // ISSUE: method pointer
      ((UnityEvent<int>) component2.onValueChanged).AddListener(new UnityAction<int>((object) cDisplayClass130, __methodptr(\u003CLoadTexturePanel\u003Eb__0)));
      GUIManager.Instance.ApplyDropdownStyle(component2, 15);
      // ISSUE: method pointer
      ((UnityEvent) component1.onClick).AddListener(new UnityAction((object) cDisplayClass130, __methodptr(\u003CLoadTexturePanel\u003Eb__1)));
      // ISSUE: method pointer
      ((UnityEvent<string>) component3.onValueChanged).AddListener(new UnityAction<string>((object) cDisplayClass130, __methodptr(\u003CLoadTexturePanel\u003Eb__2)));
      // ISSUE: method pointer
      ((UnityEvent<string>) component4.onValueChanged).AddListener(new UnityAction<string>((object) cDisplayClass130, __methodptr(\u003CLoadTexturePanel\u003Eb__3)));
      // ISSUE: method pointer
      ((UnityEvent<string>) component5.onValueChanged).AddListener(new UnityAction<string>((object) cDisplayClass130, __methodptr(\u003CLoadTexturePanel\u003Eb__4)));
      // ISSUE: method pointer
      ((UnityEvent<string>) component6.onValueChanged).AddListener(new UnityAction<string>((object) cDisplayClass130, __methodptr(\u003CLoadTexturePanel\u003Eb__5)));
      // ISSUE: method pointer
      ((UnityEvent<string>) component7.onValueChanged).AddListener(new UnityAction<string>((object) cDisplayClass130, __methodptr(\u003CLoadTexturePanel\u003Eb__6)));
      // ISSUE: reference to a compiler-generated field
      // ISSUE: reference to a compiler-generated field
      Texture texture = cDisplayClass130.mat.GetTexture(cDisplayClass130.parameterName + "Tex");
      // ISSUE: reference to a compiler-generated field
      CustomTextureGroup.CustomTexture textureByHash = cDisplayClass130.group.GetTextureByHash(StringExtensionMethods.GetStableHashCode(((Object) texture).name));
      if (textureByHash != null)
        component2.SetValueWithoutNotify(textureByHash.Index);
      // ISSUE: reference to a compiler-generated field
      // ISSUE: reference to a compiler-generated field
      this.UpdateColorButton(component1, cDisplayClass130.mat.GetColor(cDisplayClass130.parameterName + "Color"));
      // ISSUE: reference to a compiler-generated field
      // ISSUE: reference to a compiler-generated field
      Vector2 textureOffset = cDisplayClass130.mat.GetTextureOffset(cDisplayClass130.parameterName + "Tex");
      component3.SetTextWithoutNotify(textureOffset.x.ToString());
      component4.SetTextWithoutNotify(textureOffset.y.ToString());
      // ISSUE: reference to a compiler-generated field
      // ISSUE: reference to a compiler-generated field
      Vector2 textureScale = cDisplayClass130.mat.GetTextureScale(cDisplayClass130.parameterName + "Tex");
      component5.SetTextWithoutNotify(textureScale.x.ToString());
      component6.SetTextWithoutNotify(textureScale.y.ToString());
      if (!((Component) component7).gameObject.activeSelf)
        return;
      // ISSUE: reference to a compiler-generated field
      // ISSUE: reference to a compiler-generated field
      component7.SetTextWithoutNotify(cDisplayClass130.mat.GetFloat(cDisplayClass130.parameterName + "Rotation").ToString());
    }

    private void UpdateColorButton(Button button, Color color)
    {
      color.a = 1f;
      Button button1 = button;
      ColorBlock colorBlock1 = new ColorBlock();
      ((ColorBlock) ref colorBlock1).normalColor = color;
      ((ColorBlock) ref colorBlock1).highlightedColor = color;
      ((ColorBlock) ref colorBlock1).disabledColor = color;
      ((ColorBlock) ref colorBlock1).fadeDuration = 0.1f;
      ((ColorBlock) ref colorBlock1).pressedColor = color;
      ((ColorBlock) ref colorBlock1).selectedColor = color;
      ((ColorBlock) ref colorBlock1).colorMultiplier = 1f;
      ColorBlock colorBlock2 = colorBlock1;
      ((Selectable) button1).colors = colorBlock2;
    }

    private void UpdateSails()
    {
      if (!Object.op_Implicit((Object) this.m_editSail))
        return;
      this.m_editSail.m_lockedSailCorners = SailComponent.SailLockedSide.None;
      for (int index = 0; index < this.m_editLockedSailCorners.Length; ++index)
        this.m_editSail.m_lockedSailCorners |= this.m_editLockedSailCorners[index].isOn ? (SailComponent.SailLockedSide) (1 << index) : SailComponent.SailLockedSide.None;
      this.m_editSail.m_lockedSailSides = SailComponent.SailLockedSide.None;
      for (int index = 0; index < this.m_editLockedSailSides.Length; ++index)
        this.m_editSail.m_lockedSailSides |= this.m_editLockedSailSides[index].isOn ? (SailComponent.SailLockedSide) (1 << index) : SailComponent.SailLockedSide.None;
      this.m_editSail.UpdateCoefficients();
    }

    public void CloseEditPanel()
    {
      if (Object.op_Implicit((Object) this.m_editPanel))
        this.m_editPanel.SetActive(false);
      if (Object.op_Implicit((Object) this.m_editSail))
        this.m_editSail.EndEdit();
      this.m_editSail = (SailComponent) null;
      GUIManager.BlockInput(false);
    }

    public void CancelEditPanel()
    {
      if (Object.op_Implicit((Object) this.m_editSail))
        this.m_editSail.LoadZDO();
      this.CloseEditPanel();
    }

    public void SaveEditPanel()
    {
      if (Object.op_Implicit((Object) this.m_editSail))
      {
        this.m_editSail.LoadFromMaterial();
        this.m_editSail.SaveZDO();
      }
      this.CloseEditPanel();
    }
  }
}
