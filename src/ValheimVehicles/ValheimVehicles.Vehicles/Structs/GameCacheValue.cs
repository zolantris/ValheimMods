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
  internal float IntervalInSeconds { get; set; } = intervalInSeconds;
  public bool IsCached { get; set; } = false;
  private float _timer { get; set; } = 0f;
  internal TCachedResult? CachedValue = default;

  private Func<TParams, TCachedResult?> GetValueUncached { get; set; } =
    callback;

  public TCachedResult? GetValue(TParams @params)
  {
    return IsCached ? CachedValue : GetValueUncachedAndCache(@params);
  }

  public TCachedResult? GetValue(TParams @params, bool flushCache)
  {
    if (flushCache)
    {
      return GetValueUncachedAndCache(@params);
    }

    return GetValue(@params);
  }

  private TCachedResult? GetValueUncachedAndCache(TParams @params)
  {
    var value = GetValueUncached(@params);
    SetCached(value);
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
    CachedValue = value;
    IsCached = true;
  }
}