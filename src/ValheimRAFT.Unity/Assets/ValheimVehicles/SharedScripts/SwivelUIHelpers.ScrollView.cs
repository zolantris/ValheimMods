#region

using UnityEngine;
using UnityEngine.UI;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts.UI
  {

    public static partial class SwivelUIHelpers
    {
      public static GameObject CreateScrollView(Transform parent, SwivelUISharedStyles viewStyles, out ScrollRect scrollRect)
      {
        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGO.transform.SetParent(parent, false);

        scrollRect = scrollGO.GetComponent<ScrollRect>();
        var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0, 1);
        scrollRectTransform.anchorMax = new Vector2(0, 1);
        scrollRectTransform.pivot = new Vector2(0, 1);
        scrollRect.horizontal = false;
        scrollRectTransform.anchoredPosition = new Vector2(20f, -20f);
        scrollRectTransform.sizeDelta = new Vector2(Mathf.Clamp(Screen.width * 0.3f, viewStyles.minWidth, viewStyles.maxWidth), Mathf.Min(viewStyles.maxHeight, Screen.height));

        return scrollGO;
      }
    }
  }