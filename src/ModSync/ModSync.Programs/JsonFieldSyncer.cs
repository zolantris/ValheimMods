// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ModSync.Config;

namespace ModSync.Programs
{
  internal static class JsonFieldSyncer
  {
    /// <summary>
    /// Applies a single top-level JSON key/value to all targets listed.
    /// </summary>
    internal static int HandleJsonSync(string[] targets, string key, string value)
    {
      if (string.IsNullOrWhiteSpace(key))
      {
        Logger.Error("--syncJsonKey is required for syncJson");
        return 2;
      }

      var map = ModSyncConfig.Instance?.syncJsonTargets;
      if (map == null || map.Count == 0)
      {
        Logger.Debug("No syncJsonTargets found in config");
        return 0;
      }

      // If no explicit targets provided, run them all (mirrors your sync behavior)
      if (targets == null || targets.Length == 0)
      {
        var all = new List<string>(map.Keys);
        targets = all.ToArray();
      }

      var keyEsc = Regex.Escape(key);
      var pattern = $"(\"{keyEsc}\"\\s*:\\s*\")([^\"]*)(\")"; // top-level string field

      foreach (var targetName in targets)
      {
        if (!map.TryGetValue(targetName, out var t) || t == null)
        {
          Logger.Warn($"[syncJson] No target named '{targetName}'");
          continue;
        }

        var path = t.inputFilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
          Logger.Warn($"[syncJson] inputFilePath missing for '{targetName}'");
          continue;
        }

        if (!File.Exists(path))
        {
          Logger.Warn($"[syncJson] File not found: {path}");
          continue;
        }

        string original;
        try
        {
          original = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception ex)
        {
          Logger.Error($"[syncJson] Read failed: {path} -> {ex.Message}");
          continue;
        }

        var replaced = Regex.Replace(
          original,
          pattern,
          m => m.Groups[1].Value + (value ?? string.Empty) + m.Groups[3].Value,
          RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        if (string.Equals(original, replaced, StringComparison.Ordinal))
        {
          Logger.Debug($"[syncJson] No change for {path} ({key})");
          continue;
        }

        if (ModSyncConfig.IsDryRun)
        {
          Logger.Debug($"[syncJson] DRY RUN: would update {path}  {key} -> {value}");
          continue;
        }

        try
        {
          File.WriteAllText(path, replaced, new UTF8Encoding(false));
          Logger.Debug($"[syncJson] Updated {path}  {key} -> {value}");
        }
        catch (Exception ex)
        {
          Logger.Error($"[syncJson] Write failed: {path} -> {ex.Message}");
        }
      }

      return 0;
    }
  }
}