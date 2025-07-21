// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
#if UNITY_ENGINE || !DEBUG
using UnityEngine;
#else
using System;
#endif

namespace ValheimVehicles.SharedScripts.Modules
{


  public static partial class MathX
  {
    public static float Min(params float[] values)
    {
      if (values == null || values.Length == 0)
        throw new ArgumentException("No values supplied to MathX.Min");

      var min = values[0];
      for (var i = 1; i < values.Length; i++)
        min = Min(min, values[i]);
      return min;
    }
  }

// for running nunit and other tests outside of unity.
#if VALHEIM && !TEST
  public static partial class MathX // or MathMock, MathUtil, etc.
  {
    public static float Clamp(float value, float min, float max)
    {
      return Mathf.Clamp(value, min, max);
    }
    public static int Clamp(int v, int min, int max)
    {
      return Mathf.Max(min, Math.Min(max, v));
    }
    public static float Min(float a, float b)
    {
      return Mathf.Min(a, b);
    }
    public static float Max(float a, float b)
    {
      return Mathf.Max(a, b);
    }
    public static float Abs(float v)
    {
      return Mathf.Abs(v);
    }
    public static float Round(float v)
    {
      return Mathf.Round(v);
    }
  }
#else
  /// <summary>
  /// For mocking. This will switch over to UnityEngine Mathf in release builds.
  /// </summary>
  public static partial class MathX // or MathMock, MathUtil, etc.
  {
    public static float Clamp(float v, float min, float max)
    {
      return Math.Max(min, Math.Min(max, v));
    }
    public static int Clamp(int v, int min, int max)
    {
      return Math.Max(min, Math.Min(max, v));
    }
    public static float Min(float a, float b)
    {
      return Math.Min(a, b);
    }

    public static float Max(float a, float b)
    {
      return Math.Max(a, b);
    }

    public static float Round(float f)
    {
      return (float)Math.Round(f, MidpointRounding.AwayFromZero);
    }
  }
#endif
}