using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using ZdoWatcher.Patches;
using ZdoWatcher.ZdoWatcher.Config;

namespace ZdoWatcher;

[BepInPlugin(ModGuid, ModName, Version)]
[NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod,
  VersionStrictness.Minor)]
public class ZdoWatcherPlugin : BaseUnityPlugin
{
  public const string Author = "zolantris";
  public const string Version = "1.1.0";
  public const string ModName = "ZdoWatcher";
  public const string ModGuid = $"{Author}.{ModName}";
  public static string HarmonyGuid => ModGuid;
  private static Harmony _harmony;

  public const string ModDescription =
    "Valheim Mod made to share Zdo Changes and side effect through one shareable interface";

  public const string CopyRight = "Copyright © 2023-2024, GNU-v3 licensed";

  private void Awake()
  {
    ZdoWatcherConfig.BindConfig(Config);

    _harmony = new Harmony(HarmonyGuid);
    _harmony.PatchAll(typeof(ZdoPatch));
    _harmony.PatchAll(typeof(ZNetScene_Patch));
    _harmony.PatchAll(typeof(ZDOMan_Patch));


    if (ZdoWatcherConfig.GuardAgainstInvalidZNetSceneSpam != null &&
        ZdoWatcherConfig.GuardAgainstInvalidZNetSceneSpam.Value)
    {
      _harmony.PatchAll(typeof(InvalidZNetScenePatch));
    }

    ZdoWatchController.Instance = gameObject.AddComponent<ZdoWatchController>();
  }
}