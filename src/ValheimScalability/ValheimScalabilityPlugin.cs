using BepInEx;
using HarmonyLib;
using ValheimScalability.BepInExConfig;

namespace ValheimScalability;

[BepInPlugin(BepInGuid, ModName, Version)]
public class ValheimScalabilityPlugin : BaseUnityPlugin
{
  public const string Author = "zolantris";
  public const string Version = "1.0.0";
  public const string ModName = "ValheimScalability";
  public const string BepInGuid = $"{Author}.{ModName}";
  public static string HarmonyGuid => BepInGuid;
  private static Harmony _harmony = null!;

  public const string ModDescription =
    "Valheim Mod meant to unlock performance and scalability of your machine";

  public const string CopyRight = "Copyright © 2026, GNU-v3 licensed";

  public void Awake()
  {
    ValheimScalabilityConfig.Bind(Config);
    _harmony = new Harmony(HarmonyGuid);
    _harmony.PatchAll(typeof(Patches.SectorMeshClusterPatches));
    _harmony.PatchAll(typeof(Patches.SectorMeshClusterDamagePatch));
  }
}