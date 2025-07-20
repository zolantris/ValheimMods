using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModSync.Utils;
namespace ModSync.Programs;

internal static class SyncToTarget
{
  private const int MaxRecurseCount = 5;

  internal static void HandleSync(string[] targets)
  {
    if (ModSyncConfig.ConfigInstance.syncTargets == null)
    {
      Console.WriteLine("No syncTargets found in config");
      return;
    }

    foreach (var targetName in targets)
    {
      RecursiveSync(targetName, null);
    }
  }

  /// <summary>
  /// Recursive way to extend config.
  /// </summary>
  private static void RecursiveSync(string targetName, ModSyncConfig.SyncTargetShared? parentTarget = null, int recurseCount = 0)
  {
    ModSyncConfig.SyncTargetShared currentTarget;
    if (recurseCount > MaxRecurseCount)
    {
      Console.WriteLine($"[SYNC] Recursion limit reached for target {targetName}");
      return;
    }

    if (parentTarget != null)
    {
      if (!ModSyncConfig.TryGetShareDependencyKey(ModSyncConfig.ConfigInstance.sharedTargets, targetName, out currentTarget)) return;
    }
    else
    {
      if (ModSyncConfig.ConfigInstance.syncTargets == null) return;
      if (!ModSyncConfig.ConfigInstance.syncTargets.TryGetValue(targetName, out currentTarget)) return;
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

    if (ModSyncConfig.IsVerbose)
    {
      Console.WriteLine($"[SYNC] Would sync to: inputPath <{inputPath}> \\ outputPath <{outputPath}>");
    }
    if (ModSyncConfig.IsDryRun) return;
    if (inputPath == null || outputPath == null) return;
    if (!string.IsNullOrEmpty(relativePath))
    {
      outputPath = PathUtils.SafeCombine(outputPath, relativePath);
    }

    PdbToMdbConverter.TryConvertDir(currentTarget);
    CopyDirectory(inputPath, outputPath);
  }

  internal static void GenerateModArchive(string solutionDir, string targetPath, string assemblyName, string applicationVersion, bool isRelease, string pluginDeployPath, string outputDir)
  {
    var suffix = isRelease ? "" : "-beta";
    var repoDir = Path.Combine(solutionDir, "src", "ValheimRAFT");

    // Use the directory directly from MSBuild
    var outDir = outputDir;
    if (string.IsNullOrEmpty(outDir))
    {
      // Fallback to targetPath directory if outputDir is not provided
      outDir = Path.GetDirectoryName(targetPath);
      Console.WriteLine($"Warning: outputDir is not provided, using targetPath directory: {outDir}");
    }

    var modOutputDir = Path.Combine(outDir, "ModVersions");
    var tmpDir = Path.Combine(outDir, "tmp");

    // Ensure all directories exist
    Directory.CreateDirectory(outDir);
    Directory.CreateDirectory(modOutputDir);
    Directory.CreateDirectory(tmpDir);
    var thunderStoreDir = Path.Combine(repoDir, isRelease ? "ThunderStore" : "ThunderStoreBeta");

    var modNameVersion = $"{assemblyName}-{applicationVersion}{suffix}.zip";
    var autoDocName = $"{assemblyName}_AutoDoc.md";

    // Use pluginDeployPath from parameters if provided, otherwise get it from environment variables
    var effectivePluginPath = !string.IsNullOrEmpty(pluginDeployPath)
      ? pluginDeployPath
      : Environment.GetEnvironmentVariable("VALHEIM_PLUGIN_PATH") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
          "r2modmanPlus-local", "Valheim", "profiles", "Default", "BepInEx", "plugins");

    Console.WriteLine($"Using plugin path: {effectivePluginPath}");

    var autoDocPath = Path.Combine(effectivePluginPath, autoDocName);
    var localAutoDocPath = Path.Combine(solutionDir, "src", assemblyName, "docs", autoDocName);

    Console.WriteLine($"Generating mod archive: {modNameVersion}");

    // Clean up temporary directories
    if (Directory.Exists(Path.Combine(tmpDir, "plugins", "Assets")))
      Directory.Delete(Path.Combine(tmpDir, "plugins", "Assets"), true);

    if (Directory.Exists(tmpDir))
      Directory.Delete(tmpDir, true);

    // Create necessary directories
    Directory.CreateDirectory(modOutputDir);
    Directory.CreateDirectory(tmpDir);
    Directory.CreateDirectory(Path.Combine(tmpDir, "plugins"));
    Directory.CreateDirectory(Path.Combine(tmpDir, assemblyName));

    // Copy plugins
    var pluginsDir = Path.Combine(solutionDir, "plugins");
    if (Directory.Exists(pluginsDir))
      CopyDirectory(pluginsDir, Path.Combine(tmpDir, "plugins"));

    // Copy main files
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.dll"), Path.Combine(tmpDir, "plugins", $"{assemblyName}.dll"));
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.pdb"), Path.Combine(tmpDir, "plugins", $"{assemblyName}.pdb"));
    CopyFile(Path.Combine(repoDir, "README.md"), Path.Combine(tmpDir, "README.md"));

    // Copy auto-doc if it exists
    if (File.Exists(autoDocPath) && File.Exists(localAutoDocPath))
    {
      CopyFile(autoDocPath, localAutoDocPath);
    }

    // Look for assets directory
    var assetsDir = Path.Combine(solutionDir, "src", assemblyName, "Assets");
    if (!Directory.Exists(assetsDir))
    {
      assetsDir = Path.Combine(solutionDir, "Assets");
    }

    // Copy assets
    if (!string.IsNullOrEmpty(assetsDir) && Directory.Exists(assetsDir))
    {
      CopyDirectory(assetsDir, Path.Combine(tmpDir, "plugins", "Assets"));

      // Delete .meta files
      foreach (var metaFile in Directory.GetFiles(Path.Combine(tmpDir, "plugins", "Assets"), "*.png.meta", SearchOption.AllDirectories))
      {
        File.Delete(metaFile);
      }
    }

    // Copy all files from output directory
    CopyDirectory(targetPath, Path.Combine(tmpDir, "plugins"));

    // Copy ThunderStore files
    if (Directory.Exists(thunderStoreDir))
      CopyDirectory(thunderStoreDir, tmpDir);

    // Delete existing archives if they exist
    var thunderstoreZip = Path.Combine(modOutputDir, $"Thunderstore-{modNameVersion}");
    var nexusZip = Path.Combine(modOutputDir, $"Nexus-{modNameVersion}");
    var libsZip = Path.Combine(modOutputDir, $"libs-{applicationVersion}.zip");

    if (File.Exists(thunderstoreZip))
      File.Delete(thunderstoreZip);

    if (File.Exists(nexusZip))
      File.Delete(nexusZip);

    if (File.Exists(libsZip))
      File.Delete(libsZip);

    // Create ThunderStore archive
    CreateZipArchive(tmpDir, thunderstoreZip);

    // Create Nexus archive
    CopyDirectory(Path.Combine(tmpDir, "plugins"), Path.Combine(tmpDir, assemblyName));
    CreateZipArchive(Path.Combine(tmpDir, assemblyName), nexusZip);

    // Create libs archive
    var libsDir = Path.Combine(solutionDir, "libs");
    if (Directory.Exists(libsDir))
      CreateZipArchive(libsDir, libsZip);
  }

  internal static void CopyFile(string source, string destination)
  {
    if (File.Exists(source))
    {
      var dirName = Path.GetDirectoryName(destination);
      if (dirName == null)
      {
        Console.WriteLine($"Warning: Destination directory is null for file {destination}");
        return;
      }

      Directory.CreateDirectory(dirName);
      File.Copy(source, destination, true);
      Console.WriteLine($"Copied {source} to {destination}");
    }
    else
    {
      Console.WriteLine($"Warning: Source file does not exist: {source}");
    }
  }

  internal static void CopyDirectory(string sourceDir, string destinationDir)
  {
    if (!Directory.Exists(sourceDir))
    {
      Console.WriteLine($"Warning: Source directory does not exist: {sourceDir}");
      return;
    }

    if (ModSyncConfig.IsVerbose)
    {
      Console.WriteLine($"Copying directory: {sourceDir} -> {destinationDir}");
    }

    // Create destination directory if it doesn't exist
    Directory.CreateDirectory(destinationDir);

    // Copy files
    foreach (var file in Directory.GetFiles(sourceDir))
    {
      var fileName = Path.GetFileName(file);
      var destFile = Path.Combine(destinationDir, fileName);
      File.Copy(file, destFile, true);
    }

    // Copy subdirectories recursively
    foreach (var dir in Directory.GetDirectories(sourceDir))
    {
      var dirName = Path.GetFileName(dir);
      var destDir = Path.Combine(destinationDir, dirName);
      CopyDirectory(dir, destDir);
    }
  }

  internal static void CreateZipArchive(string sourceDir, string destinationZipFile)
  {
    try
    {
      var psi = new System.Diagnostics.ProcessStartInfo
      {
        FileName = "powershell",
        Arguments = $"Compress-Archive '{sourceDir}/*' '{destinationZipFile}'",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true
      };

      using (var process = System.Diagnostics.Process.Start(psi))
      {
        process.WaitForExit();
        Console.WriteLine(process.StandardOutput.ReadToEnd());

        if (process.ExitCode != 0)
        {
          Console.WriteLine($"Warning: Compress-Archive exited with code {process.ExitCode}");
        }
      }

      Console.WriteLine($"Created archive: {destinationZipFile}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error creating zip archive: {ex.Message}");
    }
  }
}