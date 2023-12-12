// ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ValheimRAFT.CustomTextureGroup

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jotunn.Utils;
using UnityEngine;
using Paths = BepInEx.Paths;

namespace ValheimRAFT;

public class CustomTextureGroup
{
  public class CustomTexture
  {
    public Texture Texture { get; internal set; } = null;


    public Texture Normal { get; internal set; } = null;


    public int Index { get; internal set; } = 0;
  }

  private static readonly string[] m_validExtensions = new string[3] { ".png", ".jpg", ".jpeg" };

  private static Dictionary<string, CustomTextureGroup> m_groups =
    new Dictionary<string, CustomTextureGroup>();

  private Dictionary<string, CustomTexture> m_textureLookUp =
    new Dictionary<string, CustomTexture>();

  private Dictionary<int, CustomTexture> m_textureHashLookUp = new Dictionary<int, CustomTexture>();

  private List<CustomTexture> m_textures = new List<CustomTexture>();

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
    if (m_groups.TryGetValue(groupName, out var group))
    {
      return group;
    }

    return null;
  }

  public static CustomTextureGroup Load(string groupName)
  {
    if (m_groups.TryGetValue(groupName, out var group))
    {
      return group;
    }

    group = new CustomTextureGroup();
    m_groups.Add(groupName, group);
    string assetDirectory = Path.Combine(Paths.PluginPath, "ValheimRAFT", "Assets");
    string[] files = Directory.GetFiles(Path.Combine(assetDirectory, groupName));
    foreach (string file in files)
    {
      string ext = Path.GetExtension(file);
      if (m_validExtensions.Contains(ext, StringComparer.InvariantCultureIgnoreCase) && !Path
            .GetFileNameWithoutExtension(file)
            .EndsWith("_normal", StringComparison.InvariantCultureIgnoreCase))
      {
        group.AddTexture(file);
      }
    }

    return group;
  }

  public void AddTexture(string file)
  {
    string name = Path.GetFileNameWithoutExtension(file);
    string ext = Path.GetExtension(file);
    string path = Path.GetDirectoryName(file);
    CustomTexture texture = new CustomTexture();
    string normal = Path.Combine(path, name + "_normal" + ext);
    if (File.Exists(normal))
    {
      texture.Normal = AssetUtils.LoadTexture(normal, false);
    }

    texture.Texture = AssetUtils.LoadTexture(file, false);
    texture.Texture.name = name;
    AddTexture(texture);
  }

  public void AddTexture(CustomTexture texture)
  {
    string name = texture.Texture.name;
    if (!m_textureLookUp.ContainsKey(name))
    {
      m_textureLookUp.Add(name, texture);
      m_textureHashLookUp.Add(name.GetStableHashCode(), texture);
      texture.Index = m_textures.Count;
      m_textures.Add(texture);
    }
  }
}