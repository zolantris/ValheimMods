using System;

namespace ValheimVehicles.Vehicles.Structs;

/// <summary>
/// Main method to get the cache or non-cache value based on timer interval
/// </summary>
public struct GameCacheValue<TCachedResult, TParams>(
  string name,
  float intervalInSeconds,
  Func<TParams, TCachedResult?> callback)
{
  public string Name { get; set; } = name;
  private float IntervalInSeconds { get; set; } = intervalInSeconds;
  public bool IsCached { get; set; } = false;
  private float _timer { get; set; } = 0f;
  private TCachedResult? _cachedValue = default;

  private Func<TParams, TCachedResult?> GetValueUncached { get; set; } =
    callback;

  // Retrieves the cached value or computes it if not cached
  public TCachedResult? GetValue(TParams @params)
  {
    return IsCached ? _cachedValue : GetValueUncachedAndCache(@params);
  }

  // Retrieves the value, optionally flushing the cache
  public TCachedResult? GetValue(TParams @params, bool flushCache)
  {
    if (flushCache)
    {
      // Bypass cache and return fresh value
      return GetValueUncachedAndCache(@params); // Always retrieves fresh value
    }

    return GetValue(@params); // Regular cached call
  }

  private TCachedResult? GetValueUncachedAndCache(TParams @params)
  {
    var value = GetValueUncached(@params);
    SetCached(value); // Cache the new value
    return value;
  }

  private void ResetCache()
  {
    IsCached = false;
  }

  public void SyncCache(float deltaSeconds)
  {
    _timer += deltaSeconds;
    if (_timer >= IntervalInSeconds)
    {
      ResetCache();
    }
  }

  private void SetCached(TCachedResult? value)
  {
    _cachedValue = value;
    IsCached = true;
  }
}