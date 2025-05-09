#region

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
    public class SwivelUIPanelComponent : SingletonBehaviour<SwivelUIPanelComponent>
    {
        [Header("UI Settings")]
        [SerializeField] public float MaxUIWidth = 700f;

        private bool _hasCreatedUI;
        private Transform layoutParent;
        private Slider maxXSlider;
        private Slider maxYSlider;
        private Slider maxZSlider;

        private TMP_Dropdown modeDropdown;
        private Slider movementSpeedSlider;
        internal GameObject panelRoot;
        private Toggle rotationReturnToggle;

        private SwivelComponent swivel;
        [SerializeField] public SwivelUISharedStyles viewStyles = new();
        private Toggle xHingeToggle;
        private Toggle yHingeToggle;
        private Toggle zHingeToggle;

        public void BindTo(SwivelComponent target)
        {
            swivel = target;
            if (swivel == null) return;

            if (!_hasCreatedUI)
                CreateUI();

            RefreshUI();
            Show();
        }

        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void CreateUI()
        {
            panelRoot = SwivelUIHelpers.CreateUICanvas("SwivelUIPanel", transform).gameObject;
            var scrollRect = SwivelUIHelpers.CreateScrollView(panelRoot.transform, viewStyles);
            var scrollViewport = SwivelUIHelpers.CreateViewport(scrollRect, viewStyles);
            var scrollViewContent = SwivelUIHelpers.CreateContent("Content", scrollViewport.transform, viewStyles);
            layoutParent = scrollViewContent.transform;
            scrollRect.content = scrollViewContent;

            SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, "Swivel Config");

            modeDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, "Swivel Mode", Enum.GetNames(typeof(SwivelMode)), swivel.Mode.ToString(), i =>
            {
                swivel.SetMode((SwivelMode)i);
                RefreshUI();
            });

            movementSpeedSlider = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Lerp Speed", 1f, 100f, swivel.MovementLerpSpeed, v =>
            {
                swivel.SetMovementLerpSpeed(v);
            });

            SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, "Rotation Settings");

            rotationReturnToggle = SwivelUIHelpers.AddToggleRow(layoutParent, viewStyles, "Return After Rotation", swivel.IsRotationReturning, val =>
            {
                swivel.SetRotationReturning(val);
            });

            xHingeToggle = SwivelUIHelpers.AddToggleRow(layoutParent, viewStyles, "Enable X Axis", (swivel.HingeAxes & HingeAxis.X) != 0, val =>
            {
                var axes = swivel.HingeAxes;
                axes = val ? (axes | HingeAxis.X) : (axes & ~HingeAxis.X);
                swivel.SetHingeAxes(axes);
            });

            yHingeToggle = SwivelUIHelpers.AddToggleRow(layoutParent, viewStyles, "Enable Y Axis", (swivel.HingeAxes & HingeAxis.Y) != 0, val =>
            {
                var axes = swivel.HingeAxes;
                axes = val ? (axes | HingeAxis.Y) : (axes & ~HingeAxis.Y);
                swivel.SetHingeAxes(axes);
            });

            zHingeToggle = SwivelUIHelpers.AddToggleRow(layoutParent, viewStyles, "Enable Z Axis", (swivel.HingeAxes & HingeAxis.Z) != 0, val =>
            {
                var axes = swivel.HingeAxes;
                axes = val ? (axes | HingeAxis.Z) : (axes & ~HingeAxis.Z);
                swivel.SetHingeAxes(axes);
            });

            maxXSlider = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Max X Angle", 0f, 180f, swivel.MaxEuler.x, v =>
            {
                var e = swivel.MaxEuler;
                e.x = v;
                swivel.SetMaxEuler(e);
            });

            maxYSlider = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Max Y Angle", 0f, 180f, swivel.MaxEuler.y, v =>
            {
                var e = swivel.MaxEuler;
                e.y = v;
                swivel.SetMaxEuler(e);
            });

            maxZSlider = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Max Z Angle", 0f, 180f, swivel.MaxEuler.z, v =>
            {
                var e = swivel.MaxEuler;
                e.z = v;
                swivel.SetMaxEuler(e);
            });

            _hasCreatedUI = true;
        }

        private void RefreshUI()
        {
            if (swivel == null) return;

            modeDropdown.SetValueWithoutNotify((int)swivel.Mode);
            rotationReturnToggle.isOn = swivel.IsRotationReturning;
            xHingeToggle.isOn = (swivel.HingeAxes & HingeAxis.X) != 0;
            yHingeToggle.isOn = (swivel.HingeAxes & HingeAxis.Y) != 0;
            zHingeToggle.isOn = (swivel.HingeAxes & HingeAxis.Z) != 0;

            // maxXSlider.SetValueWithoutNotify(swivel.MaxEuler.x);
            // maxYSlider.SetValueWithoutNotify(swivel.MaxEuler.y);
            // maxZSlider.SetValueWithoutNotify(swivel.MaxEuler.z);
            movementSpeedSlider.SetValueWithoutNotify(swivel.MovementLerpSpeed);
        }
    }
}
