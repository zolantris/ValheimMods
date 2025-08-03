using BepInEx;
using Eldritch.Core;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;
using Zolantris.Shared;

namespace Eldritch.Valheim;

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
    LoggerProvider.LogDebug("Eldritchcore initialized");

    PrefabManager.OnVanillaPrefabsAvailable += () =>
    {
      LoadAssemblies();
    };
  }

  public void InjectAlienPrefab()
  {
    if (assetBundle == null) return;
    const string XenoAdultName = "Eldritch_XenoAdult";
    const string assetName = "AlienTest";

    var prefabAsset = assetBundle.LoadAsset<GameObject>(assetName);
    if (prefabAsset == null) return;

    var originalComponents = prefabAsset.GetComponents<Component>();
    var clonedPrefab = PrefabManager.Instance.CreateClonedPrefab(XenoAdultName, prefabAsset);

    clonedPrefab.AddComponent<ZNetView>();
    clonedPrefab.AddComponent<XenoDroneSpawnHandler>();

    var components = clonedPrefab.GetComponents<Component>();
    foreach (var component in components)
    {
      LoggerProvider.LogDebug(component.GetType().ToString());
    }

    foreach (var component in originalComponents)
    {
      LoggerProvider.LogDebug(component.GetType().ToString());
    }

    PieceManager.Instance.AddPiece(new CustomPiece(clonedPrefab, true,
      new PieceConfig
      {
        PieceTable = "Hammer", // for now.
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
  }

  /// <summary>
  /// Loads from eldritch core. (this might need to be done here so eldritch.Core gameobjects are resolved with eldritch core scripts)
  /// </summary>
  public void LoadAssemblies()
  {
    assetBundle = Entry.LoadAssembly();

    if (assetBundle == null) return;

    Sprites = assetBundle.LoadAsset<SpriteAtlas>("icons");

    InjectAlienPrefab();
  }
}