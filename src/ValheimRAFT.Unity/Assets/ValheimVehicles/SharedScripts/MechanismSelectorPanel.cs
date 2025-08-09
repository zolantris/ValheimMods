// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle


using Zolantris.Shared;
#if !UNITY_2022
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using ValheimVehicles.SharedScripts.Interfaces;
#endif
namespace ValheimVehicles.SharedScripts.UI
{
  public class MechanismSelectorPanel : SingletonBehaviour<MechanismSelectorPanel>
  {
#if !UNITY_2022
    [Header("UI Settings")]
    [SerializeField] public float MaxUIWidth = 400f;

    public GameObject panelRoot;
    public GameObject panelContent;
    public bool IsEditing;

    public List<SwivelComponent> nearestSwivels = new();

    internal MechanismSwitchCustomConfig _currentPanelConfig = new();
    private TextMeshProUGUI _saveStatus;
    internal TMP_Dropdown actionDropdown;
    public IMechanismActionSetter? mechanismAction;

    public Action<MechanismAction>? OnSelectedActionChanged;
    public Action<SwivelComponent?>? OnSwivelSelectedChanged;
    internal TMP_Dropdown swivelSelectorDropdown;

    [SerializeField] public SwivelUISharedStyles viewStyles = new();

    public SwivelComponent? SelectedSwivel { get; set; }

    public virtual void BindTo(IMechanismActionSetter mechanismActionSetter, bool isToggle = false)
    {
      mechanismAction = mechanismActionSetter;
      _currentPanelConfig = new MechanismSwitchCustomConfig();
      _currentPanelConfig.ApplyFrom(mechanismAction);

      var isFirstCreate = !panelRoot;
      if (isFirstCreate)
        CreateUI();

      HideShowSwivelFinder();
      AddOrUpdateSwivelDropdown();

      actionDropdown.SetValueWithoutNotify((int)_currentPanelConfig.SelectedAction);

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
      if (panelRoot == null) return;
      panelRoot.SetActive(true);
    }

    public void Hide()
    {
      if (panelRoot != null) panelRoot.SetActive(false);
    }

    public virtual GameObject CreateUIRoot()
    {
      LoggerProvider.LogWarning("Not implemented");
      return null;
    }

    public void HideShowSwivelFinder()
    {
      if (!panelRoot || !swivelSelectorDropdown)
      {
        return;
      }
      var show = _currentPanelConfig.SelectedAction == MechanismAction.SwivelActivateMode || _currentPanelConfig.SelectedAction == MechanismAction.SwivelEditMode;
      swivelSelectorDropdown.gameObject.SetActive(show);
    }

    public void OnMechanismSwivelSelected(int index)
    {
      // Index has a unset option so it can be +1 nearestSwivels.Count
      if (index < 0 || index > nearestSwivels.Count) return;

      // This is the unset option.
      if (index == 0)
      {
        SelectedSwivel = null;
        _currentPanelConfig.TargetSwivelId = 0;

        UnsetSavedState();
        OnSwivelSelectedChanged?.Invoke(SelectedSwivel);
        return;
      }

      // always 1 less than index.
      SelectedSwivel = nearestSwivels[index - 1];
      if (SelectedSwivel == null)
      {
        return;
      }
      _currentPanelConfig.TargetSwivelId = SelectedSwivel.SwivelPersistentId;

      UnsetSavedState();
      OnSwivelSelectedChanged?.Invoke(SelectedSwivel);
    }

    public string PersistentIdToString(int persistentId)
    {
      return persistentId.ToString();
    }

    public const string unsetSwivelId = "N/A";

    public void AddOrUpdateSwivelDropdown()
    {
      if (mechanismAction == null)
      {
        swivelSelectorDropdown = SwivelUIHelpers.AddDropdownRow(panelContent.transform, viewStyles, ModTranslations.Mechanism_Switch_Swivel_SelectedSwivel,
          new[] { unsetSwivelId },
          unsetSwivelId,
          OnMechanismSwivelSelected);
        return;
      }

      var selfPos = mechanismAction.transform.position;

      nearestSwivels = mechanismAction.GetNearestSwivels();
      var options = nearestSwivels.Where(x => x != null)
        .Select(x =>
        {
          var dist = Vector3.Distance(selfPos, x.transform.position);
          return $"({dist:F1}m) {PersistentIdToString(x.SwivelPersistentId)}";
        }).ToList();

      options.Insert(0, unsetSwivelId);

      if (!swivelSelectorDropdown)
      {
        var optionIndex = options.Count > 1 ? 1 : 0;
        var selectedOption = options[optionIndex];
        swivelSelectorDropdown = SwivelUIHelpers.AddDropdownRow(panelContent.transform, viewStyles, ModTranslations.Mechanism_Switch_Swivel_SelectedSwivel,
          options.ToArray(), selectedOption,
          OnMechanismSwivelSelected);
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
        OnMechanismSwivelSelected(actionInt);
      }
    }

    protected virtual void OnPanelSave()
    {
      LoggerProvider.LogInfo("Saving panel. This should not be seen in-game.");
    }

    public virtual void UnsetSavedState()
    {
      IsEditing = true;
      if (_saveStatus) _saveStatus.text = SwivelUIPanelStrings.Save;
    }
    public virtual void SetSavedState()
    {
      IsEditing = false;
      if (_saveStatus) _saveStatus.text = SwivelUIPanelStrings.Saved;
    }

    public void OnActionChanged(int index)
    {
      _currentPanelConfig.SelectedAction = (MechanismAction)index;
      UnsetSavedState();
      CreateUI();
      HideShowSwivelFinder();
    }


    public virtual void CreateUI()
    {
      if (mechanismAction == null)
      {
        LoggerProvider.LogWarning("Failure to bind MechanismPanel correctly. IMechanismAction is null.");
        return;
      }
      if (panelRoot) Destroy(panelRoot);
      if (panelContent) Destroy(panelContent);
      if (actionDropdown) Destroy(actionDropdown);
      if (swivelSelectorDropdown) Destroy(swivelSelectorDropdown);

      panelRoot = null;
      panelContent = null;
      actionDropdown = null;
      swivelSelectorDropdown = null;

      panelRoot = CreateUIRoot();
      var scrollContainer = SwivelUIHelpers.CreateScrollView(panelRoot.transform, viewStyles, out var scrollRect);
      var viewport = SwivelUIHelpers.CreateViewport(scrollContainer.transform, viewStyles);
      // anchors new Vector2(0, 0), new Vector2(1, 1)
      panelContent = SwivelUIHelpers.CreateContent("Content", viewport.transform, viewStyles, null, null);
      scrollRect.content = panelContent.GetComponent<RectTransform>();

      SwivelUIHelpers.AddRowWithButton(panelContent.transform, viewStyles, ModTranslations.SharedKey_Mode, "X", 24, 24, out _, Hide);

      var options = Enum.GetNames(typeof(MechanismAction));
      var selectedOptionIndex = options.ToList().IndexOf(_currentPanelConfig.SelectedAction.ToString());

      actionDropdown = SwivelUIHelpers.AddDropdownRow(panelContent.transform, viewStyles, Localization.instance.Localize("$valheim_vehicles_mechanism_mode_configure"),
        options,
        _currentPanelConfig.SelectedAction.ToString(),
        OnActionChanged
      );

      // must always set this value as actionDropdown does not do this.
      actionDropdown.SetValueWithoutNotify(selectedOptionIndex);

      AddOrUpdateSwivelDropdown();

      SwivelUIHelpers.AddRowWithButton(panelContent.transform, viewStyles, null, SwivelUIPanelStrings.Save, 96f, 48f, out _saveStatus, () =>
      {
        OnPanelSave();
        SetSavedState();
      });

      HideShowSwivelFinder();
    }
#endif
  }
}