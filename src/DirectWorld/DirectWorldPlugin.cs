using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DirectWorld.Config;

namespace DirectWorld;

[BepInPlugin(BepInGuid, ModName, Version)]
public class DirectWorldPlugin : BaseUnityPlugin
{
  public const string Author = "zolantris";
  public const string Version = "1.0.0";
  public const string ModName = "DirectWorld";
  public const string BepInGuid = $"{Author}.{ModName}";
  public static string HarmonyGuid => BepInGuid;
  private static Harmony _harmony = null!;

  public const string ModDescription =
    "Valheim Mod for rapid world startup and server joining without UI interaction";

  public const string CopyRight = "Copyright © 2024, GNU-v3 licensed";

  public void Awake()
  {
#if DEBUG
    DirectWorldConfig.BindConfig(Config, null!);
#endif
    _harmony = new Harmony(HarmonyGuid);
    _harmony.PatchAll(typeof(Patches.DirectWorld_Patch));
  }
}
