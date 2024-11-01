using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using ValheimVehicles.Attributes;
using ValheimVehicles.Helpers;
using ValheimVehicles.Vehicles.Enums;

namespace ValheimVehicles.Vehicles.Controllers;

/// <summary>
/// This uses attributes to create a cache list and then each method that is calling will have a cache value and will not update within the time interval.
/// - This allows for all the cached logic to exist outside of this cache controller allowing for more organization.
/// </summary>
public class CacheController : MonoBehaviour
{
  private Dictionary<string, GameCacheValue<bool, Character>> _cacheValues =
    new();

  private void Awake()
  {
    // Use reflection to find all methods decorated with the GameCacheValueAttribute
    var methods = GetType().GetMethods(System.Reflection.BindingFlags.Static |
                                       System.Reflection.BindingFlags.Public);
    foreach (var method in methods)
    {
      var attribute = method.GetCustomAttribute<GameCacheValueAttribute>();
      if (attribute != null)
      {
        string methodName = method.Name;
        string className = method.DeclaringType?.Name;
        string name =
          attribute.Name ??
          $"{className}.{methodName}"; // Default to "<ClassName>.<MethodName>"

        // Create a GameCacheValue for each decorated method
        var cacheValue = new GameCacheValue<bool, Character>(
          name,
          attribute.IntervalInSeconds,
          (Func<Character, bool>)Delegate.CreateDelegate(
            typeof(Func<Character, bool>), method)
        );

        _cacheValues[name] = cacheValue;
      }
    }
  }

  public string ListCachedMethods()
  {
    var sb = new StringBuilder();
    sb.AppendLine("Cached Methods:");

    foreach (var kvp in _cacheValues)
    {
      var cacheValue = kvp.Value;
      sb.AppendLine($"  Name: {cacheValue.Name}");
      sb.AppendLine($"    Interval: {cacheValue.IntervalInSeconds} seconds");
      sb.AppendLine($"    Is Cached: {cacheValue.IsCached}");
      sb.AppendLine(
        $"    Cached Value: {cacheValue.GetCachedValue()}"); // Assuming Character can be null; replace with valid instance if needed
    }

    return sb.ToString();
  }

  private void FixedUpdate()
  {
    if (ZNet.m_instance == null) return;
    if (ZNetView.m_forceDisableInit) return;

    foreach (var cacheValue in _cacheValues.Values)
    {
      cacheValue.SyncCache(Time.deltaTime);
    }
  }
}