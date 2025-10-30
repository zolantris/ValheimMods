using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ModSync.Config;
using ModSync.Utils;
namespace ModSync.Programs;

public class GenerateArchive
{
  /// <summary>
  /// Singular target (non-recursive)
  /// todo add recursive target support for multiple deploys to different mods/games etc.
  /// 
  /// </summary>
  /// <param name="targetName"></param>
  internal static void HandleGenerateModArchive(string targetName)
  {
    if (ModSyncConfig.Instance.archiveTargets == null) return;
    if (!ModSyncConfig.Instance.archiveTargets.TryGetValue(targetName, out var currentTarget)) return;


    if (ModSyncConfig.IsVerbose)
    {
      Logger.Debug(Logger.SerializeForLog(currentTarget));
    }

    if (currentTarget.buildType == ModSyncConfig.ArchiveBuildType.Thunderstore)
    {
      generateThunderstoreModArchive(
        currentTarget.inputDirs,
        currentTarget.inputFiles,
        currentTarget.thunderstoreInputFiles,
        currentTarget.thunderstoreTemplateDir,
        currentTarget.outputDir,
        currentTarget.releaseName,
        currentTarget.archiveName,
        currentTarget.archiveFileType,
        currentTarget.releaseTypeSuffix,
        currentTarget.includePatterns,
        currentTarget.excludePatterns
      );
    }
    else
    {
      Logger.Debug($"Unsupported archive build type: {currentTarget.buildType} Acceptable values are {ModSyncConfig.ArchiveBuildType.Thunderstore}");
    }
    // todo add recursive syncing based on targets provided
  }

  internal static void generateThunderstoreModArchive(List<string> inputDirs, List<string> inputFiles, List<string> thunderstoreInputFiles, string thunderstoreTemplateDir, string outputDir, string releaseName, string archiveName, string archiveFileType, string releaseTypeSuffix, List<string> includesPattern, List<string> excludesPattern)
  {
    if (outputDir.Length < 1)
    {
      Logger.Error($"Output path must be a valid path got {outputDir}");
      return;
    }

    if (!Path.Exists(thunderstoreTemplateDir))
    {
      Logger.Error("No Thunderstore template directory found");
      return;
    }

    releaseTypeSuffix ??= string.Empty;

    var includesRegex = RegexGenerator.GenerateRegexFromList(includesPattern);
    var excludedFilesRegex = RegexGenerator.GenerateRegexFromList(excludesPattern);

    // var modNameVersion = $"{archiveName}-{archiveVersion}{suffix}.zip";
    var archiveOutputPath = Path.Combine(outputDir, $"{archiveName}-Thunderstore.{archiveFileType}");
    var tmpDir = Path.Combine(outputDir, $"tmp-{releaseName}-ThunderStore");

    var suffix = releaseTypeSuffix == string.Empty ? "" : $"-{releaseTypeSuffix}";
    var pluginsOutputDir = Path.Combine(tmpDir, "plugins");

    // Clean up temporary directories
    if (File.Exists(archiveOutputPath))
    {
      File.Delete(archiveOutputPath);
    }
    else if (Directory.Exists(archiveOutputPath))
    {
      Directory.Delete(archiveOutputPath);
    }

    if (Directory.Exists(tmpDir))
      Directory.Delete(tmpDir, true);

    // Create necessary directories
    Directory.CreateDirectory(tmpDir);
    Directory.CreateDirectory(pluginsOutputDir);

    // Copy template plugins to top level directory eg icons,readme,manifest.json
    FileUtils.CopyDirectory(thunderstoreTemplateDir, tmpDir);

    // typically readmes and other information that does not belong in plugins folder for project info.
    foreach (var inputFile in thunderstoreInputFiles)
    {
      var file = File.Exists(inputFile);
      if (!file)
      {
        Logger.Error("Input file does not exist: " + inputFile);
        continue;
      }
      var fileName = Path.GetFileName(inputFile);
      FileUtils.CopyFile(inputFile, Path.Combine(tmpDir, fileName));
    }


    foreach (var inputDir in inputDirs)
    {
      if (!Directory.Exists(inputDir))
      {
        Logger.Error("Input directory does not exist: " + inputDir);
        continue;
      }
      FileUtils.CopyDirectory(inputDir, pluginsOutputDir, excludedFilesRegex);
    }

    foreach (var inputFile in inputFiles)
    {
      var file = File.Exists(inputFile);
      if (!file)
      {
        Logger.Error("Input file does not exist: " + inputFile);
        continue;
      }
      var fileName = Path.GetFileName(inputFile);
      FileUtils.CopyFile(inputFile, Path.Combine(pluginsOutputDir, fileName));
    }

    if (File.Exists(archiveOutputPath))
      File.Delete(archiveOutputPath);

    // Create ThunderStore archive
    FileUtils.CreateZipArchive(tmpDir, archiveOutputPath);
  }


  /// <summary>
  /// This is a flat Archive instead of nested Thunderstore structure
  /// </summary>
  /// Implement this
  internal static void generateNexusStoreArchive(string inputPath, string outputPath, string archiveName, string releaseTypeSuffix, Regex includesRegex)
  {
    throw new NotImplementedException();
  }

  /// <summary>
  /// Todo add a copy method for external assets outside of current directory.
  /// </summary>
  public static void CopyAutoDoc()
  {
    // var autoDocName = $"{archiveName}_AutoDoc.md";
    //
    // // Use pluginDeployPath from parameters if provided, otherwise get it from environment variables
    // var effectivePluginPath = !string.IsNullOrEmpty(pluginDeployPath)
    //   ? pluginDeployPath
    //   : Environment.GetEnvironmentVariable("VALHEIM_PLUGIN_PATH") ??
    //     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    //       "r2modmanPlus-local", "Valheim", "profiles", "Default", "BepInEx", "plugins");
    //
    // Logger.Debug($"Using plugin path: {effectivePluginPath}");
    //
    // var autoDocPath = Path.Combine(effectivePluginPath, autoDocName);
    // var localAutoDocPath = Path.Combine(solutionDir, "src", assemblyName, "docs", autoDocName);
  }
}