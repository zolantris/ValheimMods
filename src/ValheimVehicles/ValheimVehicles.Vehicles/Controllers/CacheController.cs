using System;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Vehicles.Controllers;

namespace ValheimVehicles.Vehicles.Controllers;

public struct GameCacheValue<TCachedResult, TParams>(
  string name,
  float intervalInSeconds,
  Func<TParams, TCachedResult?> callback)
{
  public string Name { get; set; } = name;
  private float IntervalInSeconds { get; set; } = intervalInSeconds;
  public bool IsCached { get; set; } = false;
  private float _timer { get; set; } = 0f;
  private TCachedResult? _cachedValue = default(TCachedResult);

  private Func<TParams, TCachedResult?> GetValueUncached { get; set; } =
    callback;

  public TCachedResult? GetValue(TParams @params)
  {
    return IsCached ? _cachedValue : GetValueUncached(@params);
  }

  private void ResetCache()
  {
    IsCached = false;
  }

  public void SyncCache(float deltaSeconds)
  {
    _timer += deltaSeconds;
    if (!(_timer >= IntervalInSeconds)) return;

    ResetCache();
  }

  private void SetCached()
  {
    IsCached = true;
  }
}

public class CacheController : MonoBehaviour
{
  private static bool IsOnboardUncached(Character character)
  {
    var isCharacterWithinOnboardCollider =
      VehicleOnboardController.IsCharacterOnboard(character);
    var isCharacterStandingOnVehicle =
      WaterZoneHelper.HasShipUnderneath(character);
    return isCharacterWithinOnboardCollider && isCharacterStandingOnVehicle;
  }

  public static GameCacheValue<bool, Character> CachedOnboard =
    new("Onboard", 1f, IsOnboardUncached);

  /// <summary>
  /// Syncs caches per fixed update. This can be much more performant that any other callbacks of values that are not in fixed update
  /// </summary>
  private void FixedUpdate()
  {
    if (ZNet.m_instance == null) return;
    if (ZNetView.m_forceDisableInit) return;

    CachedOnboard.SyncCache(Time.deltaTime);
  }
}