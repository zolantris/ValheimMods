using System;

namespace ValheimVehicles.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class GameCacheValueAttribute : Attribute
{
  public string? Name { get; }
  public float IntervalInSeconds { get; }

  public GameCacheValueAttribute(float intervalInSeconds = 1f)
  {
    Name = null; // Will be set in CacheController
    IntervalInSeconds = intervalInSeconds;
  }

  public GameCacheValueAttribute(string name, float intervalInSeconds = 1f)
  {
    Name = name;
    IntervalInSeconds = intervalInSeconds;
  }
}