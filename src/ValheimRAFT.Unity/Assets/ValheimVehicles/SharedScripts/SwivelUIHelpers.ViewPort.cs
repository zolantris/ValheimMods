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
      public static RectTransform CreateViewport(Transform parent, SwivelUISharedStyles viewStyles,bool hasMask = false, bool hasVerticalLayoutGroup = false)
      {
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
        viewportGO.transform.SetParent(parent, false);

        if (hasVerticalLayoutGroup)
        {
          viewportGO.AddComponent <VerticalLayoutGroup>();
        }
        if (hasMask)
        {
          viewportGO.AddComponent<Mask>();
        }

        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.one;
        var scrollRect = viewportGO.GetComponent<ScrollRect>();
        if (scrollRect)
        {
          scrollRect.viewport = viewportRT;
        }
        var viewportImage = viewportGO.GetComponent<Image>();
        viewportImage.color = viewStyles.ScrollViewBackgroundColor;

        return viewportRT;
      }
    }
  }