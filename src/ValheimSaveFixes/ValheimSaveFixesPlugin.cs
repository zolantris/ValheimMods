using BepInEx;
using HarmonyLib;

namespace ValheimSaveFixes;

[BepInPlugin(ModGuid, ModName, Version)]
public class ValheimSaveFixesPlugin : BaseUnityPlugin
{
  public const string Author = "zolantris";
  public const string Version = "1.0.0";
  public const string ModName = "ValheimSaveFixes";
  public const string ModGuid = $"{Author}.{ModName}";
  public const string HarmonyGuid = $"{Author}.{ModName}";
  private static Harmony _harmony;

  public const string ModDescription =
    "Valheim 1.0.0 stability fix when disabling steam cloud or even when saving with steam cloud this allows saving and creating new worlds";

  public const string CopyRight = "Copyright © 2026, GNU-v3 licensed";

  private void Awake()
  {
    _harmony = new Harmony(HarmonyGuid);
    _harmony.PatchAll(typeof(Patches.SaveMountFixes));
  }
}