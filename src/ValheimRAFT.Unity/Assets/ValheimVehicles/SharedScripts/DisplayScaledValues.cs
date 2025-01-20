using UnityEngine;

namespace ValheimVehicles.SharedScripts
{
  public static class DisplayScaledValues
  {
    // Scale of 1920/1080
    private const int hdTextSize = 32;

    public const int heightScale = 1080;

    // text values
    public static float textScaleFactorMultiplier = 1.0f;

    // Screen height is more important than width due to super ultra wides etc.
    public static int GetScaledSize(float baseTextSize = hdTextSize)
    {
      var scalar = Screen.height / heightScale;
      return Mathf.RoundToInt(scalar * textScaleFactorMultiplier *
                              baseTextSize);
    }
  }
}