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
      public static RectTransform CreateViewport(ScrollRect scrollRect, SwivelUISharedStyles viewStyles)
      {
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewportGO.transform.SetParent(scrollRect.transform, false);

        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;

        scrollRect.viewport = viewportRT;
        var viewportImage = viewportGO.GetComponent<Image>();
        viewportImage.color = viewStyles.ScrollViewBackgroundColor;

        return viewportRT;
      }
    }
  }