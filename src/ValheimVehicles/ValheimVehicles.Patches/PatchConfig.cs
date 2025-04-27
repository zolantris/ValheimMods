using BepInEx.Configuration;

namespace ValheimVehicles.Config;

public static class PatchConfig
{
  private const string SectionName = "Patches";
  private static ConfigFile? Config { get; set; }

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

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    DynamicLocations = config.Bind(SectionName, "DynamicLocations",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Enables DynamicLocations mod to access ValheimRAFT/Vehicles identifiers.",
        true, true));

    ComfyGizmoPatches = Config.Bind("Patches",
      "ComfyGizmo - Enable Patch", false,
      ConfigHelpers.CreateConfigDescription(
        "Patches relative rotation allowing for copying rotation and building while the raft is at movement, this toggle is only provided in case patches regress anything in Gizmos and players need a work around."));
    ComfyGizmoPatchCreativeHasNoRotation = Config.Bind("Patches",
      "ComfyGizmo - Vehicle Creative zero Y rotation", true,
      ConfigHelpers.CreateConfigDescription(
        "Vehicle/Raft Creative mode will set all axises to 0 for rotation instead keeping the turn axis. Gizmo has issues with rotated vehicles, so zeroing things out is much safer. Works regardless of patch if mod exists"));

    ShipPausePatch = Config.Bind<bool>("Patches",
      "Vehicles Prevent Pausing", true,
      ConfigHelpers.CreateConfigDescription(
        "Prevents pausing on a boat, pausing causes a TON of desync problems and can make your boat crash or other players crash",
        true, true));
    ShipPausePatchSinglePlayer = Config.Bind<bool>("Patches",
      "Vehicles Prevent Pausing SinglePlayer", true,
      ConfigHelpers.CreateConfigDescription(
        "Prevents pausing on a boat during singleplayer. Must have the Vehicle Prevent Pausing patch as well",
        true, true));

    ForceDisablePlanBuildPatches = Config.Bind<bool>("Patches",
      "Disable Planbuild auto-patches",
      false,
      ConfigHelpers.CreateConfigDescription("Disable planbuild patches. This will prevent planbuild from working well. Only use this if valheim raft is causing planbuild to crash.", true));

    MineRockPatch = Config.Bind<bool>("Patches",
      "Ram MineRock patches",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Enable MineRock5 patches to so vehicle and rams prefabs do not trigger errors when hitting areas over the default radius size"
        , true));

  }
}