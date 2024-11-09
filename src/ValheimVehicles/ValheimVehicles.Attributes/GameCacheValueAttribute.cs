using System;

namespace ValheimVehicles.Attributes;

/// <summary>
/// Does nothing for now. But eventually this would be a way to mark methods as cacheable maybe via parent extension
/// </summary>
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