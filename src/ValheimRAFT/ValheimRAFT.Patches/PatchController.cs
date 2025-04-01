using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn;
using ValheimRAFT.Config;
using ValheimRAFT.Util;
using ValheimVehicles.ModSupport;
using ValheimVehicles.Patches;
using Zolantris.Shared;

namespace ValheimRAFT.Patches;

internal static class PatchController
{
  private const string PlanBuildGuid = "marcopogo.PlanBuild";
  private const string ComfyGizmoGuid = "bruce.valheim.comfymods.gizmo";
  public static bool HasGizmoMod = false;
  private static Harmony? _harmonyInstance;

  internal static void Apply(string harmonyGuid)
  {
      _harmonyInstance = new Harmony(harmonyGuid);

      HarmonyHelper.TryPatchAll(_harmonyInstance,
          typeof(Character_Patch),
          typeof(CharacterAnimEvent_Patch),
          typeof(Plantable_Patch),
          typeof(Player_Patch),
          typeof(Ship_Patch),
          typeof(ShipControls_Patch),
          typeof(Teleport_Patch),
          typeof(WearNTear_Patch),
          typeof(ZNetScene_Patch),
          typeof(ZNetView_Patch),
          typeof(Hud_Patch),
          typeof(MonoUpdaterPatches),
          typeof(EffectsArea_VehiclePatches),

          // water effects
          typeof(WaterVolume_WaterPatches),
          typeof(GameCamera_WaterPatches),
          typeof(GameCamera_CullingPatches),
          typeof(Character_WaterPatches),
          typeof(Fireplace_WaterPatches),
          typeof(Minimap_VehicleIcons)
      );

    if (PatchConfig.MineRockPatch.Value)
    {
        HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(MineRock_Patches));
    }

    if (Chainloader.PluginInfos.ContainsKey("zolantris.DynamicLocations"))
    {
        HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(DynamicLocations_Game_LogoutPatch));
    }

#if DEBUG
      HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(QuickStartWorld_Patch));
#endif

    if (PatchConfig.ShipPausePatch.Value)
    {
        HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(GamePause_Patch));
    }

    if (ValheimRaftPlugin.Instance.DebugRemoveStartMenuBackground.Value)
    {
        HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(StartScene_Patch));
    }

    HasGizmoMod = Chainloader.PluginInfos.ContainsKey(ComfyGizmoGuid);
    if (HasGizmoMod && PatchConfig.ComfyGizmoPatches.Value)
    {
        Logger.LogInfo("Patching ComfyGizmo GetRotation");
        HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(ComfyGizmo_Patch));
    }

    // planbuild force disable will turn off patches
    // If planbuild mod does not exist we exit.
    // if Chainloader detects planbuild we allow planbuild patch (only if Force disable is not set to true)
    if (!PatchConfig.ForceDisablePlanBuildPatches.Value && (
            Directory.Exists(Path.Combine(Paths.PluginPath, "MathiasDecrock-PlanBuild")) ||
            Directory.Exists(Path.Combine(Paths.PluginPath, "PlanBuild")) ||
            Chainloader.PluginInfos.ContainsKey(PlanBuildGuid)))
    {
        Logger.LogInfo("Applying PlanBuild Patch");
        HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(PlanBuild_Patch));
    }
  }

  public static void UnpatchSelf()
  {
      _harmonyInstance?.UnpatchSelf();
  }
}