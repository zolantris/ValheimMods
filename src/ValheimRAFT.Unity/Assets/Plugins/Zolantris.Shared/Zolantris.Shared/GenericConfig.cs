// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;

namespace GenericConfigUtil
{
  /// <summary>
  /// Consumer-settable role for the current process/session.
  /// Wire this to your own auth/privilege source.
  /// </summary>
  public enum ConfigRole
  {
    User = 0,
    Advanced = 1,
    Admin = 2
  }

  /// <summary>
  /// Metadata to tag on ConfigEntries via ConfigDescription.DescriptionTags.
  /// Portable – no game-specific deps.
  /// </summary>
  public sealed class ConfigMeta
  {
    public bool IsAdvanced { get; set; }
    public bool IsAdmin { get; set; }
    public string Category { get; set; }
    public string Tooltip { get; set; }
    public int? Order { get; set; }
    public bool? ReadOnly { get; set; } // hard lock in UI (when supported)
    public bool? Hidden { get; set; } // hide in UI (when supported)

    /// <summary>Optional dynamic guards you can provide at runtime.</summary>
    public Func<bool> IsVisibleWhen { get; set; }
    public Func<bool> IsReadOnlyWhen { get; set; }
  }

  /// <summary>
  /// Registry that creates entries with isAdvanced/isAdmin semantics,
  /// attaches metadata to DescriptionTags, and (optionally) integrates
  /// with the BepInEx ConfigurationManager UI if present (any variant).
  /// </summary>
  public sealed class ConfigRegistry
  {
    private readonly ConfigFile _config;
    private readonly Dictionary<ConfigDefinition, ConfigMeta> _metaByDef = new();

    // Any discovered ConfigurationManagerAttributes type (official or Jötunn copy)
    private readonly Type _cmAttrType;
    private readonly string[] _cmCommonPropNames =
    {
      "Category", "Order", "ReadOnly", "Browsable", "IsAdvanced", "Tooltip", "Description", "DispName", "HideDefaultButton", "HideSettingName", "DefaultValue", "ShowRangeAsPercent"
    };

    /// <summary>Global role; set during plugin init based on your environment.</summary>
    public ConfigRole CurrentRole { get; set; } = ConfigRole.User;

    public ConfigRegistry(ConfigFile config)
    {
      if (config == null) throw new ArgumentNullException("config");
      _config = config;

      // Try to detect any ConfigurationManagerAttributes definition across loaded assemblies:
      _cmAttrType = FindConfigurationManagerAttributesType();
    }

    /// <summary>
    /// Creates a config entry with additional metadata and optional acceptable values.
    /// </summary>
    public ConfigEntry<T> CreateEntry<T>(
      string section,
      string key,
      T defaultValue,
      string description = null,
      ConfigMeta meta = null,
      AcceptableValueBase acceptableValues = null)
    {
      var def = new ConfigDefinition(section, key);

      var cmAttrInstance = BuildConfigurationManagerAttributes(meta);
      var tags = BuildTags(meta, cmAttrInstance);

      var configDesc = acceptableValues == null
        ? new ConfigDescription(description ?? string.Empty, null, tags)
        : new ConfigDescription(description ?? string.Empty, acceptableValues, tags);

      var entry = _config.Bind(def, defaultValue, configDesc);

      if (meta != null)
      {
        _metaByDef[def] = meta;
      }

      // Optional hard guard: if role is insufficient, make it effectively read-only by reverting changes.
      AttachRoleGuard(entry, meta);

      return entry;
    }

    /// <summary>
    /// Convenience: create an "advanced" entry.
    /// </summary>
    public ConfigEntry<T> CreateAdvanced<T>(
      string section, string key, T defaultValue,
      string description = null,
      AcceptableValueBase acceptableValues = null,
      string category = null,
      int? order = null)
    {
      var meta = new ConfigMeta { IsAdvanced = true, Category = category, Order = order };
      return CreateEntry(section, key, defaultValue, description, meta, acceptableValues);
    }

    /// <summary>
    /// Convenience: create an "admin-only" entry.
    /// </summary>
    public ConfigEntry<T> CreateAdmin<T>(
      string section, string key, T defaultValue,
      string description = null,
      AcceptableValueBase acceptableValues = null,
      string category = null,
      int? order = null,
      bool lockReadOnly = true)
    {
      var meta = new ConfigMeta
      {
        IsAdmin = true,
        Category = category,
        Order = order,
        ReadOnly = lockReadOnly
      };
      return CreateEntry(section, key, defaultValue, description, meta, acceptableValues);
    }

    /// <summary>
    /// Query: is this entry visible for the current role? Also consults meta.IsVisibleWhen.
    /// </summary>
    public bool IsVisible(ConfigDefinition def)
    {
      ConfigMeta meta;
      if (!_metaByDef.TryGetValue(def, out meta)) return true;
      if (meta.Hidden == true) return false;

      if (meta.IsAdmin && CurrentRole < ConfigRole.Admin) return false;
      if (meta.IsAdvanced && CurrentRole < ConfigRole.Advanced) return false;

      if (meta.IsVisibleWhen != null && !meta.IsVisibleWhen()) return false;

      return true;
    }

    /// <summary>
    /// Query: is this entry editable for the current role? Also consults meta.IsReadOnlyWhen.
    /// </summary>
    public bool IsEditable(ConfigDefinition def)
    {
      ConfigMeta meta;
      if (!_metaByDef.TryGetValue(def, out meta)) return true;

      if (meta.IsAdmin && CurrentRole < ConfigRole.Admin) return false;
      if (meta.ReadOnly == true) return false;
      if (meta.IsReadOnlyWhen != null && meta.IsReadOnlyWhen()) return false;

      return true;
    }

    /// <summary>
    /// Enumerate entries that should be shown for CurrentRole.
    /// </summary>
    public IEnumerable<ConfigEntryBase> VisibleEntries()
    {
      // _config implements IEnumerable<KeyValuePair<ConfigDefinition, ConfigEntryBase>>
      foreach (var kvp in _config)
      {
        var entry = kvp.Value;
        if (IsVisible(entry.Definition)) yield return entry;
      }
    }

    /// <summary>
    /// Re-apply UI attributes (e.g., when role changes at runtime).
    /// Call after changing CurrentRole if you use ConfigurationManager.
    /// </summary>
    public void RefreshUiAttributes()
    {
      if (_cmAttrType == null) return;

      foreach (var pair in _config)
      {
        var entry = pair.Value;
        var desc = entry.Description;
        ConfigMeta meta;
        _metaByDef.TryGetValue(entry.Definition, out meta);
        var cmAttr = BuildConfigurationManagerAttributes(meta);

        var newTags = BuildTags(meta, cmAttr);
        var newDesc = new ConfigDescription(desc.Description, desc.AcceptableValues, newTags);

        var prop = typeof(ConfigEntryBase).GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null) prop.SetValue(entry, newDesc, null);
      }
    }

    private void AttachRoleGuard<T>(ConfigEntry<T> entry, ConfigMeta meta)
    {
      if (meta == null) return;

      entry.SettingChanged += (sender, args) =>
      {
        if (!IsEditable(entry.Definition))
        {
          _config.Reload(); // revert unauthorized changes
        }
      };
    }

    private object[] BuildTags(ConfigMeta meta, object cmAttr)
    {
      if (meta == null && cmAttr == null) return new object[0];
      if (meta != null && cmAttr != null) return new object[] { meta, cmAttr };
      return meta != null ? new object[] { meta } : new object[] { cmAttr };
    }

    private object BuildConfigurationManagerAttributes(ConfigMeta meta)
    {
      if (_cmAttrType == null) return null;

      var attr = Activator.CreateInstance(_cmAttrType);
      if (attr == null) return null;

      Action<string, object> Set = (name, value) =>
      {
        var prop = _cmAttrType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null) prop.SetValue(attr, value, null);
      };

      if (meta != null)
      {
        if (!string.IsNullOrEmpty(meta.Category)) SetSafe(Set, "Category", meta.Category);
        if (meta.Order.HasValue) SetSafe(Set, "Order", meta.Order.Value);
        if (meta.ReadOnly.HasValue) SetSafe(Set, "ReadOnly", meta.ReadOnly.Value);

        // Role-based visibility (UI-level)
        var role = CurrentRole;
        var visibleByRole = !(meta.IsAdmin && role < ConfigRole.Admin) &&
                            !(meta.IsAdvanced && role < ConfigRole.Advanced);

        if (meta.Hidden == true || !visibleByRole || meta.IsVisibleWhen != null && !meta.IsVisibleWhen())
        {
          SetSafe(Set, "Browsable", false);
        }

        if (meta.IsAdvanced)
        {
          SetSafe(Set, "IsAdvanced", true);
        }

        if (!string.IsNullOrEmpty(meta.Tooltip))
        {
          SetSafe(Set, "Tooltip", meta.Tooltip);
        }
      }

      return attr;
    }

    private void SetSafe(Action<string, object> setter, string name, object value)
    {
      // Only set if the CM variant actually exposes the property
      if (_cmAttrType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null)
        setter(name, value);
    }

    private static Type FindConfigurationManagerAttributesType()
    {
      // Try several common fully-qualified names first
      var candidates = new[]
      {
        "ConfigurationManager.Attributes.ConfigurationManagerAttributes, ConfigurationManager",
        "ConfigurationManager.ConfigurationManagerAttributes, ConfigurationManager",
        "ConfigurationManager.ConfigurationManagerAttributes, BepInEx.ConfigurationManager"
      };

      foreach (var name in candidates)
      {
        var t = Type.GetType(name, false);
        if (t != null) return t;
      }

      // Fallback: scan all loaded assemblies for a public type named "ConfigurationManagerAttributes"
      foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
      {
        try
        {
          var t = asm.GetTypes().FirstOrDefault(x => x.Name == "ConfigurationManagerAttributes");
          if (t != null && t.IsClass && !t.IsAbstract) return t;
        }
        catch
        {
          // ignore dynamic/ReflectionTypeLoadException assemblies
        }
      }

      return null;
    }
  }
}