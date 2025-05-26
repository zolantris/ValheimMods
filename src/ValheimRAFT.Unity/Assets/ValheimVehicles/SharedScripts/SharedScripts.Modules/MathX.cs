#if UNITY_ENGINE || !DEBUG
using UnityEngine;
#else
using System;
#endif

namespace ValheimVehicles.SharedScripts.Modules;

// for running nunit and other tests outside of unity.
#if UNITY_ENGINE || !DEBUG
public static class MathX // or MathMock, MathUtil, etc.
{
  public static float Clamp(float value, float min, float max) => Mathf.Clamp(value, min, max);
    public static float Min(float a, float b) => Mathf.Min(a, b);
    public static float Max(float a, float b) => Mathf.Max(a, b);
    public static float Abs(float v) => Mathf.Abs(v);
    public static float Round(float v) => Mathf.Round(v);
}
#else
/// <summary>
/// For mocking. This will switch over to UnityEngine Mathf in release builds.
/// </summary>
public static class MathX // or MathMock, MathUtil, etc.
{
  public static float Clamp(float v, float min, float max)
  {
    return Math.Max(min, Math.Min(max, v));
  }
  public static float Min(float a, float b)
  {
    return Math.Min(a, b);
  }

  public static float Min(params float[] values)
  {
    if (values == null || values.Length == 0)
      throw new ArgumentException("No values supplied to MathX.Min");

    var min = values[0];
    for (var i = 1; i < values.Length; i++)
      min = Min(min, values[i]);
    return min;
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