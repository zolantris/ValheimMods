using BepInEx;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using YggdrasilTerrain.Commands;
using YggdrasilTerrain.Config;
using YggdrasilTerrain.Patches;

namespace YggdrasilTerrain;

[BepInPlugin(ModGuid, ModName, Version)]
[BepInDependency(Jotunn.Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod,
  VersionStrictness.Minor)]
public class YggdrasilTerrainPlugin : BaseUnityPlugin
{
  public const string Author = "zolantris";
  public const string Version = "1.0.0";
  public const string ModName = "YggdrasilTerrain";
  public const string ModGuid = $"{Author}.{ModName}";
  public const string HarmonyGuid = $"{Author}.{ModName}";
  private static Harmony _harmony;

  public const string ModDescription =
    "Terrain mod allowing players to walk and build on the massive Yggdrassil Branch in the sky. Requires access to the branch and the ability to build above normal heights.";

  public const string CopyRight = "Copyright © 2023-2024, GNU-v3 licensed";

  private void Awake()
  {
    YggdrasilConfig.BindConfig(Config);
    _harmony = new Harmony(HarmonyGuid);
    _harmony.PatchAll(typeof(ZNetScene_Patch));
    RegisterCommands();
  }

  public void RegisterCommands()
  {
    CommandManager.Instance.AddConsoleCommand(new YggdrasilTerrainCommands());
  }
}