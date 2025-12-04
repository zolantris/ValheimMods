using System;
using System.IO;
using System.Text.RegularExpressions;
using ModSync.Config;
namespace ModSync.Programs;

public static class FileUtils
{
  internal static void CopyFile(string source, string destination)
  {
    if (File.Exists(source))
    {
      var dirName = Path.GetDirectoryName(destination);
      if (dirName == null)
      {
        Logger.Warn($"Warning: Destination directory is null for file {destination}");
        return;
      }

      Directory.CreateDirectory(dirName);
      File.Copy(source, destination, true);
      Logger.Debug($"Copied {source} to {destination}");
    }
    else
    {
      Logger.Warn($"Warning: Source file does not exist: {source}");
    }
  }

  internal static void CopyDirectory(string sourceDir, string destinationDir, Regex? excludedFilesRegex = null, Regex? includedFilesRegex = null)
  {
    if (!Directory.Exists(sourceDir))
    {
      Logger.Debug($"Warning: Source directory does not exist: {sourceDir}");
      return;
    }


    Logger.Debug($"Copying directory: {sourceDir} -> {destinationDir}");

    // Create destination directory if it doesn't exist

    if (!ModSyncConfig.IsDryRun)
    {
      Directory.CreateDirectory(destinationDir);
    }


    // Copy files
    foreach (var file in Directory.GetFiles(sourceDir))
    {
      var fileName = Path.GetFileName(file);
      if (includedFilesRegex != null && !includedFilesRegex.IsMatch(fileName)) continue;
      if (excludedFilesRegex != null && excludedFilesRegex.IsMatch(fileName)) continue;

      Logger.Debug($"FileName: {fileName}");

      var destFile = Path.Combine(destinationDir, fileName);

      if (!ModSyncConfig.IsDryRun)
      {
        File.Copy(file, destFile, true);
      }
    }

    // Copy subdirectories recursively
    foreach (var dir in Directory.GetDirectories(sourceDir))
    {
      var dirName = Path.GetFileName(dir);
      if (includedFilesRegex != null && !includedFilesRegex.IsMatch(dirName)) continue;
      if (excludedFilesRegex != null && excludedFilesRegex.IsMatch(dirName)) continue;
      var destDir = Path.Combine(destinationDir, dirName);
      CopyDirectory(dir, destDir, excludedFilesRegex);
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
        if (process == null)
        {
          Logger.Warn("Warning: Compress-Archive process is null. It exited way to early.");
          return;
        }
        process.WaitForExit();
        Logger.Debug(process.StandardOutput.ReadToEnd());

        if (process.ExitCode != 0)
        {
          Logger.Warn($"Warning: Compress-Archive exited with code {process.ExitCode}");
        }
      }

      Logger.Debug($"Created archive: {destinationZipFile}");
    }
    catch (Exception ex)
    {
      Logger.Error($"Error creating zip archive: {ex.Message}");
    }
  }
}