#region

  using System;
  using TMPro;
  using UnityEngine;
  using UnityEngine.UIElements;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts.UI
  {

    /// <summary>
    /// React/ReactNative style like types for UI panels.
    /// </summary>
    ///
    /// TODO look at Unity UIElements.
    public interface IUnity2D_ViewStyles
    {
      Vector2 anchorMin { get; set; }
      Vector2 anchorMax { get; set; }
      Vector2 position { get; set; }

      float? height { get; set; }
      float? width { get; set; }
      float? minWidth { get; set; }
      float? maxWidth { get; set; }

      FlexDirection? flexDirection { get; set; }
      DisplayStyle? display { get; set; }
    }

    /// <summary>
    /// 
    /// Todo see if some of these are not required
    /// </summary>
    public interface IUnity2d_TextStyles
    {
      FontStyle fontStyle { get; set; }
      FontWeight fontWeight { get; set; }
      int fontSize { get; set; }
      Color color { get; set; }
      TextAnchor alignment { get; set; }
    }

    public static class Unity2DStyleParser
    {
      public static Vector2 AnchorMin(this Unity2dViewStyles viewStyles)
      {
        return new Vector2(0, 0);
      }

      public static Vector2 AnchorMax(this Unity2dViewStyles viewStyles)
      {
        if (viewStyles.display == DisplayStyle.Flex)
        {
          return new Vector2(1, 1);
        }
        // if (styles.flexDirection == FlexDirection.Row )
        return new Vector2(0, 0);
      }
    }

    /// <summary>
    /// TODO swap these styles to invidual Text and Container styles
    /// </summary>
    public class SwivelUISharedStyles
    {
      public int DropdownCaptionFontSize = 24;
      public int DropdownContentPaddingBottom = 18;
      public int DropdownContentPaddingLeft = 18;
      public int DropdownContentPaddingRight = 18;
      public int DropdownContentPaddingTop = 18;
      public int DropdownContentSpacing = 18;
      public float DropdownContentHeight = 250f;
      public float DropdownItemHeight = 24f;
      public int DropdownItemLabelFontSize = 18;

      public float height = 50;

      public float maxWidth = 500f;
      public float minWidth = 500f;
      public float maxHeight = 600f;

      public Color InputTextColor = Color.black;
      public Color LabelColor = Color.white;
      public float LabelMinWidth = 160f;
      public float LabelPreferredWidth = 180f;

      public Color ScrollViewBackgroundColor = new(0.35f, 0.35f, 0.55f, 1f);

      public Color DropdownOptionsContainerColor = new(0.75f, 0.75f, 0.75f, 1f);

    #region LabelFonts

      public int FontSizeDropdownLabel = 28;
      public int FontSizeSectionLabel = 28;
      public int FontSizeRowLabel = 24;

    #endregion

    }

    [Serializable]
    public class Unity2dTextStyles : Unity2dViewStyles, IUnity2d_TextStyles
    {

      public FontStyle fontStyle
      {
        get;
        set;
      } = FontStyle.Normal;

      public FontWeight fontWeight
      {
        get;
        set;
      } = FontWeight.Regular;
      public int fontSize
      {
        get;
        set;
      } = 18;
      public Color color
      {
        get;
        set;
      } = Color.black;
      public TextAnchor alignment
      {
        get;
        set;
      } = TextAnchor.UpperLeft;
    }

    public class InternalUnity2dViewStyles : IUnity2D_ViewStyles
    {

      public Vector2 anchorMin
      {
        get;
        set;
      } = Vector2.zero;
      public Vector2 anchorMax
      {
        get;
        set;
      } = Vector2.one;

      public Vector2 position
      {
        get;
        set;
      } = Vector2.zero;

      public float? height
      {
        get;
        set;
      }
      public float? width
      {
        get;
        set;
      }
      public float? minWidth
      {
        get;
        set;
      }
      public float? maxWidth
      {
        get;
        set;
      }
      public FlexDirection? flexDirection
      {
        get;
        set;
      } = FlexDirection.Row;
      public DisplayStyle? display
      {
        get;
        set;
      } = DisplayStyle.Flex;
    }

    [Serializable]
    public class Unity2dViewStyles : IUnity2D_ViewStyles
    {
      private InternalUnity2dViewStyles _unity2DImplementation = new();

      public Vector2 anchorMin
      {
        get => this.AnchorMin();
        set => _unity2DImplementation.anchorMin = value;
      }

      public Vector2 anchorMax
      {
        get => this.AnchorMax();
        set => _unity2DImplementation.anchorMax = value;
      }

      public Vector2 position
      {
        get;
        set;
      } = Vector2.zero;

      public float? height
      {
        get => _unity2DImplementation.height;
        set => _unity2DImplementation.height = value;
      }

      public float? width
      {
        get => _unity2DImplementation.width;
        set => _unity2DImplementation.width = value;
      }

      public float? minWidth
      {
        get => _unity2DImplementation.minWidth;
        set => _unity2DImplementation.minWidth = value;
      }

      public float? maxWidth
      {
        get => _unity2DImplementation.maxWidth;
        set => _unity2DImplementation.maxWidth = value;
      }

      public FlexDirection? flexDirection
      {
        get => _unity2DImplementation.flexDirection;
        set => _unity2DImplementation.flexDirection = value;
      }

      public DisplayStyle? display
      {
        get => _unity2DImplementation.display;
        set => _unity2DImplementation.display = value;
      }
    }
  }