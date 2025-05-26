using System;
namespace UnityEngine
{


  public static class Mathf
  {
    public static float Clamp(float value, float min, float max)
    {
      return Math.Max(min, Math.Min(max, value));
    }
    public static float Min(float a, float b)
    {
      return Math.Min(a, b);
    }
    public static float Max(float a, float b)
    {
      return Math.Max(a, b);
    }
    public static float Abs(float v)
    {
      return Math.Abs(v);
    }
    public static float Round(float v)
    {
      return (float)Math.Round(v);
    }

    public static bool Approximately(float a, float b)
    {
      if (Math.Abs(a - b) < float.Epsilon)
      {
        return true;
      }
      return false;
    }
  }
}

public static class StringExtensions
{
  public static int GetStableHashCode(this string str)
  {
    return str.GetHashCode();
  }
}