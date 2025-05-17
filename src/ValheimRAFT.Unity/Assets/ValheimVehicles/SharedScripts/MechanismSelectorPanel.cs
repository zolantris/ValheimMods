// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.Structs;
using ValheimVehicles.UI;

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
    private TMP_Dropdown swivelSelectorDropdown;

    [SerializeField] public SwivelUISharedStyles viewStyles = new();

    public MechanismAction SelectedAction { get; private set; }
    public SwivelComponent SelectedSwivel { get; private set; }

    public Action<MechanismAction>? OnSelectedActionChanged;
    public Action<SwivelComponent>? OnSwivelSelectedChanged;

    public virtual void BindTo(IMechanismActionSetter mechanismActionSetter, bool isToggle = false)
    {
      _mechanismActionSetterComponent = mechanismActionSetter;
      SelectedAction = mechanismActionSetter.SelectedAction;

      if (panelRoot == null)
        CreateUI();

      if (SelectedAction == MechanismAction.SwivelActivateMode || SelectedAction == MechanismAction.SwivelEditMode)
      {
        UpdateSwivelDropdown();
      }

      HideShowSwivelFinder();

      actionDropdown.SetValueWithoutNotify((int)SelectedAction);

      if (!isToggle) Show();
      else Toggle();
    }

    public void Toggle()
    {
      if (panelRoot == null) return;
      if (!panelRoot.activeSelf) Show();
      else Hide();
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
    }

    public void HideShowSwivelFinder()
    {
      var show = SelectedAction == MechanismAction.SwivelActivateMode || SelectedAction == MechanismAction.SwivelEditMode;
      swivelSelectorDropdown.gameObject.SetActive(show);
    }

    public void UpdateSwivelDropdown()
    {
      if (_mechanismActionSetterComponent == null) return;

      var selfPos = _mechanismActionSetterComponent.transform.position;

      var options = _mechanismActionSetterComponent.NearestSwivels
        .Select(x =>
        {
          var dist = Vector3.Distance(selfPos, x.transform.position);
          return $"({dist:F1}m) {x.swivelPowerConsumer.NetworkId.Substring(0, 5)}...";
        }).ToList();

      swivelSelectorDropdown.ClearOptions();
      swivelSelectorDropdown.AddOptions(options);

      if (_mechanismActionSetterComponent.NearestSwivels.Count > 0)
      {
        swivelSelectorDropdown.SetValueWithoutNotify(0);
        SelectedSwivel = _mechanismActionSetterComponent.NearestSwivels[0];
        _mechanismActionSetterComponent.SetMechanismSwivel(SelectedSwivel);
        OnSwivelSelectedChanged?.Invoke(SelectedSwivel);
      }
    }

    public virtual void CreateUI()
    {
      panelRoot = CreateUIRoot();
      var scrollContainer = SwivelUIHelpers.CreateScrollView(panelRoot.transform, viewStyles, out var scrollRect);
      var viewport = SwivelUIHelpers.CreateViewport(scrollContainer.transform, viewStyles);
      var content = SwivelUIHelpers.CreateContent("Content", viewport.transform, viewStyles, null, null);
      scrollRect.content = content.GetComponent<RectTransform>();

      SwivelUIHelpers.AddRowWithButton(content.transform, viewStyles, "Mechanism Mode", "X", 24, 24, out _, Hide);

      actionDropdown = SwivelUIHelpers.AddDropdownRow(content.transform, viewStyles, "Mode",
        Enum.GetNames(typeof(MechanismAction)),
        SelectedAction.ToString(),
        index =>
        {
          SelectedAction = (MechanismAction)index;
          _mechanismActionSetterComponent.SetMechanismAction(SelectedAction);

          if (SelectedAction == MechanismAction.SwivelActivateMode || SelectedAction == MechanismAction.SwivelEditMode)
          {
            UpdateSwivelDropdown();
          }

          OnSelectedActionChanged?.Invoke(SelectedAction);
          HideShowSwivelFinder();
        });

      // var dropdownAction = new GenericInputAction
      // {
      //   title = "Open Save Vehicle Selector",
      //   // action = VehicleGui.OpenVehicleSelectorGUi,
      //   inputType = InputType.Dropdown,
      //   OnDropdownChanged = (int index) =>
      //   {
      //     if (index < 0 || index >= NearestSwivels.Count) return;
      //     SelectedSwivel = NearestSwivels[index];
      //     _mechanismActionSetterComponent.SetMechanismSwivel(SelectedSwivel);
      //     OnSwivelSelectedChanged?.Invoke(SelectedSwivel);
      //   }
      //   // OnPointerEnterAction = VehicleStorageAPI.RefreshVehicleSelectionGui
      // };
      // swivelSelectorDropdown = VehicleGui.AddDropdownWithAction(dropdownAction, 0, 48, content.transform).GetComponent<TMP_Dropdown>();
      var selfPos = _mechanismActionSetterComponent.transform.position;

      var options = _mechanismActionSetterComponent.NearestSwivels
        .Select(x =>
        {
          var dist = Vector3.Distance(selfPos, x.transform.position);
          return $"({dist:F1}m) NetworkId: {x.swivelPowerConsumer.NetworkId.Substring(0, 5)}...";
        }).ToArray();

      swivelSelectorDropdown = SwivelUIHelpers.AddDropdownRow(content.transform, viewStyles, "Selected Swivel",
        options,
        options?.FirstOrDefault() ?? "",
        index =>
        {
          if (index < 0 || index >= _mechanismActionSetterComponent.NearestSwivels.Count) return;
          SelectedSwivel = _mechanismActionSetterComponent.NearestSwivels[index];
          _mechanismActionSetterComponent.SetMechanismSwivel(SelectedSwivel);
          OnSwivelSelectedChanged?.Invoke(SelectedSwivel);
        });

      HideShowSwivelFinder();
    }
  }
}