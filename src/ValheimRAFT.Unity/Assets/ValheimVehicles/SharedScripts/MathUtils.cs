// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
using ValheimVehicles.SharedScripts.Modules;

namespace ValheimVehicles.SharedScripts
{
  public static class MathUtils
  {
    /// <summary>
    /// Rounds a float to the nearest hundredth (2 decimal places).
    /// </summary>
    public static float RoundToHundredth(float value)
    {
      return MathX.Round(value * 100f) / 100f;
    }

    /// <summary>
    /// Returns a formatted string of a float with exactly 2 decimal places.
    /// </summary>
    public static string FormatFloatTwoDecimals(float value)
    {
      return RoundToHundredth(value).ToString("F2");
    }

    /// <summary>
    /// Returns a formatted string of a double with exactly 2 decimal places.
    /// </summary>
    public static string FormatDoubleTwoDecimals(double value)
    {
      return Math.Round(value, 2).ToString("F2");
    }
  }
}