// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using ValheimVehicles.SharedScripts.Interfaces;
namespace ValheimVehicles.SharedScripts.UI
{
  public class MechanismSelectorPanel : SingletonBehaviour<MechanismSelectorPanel>
  {
#if !UNITY_EDITOR
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
    public Action<SwivelComponent>? OnSwivelSelectedChanged;
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

      if (_currentPanelConfig.SelectedAction == MechanismAction.SwivelActivateMode || _currentPanelConfig.SelectedAction == MechanismAction.SwivelEditMode)
      {
        AddOrUpdateSwivelDropdown();
      }

      HideShowSwivelFinder();

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
      var show = _currentPanelConfig.SelectedAction == MechanismAction.SwivelActivateMode || _currentPanelConfig.SelectedAction == MechanismAction.SwivelEditMode;
      swivelSelectorDropdown.gameObject.SetActive(show);
    }

    public void OnMechanismSwivelSelected(int index)
    {
      if (index < 0 || index >= nearestSwivels.Count) return;
      SelectedSwivel = nearestSwivels[index];
      if (SelectedSwivel == null)
      {
        // we need to update options.
        AddOrUpdateSwivelDropdown();
        return;
      }
      _currentPanelConfig.TargetSwivelId = SelectedSwivel.SwivelPersistentId;
      OnSwivelSelectedChanged?.Invoke(SelectedSwivel);
      UnsetSavedState();
    }

    public string PersistentIdToString(int persistentId)
    {
      return persistentId.ToString();
    }

    public void AddOrUpdateSwivelDropdown()
    {
      if (mechanismAction == null)
      {
        swivelSelectorDropdown = SwivelUIHelpers.AddDropdownRow(panelContent.transform, viewStyles, ModTranslations.Mechanism_Switch_Swivel_SelectedSwivel,
          new[] { "N/A" },
          "N/A",
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

      if (!swivelSelectorDropdown)
      {
        swivelSelectorDropdown = SwivelUIHelpers.AddDropdownRow(panelContent.transform, viewStyles, ModTranslations.Mechanism_Switch_Swivel_SelectedSwivel,
          options.ToArray(),
          options.FirstOrDefault() ?? "N/A",
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
        swivelSelectorDropdown.value = actionInt;
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

      actionDropdown = SwivelUIHelpers.AddDropdownRow(panelContent.transform, viewStyles, ModTranslations.SharedKey_Mode,
        Enum.GetNames(typeof(MechanismAction)),
        _currentPanelConfig.SelectedAction.ToString(),
        index =>
        {
          _currentPanelConfig.SelectedAction = (MechanismAction)index;
          HideShowSwivelFinder();
          UnsetSavedState();
        });


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