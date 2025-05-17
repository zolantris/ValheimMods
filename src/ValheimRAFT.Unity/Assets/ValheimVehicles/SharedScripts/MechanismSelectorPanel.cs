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
    public IMechanismActionSetter mechanismAction;
    private TMP_Dropdown actionDropdown;
    private TMP_Dropdown swivelSelectorDropdown;

    [SerializeField] public SwivelUISharedStyles viewStyles = new();

    public MechanismAction SelectedAction { get; set; }
    public SwivelComponent? SelectedSwivel { get; set; }

    public Action<MechanismAction>? OnSelectedActionChanged;
    public Action<SwivelComponent>? OnSwivelSelectedChanged;

    public virtual void BindTo(IMechanismActionSetter mechanismActionSetter, bool isToggle = false)
    {
      mechanismAction = mechanismActionSetter;
      SelectedAction = mechanismActionSetter.SelectedAction;

      var isFirstCreate = !panelRoot;
      if (isFirstCreate)
        CreateUI();

      if (SelectedAction == MechanismAction.SwivelActivateMode || SelectedAction == MechanismAction.SwivelEditMode)
      {
        UpdateSwivelDropdown();
      }

      HideShowSwivelFinder();

      actionDropdown.SetValueWithoutNotify((int)SelectedAction);

      if (!isToggle || isFirstCreate) Show();
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
      if (mechanismAction == null) return;

      var selfPos = mechanismAction.transform.position;

      var options = mechanismAction.NearestSwivels
        .Select(x =>
        {
          var dist = Vector3.Distance(selfPos, x.transform.position);
          return $"({dist:F1}m) {x.swivelPowerConsumer.NetworkId.Substring(0, 5)}...";
        }).ToList();

      swivelSelectorDropdown.ClearOptions();
      swivelSelectorDropdown.AddOptions(options);

      if (mechanismAction.NearestSwivels.Count > 0)
      {
        var actionInt = 0;
        if (mechanismAction.TargetSwivel != null)
        {
          for (var index = 0; index < mechanismAction.NearestSwivels.Count; index++)
          {
            var mechanismActionNearestSwivel = mechanismAction.NearestSwivels[index];
            if (mechanismActionNearestSwivel == null) continue;
            if (mechanismAction.TargetSwivel == mechanismActionNearestSwivel)
            {
              actionInt = index;
              break;
            }
          }
        }
        swivelSelectorDropdown.SetValueWithoutNotify(actionInt);
        SelectedSwivel = mechanismAction.NearestSwivels[actionInt];

        if (SelectedSwivel != mechanismAction.TargetSwivel)
        {
          mechanismAction.SetMechanismSwivel(SelectedSwivel);
        }
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
          mechanismAction.SetMechanismAction(SelectedAction);

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
      var selfPos = mechanismAction.transform.position;

      var options = mechanismAction.NearestSwivels
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
          if (index < 0 || index >= mechanismAction.NearestSwivels.Count) return;
          SelectedSwivel = mechanismAction.NearestSwivels[index];
          mechanismAction.SetMechanismSwivel(SelectedSwivel);
          OnSwivelSelectedChanged?.Invoke(SelectedSwivel);
        });

      HideShowSwivelFinder();
    }
  }
}