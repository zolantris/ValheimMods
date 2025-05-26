// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace Zolantris.Shared
{
  public static class BepInExConfigUtils
  {
    private static readonly HashSet<string> BoundKeys = new();

    private static void EnsureUniqueKey(string section, string key, string file, int line)
    {
      var fullKey = $"{section}.{key}";
      if (!BoundKeys.Add(fullKey))
      {
        throw new InvalidOperationException($"[BepInExConfigUtils] Duplicate config key: '{fullKey}'\n  - Source: {file}:{line}");
      }
    }

    // Generic fallback — only supports ConfigDescription to avoid ambiguity with string
    public static ConfigEntry<T> BindUnique<T>(
      this ConfigFile config,
      string section,
      string key,
      T defaultValue,
      ConfigDescription desc,
      [CallerFilePath] string file = "",
      [CallerLineNumber] int line = 0)
    {
      EnsureUniqueKey(section, key, file, line);
      return config.Bind(section, key, defaultValue, desc);
    }

    // Typed overloads to disambiguate all primitive types + string
    public static ConfigEntry<bool> BindUnique(
      this ConfigFile config,
      string section,
      string key,
      bool defaultValue,
      string description,
      [CallerFilePath] string file = "",
      [CallerLineNumber] int line = 0)
    {
      EnsureUniqueKey(section, key, file, line);
      return config.Bind(section, key, defaultValue, new ConfigDescription(description));
    }

    public static ConfigEntry<int> BindUnique(
      this ConfigFile config,
      string section,
      string key,
      int defaultValue,
      string description,
      [CallerFilePath] string file = "",
      [CallerLineNumber] int line = 0)
    {
      EnsureUniqueKey(section, key, file, line);
      return config.Bind(section, key, defaultValue, new ConfigDescription(description));
    }

    public static ConfigEntry<float> BindUnique(
      this ConfigFile config,
      string section,
      string key,
      float defaultValue,
      string description,
      [CallerFilePath] string file = "",
      [CallerLineNumber] int line = 0)
    {
      EnsureUniqueKey(section, key, file, line);
      return config.Bind(section, key, defaultValue, new ConfigDescription(description));
    }

    public static ConfigEntry<string> BindUnique(
      this ConfigFile config,
      string section,
      string key,
      string defaultValue,
      string description,
      [CallerFilePath] string file = "",
      [CallerLineNumber] int line = 0)
    {
      EnsureUniqueKey(section, key, file, line);
      return config.Bind(section, key, defaultValue, new ConfigDescription(description));
    }
  }
}