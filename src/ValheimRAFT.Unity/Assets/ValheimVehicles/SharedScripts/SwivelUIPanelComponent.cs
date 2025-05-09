// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

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
        public static int MinTargetOffset = -50;
        public static int MaxTargetOffset = 50;

        [Header("UI Settings")]
        [SerializeField] public float MaxUIWidth = 700f;
        public SwivelUIPanelStrings Strings = new();

        private bool _hasCreatedUI;
        private GameObject hingeAxisRow;
        private Transform layoutParent;
        private GameObject maxXRow;
        private GameObject maxYRow;
        private GameObject maxZRow;

        private TMP_Dropdown modeDropdown;
        private TMP_Dropdown motionStateDropdown;

        private GameObject movementLerpRow;
        private GameObject movementSectionLabel;
        internal GameObject panelRoot;
        private GameObject rotationSectionLabel;
        private SwivelComponent swivel;
        private GameObject targetDistanceXRow;
        private GameObject targetDistanceYRow;
        private GameObject targetDistanceZRow;
        public SwivelUISharedStyles viewStyles = new();

        public void BindTo(SwivelComponent target)
        {
            swivel = target;
            if (swivel == null) return;

            if (!_hasCreatedUI)
                CreateUI();

            modeDropdown.SetValueWithoutNotify((int)swivel.Mode);
            motionStateDropdown.SetValueWithoutNotify((int)swivel.CurrentMotionState);

            var max = swivel.MaxEuler;
            maxXRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(max.x);
            maxYRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(max.y);
            maxZRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(max.z);

            var offset = swivel.GetMovementOffset();
            targetDistanceXRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(offset.x);
            targetDistanceYRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(offset.y);
            targetDistanceZRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(offset.z);

            movementLerpRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(swivel.MovementLerpSpeed);

            RefreshUI();
            Show();
        }

        public void Show()
        {
            if (panelRoot == null) CreateUI();
            panelRoot.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.gameObject.SetActive(false);
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

            var layout = scrollRect.gameObject.AddComponent<LayoutElement>();
            layout.flexibleHeight = 800f;
            layout.flexibleWidth = 800f;
            layout.minWidth = 500f;
            layout.minHeight = 300f;
            layout.preferredHeight = 500f;

            SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, Strings.SwivelConfig);

            modeDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, Strings.SwivelMode, EnumNames<SwivelMode>(), swivel.Mode.ToString(), i =>
            {
                swivel.SetMode((SwivelMode)i);
                RefreshUI();
            });

            motionStateDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, Strings.MotionState, EnumNames<MotionState>(), swivel.CurrentMotionState.ToString(), i =>
            {
                swivel.SetMotionState((MotionState)i);
            });

            movementLerpRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, Strings.InterpolationSpeed, 1f, 100f, swivel.MovementLerpSpeed, v =>
            {
                swivel.SetMovementLerpSpeed(v);
            });

            rotationSectionLabel = SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, Strings.RotationSettings);

            hingeAxisRow = SwivelUIHelpers.AddMultiToggleRow(layoutParent, viewStyles, Strings.HingeAxes,
                new[] { "X", "Y", "Z" },
                new[]
                {
                    swivel.HingeAxes.HasFlag(HingeAxis.X),
                    swivel.HingeAxes.HasFlag(HingeAxis.Y),
                    swivel.HingeAxes.HasFlag(HingeAxis.Z)
                },
                selected =>
                {
                    var axis = HingeAxis.None;
                    if (selected[0]) axis |= HingeAxis.X;
                    if (selected[1]) axis |= HingeAxis.Y;
                    if (selected[2]) axis |= HingeAxis.Z;
                    swivel.SetHingeAxes(axis);
                });

            maxXRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, Strings.MaxXAngle, 0f, 360f, swivel.MaxEuler.x, v =>
            {
                var e = swivel.MaxEuler;
                e.x = v;
                swivel.SetMaxEuler(e);
            });

            maxYRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, Strings.MaxYAngle, 0f, 360f, swivel.MaxEuler.y, v =>
            {
                var e = swivel.MaxEuler;
                e.y = v;
                swivel.SetMaxEuler(e);
            });

            maxZRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, Strings.MaxZAngle, 0f, 360f, swivel.MaxEuler.z, v =>
            {
                var e = swivel.MaxEuler;
                e.z = v;
                swivel.SetMaxEuler(e);
            });

            movementSectionLabel = SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, Strings.MovementSettings);

            targetDistanceXRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, Strings.TargetXOffset, MinTargetOffset, MaxTargetOffset, swivel.GetMovementOffset().x, v =>
            {
                var o = swivel.GetMovementOffset();
                o.x = v;
                swivel.SetMovementOffset(o);
            });

            targetDistanceYRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, Strings.TargetYOffset, MinTargetOffset, MaxTargetOffset, swivel.GetMovementOffset().y, v =>
            {
                var o = swivel.GetMovementOffset();
                o.y = v;
                swivel.SetMovementOffset(o);
            });

            targetDistanceZRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, Strings.TargetZOffset, MinTargetOffset, MaxTargetOffset, swivel.GetMovementOffset().z, v =>
            {
                var o = swivel.GetMovementOffset();
                o.z = v;
                swivel.SetMovementOffset(o);
            });

            _hasCreatedUI = true;
        }

        private void RefreshUI()
        {
            if (swivel == null) return;
            bool isRotating = swivel.Mode == SwivelMode.Rotate;
            bool isMoving = swivel.Mode == SwivelMode.Move;

            rotationSectionLabel.SetActive(isRotating);
            hingeAxisRow.SetActive(isRotating);
            maxXRow.SetActive(isRotating);
            maxYRow.SetActive(isRotating);
            maxZRow.SetActive(isRotating);

            targetDistanceXRow.SetActive(isMoving);
            targetDistanceYRow.SetActive(isMoving);
            targetDistanceZRow.SetActive(isMoving);
            movementSectionLabel.SetActive(isMoving);
        }

        private string[] EnumNames<T>() where T : Enum => Enum.GetNames(typeof(T));
    }

    [Serializable]
    public class SwivelUIPanelStrings
    {
        public string SwivelConfig = "Swivel Config";
        public string SwivelMode = "Swivel Mode";
        public string MotionState = "Motion State";
        public string InterpolationSpeed = "Interpolation Speed";

        public string RotationSettings = "Rotation Settings";
        public string HingeAxes = "Hinge Axes";
        public string MaxXAngle = "Max X Angle";
        public string MaxYAngle = "Max Y Angle";
        public string MaxZAngle = "Max Z Angle";

        public string MovementSettings = "Movement Settings";
        public string TargetXOffset = "Target X Offset";
        public string TargetYOffset = "Target Y Offset";
        public string TargetZOffset = "Target Z Offset";
    }
}
