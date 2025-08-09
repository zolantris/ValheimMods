using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
namespace Zolantris.Shared.BepInExAutoDoc
{
  public class BepInExConfigAutoDoc
  {

    // Store Regex to get all characters after a [
    private static readonly Regex ConfigMatchRegExp = new(@"\[(.*?)\]");
    public bool runOnDebug = true;
    public bool runOnRelease = false;

    private static string? GetOutputFolderPath(PluginInfo plugin,
      string autoDocName)
    {
      var entryAssemblyDir =
        Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

      var executingAssemblyLocation =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

      LoggerProvider.LogDebug(
        $"BepInExConfigAutoDoc: GetOutputFolderPath() {entryAssemblyDir}");
      LoggerProvider.LogDebug(
        $"BepInExConfigAutoDoc: GetOutputFolderPath() executingAssemblyLocation {executingAssemblyLocation}");

      if (executingAssemblyLocation == null)
      {
        return null;
      }

      var bepinExPluginDir = Path.Combine(
        executingAssemblyLocation,
        $"{autoDocName}_AutoDoc.md");


      return bepinExPluginDir;
    }


    // Strip using the regex above from Config[x].Description.Description
    private static string StripString(string x)
    {
      return ConfigMatchRegExp.Match(x).Groups[1].Value;
    }

    private static void AutoWriteBepInExConfigDoc(PluginInfo plugin,
      ConfigFile Config, string documentName)
    {
      StringBuilder sb = new();
      var lastSection = "";
      foreach (var x in Config.Keys)
      {
        // skip first line
        if (x.Section != lastSection)
        {
          lastSection = x.Section;
          sb.Append($"{Environment.NewLine}## {x.Section}{Environment.NewLine}");
        }

        sb.Append(
          $"\n### {x.Key} [{StripString(Config[x].Description.Description)}]"
            .Replace("[]",
              "") +
          $"{Environment.NewLine}- Description: {Config[x].Description.Description.Replace("[Synced with Server]", "").Replace("[Not Synced with Server]", "")}" +
          $"{Environment.NewLine}- Default Value: {Config[x].DefaultValue}{Environment.NewLine}");
      }

      var outputPath = GetOutputFolderPath(plugin, documentName);
      if (outputPath == null) return;

      File.WriteAllText(
        outputPath,
        sb.ToString());
    }


    public void Generate(BaseUnityPlugin plugin, ConfigFile configFile,
      string documentName)
    {
      Generate(plugin, configFile, documentName);
    }

    /// <summary>
    ///   Generates a document for bepinex.Config, should only be ran in debug mode,
    ///   auto-doc is not meant for end-users
    ///   todo detect executing assembly and run based on environment flags being in
    ///   debug mode
    /// </summary>
    /// <returns></returns>
    public void Generate(PluginInfo pluginInfo, ConfigFile configFile,
      string documentName)
    {
      AutoWriteBepInExConfigDoc(pluginInfo, configFile, documentName);
    }
// #if DEBUG
//     if (runOnDebug)
//     {
//       AutoWriteBepInExConfigDoc(pluginInfo, configFile);
//     }
// #else
//     if (runOnRelease)
//     {
//       AutoWriteBepInExConfigDoc(plugin, config);
//     }
// #endif
  }
}