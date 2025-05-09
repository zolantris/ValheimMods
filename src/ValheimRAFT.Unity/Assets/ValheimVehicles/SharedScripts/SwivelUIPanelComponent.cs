// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

        private bool _hasCreatedUI;
        private Transform layoutParent;

        private GameObject maxXRow;
        private GameObject maxYRow;
        private GameObject maxZRow;
        private TMP_Dropdown modeDropdown;
        private TMP_Dropdown motionStateDropdown;
        private GameObject movementLerpRow;
        private GameObject hingeAxesRow;
        internal GameObject panelRoot;
        private SwivelComponent swivel;
        private GameObject targetDistanceXRow;
        private GameObject targetDistanceYRow;
        private GameObject targetDistanceZRow;

        [FormerlySerializedAs("styles")] [SerializeField]
        public SwivelUISharedStyles viewStyles = new();

        public void BindTo(SwivelComponent target)
        {
            swivel = target;

            if (swivel == null) return;
            if (!_hasCreatedUI)
            {
                CreateUI();
            }

            modeDropdown.SetValueWithoutNotify((int)swivel.Mode);
            motionStateDropdown.SetValueWithoutNotify((int)swivel.CurrentMotionState);

            UpdateHingeAxisToggles();

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
            panelRoot?.SetActive(true);
        }

        public void Hide()
        {
            panelRoot?.SetActive(false);
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
            var scrollViewLayoutElement = scrollRect.gameObject.AddComponent<LayoutElement>();
            scrollViewLayoutElement.flexibleHeight = 800f;
            scrollViewLayoutElement.flexibleWidth = 800f;
            scrollViewLayoutElement.minWidth = 500f;
            scrollViewLayoutElement.minHeight = 300f;
            scrollViewLayoutElement.preferredHeight = 500f;

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

            motionStateDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, "Motion State", EnumNames<MotionState>(), swivel.CurrentMotionState.ToString(), i =>
            {
                if (swivel != null)
                {
                    swivel.SetMotionState((MotionState)i);
                }
            });

            movementLerpRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Lerp Speed", 1f, 100f, swivel.MovementLerpSpeed, v =>
            {
                if (swivel != null) swivel.SetMovementLerpSpeed(v);
            });

            SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, "Rotation Settings");

            hingeAxesRow = SwivelUIHelpers.AddMultiToggleRow(layoutParent, viewStyles, "Hinge Axes", new[] { "X", "Y", "Z" }, GetHingeAxisStates(), states =>
            {
                if (swivel != null)
                {
                    HingeAxis newAxes = HingeAxis.None;
                    if (states[0]) newAxes |= HingeAxis.X;
                    if (states[1]) newAxes |= HingeAxis.Y;
                    if (states[2]) newAxes |= HingeAxis.Z;
                    swivel.SetHingeAxes(newAxes);
                }
            });

            maxXRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Max X Angle", 0f, 360f, swivel.MaxEuler.x, v =>
            {
                if (swivel != null)
                {
                    var e = swivel.MaxEuler;
                    e.x = v;
                    swivel.SetMaxEuler(e);
                }
            });

            maxYRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Max Y Angle", 0f, 360f, swivel.MaxEuler.y, v =>
            {
                if (swivel != null)
                {
                    var e = swivel.MaxEuler;
                    e.y = v;
                    swivel.SetMaxEuler(e);
                }
            });

            maxZRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Max Z Angle", 0f, 360f, swivel.MaxEuler.z, v =>
            {
                if (swivel != null)
                {
                    var e = swivel.MaxEuler;
                    e.z = v;
                    swivel.SetMaxEuler(e);
                }
            });

            SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, "Movement Settings");

            targetDistanceXRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Target X Offset", MinTargetOffset, MaxTargetOffset, swivel.GetMovementOffset().x, v =>
            {
                if (swivel != null)
                {
                    var o = swivel.GetMovementOffset();
                    o.x = v;
                    swivel.SetMovementOffset(o);
                }
            });

            targetDistanceYRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Target Y Offset", MinTargetOffset, MaxTargetOffset, swivel.GetMovementOffset().y, v =>
            {
                if (swivel != null)
                {
                    var o = swivel.GetMovementOffset();
                    o.y = v;
                    swivel.SetMovementOffset(o);
                }
            });

            targetDistanceZRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, "Target Z Offset", MinTargetOffset, MaxTargetOffset, swivel.GetMovementOffset().z, v =>
            {
                if (swivel != null)
                {
                    var o = swivel.GetMovementOffset();
                    o.z = v;
                    swivel.SetMovementOffset(o);
                }
            });

            _hasCreatedUI = true;
        }

        private void RefreshUI()
        {
            if (swivel == null) return;

            bool isRotating = swivel.Mode == SwivelMode.Rotate;
            bool isMoving = swivel.Mode == SwivelMode.Move;

            hingeAxesRow.SetActive(isRotating);
            maxXRow.SetActive(isRotating);
            maxYRow.SetActive(isRotating);
            maxZRow.SetActive(isRotating);

            targetDistanceXRow.SetActive(isMoving);
            targetDistanceYRow.SetActive(isMoving);
            targetDistanceZRow.SetActive(isMoving);
        }

        private void UpdateHingeAxisToggles()
        {
            if (hingeAxesRow == null || swivel == null) return;

            var toggles = hingeAxesRow.GetComponentsInChildren<Toggle>();
            var states = GetHingeAxisStates();

            for (int i = 0; i < toggles.Length && i < states.Length; i++)
            {
                toggles[i].SetIsOnWithoutNotify(states[i]);
            }
        }

        private bool[] GetHingeAxisStates()
        {
            if (swivel == null) return new[] { false, true, false }; // default Y only
            var axes = swivel.HingeAxes;
            return new[]
            {
                (axes & HingeAxis.X) != 0,
                (axes & HingeAxis.Y) != 0,
                (axes & HingeAxis.Z) != 0
            };
        }

        private string[] EnumNames<T>() where T : Enum
        {
            return Enum.GetNames(typeof(T));
        }
    }
}
