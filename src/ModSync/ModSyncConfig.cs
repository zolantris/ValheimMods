using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ModSync.Programs;
namespace ModSync;

internal static class ModSyncConfig
{
  public static bool IsVerbose = false;
  public static bool IsDryRun = true; // will only peek not run copy scripts.

  internal const string ConfigKey_targetName = "targetName";
  internal const string ConfigKey_inputPath = "inputPath";
  internal const string ConfigKey_outputPath = "outputPath";

  public struct ModSyncConfigObject
  {
    public Dictionary<string, SyncTargetShared>? syncTargets;
    public Dictionary<string, SyncTargetShared>? sharedTargets;
    public Dictionary<string, RunTargetItem>? runTargets;

  }

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

  public static void UpdateConfigBooleans(Dictionary<string, string> options)
  {
    IsVerbose = options.ContainsKey(ModSyncCli.Opt_Verbose);
    IsDryRun = options.ContainsKey(ModSyncCli.Opt_DryRun);
  }
}