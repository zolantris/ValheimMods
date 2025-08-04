using System;
using BepInEx;
using Eldritch.Core;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;
using Zolantris.Shared;

namespace Eldritch.Valheim;

[BepInDependency(Jotunn.Main.ModGuid)]
[BepInPlugin(ModGuid, ModName, Version)]
public class EldritchValheimPlugin : BaseUnityPlugin
{
  public const string Author = "zolantris";
  public const string Version = "1.0.0";
  public const string ModName = "EldritchValheim";
  public const string ModGuid = $"{Author}.{ModName}";

  public static AssetBundle assetBundle = null!;
  public static SpriteAtlas Sprites = null!;

  public void Awake()
  {
    LoggerProvider.Setup(Logger);
    LoggerProvider.LogDebug("Eldritchcore initialized");

    InvokeRepeating(nameof(DebugCheckinMethod), 3f, 5f);

    PrefabManager.OnVanillaPrefabsAvailable += () =>
    {
      LoadAssemblies();
    };
  }

  public void DebugCheckinMethod()
  {
    LoggerProvider.LogDebug("Checkin called");
  }

  public void InjectAlienPrefab()
  {
    if (assetBundle == null) return;
    LoggerProvider.LogDebug($"Starting InjectAlienPrefab");
    const string XenoAdultName = "Eldritch_XenoAdult";
    const string assetName = "alientest";

    var prefabAsset = assetBundle.LoadAsset<GameObject>(assetName);
    if (prefabAsset == null) return;

    var originalComponents = prefabAsset.GetComponents<Component>();
    var clonedPrefab = PrefabManager.Instance.CreateClonedPrefab(XenoAdultName, prefabAsset);

    clonedPrefab.AddComponent<ZNetView>();
    clonedPrefab.AddComponent<XenoDroneSpawnHandler>();
    var piece = clonedPrefab.AddComponent<Piece>();
    piece.m_name = XenoAdultName;
    piece.m_icon = Sprites.GetSprite("anchor");

    var components = clonedPrefab.GetComponents<Component>();
    foreach (var component in components)
    {
      if (component == null)
      {
        LoggerProvider.LogError("Got null component");
        continue;
      }
      LoggerProvider.LogDebug(component.GetType().ToString());
    }

    foreach (var component in originalComponents)
    {
      if (component == null)
      {
        LoggerProvider.LogError("Got null component");
        continue;
      }
      LoggerProvider.LogDebug(component.GetType().ToString());
    }

    PieceManager.Instance.AddPiece(new CustomPiece(clonedPrefab, true,
      new PieceConfig
      {
        PieceTable = "Hammer", // for now.
        Name = "XENO",
        Description = "Xenos....they're everywhere...",
        Icon = Sprites.GetSprite("anchor"), // for testing.
        Category = "Eldritch",
        Enabled = true,
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 1,
            Item = "Wood",
            Recover = true
          }
        ]
      }));

    LoggerProvider.LogDebug($"Added {piece}");
  }

  /// <summary>
  /// Loads from eldritch core. (this might need to be done here so eldritch.Core gameobjects are resolved with eldritch core scripts)
  /// </summary>
  public void LoadAssemblies()
  {
    if (!assetBundle)
    {
      assetBundle = Entry.LoadAssembly();
    }

    if (assetBundle == null) return;

    Sprites = assetBundle.LoadAsset<SpriteAtlas>("icons");

    try
    {

      InjectAlienPrefab();
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Failed to load alien prefab {e}");
    }
  }
}