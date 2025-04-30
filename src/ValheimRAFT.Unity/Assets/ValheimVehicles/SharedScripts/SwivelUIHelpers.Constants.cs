#region

using System;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  [Serializable]
  public class Unity2dStyles
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
}