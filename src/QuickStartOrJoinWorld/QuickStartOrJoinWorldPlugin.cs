using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using QuickStartOrJoinWorld.Config;

namespace QuickStartOrJoinWorld;

[BepInPlugin(BepInGuid, ModName, Version)]
[BepInDependency(Jotunn.Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod,
  VersionStrictness.Minor)]
public class QuickStartOrJoinWorldPlugin : BaseUnityPlugin
{
  public const string Author = "zolantris";
  public const string Version = "1.0.0";
  public const string ModName = "QuickStartOrJoinWorld";
  public const string BepInGuid = $"{Author}.{ModName}";
  public static string HarmonyGuid => BepInGuid;
  private static Harmony _harmony = null!;

  public const string ModDescription =
    "Valheim Mod for quick start or join world functionality";

  public const string CopyRight = "Copyright © 2024, GNU-v3 licensed";

  public void Awake()
  {
#if DEBUG
    QuickStartWorldConfig.BindConfig(Config, null!);
#endif
    _harmony = new Harmony(HarmonyGuid);
    _harmony.PatchAll(typeof(Patches.QuickStartWorld_Patch));
  }
}
