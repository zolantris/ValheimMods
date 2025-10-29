// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        throw new InvalidOperationException($"[BepInExConfigUtils] Duplicate config key: '{fullKey}'\n  - Source: {file}:{line}");
    }

    // ---------------- Core struct ----------------

    public sealed class BepInExConfigSettings
    {
      public AcceptableValueBase AcceptableValues { get; set; }

      // Roles / intent
      public bool Synchronize { get; set; }
      public bool IsAdmin { get; set; }
      public bool IsAdvanced { get; set; }

      // UI
      public string Category { get; set; }
      public int? Order { get; set; }
      public bool? ReadOnly { get; set; }
      public bool? Hidden { get; set; }
      public string Tooltip { get; set; }

      // Internal meta tag type for UI systems
      internal class UiMetaTag
      {
        public bool IsAdmin;
        public bool IsAdvanced;
        public string Category;
        public int? Order;
        public bool? ReadOnly;
        public bool? Hidden;
        public string Tooltip;
        public bool Synchronized;
      }
    }

    public static ConfigEntry<string> BindJson<T>(
      this ConfigFile config, string section, string key, T defaultValue, string description = "")
    {
      var json = Newtonsoft.Json.JsonConvert.SerializeObject(defaultValue, Newtonsoft.Json.Formatting.None);
      return config.Bind(section, key, json, new ConfigDescription(description));
    }

    public static T ReadJson<T>(this ConfigEntry<string> entry)
    {
      return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(entry.Value);
    }

    public static void WriteJson<T>(this ConfigEntry<string> entry, T value)
    {
      entry.Value = Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.None);
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

    // ---------------- BindUnique overload using description + settings ----------------

    public static ConfigEntry<T> BindUnique<T>(
      this ConfigFile config,
      string section,
      string key,
      T defaultValue,
      string description,
      BepInExConfigSettings settings,
      [CallerFilePath] string file = "",
      [CallerLineNumber] int line = 0)
    {
      EnsureUniqueKey(section, key, file, line);
      var desc = BuildDescription(description, settings);
      return config.Bind(section, key, defaultValue, desc);
    }

    // Optional: integrate with ServerSync adapter if you want
    public interface IConfigSyncAdapter
    {
      void Register<T>(ConfigEntry<T> entry, bool adminLock, bool synchronize);
    }

    public static ConfigEntry<T> BindUniqueWithSync<T>(
      this ConfigFile config,
      string section,
      string key,
      T defaultValue,
      string description,
      BepInExConfigSettings settings,
      IConfigSyncAdapter sync,
      [CallerFilePath] string file = "",
      [CallerLineNumber] int line = 0)
    {
      var entry = BindUnique(config, section, key, defaultValue, description, settings, file, line);
      if (sync != null)
      {
        sync.Register(entry, settings.IsAdmin, settings.Synchronize);
      }
      return entry;
    }

    // ---------------- internals ----------------

    private static ConfigDescription BuildDescription(string description, BepInExConfigSettings s)
    {
      var meta = new BepInExConfigSettings.UiMetaTag
      {
        IsAdmin = s.IsAdmin,
        IsAdvanced = s.IsAdvanced,
        Category = s.Category,
        Order = s.Order,
        ReadOnly = s.ReadOnly,
        Hidden = s.Hidden,
        Tooltip = s.Tooltip,
        Synchronized = s.Synchronize
      };

      var cmType = FindConfigurationManagerAttributesType();
      var cmAttr = BuildConfigurationManagerAttributes(cmType, meta);

      var tags = cmAttr != null ? new object[] { meta, cmAttr } : new object[] { meta };

      return new ConfigDescription(description ?? string.Empty, s.AcceptableValues, tags);
    }

    private static Type FindConfigurationManagerAttributesType()
    {
      var candidates = new[]
      {
        "ConfigurationManager.Attributes.ConfigurationManagerAttributes, ConfigurationManager",
        "ConfigurationManager.ConfigurationManagerAttributes, ConfigurationManager",
        "ConfigurationManager.ConfigurationManagerAttributes, BepInEx.ConfigurationManager"
      };
      foreach (var n in candidates)
      {
        var t = Type.GetType(n, false);
        if (t != null) return t;
      }
      foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
      {
        try
        {
          var t = asm.GetTypes().FirstOrDefault(x => x.Name == "ConfigurationManagerAttributes");
          if (t != null && !t.IsAbstract) return t;
        }
        catch {}
      }
      return null;
    }

    private static object BuildConfigurationManagerAttributes(Type cmAttrType, BepInExConfigSettings.UiMetaTag meta)
    {
      if (cmAttrType == null) return null;
      var attr = Activator.CreateInstance(cmAttrType);
      if (attr == null) return null;

      void Set(string name, object val)
      {
        var p = cmAttrType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        p?.SetValue(attr, val, null);
      }

      if (!string.IsNullOrEmpty(meta.Category)) Set("Category", meta.Category);
      if (meta.Order.HasValue) Set("Order", meta.Order.Value);
      if (meta.ReadOnly.HasValue) Set("ReadOnly", meta.ReadOnly.Value);
      if (!string.IsNullOrEmpty(meta.Tooltip)) Set("Tooltip", meta.Tooltip);
      if (meta.Hidden == true) Set("Browsable", false);
      if (meta.IsAdvanced) Set("IsAdvanced", true);
      if (meta.IsAdmin) Set("IsAdminOnly", true); // Jötunn’s copy supports this
      return attr;
    }
  }
}