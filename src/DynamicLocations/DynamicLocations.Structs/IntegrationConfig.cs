using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using JetBrains.Annotations;
using Jotunn;
using Mono.Cecil;

namespace DynamicLocations.Structs;

public struct IntegrationConfig
{
  [UsedImplicitly]
  public IntegrationConfig()
  {
    throw new Exception(
      "This constructor is not supported, please provide plugin and zdoTargetPrefabName");
  }

  public IntegrationConfig(BaseUnityPlugin plugin, string zdoTargetPrefabName)
  {
    Plugin = plugin;
    ZdoTargetTargetPrefabName = zdoTargetPrefabName;
    LoginPrefabHashCode = zdoTargetPrefabName.GetHashCode();
    // these could be getters. less performant likely.
    Name = plugin.Info.Metadata.Name;
    Version = plugin.Info.Metadata.Version.ToString();
    Guid = plugin.Info.Metadata.GUID;

    // todo remove logs, confirm setters work
    Logger.LogDebug(
      $"PluginVersion: {plugin.Info.Metadata.Version} stringVersion {Version}, Name: {Name}");
  }

  // todo might need to make this an array/list of prefab names to match with so mods do not need to have multiple integration calls, only first match would be needed, for now mods can just override the OnLoginMatchZdoPrefab
  public string ZdoTargetTargetPrefabName { get; private set; }
  public string Guid { get; set; }
  public string Version { get; set; }
  public string Name { get; set; }
  public BaseUnityPlugin Plugin { get; set; }
  public bool UseDefaultCallbacks { get; set; } = false;
  public int MovementTimeout { get; set; } = 0;
  public bool ShouldFreezePlayer { get; set; } = false;
  public int LoginPrefabHashCode { get; set; } = 0;
  public int Priority { get; set; } = 999;
  public List<string> RunBeforePlugins { get; } = [];
  public List<string> RunAfterPlugins { get; } = [];
}