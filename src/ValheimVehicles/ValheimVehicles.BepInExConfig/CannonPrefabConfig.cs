using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Storage.Serialization;
using Zolantris.Shared;
namespace ValheimVehicles.BepInExConfig;

public class CannonPrefabConfig : BepInExBaseConfig<CannonPrefabConfig>
{
  public static ConfigEntry<bool> EnableCannons = null!;

  public static ConfigEntry<bool> Cannon_HasFireAudio = null!;
  public static ConfigEntry<float> Cannon_ReloadTime = null!;
  public static ConfigEntry<float> CannonHandHeld_ReloadTime = null!;
  public static ConfigEntry<float> CannonHandHeld_ReloadStamina = null!;
  public static ConfigEntry<float> CannonHandHeld_AttackStamina = null!;
  public static ConfigEntry<bool> Cannonball_ExplosionAudio_Enabled = null!;
  public static ConfigEntry<bool> Cannonball_WindAudio_Enabled = null!;
  public static ConfigEntry<float> Cannonball_WindAudioVolume = null!;
  public static ConfigEntry<float> Cannonball_ExplosionAudioVolume = null!;
  public static ConfigEntry<float> Cannonball_ExplosiveRadius = null!;
  public static ConfigEntry<float> Cannonball_SolidBaseDamage = null!;
  public static ConfigEntry<float> Cannonball_ExplosiveBaseDamage = null!;
  public static ConfigEntry<bool> DEBUG_Cannonball_UnlimitedAmmo = null!;
  public static ConfigEntry<float> CannonBallInventoryWeight = null!;
  public static ConfigEntry<bool> Cannon_HasReloadAudio = null!;
  public static ConfigEntry<float> Cannon_ReloadAudioVolume = null!;
  public static ConfigEntry<float> Cannon_FiringDelayPerCannon = null!;
  public static ConfigEntry<float> CannonHandheld_AudioStartPosition = null!;
  public static ConfigEntry<float> Cannon_FireAudioVolume = null!;
  public static ConfigEntry<float> CannonAutoAimSpeed = null!;
  public static ConfigEntry<float> CannonAutoAimYOffset = null!;
  public static ConfigEntry<float> CannonAimMaxYRotation = null!;
  public static ConfigEntry<float> Cannon_FireVelocity = null!;

  // cannon control center
  public static ConfigEntry<float> CannonControlCenter_DiscoveryRadius = null!;
  public static ConfigEntry<float> CannonControlCenter_CannonTiltAdjustSpeed = null!;

  // left right rotation.
  public static ConfigEntry<float> CannonHandheld_AimYRotationMax = null!;
  public static ConfigEntry<float> CannonHandheld_AimYRotationMin = null!;

  public static ConfigEntry<float> CannonBarrelAimMaxTiltRotation = null!;
  public static ConfigEntry<float> PowderBarrelExplosiveChainDelay = null!;
  public static ConfigEntry<float> CannonBarrelAimMinTiltRotation = null!;
  public static ConfigEntry<Vector3> CannonAutoTargetVehicleProtectionRange = null!;
  public static ConfigEntry<float> CannonPlayerProtectionRangeRadius = null!;
  public static ConfigEntry<float> Cannonball_ShieldGeneratorDamageMultiplier = null!;


  private const string VehicleCannonsSection = "PrefabConfig: VehicleCannons";
  private const string CannonballsSection = "PrefabConfig: Cannonballs";
  private const string CannonHandheldSection = "PrefabConfig: CannonHandheld";
  private const string CannonControlCenterSection = "PrefabConfig: CannonControlCenter";
  private const string PowderBarrelSection = "PrefabConfig: PowderBarrel";

  public static void SyncHandheldItemData(ItemDrop.ItemData? itemData)
  {
    if (itemData == null) return;
    if (itemData.m_shared == null) return;
    if (itemData.m_shared.m_name != PrefabItemNameToken.CannonHandHeldName) return;
    if (itemData.m_shared.m_attack == null)
    {
      itemData.m_shared.m_attack = new Attack();
    }
    itemData.m_shared.m_attack.m_reloadTime = CannonHandHeld_ReloadTime.Value;
    itemData.m_shared.m_attack.m_attackStamina = CannonHandHeld_AttackStamina.Value;
    itemData.m_shared.m_attack.m_reloadStaminaDrain = CannonHandHeld_ReloadStamina.Value;
  }

  public static void OnCannonHandheldItemUpdate()
  {
    if (PrefabManager.Instance != null && ItemManager.Instance != null)
    {
      var cannonHandheld = ItemManager.Instance.GetItem(PrefabNames.CannonHandHeldItem);
      if (cannonHandheld != null && cannonHandheld.ItemDrop != null && cannonHandheld.ItemDrop.m_itemData != null)
      {
        SyncHandheldItemData(cannonHandheld.ItemDrop.m_itemData);
      }
    }

    CannonController.CannonHandHeld_ReloadTime = CannonHandHeld_ReloadTime.Value;
  }

  public void SetupCannonHandheld(ConfigFile config)
  {
    CannonHandHeld_ReloadTime = config.BindUnique(CannonHandheldSection, "ReloadTime", 6f, ConfigHelpers.CreateConfigDescription("Allows setting cannon-handheld reload delays. This makes cannons reload longer or shorter. Shortest value is 100ms highest is 60seconds", true, false, new AcceptableValueRange<float>(0.1f, 60f)));

    CannonHandheld_AimYRotationMax = config.BindUnique(CannonHandheldSection, "CannonHandheld_AimYRotationMax", CannonHandHeldController.maxYaw, ConfigHelpers.CreateConfigDescription("Maximum Y, the  rotational a cannon can turn toward right. Too much will overlap player and look weird. But it would allow aiming left significantly more without needing to rotate body.", true, false, new AcceptableValueRange<float>(30f, 180f)));
    CannonHandheld_AimYRotationMin = config.BindUnique(CannonHandheldSection, "CannonHandheld_AimYRotationMin", CannonHandHeldController.minYaw, ConfigHelpers.CreateConfigDescription("Minimum Y rotational a cannon can turn, left. Too much will overlap player. But it would allow aiming left significantly more without needing to rotate body.", true, false, new AcceptableValueRange<float>(-180f, -30f)));

    CannonHandHeld_AttackStamina = config.BindUnique(CannonHandheldSection, "AttackStamina", 5f, ConfigHelpers.CreateConfigDescription("Allows setting cannon-handheld reload delays. This makes cannons reload longer or shorter. Shortest value is 100ms highest is 60seconds", true, false, new AcceptableValueRange<float>(0.1f, 60f)));

    CannonHandHeld_ReloadStamina = config.BindUnique(CannonHandheldSection, "ReloadStaminaDrain", 5f, ConfigHelpers.CreateConfigDescription("Allows setting cannon-handheld reload delays. This makes cannons reload longer or shorter. Shortest value is 100ms highest is 60seconds", true, false, new AcceptableValueRange<float>(0.1f, 60f)));

    CannonHandheld_AudioStartPosition = config.BindUnique(CannonHandheldSection, "AudioStartPosition", 0.25f, ConfigHelpers.CreateConfigDescription("Set set the audio start position. This will sound like a heavy flintlock if to close to 0f. Warning: the audio will be desynced it plays the click when the cannonball is already firing (0 - 0.15f)", false, true, new AcceptableValueRange<float>(0f, 1.5f)));


    CannonController.CannonHandHeld_ReloadTime = CannonHandHeld_ReloadTime.Value;
    CannonHandHeldController.maxYaw = CannonHandheld_AimYRotationMax.Value;
    CannonHandHeldController.minYaw = CannonHandheld_AimYRotationMin.Value;
    CannonController.CannonHandheld_FireAudioStartTime = CannonHandheld_AudioStartPosition.Value;

    CannonHandheld_AimYRotationMax.SettingChanged += (sender, args) => CannonHandHeldController.maxYaw = CannonHandheld_AimYRotationMax.Value;
    CannonHandheld_AimYRotationMin.SettingChanged += (sender, args) => CannonHandHeldController.minYaw = CannonHandheld_AimYRotationMin.Value;
    CannonHandHeld_ReloadTime.SettingChanged += (sender, args) => OnCannonHandheldItemUpdate();
    CannonHandHeld_ReloadStamina.SettingChanged += (sender, args) => OnCannonHandheldItemUpdate();
    CannonHandHeld_AttackStamina.SettingChanged += (sender, args) => OnCannonHandheldItemUpdate();
    CannonHandheld_AudioStartPosition.SettingChanged += (sender, args) =>
    {
      CannonController.CannonHandheld_FireAudioStartTime = CannonHandheld_AudioStartPosition.Value;
    };
  }

  public void SetupCannonPrefab(ConfigFile config)
  {
    Cannon_HasFireAudio = config.BindUnique(VehicleCannonsSection, "Cannon_HasFireAudio", true, ConfigHelpers.CreateConfigDescription("Allows toggling the cannon fire audio", false, false));
    Cannon_HasReloadAudio = config.BindUnique(VehicleCannonsSection, "UNSTABLE_Cannon_HasReloadAudio", false, ConfigHelpers.CreateConfigDescription("Allows toggling the reload audio. Unstable b/c it does not sound great when many of these are fired together.", false, false));

    Cannon_FireVelocity = config.BindUnique(VehicleCannonsSection, "Cannon_FireVelocity", 90f, ConfigHelpers.CreateConfigDescription("Allows setting cannon firing velocity", true, false, new AcceptableValueRange<float>(90f, 300f)));

    CannonController.cannonballSpeed = Cannon_FireVelocity.Value;
    Cannon_FireVelocity.SettingChanged += (sender, args) =>
    {
      CannonController.cannonballSpeed = Cannon_FireVelocity.Value;
    };

    Cannon_FiringDelayPerCannon = config.BindUnique(VehicleCannonsSection, "Cannon_FiringDelayPerCannon", 0.1f, ConfigHelpers.CreateConfigDescription("Allows customizing cannon firing delays. This makes cannons fire in a order.", true, false, new AcceptableValueRange<float>(0.05f, 0.3f)));
    Cannon_ReloadTime = config.BindUnique(VehicleCannonsSection, "Cannon_ReloadTime", 6f, ConfigHelpers.CreateConfigDescription("Allows setting cannon reload delays. This makes cannons reload longer or shorter. Shortest value is 100ms highest is 60seconds", true, false, new AcceptableValueRange<float>(0.1f, 60f)));

    CannonAutoAimYOffset = config.BindUnique(VehicleCannonsSection, "CannonAutoAimYOffset", 1f, ConfigHelpers.CreateConfigDescription("Set the Y offset where the cannonball attempt to hit. 0 will aim deadcenter, but it could miss due to gravity. Using above 0 will aim from center to top (1).", true, false, new AcceptableValueRange<float>(-1f, 1f)));
    CannonAutoAimSpeed = config.BindUnique(VehicleCannonsSection, "CannonAutoAimSpeed", 10f, ConfigHelpers.CreateConfigDescription("Set how fast a cannon can adjust aim and fire. This speeds up both firing and animations. Lower values might not be able to fire cannons at all for smaller targets. Keep in mind sea swell will impact the aiming of cannons.", true, false, new AcceptableValueRange<float>(5f, 50f)));
    CannonAimMaxYRotation = config.BindUnique(VehicleCannonsSection, "CannonAimMaxYRotation", 15f, ConfigHelpers.CreateConfigDescription("Maximum Y rotational a cannon can turn. Left to right. Front to bow etc.", true, false, new AcceptableValueRange<float>(5f, 50f)));
    CannonControlCenter_DiscoveryRadius = config.BindUnique(CannonControlCenterSection, "DiscoveryRadius", 15f, ConfigHelpers.CreateConfigDescription("The radius in which a single cannon control center controls all cannons and detect and prevents other control radiuses from being placed. Requires a reload of the area when updating.", true, false, new AcceptableValueRange<float>(30f, 80f)));
    CannonControlCenter_CannonTiltAdjustSpeed = config.BindUnique(CannonControlCenterSection, "CannonTiltAdjustSpeed", 0.5f, ConfigHelpers.CreateConfigDescription("Tilt adjust speed for the manual cannons while using the control center. This is a percentage 0% is 10x slower than 100%", true, false, new AcceptableValueRange<float>(0f, 1f)));


    CannonBarrelAimMaxTiltRotation = config.BindUnique(VehicleCannonsSection, "CannonBarrelAimMaxTiltRotation", 180f, ConfigHelpers.CreateConfigDescription("Maximum X rotation the barrel of the cannon can turn. Left to right", true, false, new AcceptableValueRange<float>(5f, 50f)));
    CannonBarrelAimMinTiltRotation = config.BindUnique(VehicleCannonsSection, "CannonBarrelAimMinTiltRotation", -180f, ConfigHelpers.CreateConfigDescription("Min X rotation the barrel of the cannon can turn. This is the downwards rotation.", true, false, new AcceptableValueRange<float>(5f, 50f)));

    CannonPlayerProtectionRangeRadius = config.BindUnique(VehicleCannonsSection, "CannonPlayerProtectionRange", 15f, ConfigHelpers.CreateConfigDescription("Player protection range of vehicle. This will be applied the moment they enter the vehicle and leave the vehicle. Players nearby the vehicle will not be included (for now).", true, false, new AcceptableValueRange<float>(5f, 150f)));

    CannonAutoTargetVehicleProtectionRange = config.BindUnique(VehicleCannonsSection, "CannonVehicleProtectionRange", new SerializableVector3(Vector3.one).ToVector3(), ConfigHelpers.CreateConfigDescription("Vehicle Protection Range of Cannons. This is added on top of the current vehicle Box bounds in X, Y, Z. NOT YET CONNECTED. ZONE SYSTEMS NEED TO BE SUPPORTED FOR THIS TO WORK.", true, false));


    Cannon_ReloadAudioVolume = config.BindUnique(VehicleCannonsSection, "Cannon_ReloadAudioVolume", 1f, ConfigHelpers.CreateConfigDescription("Allows customizing cannon firing audio volume", false, false));
    Cannon_FireAudioVolume = config.BindUnique(VehicleCannonsSection, "Cannon_FireAudioVolume", 1f, ConfigHelpers.CreateConfigDescription("Allows customizing cannon reload audio volume", false, false));

    // assignments
    TargetController.UpdateCannonTiltSpeedLerp();
    TargetController.CannonControlCenterDiscoveryRadius = CannonControlCenter_DiscoveryRadius.Value;
    CannonController.ReloadTimeOverride = Cannon_ReloadTime.Value;


    //events.
    Cannon_ReloadTime.SettingChanged += (sender, args) =>
    {
      CannonController.ReloadTimeOverride = Cannon_ReloadTime.Value;
    };
    CannonControlCenter_DiscoveryRadius.SettingChanged += (sender, args) =>
    {
      TargetController.CannonControlCenterDiscoveryRadius = CannonControlCenter_DiscoveryRadius.Value;
    };
    CannonControlCenter_CannonTiltAdjustSpeed.SettingChanged += (sender, args) =>
    {
      TargetController.UpdateCannonTiltSpeedLerp();
    };
    CannonPlayerProtectionRangeRadius.SettingChanged += (sender, args) => TargetController.MAX_DEFEND_SEARCH_RADIUS = CannonPlayerProtectionRangeRadius.Value;
    CannonAutoAimYOffset.SettingChanged += (sender, args) =>
    {
      CannonController.CannonAimingCenterOffsetY = CannonAutoAimYOffset.Value;
    };
    Cannon_HasFireAudio.SettingChanged += (sender, args) => CannonController.HasFireAudio = Cannon_HasFireAudio.Value;
    Cannon_FireAudioVolume.SettingChanged += (sender, args) => CannonController.CannonFireAudioVolume = Cannon_FireAudioVolume.Value;
    Cannon_HasReloadAudio.SettingChanged += (sender, args) => CannonController.HasReloadAudio = Cannon_HasReloadAudio.Value;
    Cannon_ReloadAudioVolume.SettingChanged += (sender, args) => CannonController.CannonReloadAudioVolume = Cannon_ReloadAudioVolume.Value;
    Cannon_FiringDelayPerCannon.SettingChanged += (sender, args) => TargetController.FiringDelayPerCannon = Cannon_FiringDelayPerCannon.Value;
    CannonAutoAimSpeed.SettingChanged += (sender, args) => CannonController.CannonAimSpeed = CannonAutoAimSpeed.Value;
    CannonAimMaxYRotation.SettingChanged += (sender, args) => CannonController.MaxFiringRotationYOverride = CannonAimMaxYRotation.Value;
    CannonBarrelAimMinTiltRotation.SettingChanged += (sender, args) => CannonController.MinFiringPitchOverride = CannonBarrelAimMinTiltRotation.Value;
    CannonBarrelAimMaxTiltRotation.SettingChanged += (sender, args) => CannonController.MaxFiringPitchOverride = CannonBarrelAimMaxTiltRotation.Value;
  }

  public void SetupPowderBarrel(ConfigFile config)
  {
    PowderBarrelExplosiveChainDelay = config.BindUnique(PowderBarrelSection, "PowderBarrelExplosiveChainDelay", 0.25f, ConfigHelpers.CreateConfigDescription("Set the powder barrel explosive chain delay. It will blow up nearby barrels but at a delayed fuse to make things a bit more realistic or at least cinematic.", false, false, new AcceptableValueRange<float>(0f, 2f)));
    PowderBarrel.BarrelExplosionChainDelay = PowderBarrelExplosiveChainDelay.Value;

    PowderBarrelExplosiveChainDelay.SettingChanged += (sender, args) =>
    {
      PowderBarrel.BarrelExplosionChainDelay = PowderBarrelExplosiveChainDelay.Value;
    };
  }

  public static void OnCannonballItemChange()
  {
    if (PrefabManager.Instance == null || ItemManager.Instance == null) return;
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
  }

  public void SetupCannonball(ConfigFile config)
  {
    Cannonball_ShieldGeneratorDamageMultiplier = config.BindUnique(CannonballsSection, "ShieldGeneratorDamageMultiplier", 0.25f, ConfigHelpers.CreateConfigDescription("Set the damage cannons do to shield generators. Shield generators should soak more damage to be balanced. So consider using a low number for cannonballs otherwise only ~3 hits can collapse a generator", true, false, new AcceptableValueRange<float>(0, 1)));
    CannonBallInventoryWeight = config.BindUnique(CannonballsSection, "InventoryWeight", 4f, ConfigHelpers.CreateConfigDescription("Set the weight of cannonballs. For realism 12-48lbs for these cannons.", true, false, new AcceptableValueRange<float>(0, 100)));

    Cannonball_ExplosiveRadius = config.BindUnique(CannonballsSection, "ExplosionRadius", 7.5f, ConfigHelpers.CreateConfigDescription("Allows customizing cannonball explosion radius/aoe. Large sizes will lag out objects like rocks", true, false, new AcceptableValueRange<float>(3f, 20f)));

    Cannonball_SolidBaseDamage = config.BindUnique(CannonballsSection, "SolidShell_BaseDamage", 85f, ConfigHelpers.CreateConfigDescription("Set the amount of damage a solid cannon ball does. This value is multiplied by the velocity of the cannonball around 90 at max speed decreasing to 20 m/s at lowest hit damage level.", true, false, new AcceptableValueRange<float>(25f, 500f)));
    Cannonball_ExplosiveBaseDamage = config.BindUnique(CannonballsSection, "ExplosiveShell_BaseDamage", 50f, ConfigHelpers.CreateConfigDescription("Allows customizing cannonball explosion hit AOE damage. The damage is uniform across the entire radius.", true, false, new AcceptableValueRange<float>(25f, 500f)));

    DEBUG_Cannonball_UnlimitedAmmo = config.BindUnique(CannonballsSection, "DEBUG_UnlimitedAmmo", false, ConfigHelpers.CreateConfigDescription("Allows unlimited ammo for cannons. This is meant for testing cannons but not realistic.", true, false));


    Cannonball_ExplosionAudioVolume = config.BindUnique(CannonballsSection, "ExplosionAudioVolume", 1f, ConfigHelpers.CreateConfigDescription("Allows customizing cannon reload audio volume", false, false));
    Cannonball_WindAudio_Enabled = config.BindUnique(CannonballsSection, "WindAudio_Enabled", true, ConfigHelpers.CreateConfigDescription("Allows enable cannonball wind audio - which can be heard if a cannonball passes nearby.", false, false));
    Cannonball_WindAudioVolume = config.BindUnique(CannonballsSection, "WindAudioVolume", 0.2f, ConfigHelpers.CreateConfigDescription("Allows customizing cannonball wind audio - which can be heard if a cannonball passes nearby. Recommended below 0.2f", false, false));
    Cannonball_ExplosionAudio_Enabled = config.BindUnique(CannonballsSection, "ExplosionAudio_Enabled", true, ConfigHelpers.CreateConfigDescription("Allows toggling the cannonball explosion/impact audio. Unstable b/c it does not sound great when many of these are fired together.", false, false));

    // setters
    Cannonball.ExplosionShellRadius = Cannonball_ExplosiveRadius.Value;
    CannonballHitScheduler.BaseDamageSolidCannonball = Cannonball_SolidBaseDamage.Value;
    CannonballHitScheduler.BaseDamageExplosiveCannonball = Cannonball_ExplosiveBaseDamage.Value;

    // events
    Cannonball_WindAudio_Enabled.SettingChanged += (sender, args) => Cannonball.HasCannonballWindAudio = Cannonball_WindAudio_Enabled.Value;
    CannonBallInventoryWeight.SettingChanged += (sender, args) => OnCannonballItemChange();
    Cannonball_ExplosiveRadius.SettingChanged += (sender, args) =>
    {
      Cannonball.ExplosionShellRadius = Cannonball_ExplosiveRadius.Value;
    };
    Cannonball_SolidBaseDamage.SettingChanged += (sender, args) =>
    {
      CannonballHitScheduler.BaseDamageSolidCannonball = Cannonball_SolidBaseDamage.Value;
    };
    Cannonball_ExplosiveBaseDamage.SettingChanged += (sender, args) =>
    {
      CannonballHitScheduler.BaseDamageExplosiveCannonball = Cannonball_ExplosiveBaseDamage.Value;
    };
    DEBUG_Cannonball_UnlimitedAmmo.SettingChanged += (sender, args) =>
    {
      AmmoController.HasUnlimitedAmmo = DEBUG_Cannonball_UnlimitedAmmo.Value;
    };

    Cannonball_WindAudioVolume.SettingChanged += (sender, args) => Cannonball.CannonballWindAudioVolume = Cannonball_WindAudioVolume.Value;
    Cannonball_ExplosionAudio_Enabled.SettingChanged += (sender, args) => Cannonball.HasExplosionAudio = Cannonball_ExplosionAudio_Enabled.Value;
    Cannonball_ExplosionAudioVolume.SettingChanged += (sender, args) => Cannonball.ExplosionAudioVolume = Cannonball_ExplosionAudioVolume.Value;
  }

  public static string PrefabSection = "Prefabs";

  public override void OnBindConfig(ConfigFile config)
  {
    SetupCannonPrefab(config);
    SetupCannonHandheld(config);
    SetupCannonball(config);
    SetupPowderBarrel(config);

    EnableCannons = config.BindUnique(PrefabConfig.PrefabConfigKey, "CannonPrefabs_Enabled", true, ConfigHelpers.CreateConfigDescription("Allows servers to enable/disable cannons feature.", true, false));
    EnableCannons.SettingChanged += (_, _) => CannonPrefabs.OnEnabledChange();
  }
}