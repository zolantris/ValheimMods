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

    // ReSharper disable once RedundantNameQualifier
    Eldritch.Core.Nav.Pathfinding.Register(new ValheimPathfindingShim());

    InvokeRepeating(nameof(DebugCheckinMethod), 3f, 5f);

    PrefabManager.OnVanillaPrefabsAvailable += () =>
    {
      PrefabRegistry.RegisterAllPrefabs();
    };
  }

  public void DebugCheckinMethod()
  {
    LoggerProvider.LogDebug("Checkin called");
  }
}