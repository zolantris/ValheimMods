// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using TMPro;
using UnityEngine;
using ValheimVehicles.Interfaces;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public class MechanismSelectorPanel : SingletonBehaviour<MechanismSelectorPanel>
  {
    [Header("UI Settings")]
    [SerializeField] public float MaxUIWidth = 400f;
    private TMP_Dropdown actionDropdown;

    internal GameObject panelRoot;
    [SerializeField] public SwivelUISharedStyles viewStyles = new();
    private IMechanismActionSetter _mechanismActionSetterComponent;
    public MechanismAction SelectedAction
    {
      get;
      private set;
    }
#if UNITY_EDITOR
    public override void Awake()
    {
      base.Awake();
      BindTo(MechanismActions.SwivelEditMode);
    }
#endif

    public Action<MechanismAction>? OnSelectedActionChanged;

    public virtual void BindTo(MechanismAction current, IMechanismActionSetter mechanismActionSetter)
    {
      _mechanismActionSetterComponent = mechanismActionSetter;
      SelectedAction = current;

      if (panelRoot == null)
        CreateUI();

      actionDropdown.SetValueWithoutNotify((int)SelectedAction);
      Show();
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
      var root = SwivelUIHelpers.CreateUICanvas("ToggleUICanvas", transform);
      return root.gameObject;
    }

    private void CreateUI()
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