using System;
using System.IO;
namespace ModSync.Programs;

public static class PdbToMdbConverter
{
  public static void ConvertPdbToMdb(string solutionDir, string targetPath, string assemblyName)
  {
    Console.WriteLine("Converting PDB to MDB...");
    var targetDll = Path.Combine(targetPath, $"{assemblyName}.dll");
    var pdb2mdbPath = Path.Combine(solutionDir, "pdb2mdb.exe");

    if (!File.Exists(pdb2mdbPath))
    {
      Console.WriteLine($"Warning: pdb2mdb.exe not found at {pdb2mdbPath}, skipping conversion");
      return;
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