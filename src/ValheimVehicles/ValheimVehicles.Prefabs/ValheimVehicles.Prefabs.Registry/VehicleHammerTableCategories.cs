// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimVehicles.Prefabs.Registry
{
  /// <summary>
  /// Stable category identifiers (English) + mapping to localization keys.
  /// Keep ALL game logic/config using the English canonical names.
  /// Only the UI layer turns these into localized labels.
  /// </summary>
  public static class VehicleHammerTableCategories
  {
    // Canonical (stable) category IDs used everywhere in config & code
    public const string Vehicles = "Vehicles";
    public const string Tools = "Tools";
    public const string Propulsion = "Propulsion";
    public const string Power = "Power";
    public const string Structure = "Structure";
    public const string Hull = "Hull";
    public const string Deprecated = "Deprecated";

    /// <summary>
    /// Default order of canonical IDs (used to append any missing that user omitted).
    /// </summary>
    public static readonly List<string> AllVehicleHammerCategoriesFallbackNames =
      new() { Tools, Hull, Structure, Power, Propulsion, Vehicles, Deprecated };

    /// <summary>
    /// English canonical -> localization key ($key) mapping.
    /// If a canonical is missing here, we fall back to English at runtime.
    /// </summary>
    private static readonly Dictionary<string, string> EnglishToLocKey =
      new(StringComparer.Ordinal)
      {
        { Tools, "$valheim_vehicles_build_hammer_category_tools" },
        { Hull, "$valheim_vehicles_build_hammer_category_hull" },
        { Structure, "$valheim_vehicles_build_hammer_category_structure" },
        { Power, "$valheim_vehicles_build_hammer_category_power" },
        { Propulsion, "$valheim_vehicles_build_hammer_category_propulsion" },
        { Vehicles, "$valheim_vehicles_build_hammer_category_vehicles" },
        { Deprecated, "$valheim_vehicles_build_hammer_category_deprecated" }
      };

    /// <summary>
    /// Returns true if the provided string is a valid canonical category ID (English).
    /// </summary>
    public static bool IsHammerTableCategory(string val)
    {
      return AllVehicleHammerCategoriesFallbackNames.Contains(val);
    }

    /// <summary>
    /// Map a canonical category (English) to a localized label for display.
    /// Falls back to the canonical if no Localization instance or key is missing.
    /// </summary>
    public static string ToLocalizedLabel(string canonical)
    {
      // Defensive guard: if we don't know this canonical, just show what we got.
      if (string.IsNullOrEmpty(canonical))
        return canonical ?? string.Empty;

      // No localization system loaded yet -> show English canonical
      if (Localization.instance == null)
        return canonical;

      // If we have a key mapping, localize that. Otherwise, try a "$"-prefixed guess, then fall back.
      if (EnglishToLocKey.TryGetValue(canonical, out var key))
      {
        var localized = Localization.instance.Localize(key);
        return string.IsNullOrEmpty(localized) ? canonical : localized;
      }

      // Optional heuristic: allow "$<canonical>" if you ever decide to migrate canonicals to keys.
      var probe = "$" + canonical;
      var maybe = Localization.instance.Localize(probe);
      return string.IsNullOrEmpty(maybe) ? canonical : maybe;
    }

    /// <summary>
    /// Utility: map an entire list of canonicals to localized labels.
    /// </summary>
    public static List<string> ToLocalizedLabels(IEnumerable<string> canonicals)
    {
      return canonicals.Select(ToLocalizedLabel).ToList();
    }

    /// <summary>
    /// Expose a copy of the mapping (read-only) if needed by diagnostics.
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetCanonicalToKeyMap()
    {
      return EnglishToLocKey;
    }
  }
}