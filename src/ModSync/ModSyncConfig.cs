using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using ModSync.Programs;
using ModSync.Utils;
namespace ModSync;

internal static class ModSyncConfig
{
  public static bool IsVerbose = false;
  public static bool IsDryRun = true; // will only peek not run copy scripts.

  public static ModSyncConfigObject ConfigInstance = new();
  public static string ConfigPath = ModSyncCli.configFileName;
  private static bool _hasRunConfig = false;

  public struct ConfigJson
  {
    public bool? verbose;
    public bool? dryRun;
  }

  public class ModSyncConfigObject
  {
    public ConfigJson configDefaults;
    public Dictionary<string, string>? initialVariables;
    public Dictionary<string, string>? variables;
    public Dictionary<string, SyncTargetShared>? syncTargets;
    public Dictionary<string, SyncTargetShared>? sharedTargets;
    public Dictionary<string, RunTargetItem>? runTargets;
  }

  public static List<string> IgnoredConfigKeysForResolver = ["configDefaults", "initialVariables", "variables"];


  public struct InputItem
  {
    public string inputPath;
  }

  public struct RunTargetItem
  {
    public string binaryTarget;
    public string args;
    public bool isConditional;
    public string[]? conditions;
  }

  public class SyncTargetShared
  {
    public string? outputPath;
    public string? relativeOutputPath;
    public string? inputPath;
    public List<string> generatedFilesRegexp;
    // shared for syncing targets and nesting them
    public bool? isSharedInput;
    public string[]? dependsOn;

    // for creating pdb and mdb files for debugging.
    public bool? canGenerateDebugFiles;
  }

  public static Dictionary<string, bool> GenerateDebugTargets = new();

  public static bool TryGetShareDependencyKey(Dictionary<string, SyncTargetShared>? sharedTargets, string key, [NotNullWhen(true)] out SyncTargetShared? syncTargetShared)
  {
    syncTargetShared = null;
    if (sharedTargets == null) return false;
    return sharedTargets.TryGetValue(key, out syncTargetShared);
  }

  public static bool TryCreateConfig(string configPath)
  {
    _hasRunConfig = true;
    // for later logging/debugging.
    ConfigPath = configPath;

    var json5String = File.ReadAllText(configPath);
    var config = Json5Core.Json5.Deserialize<ModSyncConfigObject>(json5String);
    if (config == null) return false;

    if (config.initialVariables != null)
    {
      Console.WriteLine("Warning: Unsupported initialVariables in json structure. This is a generates value. It will be ignored");
      config.initialVariables = null;
    }

    if (config.variables != null)
    {
      var initialVariables = config.variables.ToDictionary();

      config.variables = VariableResolver.ResolveAll(config.variables);
      var ignoredKeysRegex = RegexGenerator.GenerateRegexFromList(IgnoredConfigKeysForResolver);

      VariableResolver.RecursivelyResolveObject(config, config.variables, ignoredKeysRegex);
      config.initialVariables = initialVariables;
    }

    if (config.runTargets == null && config.sharedTargets == null && config.syncTargets == null)
    {
      return false;
    }

    ConfigInstance = config;
    return true;
  }

  /// <summary>
  /// Needs to be run after TryCreateConfig
  /// </summary>
  /// <param name="options"></param>
  public static void UpdateConfigBooleans(Dictionary<string, string> options)
  {
    if (!_hasRunConfig)
    {
      throw new Exception("Config not loaded yet. Please call TryCreateConfig first.");
    }
    IsVerbose = options.ContainsKey(ModSyncCli.Opt_Verbose);
    IsDryRun = options.ContainsKey(ModSyncCli.Opt_DryRun);

    if (!IsVerbose && ConfigInstance.configDefaults.dryRun.HasValue)
    {
      IsDryRun = ConfigInstance.configDefaults.dryRun.Value;
    }

    if (!IsVerbose && ConfigInstance.configDefaults.verbose.HasValue)
    {
      IsVerbose = ConfigInstance.configDefaults.verbose.Value;
    }
  }
}