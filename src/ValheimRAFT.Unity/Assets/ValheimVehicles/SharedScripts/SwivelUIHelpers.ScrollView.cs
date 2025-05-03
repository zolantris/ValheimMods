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
      public static ScrollRect CreateScrollView(Transform parent, SwivelUISharedStyles viewStyles)
      {
        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGO.transform.SetParent(parent, false);

        var scrollRect = scrollGO.GetComponent<ScrollRect>();
        var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0, 1);
        scrollRectTransform.anchorMax = new Vector2(0, 1);
        scrollRectTransform.pivot = new Vector2(0, 1);
        scrollRectTransform.anchoredPosition = new Vector2(20f, -20f);
        scrollRectTransform.sizeDelta = new Vector2(Mathf.Clamp(Screen.width * 0.3f, viewStyles.minWidth, viewStyles.maxWidth), Mathf.Min(viewStyles.maxHeight, Screen.height));

        return scrollRect;
      }
    }
  }