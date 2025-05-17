// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Linq;
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
    public GameObject panelContent;
    public IMechanismActionSetter? mechanismAction;
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
        AddOrUpdateSwivelDropdown();
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

    public void AddOrUpdateSwivelDropdown()
    {
      if (mechanismAction == null) return;

      var selfPos = mechanismAction.transform.position;

      var nearestSwivels = mechanismAction.GetNearestSwivels();
      var options = nearestSwivels.Where(x => x != null)
        .Select(x =>
        {
          var dist = Vector3.Distance(selfPos, x.transform.position);
          return $"({dist:F1}m) {x.swivelPowerConsumer.NetworkId.Substring(0, 5)}...";
        }).ToList();

      if (!swivelSelectorDropdown)
      {
        swivelSelectorDropdown = SwivelUIHelpers.AddDropdownRow(panelContent.transform, viewStyles, "Selected Swivel",
          options.ToArray(),
          options.FirstOrDefault() ?? "None",
          index =>
          {
            if (index < 0 || index >= nearestSwivels.Count) return;
            SelectedSwivel = nearestSwivels[index];
            mechanismAction.SetMechanismSwivel(SelectedSwivel);
            OnSwivelSelectedChanged?.Invoke(SelectedSwivel);
          });
      }
      else
      {
        swivelSelectorDropdown.ClearOptions();
        swivelSelectorDropdown.AddOptions(options);
      }

      if (nearestSwivels.Count > 0)
      {
        var actionInt = 0;
        if (mechanismAction.TargetSwivel != null)
        {
          for (var index = 0; index < nearestSwivels.Count; index++)
          {
            var mechanismActionNearestSwivel = nearestSwivels[index];
            if (mechanismAction.TargetSwivel == mechanismActionNearestSwivel)
            {
              actionInt = index;
              break;
            }
          }
        }
        swivelSelectorDropdown.SetValueWithoutNotify(actionInt);
        SelectedSwivel = nearestSwivels[actionInt];

        if (SelectedSwivel != mechanismAction.TargetSwivel)
        {
          mechanismAction.SetMechanismSwivel(SelectedSwivel);
        }
        OnSwivelSelectedChanged?.Invoke(SelectedSwivel);

        swivelSelectorDropdown.RefreshShownValue();
      }
    }

    public virtual void CreateUI()
    {
      if (mechanismAction == null)
      {
        LoggerProvider.LogWarning("Failure to bind MechanismPanel correctly. IMechanismAction is null.");
        return;
      }

      panelRoot = CreateUIRoot();
      var scrollContainer = SwivelUIHelpers.CreateScrollView(panelRoot.transform, viewStyles, out var scrollRect);
      var viewport = SwivelUIHelpers.CreateViewport(scrollContainer.transform, viewStyles);
      panelContent = SwivelUIHelpers.CreateContent("Content", viewport.transform, viewStyles, null, null);
      scrollRect.content = panelContent.GetComponent<RectTransform>();

      SwivelUIHelpers.AddRowWithButton(panelContent.transform, viewStyles, "Mechanism Mode", "X", 24, 24, out _, Hide);

      actionDropdown = SwivelUIHelpers.AddDropdownRow(panelContent.transform, viewStyles, "Mode",
        Enum.GetNames(typeof(MechanismAction)),
        SelectedAction.ToString(),
        index =>
        {
          SelectedAction = (MechanismAction)index;
          mechanismAction.SetMechanismAction(SelectedAction);

          if (SelectedAction == MechanismAction.SwivelActivateMode || SelectedAction == MechanismAction.SwivelEditMode)
          {
            AddOrUpdateSwivelDropdown();
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

      AddOrUpdateSwivelDropdown();

      HideShowSwivelFinder();
    }
  }
}