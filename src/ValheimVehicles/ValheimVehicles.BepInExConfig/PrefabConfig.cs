using System.Collections.Specialized;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.Storage.Serialization;
using Zolantris.Shared;

namespace ValheimVehicles.BepInExConfig;

public class PrefabConfig : BepInExBaseConfig<PrefabConfig>
{
  public static ConfigEntry<VehicleShipInitPiece>? StartingPiece
  {
    get;
    private set;
  }

  public static ConfigEntry<bool> ProtectVehiclePiecesOnErrorFromWearNTearDamage
  {
    get;
    set;
  }
  public static ConfigEntry<int> VehicleLandMaxTreadWidth = null!;
  public static ConfigEntry<int> VehicleLandMaxTreadLength = null!;
  public static ConfigEntry<float> VehicleDockVerticalHeight = null!;
  public static ConfigEntry<float> VehicleDockSphericalRadius = null!;
  public static ConfigEntry<float> VehicleDockPositionChangeSpeed = null!;

  public static ConfigEntry<bool> HasCannonFireAudio { get; set; } = null!;
  public static ConfigEntry<float> CannonBallInventoryWeight { get; set; } = null!;
  public static ConfigEntry<bool> HasCannonReloadAudio { get; set; } = null!;
  public static ConfigEntry<float> CannonReloadAudioVolume { get; set; } = null!;
  public static ConfigEntry<float> CannonFiringDelayPerCannon { get; set; } = null!;
  public static ConfigEntry<float> CannonFireAudioVolume { get; set; } = null!;
  public static ConfigEntry<float> CannonAutoAimSpeed { get; set; } = null!;
  public static ConfigEntry<float> CannonAimMaxYRotation { get; set; } = null!;
  public static ConfigEntry<float> CannonBarrelAimMaxTiltRotation { get; set; } = null!;
  public static ConfigEntry<float> CannonBarrelAimMinTiltRotation { get; set; } = null!;
  public static ConfigEntry<Vector3> CannonVehicleProtectionRange { get; set; } = null!;
  public static ConfigEntry<float> CannonPlayerProtectionRangeRadius { get; set; } = null!;

  public static ConfigEntry<bool> MakeAllPiecesWaterProof { get; set; }
  public static ConfigEntry<bool> AllowTieredMastToRotate { get; set; }
  public static ConfigEntry<bool> AllowExperimentalPrefabs { get; set; }
  public static ConfigEntry<bool> AdminsCanOnlyBuildRaft { get; set; }
  public static ConfigEntry<Color> GlassDefaultColor { get; private set; } =
    null!;

  public static ConfigEntry<int> RopeLadderRunMultiplier { get; private set; } =
    null!;

  public static ConfigEntry<bool> RopeLadderHints { get; private set; } = null!;

  public static ConfigEntry<Vector3>
    RopeLadderEjectionOffset { get; private set; } = null!;

  public static ConfigEntry<bool> EnableLandVehicles { get; private set; } =
    null!;

  public static ConfigEntry<float> VehicleStaminaHaulingCost = null!;
  public static ConfigEntry<bool> VehicleHaulingSnapsOnStaminaZero = null!;
  public static ConfigEntry<bool> Graphics_AllowSailsFadeInFog { get; set; } = null!;


  public enum VehicleShipInitPiece
  {
    Hull4X8,
    HullFloor2X2,
    HullFloor4X4,
    WoodFloor2X2,
    Nautilus
  }

  public static ConfigEntry<float> ExperimentalTreadScaleX = null!;

  private const string SectionKey = "PrefabConfig";
  private const string VehicleCannonsSection = "PrefabConfig: VehicleCannons";


  public static void UpdatePrefabEnabled(string prefabName, bool enabled)
  {
    if (PieceManager.Instance == null) return;
    var prefab = PieceManager.Instance.GetPiece(prefabName);
    if (prefab == null) return;
    prefab.Piece.m_enabled = enabled;
  }


  public override void OnBindConfig(ConfigFile config)
  {


    AllowTieredMastToRotate = config.BindUnique(SectionKey, "AllowTieredMastToRotateInWind", true, "allows the tiered mast to rotate in wind");
    RopeLadderEjectionOffset = config.BindUnique(SectionKey,
      "RopeLadderEjectionPoint", Vector3.zero,
      ConfigHelpers.CreateConfigDescription("The place the player is placed after they leave the ladder. Defaults to Y +0.25 and Z +0.5 meaning you are placed forwards of the ladder."));


    StartingPiece = config.BindUnique(SectionKey, "Vehicle Hull Starting Piece",
      VehicleShipInitPiece.Hull4X8,
      ConfigHelpers.CreateConfigDescription(
        "Allows you to customize what piece the raft initializes with. Admins only as this can be overpowered.",
        true, true));
    RopeLadderRunMultiplier = config.BindUnique(SectionKey,
      "ropeLadderRunClimbSpeedMult", 2,
      ConfigHelpers.CreateConfigDescription(
        "Allows you to customize how fast you can climb a ladder when in run mode",
        false, true, new AcceptableValueRange<int>(1, 10)));
    RopeLadderHints = config.BindUnique(SectionKey, "ropeLadderHints", true,
      ConfigHelpers.CreateConfigDescription(
        "Shows the controls required to auto ascend/descend and run to speedup ladder"));

    ProtectVehiclePiecesOnErrorFromWearNTearDamage = config.BindUnique(
      SectionKey,
      "Protect Vehicle pieces from breaking on Error", true,
      ConfigHelpers.CreateConfigDescription(
        "Protects against crashes breaking raft/vehicle initialization causing raft/vehicles to slowly break pieces attached to it. This will make pieces attached to valid raft ZDOs unbreakable from damage, but still breakable with hammer",
        true, true));

    GlassDefaultColor = config.BindUnique(SectionKey,
      "GlassDefaultColor",
      new Color(0.60f, 0.60f, 0.60f, 0.05f),
      ConfigHelpers.CreateConfigDescription(
        "Set the experimental glass color for your vehicle. This will be used for most glass meshes. This is the default color. Eventually players can customize the color of the glass.",
        true, true));

    EnableLandVehicles = config.BindUnique(SectionKey, "enableLandVehicles", false,
      ConfigHelpers.CreateConfigDescription(
        "Vehicles land vehicle prefab will be enabled. LandVehicles will be available for all version above V3.0.0",
        true));

    VehicleStaminaHaulingCost = config.BindUnique(SectionKey,
      "VehicleStaminaHaulingCost",
      5f,
      ConfigHelpers.CreateConfigDescription(
        "The cost per 1 meter of hauling a vehicle. This cost is on incurred if the vehicle is being pulled towards the player. When stamina runs out, the player is damaged by this amount until they release the vehicle.", true, false, new AcceptableValueRange<float>(0, 10f)));
    VehicleHaulingSnapsOnStaminaZero = config.BindUnique(SectionKey,
      "VehicleHaulingSnapsOnStaminaZero", false,
      ConfigHelpers.CreateConfigDescription(
        "Instead of allowing the viking to use health. The vehicle hauling line will snap when you have zero stamina doing a single one-time damage.", true, false));

    ExperimentalTreadScaleX = config.BindUnique(SectionKey,
      "Experimental_TreadScaleX", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the tank per tread piece X scale (width). This will make the treads larger or smaller allowing more/less grip.", true, false, new AcceptableValueRange<float>(0.5f, 5f)));

    AdminsCanOnlyBuildRaft = config.BindUnique("Server config",
      "AdminsCanOnlyBuildRaft", false,
      ConfigHelpers.CreateConfigDescription(
        "ValheimRAFT hammer menu pieces are registered as disabled unless the user is an Admin, allowing only admins to create rafts. This will update automatically make sure to un-equip the hammer to see it apply (if your remove yourself as admin). Server / client does not need to restart",
        true, true));

    AllowExperimentalPrefabs = config.BindUnique(SectionKey,
      "AllowExperimentalPrefabs", false,
      ConfigHelpers.CreateConfigDescription(
        "Allows >=v2.0.0 experimental prefabs such as Iron variants of slabs, hulls, and ribs. They do not look great so they are disabled by default",
        true, false));



    Graphics_AllowSailsFadeInFog = config.BindUnique("Graphics", "Sails Fade In Fog",
      true,
      "Allow sails to fade in fog. Unchecking this will be slightly better FPS but less realistic. Should be fine to keep enabled");


    MakeAllPiecesWaterProof = config.BindUnique<bool>("Server config",
      "MakeAllPiecesWaterProof", true, ConfigHelpers.CreateConfigDescription(
        "Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.",
        true
      ));

    const string customSettingMessage = "This is just a default. Any vehicle can be configured directly via config menu.";


    VehicleLandMaxTreadWidth = config.BindUnique(SectionKey,
      "LandVehicle Max Tread Width",
      8,
      ConfigHelpers.CreateConfigDescription(
        $"Max width the treads can expand to. Lower values will let you make motor bikes. This affects all vehicles. {customSettingMessage}", true, false, new AcceptableValueRange<int>(1, 20)));

    VehicleLandMaxTreadLength = config.BindUnique(SectionKey,
      "LandVehicle Max Tread Length",
      20,
      ConfigHelpers.CreateConfigDescription(
        $"Max length the treads can expand to. {customSettingMessage}", true, false, new AcceptableValueRange<int>(4, 100)));

    VehicleDockPositionChangeSpeed = config.BindUnique(SectionKey,
      "VehicleDockPositionChangeSpeed",
      1f,
      ConfigHelpers.CreateConfigDescription(
        $"Dock position change speed. Higher values will make the vehicle move faster but could cause physics problems.", true, false, new AcceptableValueRange<float>(0.001f, 100f)));

    VehicleDockVerticalHeight = config.BindUnique(SectionKey,
      "VehicleDockVerticalHeight",
      200f,
      ConfigHelpers.CreateConfigDescription(
        $"MaxTowing height where a landvehicle can be grabbed/towed by a ship or flying ship. This is cast from the vehicle's upper most bounds and continues directly upwards without any rotation.", true, false, new AcceptableValueRange<float>(50f, 2000f)));
    VehicleDockSphericalRadius = config.BindUnique(SectionKey,
      "VehicleDockSphericalRadius",
      20f,
      ConfigHelpers.CreateConfigDescription(
        $"MaxTowing radius where a landvehicle can be grabbed/towed by a ship or flying ship. Spheres are significantly less accurate so a higher value could result in accidental matches with wrong vehicle", true, false, new AcceptableValueRange<float>(1f, 50f)));


    CannonBallInventoryWeight = config.BindUnique(VehicleCannonsSection, "CannonBallInventoryWeight", 12f, ConfigHelpers.CreateConfigDescription("Set the weight of cannonballs. For realism 12-48lbs for these cannons.", false, false, new AcceptableValueRange<float>(0, 100)));

    HasCannonFireAudio = config.BindUnique(VehicleCannonsSection, "HasCannonFireAudio", true, ConfigHelpers.CreateConfigDescription("Allows toggling the cannon fire audio", false, false));
    HasCannonReloadAudio = config.BindUnique(VehicleCannonsSection, "HasCannonReloadAudio", true, ConfigHelpers.CreateConfigDescription("Allows toggling the reload audio", false, false));

    CannonFiringDelayPerCannon = config.BindUnique(VehicleCannonsSection, "CannonFireVolume", 0.01f, ConfigHelpers.CreateConfigDescription("Allows setting cannon audio", false, false, new AcceptableValueRange<float>(0, 0.3f)));

    CannonReloadAudioVolume = config.BindUnique(VehicleCannonsSection, "CannonFireVolume", 1f, ConfigHelpers.CreateConfigDescription("Allows setting cannon audio", false, false));
    CannonFireAudioVolume = config.BindUnique(VehicleCannonsSection, "CannonReloadVolume", 1f, ConfigHelpers.CreateConfigDescription("Allows setting the reload audio", false, false));

    CannonAutoAimSpeed = config.BindUnique(VehicleCannonsSection, "CannonAutoAimSpeed", 10f, ConfigHelpers.CreateConfigDescription("Set how fast a cannon can adjust aim and fire. This speeds up both firing and animations. Lower values might not be able to fire cannons at all for smaller targets. Keep in mind sea swell will impact the aiming of cannons.", true, false, new AcceptableValueRange<float>(5f, 50f)));

    CannonAimMaxYRotation = config.BindUnique(VehicleCannonsSection, "CannonAimMaxYRotation", 15f, ConfigHelpers.CreateConfigDescription("Maximum Y rotational a cannon can turn. Left to right. Front to bow etc.", true, false, new AcceptableValueRange<float>(5f, 50f)));

    CannonBarrelAimMaxTiltRotation = config.BindUnique(VehicleCannonsSection, "CannonBarrelAimMaxTiltRotation", 180f, ConfigHelpers.CreateConfigDescription("Maximum X rotation the barrel of the cannon can turn. Left to right", true, false, new AcceptableValueRange<float>(5f, 50f)));
    CannonBarrelAimMinTiltRotation = config.BindUnique(VehicleCannonsSection, "CannonBarrelAimMinTiltRotation", -180f, ConfigHelpers.CreateConfigDescription("Min X rotation the barrel of the cannon can turn. This is the downwards rotation.", true, false, new AcceptableValueRange<float>(5f, 50f)));

    CannonPlayerProtectionRangeRadius = config.BindUnique(VehicleCannonsSection, "CannonPlayerProtectionRange", 15f, ConfigHelpers.CreateConfigDescription("Player protection range of vehicle. This will be applied the moment they enter the vehicle and leave the vehicle. Players nearby the vehicle will not be included (for now).", true, false, new AcceptableValueRange<float>(5f, 150f)));

    CannonVehicleProtectionRange = config.BindUnique(VehicleCannonsSection, "CannonVehicleProtectionRange", new SerializableVector3(Vector3.one).ToVector3(), ConfigHelpers.CreateConfigDescription("Vehicle Protection Range of Cannons. This is added on top of the current vehicle Box bounds in X, Y, Z. NOT YET CONNECTED. ZONE SYSTEMS NEED TO BE SUPPORTED FOR THIS TO WORK.", true, false));

    CannonPlayerProtectionRangeRadius.SettingChanged += (sender, args) => TargetController.MAX_DEFEND_SEARCH_RADIUS = CannonPlayerProtectionRangeRadius.Value;
    // CannonVehicleProtectionRange.SettingChanged += (sender, args) => TargetController.HasReloadAudio = HasCannonReloadAudio.Value;

    HasCannonFireAudio.SettingChanged += (sender, args) => CannonController.HasFireAudio = HasCannonFireAudio.Value;
    HasCannonReloadAudio.SettingChanged += (sender, args) => CannonController.HasReloadAudio = HasCannonReloadAudio.Value;
    CannonFireAudioVolume.SettingChanged += (sender, args) => CannonController.CannonFireAudioVolume = CannonFireAudioVolume.Value;
    CannonReloadAudioVolume.SettingChanged += (sender, args) => CannonController.CannonReloadAudioVolume = CannonReloadAudioVolume.Value;

    CannonFiringDelayPerCannon.SettingChanged += (sender, args) => TargetController.FiringDelayPerCannon = CannonFiringDelayPerCannon.Value;
    CannonAutoAimSpeed.SettingChanged += (sender, args) => CannonController.CannonAimSpeed = CannonAutoAimSpeed.Value;
    CannonAimMaxYRotation.SettingChanged += (sender, args) => CannonController.MaxFiringRotationYOverride = CannonAimMaxYRotation.Value;
    CannonBarrelAimMinTiltRotation.SettingChanged += (sender, args) => CannonController.MinFiringPitchOverride = CannonBarrelAimMinTiltRotation.Value;
    CannonBarrelAimMaxTiltRotation.SettingChanged += (sender, args) => CannonController.MaxFiringPitchOverride = CannonBarrelAimMaxTiltRotation.Value;

    VehicleLandMaxTreadWidth.SettingChanged += (sender, args) => VehicleManager.UpdateAllLandMovementControllers();
    VehicleLandMaxTreadLength.SettingChanged += (sender, args) => VehicleManager.UpdateAllLandMovementControllers();

    AllowExperimentalPrefabs.SettingChanged +=
      ExperimentalPrefabRegistry.OnExperimentalPrefabSettingsChange;

    ExperimentalTreadScaleX.SettingChanged += (sender, args) => VehicleManager.UpdateAllLandMovementControllers();
    VehicleStaminaHaulingCost.SettingChanged += (_, __) =>
    {
      VehicleMovementController.staminaHaulCost = VehicleStaminaHaulingCost.Value;
    };
    VehicleHaulingSnapsOnStaminaZero.SettingChanged += (_, __) =>
    {
      VehicleMovementController.ShouldHaulingLineSnapOnZeroStamina = VehicleHaulingSnapsOnStaminaZero.Value;
    };
    EnableLandVehicles.SettingChanged += (_, __) => UpdatePrefabEnabled(PrefabNames.LandVehicle, EnableLandVehicles.Value);
  }
}