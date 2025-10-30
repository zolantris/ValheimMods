using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ModSync.Config;
using ModSync.Utils;
namespace ModSync.Programs;

internal static class SyncToTarget
{
  private const int MaxRecurseCount = 5;

  internal static void HandleSync(string[] targets)
  {
    if (ModSyncConfig.Instance.syncTargets == null)
    {
      Logger.Debug("No syncTargets found in config");
      return;
    }

    foreach (var targetName in targets)
    {
      RecursiveSync(targetName, null);
    }

    Logger.Debug("Successfully synced all targets");
  }

  /// <summary>
  /// Recursive way to extend config.
  /// </summary>
  private static void RecursiveSync(string targetName, ModSyncConfig.SyncTargetShared? parentTarget = null, int recurseCount = 0)
  {
    ModSyncConfig.SyncTargetShared currentTarget;
    if (recurseCount > MaxRecurseCount)
    {
      Logger.Debug($"[SYNC] Recursion limit reached for target {targetName}");
      return;
    }

    if (parentTarget != null)
    {
      if (!ModSyncConfig.TryGetShareDependencyKey(ModSyncConfig.Instance.sharedTargets, targetName, out currentTarget)) return;
    }
    else
    {
      if (ModSyncConfig.Instance.syncTargets == null) return;
      if (!ModSyncConfig.Instance.syncTargets.TryGetValue(targetName, out currentTarget)) return;
    }

    if (currentTarget == null) return;

    var inputPath = parentTarget?.inputPath ?? currentTarget.inputPath;
    var outputPath = parentTarget?.outputPath ?? currentTarget.outputPath;
    var relativePath = currentTarget.relativeOutputPath;
    var dependsOn = currentTarget.dependsOn;

    if (dependsOn is
        {
          Length: > 0
        })
    {
      foreach (var otherTargetName in dependsOn)
      {
        RecursiveSync(otherTargetName, currentTarget, recurseCount + 1);
      }
    }

    Logger.Debug($"[SYNC] Would sync to: inputPath <{inputPath}> \\ outputPath <{outputPath}>");

    if (ModSyncConfig.IsDryRun) return;
    if (inputPath == null || outputPath == null) return;
    if (!string.IsNullOrEmpty(relativePath))
    {
      outputPath = PathUtils.SafeCombine(outputPath, relativePath);
    }

    PdbToMdbConverter.TryConvertDir(currentTarget);
    FileUtils.CopyDirectory(inputPath, outputPath, ExcludedFilesRegex);
  }

  public static List<string> excludedFiles = ["SolutionPostBuild", "ModSync"];
  public static Regex ExcludedFilesRegex = RegexGenerator.GenerateRegexFromList(excludedFiles);

}