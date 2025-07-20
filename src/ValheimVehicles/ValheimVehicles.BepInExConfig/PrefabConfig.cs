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

  public static ConfigEntry<bool> Cannon_HasFireAudio { get; set; } = null!;
  public static ConfigEntry<float> Cannon_ReloadTime { get; set; } = null!;
  public static ConfigEntry<float> Cannon_HandHeldReloadTime { get; set; } = null!;
  public static ConfigEntry<bool> Cannonball_HasExplosionAudio { get; set; } = null!;
  public static ConfigEntry<bool> HasCannonballWindAudio { get; set; } = null!;
  public static ConfigEntry<float> CannonballWindAudioVolume { get; set; } = null!;
  public static ConfigEntry<float> Cannonball_ExplosionAudioVolume { get; set; } = null!;
  public static ConfigEntry<float> Cannonball_ExplosiveRadius { get; set; } = null!;
  public static ConfigEntry<float> Cannonball_SolidBaseDamage { get; set; } = null!;
  public static ConfigEntry<float> Cannonball_ExplosiveBaseDamage { get; set; } = null!;
  public static ConfigEntry<bool> DEBUG_CannonballUnlimitedAmmo { get; set; } = null!;
  public static ConfigEntry<float> CannonBallInventoryWeight { get; set; } = null!;
  public static ConfigEntry<bool> Cannon_HasReloadAudio { get; set; } = null!;
  public static ConfigEntry<float> Cannon_ReloadAudioVolume { get; set; } = null!;
  public static ConfigEntry<float> Cannon_FiringDelayPerCannon { get; set; } = null!;
  public static ConfigEntry<float> CannonHandheld_AudioStartPosition { get; set; } = null!;
  public static ConfigEntry<float> Cannon_FireAudioVolume { get; set; } = null!;
  public static ConfigEntry<float> CannonballSolidDamage { get; set; } = null!;
  public static ConfigEntry<float> CannonballExplosiveDamage { get; set; } = null!;
  public static ConfigEntry<float> CannonAutoAimSpeed { get; set; } = null!;
  public static ConfigEntry<float> CannonAutoAimYOffset { get; set; } = null!;
  public static ConfigEntry<float> CannonAimMaxYRotation { get; set; } = null!;

  // left right rotation.
  public static ConfigEntry<float> CannonHandheld_AimYRotationMax { get; set; } = null!;
  public static ConfigEntry<float> CannonHandheld_AimYRotationMin { get; set; } = null!;

  public static ConfigEntry<float> CannonBarrelAimMaxTiltRotation { get; set; } = null!;
  public static ConfigEntry<float> PowderBarrelExplosiveChainDelay { get; set; } = null!;
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
  private const string PowderBarrelSection = "PrefabConfig: PowderBarrel";


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

    // todo would have to subscribe to prefab registry to update this.
    CannonBallInventoryWeight = config.BindUnique(VehicleCannonsSection, "CannonBallInventoryWeight", 4f, ConfigHelpers.CreateConfigDescription("Set the weight of cannonballs. For realism 12-48lbs for these cannons.", true, false, new AcceptableValueRange<float>(0, 100)));
    CannonBallInventoryWeight.SettingChanged += (sender, args) =>
    {
      if (PrefabManager.Instance == null) return;
      var cannonballExplosive = ItemManager.Instance.GetItem(PrefabNames.CannonballExplosive);
      var cannonballSolid = ItemManager.Instance.GetItem(PrefabNames.CannonballSolid);

      if (cannonballSolid != null)
      {
        cannonballSolid.ItemDrop.m_itemData.m_shared.m_weight = CannonBallInventoryWeight.Value;
      }

      if (cannonballExplosive != null)
      {
        cannonballExplosive.ItemDrop.m_itemData.m_shared.m_weight = CannonBallInventoryWeight.Value;
      }
    };

    CannonHandheld_AudioStartPosition = config.BindUnique(VehicleCannonsSection, "CannonHandheld_AudioStartPosition", 0.35f, ConfigHelpers.CreateConfigDescription("Set set the audio start position. This will sound like a heavy flintlock if to close to 0f", false, true, new AcceptableValueRange<float>(0f, 1.5f)));
    CannonHandheld_AudioStartPosition.SettingChanged += (sender, args) =>
    {
      CannonController.CannonHandheld_FireAudioStartTime = CannonHandheld_AudioStartPosition.Value;
    };

    Cannon_HasFireAudio = config.BindUnique(VehicleCannonsSection, "Cannon_HasFireAudio", true, ConfigHelpers.CreateConfigDescription("Allows toggling the cannon fire audio", false, false));
    Cannon_HasReloadAudio = config.BindUnique(VehicleCannonsSection, "UNSTABLE_Cannon_HasReloadAudio", false, ConfigHelpers.CreateConfigDescription("Allows toggling the reload audio. Unstable b/c it does not sound great when many of these are fired together.", false, false));
    Cannonball_HasExplosionAudio = config.BindUnique(VehicleCannonsSection, "Cannonball_HasExplosionAudio", true, ConfigHelpers.CreateConfigDescription("Allows toggling the cannonball explosion/impact audio. Unstable b/c it does not sound great when many of these are fired together.", false, false));

    Cannon_FiringDelayPerCannon = config.BindUnique(VehicleCannonsSection, "Cannon_FiringDelayPerCannon", 0.01f, ConfigHelpers.CreateConfigDescription("Allows setting cannon firing delays. This makes cannons fire in a order.", false, false, new AcceptableValueRange<float>(0, 0.3f)));
    Cannon_ReloadTime = config.BindUnique(VehicleCannonsSection, "Cannon_ReloadTime", 6f, ConfigHelpers.CreateConfigDescription("Allows setting cannon reload delays. This makes cannons reload longer or shorter. Shortest value is 100ms highest is 60seconds", false, false, new AcceptableValueRange<float>(0.1f, 60f)));
    Cannon_ReloadTime.SettingChanged += (sender, args) =>
    {
      CannonController.ReloadTimeOverride = Cannon_ReloadTime.Value;
    };
    CannonController.ReloadTimeOverride = Cannon_ReloadTime.Value;

    Cannon_HandHeldReloadTime = config.BindUnique(VehicleCannonsSection, "Cannon_HandHeldReloadTime", 6f, ConfigHelpers.CreateConfigDescription("Allows setting cannon-handheld reload delays. This makes cannons reload longer or shorter. Shortest value is 100ms highest is 60seconds", false, false, new AcceptableValueRange<float>(0.1f, 60f)));
    Cannon_HandHeldReloadTime.SettingChanged += (sender, args) =>
    {
      CannonController.Cannon_HandHeldReloadTime = Cannon_HandHeldReloadTime.Value;
    };
    CannonController.Cannon_HandHeldReloadTime = Cannon_HandHeldReloadTime.Value;

    Cannon_ReloadAudioVolume = config.BindUnique(VehicleCannonsSection, "Cannon_ReloadAudioVolume", 1f, ConfigHelpers.CreateConfigDescription("Allows customizing cannon firing audio volume", false, false));
    Cannon_FireAudioVolume = config.BindUnique(VehicleCannonsSection, "Cannon_FireAudioVolume", 1f, ConfigHelpers.CreateConfigDescription("Allows customizing cannon reload audio volume", false, false));
    Cannonball_ExplosionAudioVolume = config.BindUnique(VehicleCannonsSection, "Cannonball_ExplosionAudioVolume", 1f, ConfigHelpers.CreateConfigDescription("Allows customizing cannon reload audio volume", false, false));

    Cannonball_ExplosiveRadius = config.BindUnique(VehicleCannonsSection, "Cannonball_ExplosionRadius", 7.5f, ConfigHelpers.CreateConfigDescription("Allows customizing cannonball explosion radius/aoe. Large sizes will lag out objects like rocks", false, false, new AcceptableValueRange<float>(3f, 20f)));
    Cannonball_ExplosiveRadius.SettingChanged += (sender, args) =>
    {
      CannonballHitScheduler.ExplosionShellRadius = Cannonball_ExplosiveRadius.Value;
    };
    CannonballHitScheduler.ExplosionShellRadius = Cannonball_ExplosiveRadius.Value;

    Cannonball_SolidBaseDamage = config.BindUnique(VehicleCannonsSection, "Cannonball_SolidShell_BaseDamage", 85f, ConfigHelpers.CreateConfigDescription("Allows customizing cannonball solid hit damage", false, false, new AcceptableValueRange<float>(25f, 500f)));
    Cannonball_SolidBaseDamage.SettingChanged += (sender, args) =>
    {
      CannonballHitScheduler.BaseDamageSolidCannonball = Cannonball_SolidBaseDamage.Value;
    };
    CannonballHitScheduler.BaseDamageSolidCannonball = Cannonball_SolidBaseDamage.Value;

    Cannonball_ExplosiveBaseDamage = config.BindUnique(VehicleCannonsSection, "Cannonball_ExplosiveShell_BaseDamage", 50f, ConfigHelpers.CreateConfigDescription("Allows customizing cannonball explosion hit AOE damage. The damage is uniform across the entire radius.", false, false, new AcceptableValueRange<float>(25f, 500f)));
    Cannonball_ExplosiveBaseDamage.SettingChanged += (sender, args) =>
    {
      CannonballHitScheduler.BaseDamageExplosiveCannonball = Cannonball_ExplosiveBaseDamage.Value;
    };
    CannonballHitScheduler.BaseDamageExplosiveCannonball = Cannonball_ExplosiveBaseDamage.Value;


    DEBUG_CannonballUnlimitedAmmo = config.BindUnique(VehicleCannonsSection, "DEBUG_CannonballUnlimitedAmmo", false, ConfigHelpers.CreateConfigDescription("Allows unlimited ammo for cannons.", true, false));
    DEBUG_CannonballUnlimitedAmmo.SettingChanged += (sender, args) =>
    {
      AmmoController.HasUnlimitedAmmo = DEBUG_CannonballUnlimitedAmmo.Value;
    };



    HasCannonballWindAudio = config.BindUnique(VehicleCannonsSection, "Cannonball_HasWindAudio", true, ConfigHelpers.CreateConfigDescription("Allows enable cannonball wind audio - which can be heard if a cannonball passes nearby.", false, false));
    CannonballWindAudioVolume = config.BindUnique(VehicleCannonsSection, "Cannonball_WindAudioVolume", 0.2f, ConfigHelpers.CreateConfigDescription("Allows customizing cannonball wind audio - which can be heard if a cannonball passes nearby. Recommended below 0.2f", false, false));

    PowderBarrelExplosiveChainDelay = config.BindUnique(PowderBarrelSection, "PowderBarrelExplosiveChainDelay", 0.25f, ConfigHelpers.CreateConfigDescription("Set the powder barrel explosive chain delay. It will blow up nearby barrels but at a delayed fuse to make things a bit more realistic or at least cinematic.", false, false, new AcceptableValueRange<float>(0f, 2f)));
    PowderBarrelExplosiveChainDelay.SettingChanged += (sender, args) =>
    {
      PowderBarrel.BarrelExplosionChainDelay = PowderBarrelExplosiveChainDelay.Value;
    };

    CannonballSolidDamage = config.BindUnique(VehicleCannonsSection, "Cannonball_SolidDamage", 30f, ConfigHelpers.CreateConfigDescription("Set the amount of damage a solid cannon ball does. This value is multiplied by the velocity of the cannonball around 90 at max speed decreasing to 20 m/s at lowest hit damage level.", false, false));
    CannonballSolidDamage.SettingChanged += (sender, args) => CannonballHitScheduler.BaseDamageSolidCannonball = CannonballSolidDamage.Value;

    CannonballExplosiveDamage = config.BindUnique(VehicleCannonsSection, "Cannonball_ExplosiveDamage", 30f, ConfigHelpers.CreateConfigDescription("Set the amount of damage a explosive cannon ball does. This damage includes both the AOE and hit. AOE will do same damage on top of the impact of the shot.", false, false));
    CannonballExplosiveDamage.SettingChanged += (sender, args) => CannonballHitScheduler.BaseDamageExplosiveCannonball = CannonballExplosiveDamage.Value;


    CannonAutoAimYOffset = config.BindUnique(VehicleCannonsSection, "CannonAutoAimYOffset", 1f, ConfigHelpers.CreateConfigDescription("Set the Y offset where the cannonball attempt to hit. 0 will aim deadcenter, but it could miss due to gravity. Using above 0 will aim from center to top (1).", true, false, new AcceptableValueRange<float>(-1f, 1f)));
    CannonAutoAimSpeed = config.BindUnique(VehicleCannonsSection, "CannonAutoAimSpeed", 10f, ConfigHelpers.CreateConfigDescription("Set how fast a cannon can adjust aim and fire. This speeds up both firing and animations. Lower values might not be able to fire cannons at all for smaller targets. Keep in mind sea swell will impact the aiming of cannons.", true, false, new AcceptableValueRange<float>(5f, 50f)));

    CannonAimMaxYRotation = config.BindUnique(VehicleCannonsSection, "CannonAimMaxYRotation", 15f, ConfigHelpers.CreateConfigDescription("Maximum Y rotational a cannon can turn. Left to right. Front to bow etc.", true, false, new AcceptableValueRange<float>(5f, 50f)));


    CannonHandheld_AimYRotationMax = config.BindUnique(VehicleCannonsSection, "CannonHandheld_AimYRotationMax", CannonHandHeldController.minYaw, ConfigHelpers.CreateConfigDescription("Maximum Y, the  rotational a cannon can turn toward right. Too much will overlap player and look weird. But it would allow aiming left significantly more without needing to rotate body.", true, false, new AcceptableValueRange<float>(30f, 180f)));
    CannonHandheld_AimYRotationMin = config.BindUnique(VehicleCannonsSection, "CannonHandheld_AimYRotationMin", CannonHandHeldController.minYaw, ConfigHelpers.CreateConfigDescription("Minimum Y rotational a cannon can turn, left. Too much will overlap player. But it would allow aiming left significantly more without needing to rotate body.", true, false, new AcceptableValueRange<float>(-180f, -30f)));
    CannonHandheld_AimYRotationMax.SettingChanged += (sender, args) => CannonHandHeldController.maxYaw = CannonHandheld_AimYRotationMax.Value;
    CannonHandheld_AimYRotationMin.SettingChanged += (sender, args) => CannonHandHeldController.minYaw = CannonHandheld_AimYRotationMin.Value;
    CannonHandHeldController.maxYaw = CannonHandheld_AimYRotationMax.Value;
    CannonHandHeldController.minYaw = CannonHandheld_AimYRotationMin.Value;


    CannonBarrelAimMaxTiltRotation = config.BindUnique(VehicleCannonsSection, "CannonBarrelAimMaxTiltRotation", 180f, ConfigHelpers.CreateConfigDescription("Maximum X rotation the barrel of the cannon can turn. Left to right", true, false, new AcceptableValueRange<float>(5f, 50f)));
    CannonBarrelAimMinTiltRotation = config.BindUnique(VehicleCannonsSection, "CannonBarrelAimMinTiltRotation", -180f, ConfigHelpers.CreateConfigDescription("Min X rotation the barrel of the cannon can turn. This is the downwards rotation.", true, false, new AcceptableValueRange<float>(5f, 50f)));

    CannonPlayerProtectionRangeRadius = config.BindUnique(VehicleCannonsSection, "CannonPlayerProtectionRange", 15f, ConfigHelpers.CreateConfigDescription("Player protection range of vehicle. This will be applied the moment they enter the vehicle and leave the vehicle. Players nearby the vehicle will not be included (for now).", true, false, new AcceptableValueRange<float>(5f, 150f)));

    CannonVehicleProtectionRange = config.BindUnique(VehicleCannonsSection, "CannonVehicleProtectionRange", new SerializableVector3(Vector3.one).ToVector3(), ConfigHelpers.CreateConfigDescription("Vehicle Protection Range of Cannons. This is added on top of the current vehicle Box bounds in X, Y, Z. NOT YET CONNECTED. ZONE SYSTEMS NEED TO BE SUPPORTED FOR THIS TO WORK.", true, false));

    CannonPlayerProtectionRangeRadius.SettingChanged += (sender, args) => TargetController.MAX_DEFEND_SEARCH_RADIUS = CannonPlayerProtectionRangeRadius.Value;

    CannonAutoAimYOffset.SettingChanged += (sender, args) =>
    {
      CannonController.CannonAimingCenterOffsetY = CannonAutoAimYOffset.Value;
    };
    Cannon_HasFireAudio.SettingChanged += (sender, args) => CannonController.HasFireAudio = Cannon_HasFireAudio.Value;
    Cannon_FireAudioVolume.SettingChanged += (sender, args) => CannonController.CannonFireAudioVolume = Cannon_FireAudioVolume.Value;

    HasCannonballWindAudio.SettingChanged += (sender, args) => Cannonball.HasCannonballWindAudio = HasCannonballWindAudio.Value;
    CannonballWindAudioVolume.SettingChanged += (sender, args) => Cannonball.CannonballWindAudioVolume = CannonballWindAudioVolume.Value;

    Cannonball_HasExplosionAudio.SettingChanged += (sender, args) => Cannonball.HasExplosionAudio = Cannonball_HasExplosionAudio.Value;
    Cannonball_ExplosionAudioVolume.SettingChanged += (sender, args) => Cannonball.ExplosionAudioVolume = Cannonball_ExplosionAudioVolume.Value;

    Cannon_HasReloadAudio.SettingChanged += (sender, args) => CannonController.HasReloadAudio = Cannon_HasReloadAudio.Value;
    Cannon_ReloadAudioVolume.SettingChanged += (sender, args) => CannonController.CannonReloadAudioVolume = Cannon_ReloadAudioVolume.Value;

    Cannon_FiringDelayPerCannon.SettingChanged += (sender, args) => TargetController.FiringDelayPerCannon = Cannon_FiringDelayPerCannon.Value;
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