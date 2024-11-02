using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.PlayerLoop;
using ValheimVehicles.Attributes;
using ValheimVehicles.Helpers;
using ValheimVehicles.Vehicles.Structs;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Controllers;

/// <summary>
/// This uses attributes to create a cache list and then each method that is calling will have a cache value and will not update within the time interval.
/// - This allows for all the cached logic to exist outside of this cache controller allowing for more organization.
/// </summary>
public class GameCacheController : MonoBehaviour
{
  private static readonly Dictionary<string, GameCacheValue<object, object>>
    CacheValues = new();

  public void InitAllMethodsFromAssemblies()
  {
    // Get all types from all loaded assemblies
    // .Where(assembly => assembly.FullName.Contains("ValheimRAFT"))
    var allTypes = AppDomain.CurrentDomain.GetAssemblies()
      .SelectMany(assembly => assembly.GetTypes())
      .Where(type => !type.IsAbstract && !type.IsGenericType)
      .ToList();

    foreach (var type in allTypes)
    {
      RegisterMethodsFromType(type);
    }
  }

  public void InitAllMethodsFromSupportedClasses(Type[] supportedClasses)
  {
    foreach (var type in supportedClasses)
    {
      RegisterMethodsFromType(type);
    }
  }

  private void RegisterMethodsFromType(Type type)
  {
    foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance |
                                                  BindingFlags.Public |
                                                  BindingFlags.NonPublic))
    {
      var attribute = method.GetCustomAttribute<GameCacheValueAttribute>();
      if (attribute != null)
      {
        RegisterMethod(method, attribute);
      }
    }
  }

  private void RegisterMethod(MethodInfo method,
    GameCacheValueAttribute attribute)
  {
    var parameters = method.GetParameters();
    if (CacheValues.ContainsKey(attribute.Name))
    {
      Logger.LogError(
        $"Duplicate key {attribute.Name} to cache. This registration will be skipped.");
      return;
    }

    Delegate callback;
    if (parameters.Length == 0)
    {
      // Handle methods with no parameters
      callback =
        (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), this,
          method);
    }
    else
    {
      // Handle methods with parameters
      var callbackType =
        typeof(Func<,>).MakeGenericType(parameters[0].ParameterType,
          method.ReturnType);
      callback = Delegate.CreateDelegate(callbackType, this, method);
    }

    var cacheValue = CreateCacheValue(attribute.Name,
      attribute.IntervalInSeconds, callback);
    CacheValues[attribute.Name] = cacheValue;
  }

  private GameCacheValue<object, object> CreateCacheValue(string name,
    float intervalInSeconds, Delegate callback)
  {
    var cacheValueType = typeof(GameCacheValue<object, object>).MakeGenericType(
      callback.Method.ReturnType,
      callback.Method.GetParameters()[0].ParameterType);
    var cacheValueInstance = Activator.CreateInstance(cacheValueType, name,
      intervalInSeconds, callback);
    return (GameCacheValue<object, object>)cacheValueInstance;
  }


  // Example of initializing from supported classes
  private Type[] CachedClasses = new Type[]
  {
    typeof(WaterZoneHelpers),
    // Add other controller types here
  };

  public void Setup(Type[] supportedClasses)
  {
    // Example of initializing from all assemblies
    InitAllMethodsFromAssemblies();
    // InitAllMethodsFromSupportedClasses(supportedClasses);

    // List all cached methods
    ListCachedMethods();
  }

  private void Awake()
  {
    Setup(CachedClasses);
  }

  private void OnDestroy()
  {
    CacheValues.Clear();
  }

  public static void ListCachedMethods()
  {
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("Cached Methods:");

    foreach (var kvp in CacheValues)
    {
      var cacheValue = kvp.Value;
      sb.AppendLine($"  Name: {cacheValue.Name}");
      sb.AppendLine($"    Interval: {cacheValue.IntervalInSeconds} seconds");
      sb.AppendLine($"    Is Cached: {cacheValue.IsCached}");
      sb.AppendLine($"    Cached Value: {cacheValue.CachedValue}");
    }

    sb.AppendLine($"Total Cached Methods: {CacheValues.Count}");
    Logger.LogInfo(sb.ToString());
  }

  public static bool IsOnboard(Character character)
  {
    ListCachedMethods();
    if (CacheValues.TryGetValue("IsOnboard", out var isOnboard))
    {
      return (bool)isOnboard.GetValue(character);
    }

    return false;
  }

  /// <summary>
  /// Main way to call the method
  /// </summary>
  /// <param name="methodName"></param>
  /// <param name="parameter"></param>
  /// <returns></returns>
  public static object? GetCachedValue(string methodName, object parameter)
  {
    if (CacheValues.TryGetValue(methodName, out var cacheValue))
    {
      return cacheValue.GetValue(parameter);
    }

    Logger.LogError($"No cached method found with name: {methodName}");
    return null;
  }
}