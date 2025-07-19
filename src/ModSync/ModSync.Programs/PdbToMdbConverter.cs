using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
namespace ModSync.Programs;

internal static class PdbToMdbConverter
{
  public static Regex GenerateRegexFromList(List<string> keyNames)
  {
    // Escape special characters in the strings and join them with a pipe (|) for OR condition
    var escapedPrefixes = new List<string>();
    foreach (var prefix in keyNames)
    {
      escapedPrefixes.Add(Regex.Escape(prefix));
    }

    // Create a regex pattern that matches the start of the string (^)
    // It will match any of the provided prefixes at the start of the string
    var pattern = "^(" + string.Join("|", escapedPrefixes) + ")";
    return new Regex(pattern, RegexOptions.Compiled);
  }

  internal static void TryConvertDir(ModSyncConfig.SyncTargetShared syncTarget)
  {
    if (syncTarget.outputPath == null || syncTarget.inputPath == null || syncTarget.canGenerateDebugFiles != true || syncTarget.generatedFilesRegexp == null || syncTarget.generatedFilesRegexp.Count == 0)
    {
      return;
    }

    if (!ModSyncConfig.GenerateDebugTargets.TryAdd(syncTarget.inputPath, true)) return;

    var files = Directory.GetFiles(syncTarget.inputPath, "*.dll", SearchOption.AllDirectories).Select(x => x.Replace($"{syncTarget.inputPath}\\", ""));

    // remove previous mdb files.
    var mdbFiles = Directory.GetFiles(syncTarget.inputPath, "*.mdb", SearchOption.AllDirectories);
    foreach (var mdbFile in mdbFiles)
    {
      File.Delete(mdbFile);
    }

    var regexp = GenerateRegexFromList(syncTarget.generatedFilesRegexp.ToList());
    foreach (var file in files)
    {
      if (!regexp.IsMatch(file)) continue;
      Console.WriteLine($"Converting file: <{file}>");
      ConvertPdbToMdb(Environment.CurrentDirectory, syncTarget.inputPath, file);
    }
  }
  public static void ConvertPdbToMdb(string solutionDir, string targetPath, string assemblyName)
  {
    var assemblyWithDlcExtension = assemblyName.EndsWith(".dll") ? assemblyName : $"{assemblyName}.dll";
    var targetDll = Path.Combine(targetPath, assemblyWithDlcExtension);
    var pdb2mdbPath = Path.Combine(solutionDir, "pdb2mdb.exe");

    if (!File.Exists(pdb2mdbPath))
    {
      Console.WriteLine($"Warning: pdb2mdb.exe not found at {pdb2mdbPath}, skipping conversion");
      return;
    }

    if (ModSyncConfig.IsVerbose)
    {
      Console.WriteLine($"Converting: targetDll {targetDll} ...");
    }

    try
    {
      // Run pdb2mdb.exe
      var psi = new System.Diagnostics.ProcessStartInfo
      {
        FileName = pdb2mdbPath,
        Arguments = $"\"{targetDll}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true
      };

      using (var process = System.Diagnostics.Process.Start(psi))
      {
        if (process != null)
        {
          process.WaitForExit();
        }
        else
        {
          Console.WriteLine("Warning: pdb2mdb.exe process is null. It exited way to early.");
        }

        Console.WriteLine(process.StandardOutput.ReadToEnd());

        if (process.ExitCode != 0)
        {
          Console.WriteLine($"Warning: pdb2mdb.exe exited with code {process.ExitCode}");
        }
      }

      // Rename the .dll.mdb to .mdb
      var sourceMdb = Path.Combine(targetPath, $"{assemblyName}.dll.mdb");
      var targetMdb = Path.Combine(targetPath, $"{assemblyName}.mdb");

      if (File.Exists(sourceMdb))
      {
        if (File.Exists(targetMdb))
          File.Delete(targetMdb);

        File.Move(sourceMdb, targetMdb);
        Console.WriteLine($"Renamed {sourceMdb} to {targetMdb}");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error during PDB to MDB conversion: {ex.Message}");
    }
  }
}