// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Linq;
using ValheimVehicles.BepInExConfig;

namespace ValheimVehicles.SharedScripts
{
  /// <summary>
  /// Piece menu sorting utilities that optionally cluster items by material
  /// based on PrefabConfig.SortModePieceMenuItemsByMaterial.Value.
  /// </summary>
  public static class PieceMenuSortUtil
  {
    /// <summary>
    /// If PrefabConfig.SortModePieceMenuItemsByMaterial.Value is true, returns a new list
    /// clustered by material according to <paramref name="materialPriority"/>.
    /// Otherwise returns the original list reference unchanged.
    /// Preserves original order within each material bucket (stable).
    /// </summary>
    /// <param name="items">Source list (not mutated unless you call the InPlace variant).</param>
    /// <param name="materialPriority">
    /// Optional ordered material priority (e.g., ["wood","iron","bronze"]). Defaults to ["wood","iron"].
    /// </param>
    /// <param name="placeUnknownLast">
    /// Unknown materials go last if true; first if false.
    /// </param>
    /// <param name="getMaterial">
    /// Optional extractor for a material token from an item. Defaults to suffix after last underscore.
    /// </param>
    public static List<string> MaybeSortByMaterial(
      IReadOnlyList<string> items,
      IReadOnlyList<string> materialPriority = null,
      bool placeUnknownLast = true,
      Func<string, string> getMaterial = null)
    {
      // Guard: no items or sorting disabled -> return original reference (no allocs)
      if (items == null || items.Count == 0) return items as List<string> ?? new List<string>();
      if (!IsMaterialSortEnabled())
        return items as List<string> ?? new List<string>(items);

      materialPriority ??= s_defaultPriority;
      getMaterial ??= DefaultGetMaterial;

      var rank = BuildRank(materialPriority);
      var unknownRank = placeUnknownLast ? materialPriority.Count : -1;

      // Stable: (rank, originalIndex)
      return items
        .Select((val, idx) =>
        {
          var mat = getMaterial(val);
          var r = rank.TryGetValue(mat, out var rr) ? rr : unknownRank;
          return new Key(val, idx, r);
        })
        .OrderBy(k => k.Rank)
        .ThenBy(k => k.Index)
        .Select(k => k.Value)
        .ToList();
    }

    /// <summary>
    /// In-place variant. Mutates <paramref name="items"/> only when sorting is enabled.
    /// Stability is enforced via original index as a tiebreaker.
    /// </summary>
    public static void MaybeSortByMaterialInPlace(
      List<string> items,
      IReadOnlyList<string> materialPriority = null,
      bool placeUnknownLast = true,
      Func<string, string> getMaterial = null)
    {
      if (items == null || items.Count == 0) return;
      if (!IsMaterialSortEnabled()) return;

      materialPriority ??= s_defaultPriority;
      getMaterial ??= DefaultGetMaterial;

      var rank = BuildRank(materialPriority);
      var unknownRank = placeUnknownLast ? materialPriority.Count : -1;

      // Precompute keys once (avoid O(n^2) IndexOf)
      var keys = new Key[items.Count];
      for (var i = 0; i < items.Count; i++)
      {
        var mat = getMaterial(items[i]);
        var r = rank.TryGetValue(mat, out var rr) ? rr : unknownRank;
        keys[i] = new Key(items[i], i, r);
      }

      var idxs = Enumerable.Range(0, items.Count).ToArray();
      Array.Sort(idxs, (a, b) =>
      {
        var ka = keys[a];
        var kb = keys[b];
        var cmp = ka.Rank.CompareTo(kb.Rank);
        return cmp != 0 ? cmp : ka.Index.CompareTo(kb.Index);
      });

      // Apply permutation
      var tmp = new List<string>(items.Count);
      for (var i = 0; i < idxs.Length; i++) tmp.Add(items[idxs[i]]);
      for (var i = 0; i < items.Count; i++) items[i] = tmp[i];
    }

    /// <summary>
    /// Default: "wood" before "iron".
    /// </summary>
    private static readonly string[] s_defaultPriority = { "wood", "iron" };

    /// <summary>
    /// Default material extractor: substring after the last underscore.
    /// "hull_bow_tri_left_iron" -> "iron".
    /// </summary>
    private static string DefaultGetMaterial(string s)
    {
      if (string.IsNullOrEmpty(s)) return string.Empty;
      var i = s.LastIndexOf('_');
      return i >= 0 && i + 1 < s.Length ? s.Substring(i + 1) : string.Empty;
    }

    private static Dictionary<string, int> BuildRank(IReadOnlyList<string> materials)
    {
      var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
      for (var i = 0; i < materials.Count; i++) map[materials[i]] = i;
      return map;
    }

    private readonly struct Key
    {
      public readonly string Value;
      public readonly int Index;
      public readonly int Rank;
      public Key(string value, int index, int rank)
      {
        Value = value;
        Index = index;
        Rank = rank;
      }
    }

    /// <summary>
    /// Centralized guard for toggling material-based sort. Kept as a method so callers
    /// can be updated in one spot if the flag or access pattern changes.
    /// </summary>
    private static bool IsMaterialSortEnabled()
    {
      // Assumes your config object exists in scope:
      // PrefabConfig.SortModePieceMenuItemsByMaterial.Value
      // If the config object can be null during early boot, guard accordingly.
      try
      {
        return PrefabConfig.SortModePieceMenuItemsByMaterial.Value;
      }
      catch
      {
        // Fail-safe: no sort if config isn't ready yet.
        return false;
      }
    }
  }
}