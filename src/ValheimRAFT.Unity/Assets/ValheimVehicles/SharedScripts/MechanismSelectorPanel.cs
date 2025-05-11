// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using TMPro;
using UnityEngine;
using ValheimVehicles.SharedScripts.Interfaces;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public class MechanismSelectorPanel : SingletonBehaviour<MechanismSelectorPanel>
  {
    [Header("UI Settings")]
    [SerializeField] public float MaxUIWidth = 400f;

    public GameObject panelRoot;
    private IMechanismActionSetter _mechanismActionSetterComponent;
    private TMP_Dropdown actionDropdown;

    public Action<MechanismAction>? OnSelectedActionChanged;
    [SerializeField] public SwivelUISharedStyles viewStyles = new();
    public MechanismAction SelectedAction
    {
      get;
      private set;
    }

    public virtual void BindTo(IMechanismActionSetter mechanismActionSetter, bool isToggle = false)
    {
      _mechanismActionSetterComponent = mechanismActionSetter;
      SelectedAction = mechanismActionSetter.SelectedAction;

      if (panelRoot == null)
        CreateUI();

      actionDropdown.SetValueWithoutNotify((int)SelectedAction);
      if (!isToggle)
      {
        Show();
      }
      else
      {
        Toggle();
      }
    }

    public void Toggle()
    {
      if (panelRoot == null) return;
      if (!panelRoot.activeSelf)
      {
        Show();
        return;
      }
      Hide();
    }

    public void Show()
    {
      if (panelRoot == null) CreateUI();
      panelRoot.SetActive(true);
    }

    public void Hide()
    {
      if (panelRoot != null) panelRoot.SetActive(false);
    }

    public virtual GameObject CreateUIRoot()
    {
      throw new NotImplementedException();
      // var root = SwivelUIHelpers.CreateUICanvas("ToggleUICanvas", transform);
      // return root.gameObject;
    }

    public virtual void CreateUI()
    {
      panelRoot = CreateUIRoot();
      var scrollContainer = SwivelUIHelpers.CreateScrollView(panelRoot.transform, viewStyles, out var scrollRect);
      var viewport = SwivelUIHelpers.CreateViewport(scrollContainer.transform, viewStyles);
      var content = SwivelUIHelpers.CreateContent("Content", viewport.transform, viewStyles, null, null);
      var contentRect = content.GetComponent<RectTransform>();
      scrollRect.content = contentRect;

      SwivelUIHelpers.AddRowWithButton(content.transform, viewStyles, "Mechanism Mode", "X", 24, 24, out _, Hide);

      actionDropdown = SwivelUIHelpers.AddDropdownRow(content.transform, viewStyles, "Mode",
        Enum.GetNames(typeof(MechanismAction)),
        SelectedAction.ToString(),
        index =>
        {
          SelectedAction = (MechanismAction)index;
          _mechanismActionSetterComponent.SetMechanismAction(SelectedAction);
          OnSelectedActionChanged?.Invoke(SelectedAction);
        });
    }
  }
}