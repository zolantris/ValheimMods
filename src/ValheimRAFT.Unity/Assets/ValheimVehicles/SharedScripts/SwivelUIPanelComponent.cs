#region

  using System;
  using TMPro;
  using UnityEngine;
  using UnityEngine.Serialization;
  using UnityEngine.UI;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts.UI
  {

    public class SwivelUIPanelComponent : SingletonBehaviour<SwivelUIPanelComponent>
    {
      [Header("UI Settings")]
      [SerializeField] public float MaxUIWidth = 700f;

      [FormerlySerializedAs("styles")] [SerializeField]
      public SwivelUISharedStyles viewStyles = new();
      private bool _hasCreatedUI;
      private Toggle doorToggle;
      private TMP_Dropdown hingeDropdown;
      private Transform layoutParent;
      private Slider maxYSlider;
      private Slider maxZSlider;
      private TMP_Dropdown modeDropdown;

      private SwivelComponent swivel;
      private TMP_Dropdown yDirDropdown;
      private TMP_Dropdown zDirDropdown;

      internal GameObject panelRoot;

      public void BindTo(SwivelComponent target)
      {
        swivel = target;

        if (swivel == null) return;
        if (!_hasCreatedUI)
        {
          CreateUI();
        }

        modeDropdown.SetValueWithoutNotify((int)swivel.Mode);
        doorToggle.isOn = swivel.IsDoorOpen;
        hingeDropdown.SetValueWithoutNotify((int)swivel.CurrentHingeMode);
        zDirDropdown.SetValueWithoutNotify((int)swivel.CurrentZHingeDirection);
        yDirDropdown.SetValueWithoutNotify((int)swivel.CurrentYHingeDirection);
        maxZSlider.SetValueWithoutNotify(swivel.MaxInclineZ);
        maxYSlider.SetValueWithoutNotify(swivel.MaxYAngle);

        RefreshUI();
        Show();
      }

      public void Show()
      {
        if (panelRoot == null)
        {
          CreateUI();
        }

        if (panelRoot)
        {
          panelRoot.gameObject.SetActive(true);
        }
      }
      public void Hide()
      {
        gameObject.SetActive(false);
      }

      public virtual GameObject CreateUIRoot()
      {
        var rootUI = SwivelUIHelpers.CreateUICanvas("SwivelUICanvas", transform);
        return rootUI.gameObject;
      }

      private void CreateUI()
      {
        panelRoot = CreateUIRoot();
        var scrollRect = SwivelUIHelpers.CreateScrollView(panelRoot.transform, viewStyles);
        var scrollViewport = SwivelUIHelpers.CreateViewport(scrollRect, viewStyles);

        var scrollViewContent = SwivelUIHelpers.CreateContent("Content", scrollViewport.transform, viewStyles);

        layoutParent = scrollViewContent.transform;
        scrollRect.content = scrollViewContent;

        SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, "Swivel Config");

        modeDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, "Swivel Mode", EnumNames<SwivelMode>(), swivel.Mode.ToString(), i =>
        {
          if (swivel != null)
          {
            swivel.SetMode((SwivelMode)i);
            RefreshUI();
          }
        });

        SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, "Door Mode Settings");
        doorToggle = SwivelUIHelpers.AddToggleRow(layoutParent, viewStyles, "Door Open", false, isOn =>
        {
          if (swivel != null) swivel.SetDoorOpen(isOn);
        });
        hingeDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, "Hinge Mode", EnumNames<SwivelComponent.DoorHingeMode>(), swivel.CurrentHingeMode.ToString(), i =>
        {
          if (swivel != null)
          {
            swivel.SetHingeMode((SwivelComponent.DoorHingeMode)i);
            RefreshUI();
          }
        });
        zDirDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, "Z Hinge Dir", EnumNames<SwivelComponent.HingeDirection>(), swivel.CurrentZHingeDirection.ToString(), i =>
        {
          if (swivel != null) swivel.SetZHingeDirection((SwivelComponent.HingeDirection)i);
        });
        yDirDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, "Y Hinge Dir", EnumNames<SwivelComponent.HingeDirection>(), swivel.CurrentYHingeDirection.ToString(), i =>
        {
          if (swivel != null) swivel.SetYHingeDirection((SwivelComponent.HingeDirection)i);
        });
        maxZSlider = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Max Z Angle", 0f, 90f, 0f, v =>
        {
          if (swivel != null) swivel.SetMaxInclineZ(v);
        });
        maxYSlider = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Max Y Angle", 0f, 90f, 0f, v =>
        {
          if (swivel != null) swivel.SetMaxYAngle(v);
        });

        _hasCreatedUI = true;
      }

      private void RefreshUI()
      {
        if (swivel == null) return;
        var isDoor = swivel.Mode == SwivelMode.DoorMode;
        doorToggle.gameObject.SetActive(isDoor);
        hingeDropdown.gameObject.SetActive(isDoor);
        zDirDropdown.gameObject.SetActive(isDoor);
        yDirDropdown.gameObject.SetActive(isDoor);
        maxZSlider.gameObject.SetActive(isDoor);
        maxYSlider.gameObject.SetActive(isDoor);
      }

      private string[] EnumNames<T>() where T : Enum
      {
        return Enum.GetNames(typeof(T));
      }
    }
  }