// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using ServerSync;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.ValheimVehicles.Plugins
{
  public static class ServerSyncConfigSyncUtil
  {
    public static void RegisterAllConfigEntries(ConfigSync sync, Type configType)
    {
      var alreadyRegistered = new HashSet<ConfigEntryBase>();

      foreach (var field in configType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
      {
        if (field.GetValue(null) is ConfigEntryBase entry && alreadyRegistered.Add(entry))
        {
          var entryType = entry.GetType();
          if (entryType.IsGenericType && entryType.GetGenericTypeDefinition() == typeof(ConfigEntry<>))
          {
            var genericArg = entryType.GetGenericArguments()[0];
            var addMethod = typeof(ConfigSync)
              .GetMethod(nameof(ConfigSync.AddConfigEntry))?
              .MakeGenericMethod(genericArg);

            if (addMethod != null)
            {
              addMethod.Invoke(sync, new object[] { entry });
            }
            else
            {
              LoggerProvider.LogWarning($"[ServerSync] Could not find AddConfigEntry for type {genericArg}");
            }
          }
          else
          {
            LoggerProvider.LogWarning($"[ServerSync] Skipped non-generic config entry: {entry.Definition.Key}");
          }
        }
      }
    }
  }
}