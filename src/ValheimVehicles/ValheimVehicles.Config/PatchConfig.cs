using BepInEx.Bootstrap;
using BepInEx.Configuration;
using ValheimVehicles.Helpers;
using Zolantris.Shared;

namespace ValheimVehicles.Config;

public class PatchConfig : BepInExBaseConfig<PatchConfig>
{
  private const string ComfyGizmoGuid = "bruce.valheim.comfymods.gizmo";
  private const string SectionName = "Patches";

  public static ConfigEntry<bool>? DynamicLocations { get; private set; }
  public static ConfigEntry<bool> ComfyGizmoPatches { get; set; }

  public static ConfigEntry<bool> ForceDisablePlanBuildPatches { get; set; }

  public static ConfigEntry<bool> MineRockPatch { get; set; }


  public static ConfigEntry<bool> ComfyGizmoPatchCreativeHasNoRotation
  {
    get;
    set;
  }

  public static ConfigEntry<bool> ShipPausePatch { get; set; }
  public static ConfigEntry<bool> ShipPausePatchSinglePlayer { get; set; }

  public static bool HasGizmoModEnabled;

  public static void CheckForGizmoMod()
  {
    HasGizmoModEnabled = Chainloader.PluginInfos.ContainsKey(ComfyGizmoGuid);
  }

  public override void OnBindConfig(ConfigFile config)
  {
    DynamicLocations = config.BindUnique(SectionName, "DynamicLocations",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Enables DynamicLocations mod to access ValheimRAFT/Vehicles identifiers.",
        true, true));

    ComfyGizmoPatches = config.BindUnique("Patches",
      "ComfyGizmo - Enable Patch", false,
      ConfigHelpers.CreateConfigDescription(
        "Patches relative rotation allowing for copying rotation and building while the raft is at movement, this toggle is only provided in case patches regress anything in Gizmos and players need a work around."));
    ComfyGizmoPatchCreativeHasNoRotation = config.BindUnique("Patches",
      "ComfyGizmo - Vehicle Creative zero Y rotation", true,
      ConfigHelpers.CreateConfigDescription(
        "Vehicle/Raft Creative mode will set all axises to 0 for rotation instead keeping the turn axis. Gizmo has issues with rotated vehicles, so zeroing things out is much safer. Works regardless of patch if mod exists"));

    ShipPausePatch = config.BindUnique<bool>("Patches",
      "Vehicles Prevent Pausing", true,
      ConfigHelpers.CreateConfigDescription(
        "Prevents pausing on a boat, pausing causes a TON of desync problems and can make your boat crash or other players crash",
        true, true));
    ShipPausePatchSinglePlayer = config.BindUnique<bool>("Patches",
      "Vehicles Prevent Pausing SinglePlayer", true,
      ConfigHelpers.CreateConfigDescription(
        "Prevents pausing on a boat during singleplayer. Must have the Vehicle Prevent Pausing patch as well",
        true, true));

    ForceDisablePlanBuildPatches = config.BindUnique<bool>("Patches",
      "Disable Planbuild auto-patches",
      false,
      ConfigHelpers.CreateConfigDescription("Disable planbuild patches. This will prevent planbuild from working well. Only use this if valheim raft is causing planbuild to crash.", true));

    MineRockPatch = config.BindUnique<bool>("Patches",
      "Ram MineRock patches",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Enable MineRock5 patches to so vehicle and rams prefabs do not trigger errors when hitting areas over the default radius size"
        , true));

    CheckForGizmoMod();
  }
}