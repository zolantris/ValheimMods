using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using ValheimScalability.BepInExConfig;

namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Central parsed/cached user filter engine.
///
/// Precedence:
/// 1. Layer/object/material/shader exclude -> Original.
/// 2. Include filters (when configured) must match.
/// 3. Wind/vegetation hints -> Instanced when supported.
/// 4. Otherwise -> CombinedMesh.
///
/// User filter config is cached and rebuilt only after SettingChanged.
/// </summary>
public static class ClusterBatchFilter
{
  private static readonly object Sync = new();

  private static bool _dirty = true;

  private static HashSet<int> _includedLayers = new();
  private static HashSet<int> _excludedLayers = new();

  private static string[] _includedObjectNames = Array.Empty<string>();
  private static string[] _excludedObjectNames = Array.Empty<string>();
  private static Regex[] _includedObjectRegex = Array.Empty<Regex>();
  private static Regex[] _excludedObjectRegex = Array.Empty<Regex>();

  private static string[] _excludedMaterialNames = Array.Empty<string>();
  private static Regex[] _excludedMaterialRegex = Array.Empty<Regex>();

  private static string[] _excludedShaderNames = Array.Empty<string>();
  private static Regex[] _excludedShaderRegex = Array.Empty<Regex>();

  private static string[] _instancedObjectHints = Array.Empty<string>();
  private static string[] _instancedMaterialHints = Array.Empty<string>();
  private static string[] _instancedShaderHints = Array.Empty<string>();
  private static Regex[] _instancedCandidateRegex = Array.Empty<Regex>();
  private static string[] _windPropertyNames = Array.Empty<string>();

  private static bool _hasObjectIncludeFilter;

  public static void Invalidate()
  {
    lock (Sync)
    {
      _dirty = true;
    }
  }

  public static bool IsLayerAllowed(int layer)
  {
    EnsureParsed();

    if (_excludedLayers.Contains(layer))
    {
      return false;
    }

    return _includedLayers.Count == 0 ||
           _includedLayers.Contains(layer);
  }

  public static bool IsObjectAllowed(
    string rootName,
    string rendererName)
  {
    EnsureParsed();

    if (MatchesSubstring(rootName, _excludedObjectNames) ||
        MatchesSubstring(rendererName, _excludedObjectNames) ||
        MatchesRegex(rootName, _excludedObjectRegex) ||
        MatchesRegex(rendererName, _excludedObjectRegex))
    {
      return false;
    }

    if (!_hasObjectIncludeFilter)
    {
      return true;
    }

    return MatchesSubstring(rootName, _includedObjectNames) ||
           MatchesSubstring(rendererName, _includedObjectNames) ||
           MatchesRegex(rootName, _includedObjectRegex) ||
           MatchesRegex(rendererName, _includedObjectRegex);
  }

  public static bool IsMaterialAllowed(Material material)
  {
    EnsureParsed();

    if (!material)
    {
      return false;
    }

    return !MatchesSubstring(material.name, _excludedMaterialNames) &&
           !MatchesRegex(material.name, _excludedMaterialRegex);
  }

  public static bool IsShaderAllowed(Shader shader)
  {
    EnsureParsed();

    if (!shader)
    {
      return false;
    }

    return !MatchesSubstring(shader.name, _excludedShaderNames) &&
           !MatchesRegex(shader.name, _excludedShaderRegex);
  }

  public static bool IsInstancingCandidate(
    string rootName,
    string rendererName,
    Material[] materials)
  {
    EnsureParsed();

    // This method classifies transform-dependent/wind-style renderers.
    // It intentionally does NOT check whether GPU instancing is enabled/supported.
    // A classified wind renderer must never fall through to CombineMeshes just
    // because instancing is unavailable; the caller will keep it original instead.
    if (MatchesSubstring(rootName, _instancedObjectHints) ||
        MatchesSubstring(rendererName, _instancedObjectHints))
    {
      return true;
    }

    var descriptor = rootName + "|" + rendererName;

    foreach (var material in materials)
    {
      if (!material)
      {
        continue;
      }

      descriptor += "|" + material.name;

      if (MatchesSubstring(material.name, _instancedMaterialHints))
      {
        return true;
      }

      if (material.shader)
      {
        descriptor += "|" + material.shader.name;

        if (MatchesSubstring(material.shader.name, _instancedShaderHints))
        {
          return true;
        }
      }

      foreach (var propertyName in _windPropertyNames)
      {
        if (!string.IsNullOrWhiteSpace(propertyName) &&
            material.HasProperty(propertyName))
        {
          return true;
        }
      }
    }

    return MatchesRegex(descriptor, _instancedCandidateRegex);
  }

  private static void EnsureParsed()
  {
    if (!_dirty)
    {
      return;
    }

    lock (Sync)
    {
      if (!_dirty)
      {
        return;
      }

      _includedLayers = ParseLayers(
        ValheimScalabilityConfig.IncludedLayers?.Value);

      _excludedLayers = ParseLayers(
        ValheimScalabilityConfig.ExcludedLayers?.Value);

      _includedObjectNames = ParseCsv(
        ValheimScalabilityConfig.IncludedObjectNames?.Value);

      _excludedObjectNames = ParseCsv(
        ValheimScalabilityConfig.ExcludedObjectNames?.Value);

      _includedObjectRegex = ParseRegexList(
        ValheimScalabilityConfig.IncludedObjectRegexList?.Value,
        "IncludedObjectRegexList");

      _excludedObjectRegex = ParseRegexList(
        ValheimScalabilityConfig.ExcludedObjectRegexList?.Value,
        "ExcludedObjectRegexList");

      _excludedMaterialNames = ParseCsv(
        ValheimScalabilityConfig.ExcludedMaterialNames?.Value);

      _excludedMaterialRegex = ParseRegexList(
        ValheimScalabilityConfig.ExcludedMaterialRegexList?.Value,
        "ExcludedMaterialRegexList");

      _excludedShaderNames = ParseCsv(
        ValheimScalabilityConfig.ExcludedShaderNames?.Value);

      _excludedShaderRegex = ParseRegexList(
        ValheimScalabilityConfig.ExcludedShaderRegexList?.Value,
        "ExcludedShaderRegexList");

      _instancedObjectHints = ParseCsv(
        ValheimScalabilityConfig.InstancedObjectNameHints?.Value);

      _instancedMaterialHints = ParseCsv(
        ValheimScalabilityConfig.InstancedMaterialNameHints?.Value);

      _instancedShaderHints = ParseCsv(
        ValheimScalabilityConfig.InstancedShaderNameHints?.Value);

      _instancedCandidateRegex = ParseRegexList(
        ValheimScalabilityConfig.InstancedCandidateRegexList?.Value,
        "InstancedCandidateRegexList");

      _windPropertyNames = ParseCsv(
        ValheimScalabilityConfig.WindShaderPropertyNames?.Value);

      _hasObjectIncludeFilter =
        _includedObjectNames.Length > 0 ||
        _includedObjectRegex.Length > 0;

      _dirty = false;
    }
  }

  private static string[] ParseCsv(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return Array.Empty<string>();
    }

    var split = value.Split(',');
    var result = new List<string>(split.Length);

    foreach (var item in split)
    {
      var trimmed = item.Trim();
      if (!string.IsNullOrWhiteSpace(trimmed))
      {
        result.Add(trimmed);
      }
    }

    return result.ToArray();
  }

  /// <summary>
  /// Regex entries use semicolon/newline separators so regex patterns remain free
  /// to use commas.
  /// </summary>
  private static Regex[] ParseRegexList(
    string value,
    string settingName)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return Array.Empty<Regex>();
    }

    var split = value.Split(
      new[] { ';', '\r', '\n' },
      StringSplitOptions.RemoveEmptyEntries);

    var result = new List<Regex>(split.Length);

    foreach (var item in split)
    {
      var pattern = item.Trim();
      if (string.IsNullOrWhiteSpace(pattern))
      {
        continue;
      }

      try
      {
        result.Add(
          new Regex(
            pattern,
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled));
      }
      catch (ArgumentException exception)
      {
        Debug.LogWarning(
          $"[ValheimScalability] Ignoring invalid regex in {settingName}: '{pattern}'. {exception.Message}");
      }
    }

    return result.ToArray();
  }

  private static HashSet<int> ParseLayers(string value)
  {
    var result = new HashSet<int>();

    if (string.IsNullOrWhiteSpace(value))
    {
      return result;
    }

    foreach (var token in value.Split(','))
    {
      var trimmed = token.Trim();
      if (string.IsNullOrWhiteSpace(trimmed))
      {
        continue;
      }

      if (int.TryParse(trimmed, out var numericLayer))
      {
        if (numericLayer >= 0 && numericLayer <= 31)
        {
          result.Add(numericLayer);
        }

        continue;
      }

      var layer = LayerMask.NameToLayer(trimmed);
      if (layer >= 0)
      {
        result.Add(layer);
      }
      else
      {
        Debug.LogWarning(
          $"[ValheimScalability] Unknown layer '{trimmed}' in clustering filter.");
      }
    }

    return result;
  }

  private static bool MatchesSubstring(
    string value,
    string[] patterns)
  {
    if (string.IsNullOrEmpty(value))
    {
      return false;
    }

    foreach (var pattern in patterns)
    {
      if (value.IndexOf(
            pattern,
            StringComparison.OrdinalIgnoreCase) >= 0)
      {
        return true;
      }
    }

    return false;
  }

  private static bool MatchesRegex(
    string value,
    Regex[] regexes)
  {
    if (string.IsNullOrEmpty(value))
    {
      return false;
    }

    foreach (var regex in regexes)
    {
      if (regex.IsMatch(value))
      {
        return true;
      }
    }

    return false;
  }
}
