// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.CustomTextureGroup
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using BepInEx;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Paths = BepInEx.Paths;

namespace ValheimRAFT
{
  public class CustomTextureGroup
  {
    private static readonly string[] m_validExtensions = new string[3]
    {
      ".png",
      ".jpg",
      ".jpeg"
    };

    private static Dictionary<string, CustomTextureGroup> m_groups =
      new Dictionary<string, CustomTextureGroup>();

    private Dictionary<string, CustomTextureGroup.CustomTexture> m_textureLookUp =
      new Dictionary<string, CustomTextureGroup.CustomTexture>();

    private Dictionary<int, CustomTextureGroup.CustomTexture> m_textureHashLookUp =
      new Dictionary<int, CustomTextureGroup.CustomTexture>();

    private List<CustomTextureGroup.CustomTexture> m_textures =
      new List<CustomTextureGroup.CustomTexture>();

    public CustomTextureGroup.CustomTexture GetTextureByHash(int hash)
    {
      CustomTextureGroup.CustomTexture customTexture;
      return this.m_textureHashLookUp.TryGetValue(hash, out customTexture)
        ? customTexture
        : (CustomTextureGroup.CustomTexture)null;
    }

    public CustomTextureGroup.CustomTexture GetTextureByName(string name)
    {
      CustomTextureGroup.CustomTexture customTexture;
      return this.m_textureLookUp.TryGetValue(name, out customTexture)
        ? customTexture
        : (CustomTextureGroup.CustomTexture)null;
    }

    public List<CustomTextureGroup.CustomTexture> Textures => this.m_textures;

    public static CustomTextureGroup Get(string groupName)
    {
      CustomTextureGroup customTextureGroup;
      return CustomTextureGroup.m_groups.TryGetValue(groupName, out customTextureGroup)
        ? customTextureGroup
        : (CustomTextureGroup)null;
    }

    public static CustomTextureGroup Load(string groupName)
    {
      CustomTextureGroup customTextureGroup1;
      if (CustomTextureGroup.m_groups.TryGetValue(groupName, out customTextureGroup1))
        return customTextureGroup1;
      CustomTextureGroup customTextureGroup2 = new CustomTextureGroup();
      CustomTextureGroup.m_groups.Add(groupName, customTextureGroup2);
      foreach (string file in Directory.GetFiles(
                 Path.Combine(Path.Combine(Paths.PluginPath, "ValheimRAFT", "Assets"), groupName)))
      {
        string extension = Path.GetExtension(file);
        if (((IEnumerable<string>)CustomTextureGroup.m_validExtensions).Contains<string>(extension,
              (IEqualityComparer<string>)StringComparer.InvariantCultureIgnoreCase) && !Path
              .GetFileNameWithoutExtension(file)
              .EndsWith("_normal", StringComparison.InvariantCultureIgnoreCase))
          customTextureGroup2.AddTexture(file);
      }

      return customTextureGroup2;
    }

    public void AddTexture(string file)
    {
      string withoutExtension = Path.GetFileNameWithoutExtension(file);
      string extension = Path.GetExtension(file);
      string directoryName = Path.GetDirectoryName(file);
      CustomTextureGroup.CustomTexture texture = new CustomTextureGroup.CustomTexture();
      string path = Path.Combine(directoryName, withoutExtension + "_normal" + extension);
      if (File.Exists(path))
        texture.Normal = (Texture)AssetUtils.LoadTexture(path, false);
      texture.Texture = (Texture)AssetUtils.LoadTexture(file, false);
      (texture.Texture).name = withoutExtension;
      this.AddTexture(texture);
    }

    public void AddTexture(CustomTextureGroup.CustomTexture texture)
    {
      string name = (texture.Texture).name;
      if (this.m_textureLookUp.ContainsKey(name))
        return;
      this.m_textureLookUp.Add(name, texture);
      this.m_textureHashLookUp.Add(StringExtensionMethods.GetStableHashCode(name), texture);
      texture.Index = this.m_textures.Count;
      this.m_textures.Add(texture);
    }

    public class CustomTexture
    {
      public Texture Texture { get; internal set; } = (Texture)null;

      public Texture Normal { get; internal set; } = (Texture)null;

      public int Index { get; internal set; } = 0;
    }
  }
}