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
      public static GameObject CreateContent(string name, Transform parent, SwivelUISharedStyles viewStyles, Vector2? anchorMin, Vector2? anchorMax)
      {
        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
        contentGO.transform.SetParent(parent, false);

        var layoutGroup = contentGO.GetComponent<VerticalLayoutGroup>();
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.spacing = 10f;
        layoutGroup.padding = new RectOffset(20, 20, 20, 20);
        layoutGroup.childAlignment = TextAnchor.UpperLeft;

        var fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = anchorMin ?? new Vector2(0, 1);
        contentRT.anchorMax = anchorMax ?? new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);
        return contentGO;
      }
    }
  }