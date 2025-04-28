using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jotunn.Utils;
using ValheimVehicles.Compat;
using ValheimVehicles.Config;
using Paths = BepInEx.Paths;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Injections;

public class CustomTextureGroup
{
  private static readonly string[] m_validExtensions =
    new string[3] { ".png", ".jpg", ".jpeg" };

  private static Dictionary<string, CustomTextureGroup> m_groups = new();

  private Dictionary<string, CustomTexture> m_textureLookUp = new();

  private Dictionary<int, CustomTexture> m_textureHashLookUp = new();

  private List<CustomTexture> m_textures = new();

  public List<CustomTexture> Textures => m_textures;

  public CustomTexture GetTextureByHash(int hash)
  {
    CustomTexture texture;
    return m_textureHashLookUp.TryGetValue(hash, out texture) ? texture : null;
  }

  public CustomTexture GetTextureByName(string name)
  {
    CustomTexture texture;
    return m_textureLookUp.TryGetValue(name, out texture) ? texture : null;
  }

  public static CustomTextureGroup Get(string groupName)
  {
    if (m_groups.TryGetValue(groupName, out var group)) return group;

    return null;
  }

  public static string[] GetFiles(string groupName, string modFolderName)
  {
    /*
     * Early exit to retry with other options
     */
    if (!Directory.Exists(Path.Combine(Paths.PluginPath, modFolderName)))
      return Array.Empty<string>();

    var assetDirectory =
      Path.Combine(Paths.PluginPath, modFolderName, "Assets");

    if (!Directory.Exists(assetDirectory))
      /*
       * fallback so if user somehow breaks their install, the mod will warn them.
       */
      Logger.LogError(
        $"Invalid setup, Asset Directory missing within {assetDirectory}");

    string[] files =
      Directory.GetFiles(Path.Combine(assetDirectory, groupName));
    return files;
  }

  public static CustomTextureGroup Load(string groupName)
  {
    if (m_groups.TryGetValue(groupName, out var group)) return group;

    group = new CustomTextureGroup();
    m_groups.Add(groupName, group);

    var files = new string[] { };

    var modFolderName = ModSupportConfig.PluginFolderName.Value;
    /*
     * This if blocks is provided to make the PluginFolderName check a bit more verbose
     * - It could be added to the array, but would have to be done after CONFIG initializes
     */
    if (modFolderName != "" &&
        Directory.Exists(
          Path.Combine(Paths.PluginPath,
            modFolderName)))
    {
      Logger.LogDebug(
        $"PluginFolderName path detected, resolving assets from that folder");
      files = GetFiles(groupName,
        modFolderName);
    }
    else
    {
      {
        foreach (var possibleModFolderName in ValheimRAFT_API.possibleModFolderNames)
        {
          if (!Directory.Exists(Path.Combine(Paths.PluginPath,
                possibleModFolderName))) continue;

          files = GetFiles(groupName, possibleModFolderName);
          break;
        }

        /*
         * this log will not be reached if the "guess" path matches
         */
        if (files.Length == 0)
          Logger.LogError(
            $"ValheimRAFT: Unable to detect modFolder path, this will cause mesh issues with sails. Please set ValheimRAFT mod folder in the BepInExConfig file. The ValheimRAFT folder should found within this directory {Paths.PluginPath}");
      }
    }

    foreach (var file in files)
    {
      var ext = Path.GetExtension(file);
      if (m_validExtensions.Contains(ext,
            StringComparer.InvariantCultureIgnoreCase) && !Path
            .GetFileNameWithoutExtension(file)
            .EndsWith("_normal", StringComparison.InvariantCultureIgnoreCase))
        group.AddTexture(file);
    }

    return group;
  }

  public void AddTexture(string file)
  {
    var name = Path.GetFileNameWithoutExtension(file);
    var ext = Path.GetExtension(file);
    var path = Path.GetDirectoryName(file);
    var texture = new CustomTexture();
    var normal = Path.Combine(path, name + "_normal" + ext);
    if (File.Exists(normal))
      texture.Normal = AssetUtils.LoadTexture(normal, false);

    texture.Texture = AssetUtils.LoadTexture(file, false);
    texture.Texture.name = name;
    AddTexture(texture);
  }

  public void AddTexture(CustomTexture texture)
  {
    var name = texture.Texture.name;
    if (!m_textureLookUp.ContainsKey(name))
    {
      m_textureLookUp.Add(name, texture);
      m_textureHashLookUp.Add(name.GetStableHashCode(), texture);
      texture.Index = m_textures.Count;
      m_textures.Add(texture);
    }
  }
}