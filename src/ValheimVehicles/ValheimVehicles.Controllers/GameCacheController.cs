using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using ValheimVehicles.Attributes;
using ValheimVehicles.Components;
using ValheimVehicles.Structs;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Controllers;

/// <summary>
/// This uses attributes to create a cache list and then each method that is calling will have a cache value and will not update within the time interval.
/// - This allows for all the cached logic to exist outside of this cache controller allowing for more organization.
///
/// TODO this component does nothing currently
/// </summary>
public class GameCacheController : MonoBehaviour
{
  private Dictionary<string, GameCacheValue<object, object>> _cacheValues =
    new();

  // todo add a way to basically cache every callback within WaterZoneCharacterData as a "GameCacheValue" so it only updates per unique character instance per XYZ time.
  private Dictionary<ZDOID, WaterZoneCharacterData> WaterZoneCharacterDatas =
    new();

  // private void Awake()
  // {
  //   // Use reflection to find all methods with the GameCacheValue attribute
  //   foreach (MethodInfo method in GetType().GetMethods(BindingFlags.Instance |
  //              BindingFlags.Public | BindingFlags.NonPublic))
  //   {
  //     var attribute = method.GetCustomAttribute<GameCacheValueAttribute>();
  //     if (attribute != null)
  //     {
  //       // Create a delegate for the method, handling any return type and parameters
  //       var parameters = method.GetParameters();
  //       if (_cacheValues.ContainsKey(attribute.Name))
  //       {
  //         Logger.LogError(
  //           $"GameCacheController attempted to register a duplicate key {attribute.Name} to cache. This should not be allowed. This registration will be skipped.");
  //         return;
  //       }
  //
  //       if (parameters.Length == 0)
  //       {
  //         // Handle methods with no parameters
  //         var callback =
  //           (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), this,
  //             method);
  //         var cacheValue = CreateCacheValue(attribute.Name,
  //           attribute.IntervalInSeconds, callback);
  //         _cacheValues[attribute.Name] = cacheValue;
  //       }
  //       else
  //       {
  //         // Handle methods with parameters
  //         var callbackType =
  //           typeof(Func<,>).MakeGenericType(parameters[0].ParameterType,
  //             method.ReturnType);
  //         var callback = Delegate.CreateDelegate(callbackType, this, method);
  //         var cacheValue = CreateCacheValue(attribute.Name,
  //           attribute.IntervalInSeconds, callback);
  //         _cacheValues[attribute.Name] = cacheValue;
  //       }
  //     }
  //   }
  // }
  public static object? WithCached(Func<object, object> cachedFunction)
  {
    if (cachedFunction == null)
      throw new Exception("No cached function provided");

    return null;
  }

  private GameCacheValue<object, object> CreateCacheValue(string name,
    float intervalInSeconds,
    Delegate callback)
  {
    var cacheValueType = typeof(GameCacheValue<object, object>).MakeGenericType(
      callback.Method.ReturnType,
      callback.Method.GetParameters()[0].ParameterType);
    var cacheValueInstance = Activator.CreateInstance(cacheValueType, name,
      intervalInSeconds, callback);
    return (GameCacheValue<object, object>)cacheValueInstance;
  }

  private void OnDestroy()
  {
    _cacheValues.Clear();
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
        $"    Cached Value: {cacheValue.CachedValue}"); // Assuming Character can be null; replace with valid instance if needed
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