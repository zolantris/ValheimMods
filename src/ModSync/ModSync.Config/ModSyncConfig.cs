using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using ModSync.Programs;
using ModSync.Utils;
// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace ModSync.Config;

internal static class ModSyncConfig
{
  public static bool IsVerbose = false;
  public static bool IsDryRun = true; // will only peek not run copy scripts.

  internal static ConfigData Instance = new();
  private static bool _hasRunConfig = false;
  private static string _configPath;

  public struct ConfigJson
  {
    public bool? verbose;
    public bool? dryRun;
  }

  /// <summary>
  /// Should only be used by ModSyncConfigObject 
  /// </summary>
  internal class VariablesData
  {
    public Dictionary<string, string>? variables;
  }

  internal class ConfigTargetData
  {
    // sync targets
    public Dictionary<string, SyncTargetShared>? syncTargets = null;
    // shared schema for all targets
    public Dictionary<string, SyncTargetShared>? sharedTargets = null;
    // binary running
    public Dictionary<string, RunTargetItem>? runTargets = null;
    // json data syncing
    public Dictionary<string, JsonSyncTarget>? syncJsonTargets = null;
    // archiving targets (nexus/thunderstore zip and publishing for github releases)
    public Dictionary<string, ArchiveTarget>? archiveTargets = null;
  }

  /// <summary>
  /// Main config object
  /// 
  /// - warning  -> All keys that must be read from config must be public otherwise the serializer will always default them to null when parsing.
  /// </summary>
  internal class ConfigData : ConfigTargetData
  {
    public ConfigJson configDefaults;
    public Dictionary<string, VariablesData>? environments;
    public Dictionary<string, string>? variables;
    internal Dictionary<string, string>? initialVariables;
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

  public enum ArchiveBuildType
  {
    Thunderstore,
    Nexus
  }

  /// <summary>
  /// includePatterns turns into a regex that matches files to include in the archive.
  /// </summary>
  public class ArchiveTarget
  {
    public List<string> includePatterns = [];
    public List<string> excludePatterns = [];
    public List<string> inputDirs = [];
    public List<string> inputFiles = [];
    public List<string> thunderstoreInputFiles = [];
    public ArchiveBuildType buildType;
    public string outputDir;
    public string thunderstoreTemplateDir;
    public string releaseName;
    public string archiveName;
    public string archiveFileType;
    public string releaseVersion;
    public string releaseTypeSuffix;
  }

  public class SyncTargetShared
  {
    public string? inputPath;
    public string? outputPath;
    public string? relativeOutputPath;
    public List<string>? generatedFilesRegexp;
    // shared for syncing targets and nesting them
    public bool? isSharedInput;
    public string[]? dependsOn;

    // for creating pdb and mdb files for debugging.
    public bool? canGenerateDebugFiles;
  }

  public static Dictionary<string, bool> GenerateDebugTargets = new();

#region Public APIS

  public static ConfigData GetConfigData()
  {
    return Instance;
  }

  public static string GetConfigPath()
  {
    return _configPath;
  }

#endregion

  public static bool TryGetShareDependencyKey(Dictionary<string, SyncTargetShared>? sharedTargets, string key, [NotNullWhen(true)] out SyncTargetShared? syncTargetShared)
  {
    syncTargetShared = null;
    if (sharedTargets == null) return false;
    return sharedTargets.TryGetValue(key, out syncTargetShared);
  }

  public static Dictionary<string, string> GetOverrideVariables(Dictionary<string, string> options)
  {
    return options.Where(x => x.Key.StartsWith(ModSyncCli.Opt_OverridePrefix, StringComparison.OrdinalIgnoreCase)).ToDictionary(kv => kv.Key.Replace(ModSyncCli.Opt_OverridePrefix, ""), kv => kv.Value);
  }

  public static bool TryCreateConfig(string configPath, string? envName, Dictionary<string, string> options)
  {
    _hasRunConfig = true;
    // for later logging/debugging.
    _configPath = configPath;

    var overrideVariables = GetOverrideVariables(options);
    var json5String = File.ReadAllText(configPath);
    var config = Json5Core.Json5.Deserialize<ConfigData>(json5String);
    if (config == null) return false;

    if (config.initialVariables != null)
    {
      Logger.Warn("Warning: Unsupported initialVariables in json structure. This is a generates value. It will be ignored");
      config.initialVariables = null;
    }

    if (config.variables == null) config.variables = new Dictionary<string, string>();

    // merge any overrides into other keys
    MergeEnvironmentVariables(config.variables, config.environments, envName);
    MergeOverrideVariables(config.variables, overrideVariables);

    var initialVariables = config.variables.ToDictionary();

    config.variables = VariableResolver.ResolveAll(config.variables);
    var ignoredKeysRegex = RegexGenerator.GenerateRegexFromList(IgnoredConfigKeysForResolver);

    VariableResolver.RecursivelyResolveObject(config, config.variables, ignoredKeysRegex);

    config.initialVariables = initialVariables;

    if (config.runTargets == null && config.sharedTargets == null && config.syncTargets == null)
    {
      Logger.Error("No runTargets, sharedTargets, or SyncTargets specified. ModSync will do nothing. Exiting...");
      return false;
    }

    UpdateConfigBooleansFromConfig();

    if (IsVerbose)
    {
      Logger.Debug($"Loading config from: <{configPath}>");
      Logger.Debug($"Config: {Json5Core.Json5.Serialize(config)}");
    }

    Instance = config;
    return true;
  }

  public static void MergeOverrideVariables(
    Dictionary<string, string> baseVars,
    Dictionary<string, string> overrides)
  {
    foreach (var kv in overrides)
    {
      baseVars[kv.Key] = kv.Value; // override or add
    }
  }


  public static void MergeEnvironmentVariables(
    Dictionary<string, string> baseVars,
    Dictionary<string, VariablesData>? environments,
    string? selectedEnv)
  {
    if (string.IsNullOrEmpty(selectedEnv) || environments == null || !environments.TryGetValue(selectedEnv, out var envVars))
      return;

    if (envVars?.variables == null) return;

    foreach (var kv in envVars.variables)
    {
      if (kv.Key == "environments")
      {
        Logger.Warn("Warning: environment variables cannot override the 'environments' key. This is a generates value. It will be ignored");
        continue;
      }
      baseVars[kv.Key] = kv.Value; // override or add
    }
  }

  // flags win (otherwise if no flags apply updates)
  private static void UpdateConfigBooleansFromConfig()
  {
    if (!IsDryRun && Instance.configDefaults.dryRun.HasValue)
    {
      IsDryRun = Instance.configDefaults.dryRun.Value;
    }
    if (!IsVerbose && Instance.configDefaults.verbose.HasValue)
    {
      IsVerbose = Instance.configDefaults.verbose.Value;
    }
  }

  /// <summary>
  /// Needs to be run before TrySyncConfig
  /// </summary>
  /// <param name="options"></param>
  public static void UpdateConfigBooleans(Dictionary<string, string> options)
  {
    IsVerbose = options.ContainsKey(ModSyncCli.Opt_Verbose);
    IsDryRun = options.ContainsKey(ModSyncCli.Opt_DryRun);
  }
}