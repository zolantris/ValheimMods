﻿using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn;
using ValheimRAFT.Config;
using ValheimRAFT.Util;
using ValheimVehicles.ModSupport;
using ValheimVehicles.Patches;

namespace ValheimRAFT.Patches;

internal static class PatchController
{
  private const string PlanBuildGuid = "marcopogo.PlanBuild";
  private const string ComfyGizmoGuid = "bruce.valheim.comfymods.gizmo";
  public static bool HasGizmoMod = false;
  private static Harmony? Harmony;

  internal static void Apply(string harmonyGuid)
  {
    Harmony = new Harmony(harmonyGuid);
    Harmony.PatchAll(typeof(Character_Patch));
    Harmony.PatchAll(typeof(CharacterAnimEvent_Patch));
    Harmony.PatchAll(typeof(Plantable_Patch));
    Harmony.PatchAll(typeof(Player_Patch));
    Harmony.PatchAll(typeof(Ship_Patch));
    Harmony.PatchAll(typeof(ShipControls_Patch));
    Harmony.PatchAll(typeof(Teleport_Patch));
    Harmony.PatchAll(typeof(WearNTear_Patch));
    Harmony.PatchAll(typeof(ZNetScene_Patch));
    Harmony.PatchAll(typeof(ZNetView_Patch));
    Harmony.PatchAll(typeof(Hud_Patch));
    Harmony.PatchAll(typeof(MonoUpdaterPatches));
    Harmony.PatchAll(typeof(EffectsArea_VehiclePatches));

    // water effects
    Harmony.PatchAll(typeof(WaterVolume_WaterPatches));
    Harmony.PatchAll(typeof(GameCamera_WaterPatches));
    Harmony.PatchAll(typeof(GameCamera_CullingPatches));
    Harmony.PatchAll(typeof(Character_WaterPatches));
    Harmony.PatchAll(typeof(Fireplace_WaterPatches));
    Harmony.PatchAll(typeof(Minimap_VehicleIcons));

    // RamAOE
    if (PatchConfig.MineRockPatch.Value)
    {
      Harmony.PatchAll(typeof(MineRock_Patches));
    }

    if (Chainloader.PluginInfos.ContainsKey("zolantris.DynamicLocations"))
      Harmony.PatchAll(typeof(DynamicLocations_Game_LogoutPatch));

#if DEBUG
    Harmony.PatchAll(typeof(QuickStartWorld_Patch));
#endif
// #if DEBUG
//     // Debug only for now, this needs to be refined to ignore collisions with ship colliders
//     Harmony.PatchAll(typeof(GameCamera_VehiclePiecesPatch));
// #endif

    if (PatchConfig.ShipPausePatch.Value)
      Harmony.PatchAll(typeof(GamePause_Patch));

    if (ValheimRaftPlugin.Instance.DebugRemoveStartMenuBackground.Value)
      Harmony.PatchAll(typeof(StartScene_Patch));

    HasGizmoMod = Chainloader.PluginInfos.ContainsKey(ComfyGizmoGuid);
    if (HasGizmoMod && PatchConfig.ComfyGizmoPatches.Value)
    {
      Logger.LogInfo("Patching ComfyGizmo GetRotation");
      Harmony.PatchAll(typeof(ComfyGizmo_Patch));
    }

    /*
     * PlanBuild uses mmmHookgen, so it cannot be detected with bepinex
     *
     * The patch flag must be enabled and the folder must be detected otherwise it will not apply to patch a mod that does not exist
     *
     * So it does not show up on Chainloader.PluginInfos.ContainsKey(PlanBuildGUID)
     */
    if (PatchConfig.PlanBuildPatches.Value &&
        (Directory.Exists(Path.Combine(Paths.PluginPath,
           "MathiasDecrock-PlanBuild")) ||
         Directory.Exists(Path.Combine(Paths.PluginPath, "PlanBuild")) ||
         Chainloader.PluginInfos.ContainsKey(PlanBuildGuid)))
    {
      Logger.LogInfo("Applying PlanBuild Patch");
      Harmony.PatchAll(typeof(PlanBuild_Patch));
    }
  }

  public static void UnpatchSelf()
  {
    Harmony?.UnpatchSelf();
  }
}