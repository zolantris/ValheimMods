#region

using UnityEngine;
using UnityEngine.UI;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public static partial class SwivelUIHelpers
  {
    private Canvas CreateSwivelUICanvas(Transform parent)
    {
      var canvasGO = new GameObject("SwivelUICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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