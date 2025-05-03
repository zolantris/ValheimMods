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
      /// <summary>
      /// This is for Debug testing only there is a UI layer for valheim plugins that we should use from Jotunn. This would include both a Canvas and a Draggable option.
      /// </summary>
      public static Canvas CreateUICanvas(string name, Transform parent)
      {
        var canvasGO = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(parent, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        return canvas;
      }
    }
  }