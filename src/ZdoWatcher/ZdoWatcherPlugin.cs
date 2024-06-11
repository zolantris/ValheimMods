using BepInEx;
using Jotunn.Utils;

namespace ZdoWatcher;

[BepInPlugin(BepInGuid, ModName, Version)]
[NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
public class ZdoWatcherPlugin : BaseUnityPlugin
{
  public const string Author = "zolantris";
  public const string Version = "1.0.0";
  public const string ModName = "ZdoWatcher";
  public const string BepInGuid = $"{Author}.{ModName}";
  public const string HarmonyGuid = $"{Author}.{ModName}";

  public const string ModDescription =
    "Valheim Mod made to share Zdo Changes and side effect through one shareable interface";

  public const string CopyRight = "Copyright © 2023-2024, GNU-v3 licensed";

  public static ZdoWatcherPlugin Instance;

  private void Awake()
  {
    Instance = this;
  }
}