using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Patches;
using ValheimVehicles.QuickStartWorld.Patches;
using Zolantris.Shared;

namespace ValheimVehicles.Controllers;

public static class PatchController
{
  private const string PlanBuildGuid = "marcopogo.PlanBuild";
  private static Harmony? _harmonyInstance;

  public static void Apply(string harmonyGuid)
  {
    _harmonyInstance = new Harmony(harmonyGuid);

    HarmonyHelper.TryPatchAll(_harmonyInstance,
      typeof(Character_Patch),
      typeof(CharacterAnimEvent_Patch),
      typeof(Plantable_Patch),
      typeof(Player_Patch),
      typeof(Piece_Patch),
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
      typeof(Minimap_VehicleIcons),
#if DEBUG
      typeof(RPCRegistryDebugger_Patches),
#endif
      typeof(RPCManager_Patches),
      typeof(Humanoid_EquipPatch),
      typeof(Container_Patches)
    );

    if (PatchConfig.MineRockPatch.Value)
    {
      HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(MineRock_Patches));
    }


    TryPatchDynamicLocations();

#if DEBUG
    HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(QuickStartWorld_Patch));
    // HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(ZNetViewInvokeRPCHook));
#endif

    if (PatchConfig.ShipPausePatch.Value)
    {
      HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(GamePause_Patch));
    }

    if (ModSupportConfig.DebugRemoveStartMenuBackground.Value)
    {
      HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(StartScene_Patch));
    }

    TryPatchGizmoMod();
    TryPatchPlanBuild();
  }

  public static void TryPatchDynamicLocations()
  {
    if (_harmonyInstance == null) return;
    if (Chainloader.PluginInfos.ContainsKey("zolantris.DynamicLocations"))
    {
      HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(DynamicLocations_Game_LogoutPatch));
    }
  }

  public static void TryPatchGizmoMod()
  {
    if (_harmonyInstance == null) return;
    PatchConfig.CheckForGizmoMod();
    if (PatchConfig.HasGizmoModEnabled && PatchConfig.ComfyGizmoPatches.Value)
    {
      Logger.LogInfo("Patching ComfyGizmo GetRotation");
      HarmonyHelper.TryPatchAll(_harmonyInstance, typeof(ComfyGizmo_Patch));
    }
  }

  /// <summary>
  /// Planbuild does not get detected by chainloader well. Likely due to hooks-gen.
  /// 
  /// Note: planbuild force-disable will turn off planbuild patching. Meaning it will not work for vehicles.
  ///
  /// Note: >=3.2.0 as you can copy vehicles directly now so planbuild is not needed.
  /// 
  /// </summary>
  public static void TryPatchPlanBuild()
  {
    if (_harmonyInstance == null) return;
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