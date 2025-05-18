using System;
using System.Collections.Generic;
using BepInEx.Configuration;
namespace ValheimVehicles.Config;

public class BepInExConfigUtils
{
  private static readonly HashSet<string> BoundKeys = new();

  public static ConfigEntry<T> BindUnique<T>(ConfigFile config, string section, string key, T defaultValue, ConfigDescription desc)
  {
    var fullKey = $"{section}.{key}";
    if (!BoundKeys.Add(fullKey))
    {
      throw new InvalidOperationException($"Config key already bound: {fullKey}");
    }

    return config.Bind(section, key, defaultValue, desc);
  }
}