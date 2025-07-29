// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using ServerSync;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;

namespace ValheimVehicles.ValheimVehicles.Plugins
{
  public static class ServerSyncConfigSyncUtil
  {
    private static readonly HashSet<ConfigEntryBase> RegisteredEntries = new();
    private static readonly HashSet<string> RegisteredKeys = new();

    public static void RegisterAllConfigEntries(ConfigSync sync, Type configType)
    {
      foreach (var field in configType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
      {
        if (field.GetValue(null) is not ConfigEntryBase entry) continue;

        var key = $"{entry.Definition.Section}.{entry.Definition.Key}";

        if (!RegisteredEntries.Add(entry))
        {
          LoggerProvider.LogWarning(
            $"[ServerSync] DUPLICATE ConfigEntry instance skipped: {key} (Field: {field.Name}, Type: {configType.FullName})");
          continue;
        }

        if (!RegisteredKeys.Add(key))
        {
          LoggerProvider.LogWarning(
            $"[ServerSync] DUPLICATE Config key skipped: {key} (Field: {field.Name}, Type: {configType.FullName})");
          continue;
        }

        var entryType = entry.GetType();
        if (entryType.IsGenericType && entryType.GetGenericTypeDefinition() == typeof(ConfigEntry<>))
        {
          var genericArg = entryType.GetGenericArguments()[0];
          var addMethod = typeof(ConfigSync).GetMethod(nameof(ConfigSync.AddConfigEntry))?.MakeGenericMethod(genericArg);

          if (addMethod != null)
          {
            addMethod.Invoke(sync, new object[] { entry });
            LoggerProvider.LogDebug($"[ServerSync] Registered config: {key}");
          }
          else
          {
            LoggerProvider.LogWarning($"[ServerSync] Could not resolve AddConfigEntry<{genericArg}> for config key: {key}");
          }
        }
        else
        {
          LoggerProvider.LogWarning($"[ServerSync] Skipped non-generic config entry: {key}");
        }
      }
    }
  }
}