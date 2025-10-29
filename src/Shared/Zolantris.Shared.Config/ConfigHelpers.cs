// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;

namespace Zolantris.Shared
{
  /// <summary>
  /// Extend your existing CreateConfigDescription to carry Admin/Advanced + UI hints
  /// without changing your call sites. Works with:
  /// - Official BepInEx ConfigurationManager
  /// - Jötunn's ConfigurationManagerAttributes copy (supports IsAdminOnly)
  /// Falls back cleanly if neither is present.
  /// </summary>
  public static class ConfigHelpers
  {
    /// <summary>
    /// Portable tag your UI (or other mods) can read from ConfigDescription.DescriptionTags.
    /// </summary>
    public sealed class UiMetaTag
    {
      public AcceptableValueBase AcceptableValues { get; set; }
      public bool IsAdmin { get; set; }
      public bool IsAdvanced { get; set; }
      public string Category { get; set; }
      public int? Order { get; set; }
      public bool? ReadOnly { get; set; }
      public bool? Hidden { get; set; }
      public string Tooltip { get; set; }
      public bool Synchronized { get; set; } // intent only; wire to ServerSync explicitly
    }

    /// <summary>
    /// Backwards-compatible signature used across your configs:
    /// description, synchronize (intent), isAdmin, acceptableValues
    /// Extra optional UI args keep old call sites intact.
    /// </summary>
    public static ConfigDescription CreateConfigDescription(
      string description,
      AcceptableValueBase acceptableValues = null,
      bool isSynchronized = true,
      bool isAdvanced = false,
      string category = null,
      int? order = null,
      bool? readOnly = null,
      bool? hidden = null,
      string tooltip = null)
    {
      var uiTag = new UiMetaTag
      {
        IsAdmin = isSynchronized,
        IsAdvanced = isAdvanced,
        Category = category,
        Order = order,
        ReadOnly = readOnly,
        Hidden = hidden,
        Tooltip = tooltip,
        Synchronized = isSynchronized,
        AcceptableValues = acceptableValues
      };

      var cmAttrType = FindConfigurationManagerAttributesType();
      var cmAttr = BuildConfigurationManagerAttributes(cmAttrType, uiTag);

      object[] tags;
      if (cmAttr != null)
        tags = new object[] { uiTag, cmAttr };
      else
        tags = new object[] { uiTag };

      return acceptableValues == null
        ? new ConfigDescription(description ?? string.Empty, null, tags)
        : new ConfigDescription(description ?? string.Empty, acceptableValues, tags);
    }

    // ---------- internals ----------

    private static Type FindConfigurationManagerAttributesType()
    {
      // Known names: official CM and Jötunn's embedded copy (sometimes same name)
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

      // Fallback: scan for any public type named "ConfigurationManagerAttributes"
      foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
      {
        try
        {
          var t = asm.GetTypes().FirstOrDefault(x => x.Name == "ConfigurationManagerAttributes");
          if (t != null && t.IsClass && !t.IsAbstract) return t;
        }
        catch
        {
          /* ignore reflection load errors */
        }
      }
      return null;
    }

    private static object BuildConfigurationManagerAttributes(Type cmAttrType, UiMetaTag meta)
    {
      if (cmAttrType == null) return null;
      var attr = Activator.CreateInstance(cmAttrType);
      if (attr == null) return null;

      void SetIf(string name, object value)
      {
        var p = cmAttrType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (p != null) p.SetValue(attr, value, null);
      }

      // Common CM fields
      if (!string.IsNullOrEmpty(meta.Category)) SetIf("Category", meta.Category);
      if (meta.Order.HasValue) SetIf("Order", meta.Order.Value);
      if (meta.ReadOnly.HasValue) SetIf("ReadOnly", meta.ReadOnly.Value);
      if (!string.IsNullOrEmpty(meta.Tooltip)) SetIf("Tooltip", meta.Tooltip);

      // Some CM builds support "IsAdvanced"
      if (meta.IsAdvanced) SetIf("IsAdvanced", true);

      // Jötunn's copy also supports "IsAdminOnly" and will auto-lock for non-admins
      if (meta.IsAdmin) SetIf("IsAdminOnly", true);

      // Hide entry, if requested
      if (meta.Hidden == true) SetIf("Browsable", false);

      return attr;
    }
  }
}