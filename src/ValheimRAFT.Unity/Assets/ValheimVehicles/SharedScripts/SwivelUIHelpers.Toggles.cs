#region

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts.UI
{
    public static partial class SwivelUIHelpers
    {
        public static GameObject AddToggleRow(Transform parent, SwivelUISharedStyles viewStyles, string label, bool initial, UnityAction<bool> onChanged)
        {
            var row = CreateRow(parent, viewStyles, label, out _, false);

            // === Toggle Container (fix width/height to enforce square) ===
            var toggleContainer = new GameObject("ToggleContainer", typeof(RectTransform), typeof(LayoutElement));
            toggleContainer.transform.SetParent(row.transform, false);

            var toggleContainerRT = toggleContainer.GetComponent<RectTransform>();
            toggleContainerRT.sizeDelta = new Vector2(48, 48);

            var toggleContainerLE = toggleContainer.GetComponent<LayoutElement>();
            toggleContainerLE.minWidth = 48;
            toggleContainerLE.minHeight = 48;
            toggleContainerLE.preferredWidth = 48;
            toggleContainerLE.preferredHeight = 48;
            toggleContainerLE.flexibleWidth = 0f;

            // === Toggle ===
            var toggleGO = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggleGO.transform.SetParent(toggleContainer.transform, false);
            var toggleRT = toggleGO.GetComponent<RectTransform>();
            toggleRT.anchorMin = Vector2.zero;
            toggleRT.anchorMax = Vector2.one;
            toggleRT.offsetMin = Vector2.zero;
            toggleRT.offsetMax = Vector2.zero;

            var toggle = toggleGO.GetComponent<Toggle>();
            var toggleImage = toggleGO.GetComponent<Image>();
            toggleImage.color = SwivelUIColors.grayBg; // outer box
            toggle.targetGraphic = toggleImage;
            toggle.isOn = initial;

            // === Checkmark ===
            var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmarkGO.transform.SetParent(toggleGO.transform, false);
            var checkmarkImage = checkmarkGO.GetComponent<Image>();
            checkmarkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f); // filled green check
            checkmarkImage.raycastTarget = false;

            var checkmarkRT = checkmarkGO.GetComponent<RectTransform>();
            checkmarkRT.anchorMin = new Vector2(0.2f, 0.2f);
            checkmarkRT.anchorMax = new Vector2(0.8f, 0.8f);
            checkmarkRT.offsetMin = Vector2.zero;
            checkmarkRT.offsetMax = Vector2.zero;

            toggle.graphic = checkmarkImage;
            toggle.onValueChanged.AddListener(onChanged);

            return row;
        }
    }
}
