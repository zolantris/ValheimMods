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

namespace ValheimVehicles.Config;

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

  public static ConfigEntry<bool> MakeAllPiecesWaterProof { get; set; }
  public static ConfigEntry<bool> AllowTieredMastToRotate { get; set; }
  public static ConfigEntry<bool> Swivels_DoNotRequirePower { get; set; }

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

  public static ConfigEntry<float> SwivelPowerDrain { get; set; } = null!;
  public static ConfigEntry<float> PowerSource_FuelCapacity { get; set; } = null!;
  public static ConfigEntry<float> PowerSource_EitrEfficiency { get; set; } = null!;
  public static ConfigEntry<float> PowerSource_BaseFuelEfficiency { get; set; } = null!;
  public static ConfigEntry<float> PowerSource_FuelConsumptionRate { get; set; } = null!;
  public static ConfigEntry<float> PowerStorage_Capacity { get; set; } = null!;


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

  public static void UpdatePrefabEnabled(string prefabName, bool enabled)
  {
    if (PieceManager.Instance == null) return;
    var prefab = PieceManager.Instance.GetPiece(prefabName);
    if (prefab == null) return;
    prefab.Piece.m_enabled = enabled;
  }

  public void UpdatePowerSources()
  {
    PowerSourceComponent.EitrFuelEfficiency = PowerSource_EitrEfficiency.Value;
    PowerSourceComponent.BaseFuelEfficiency = PowerSource_BaseFuelEfficiency.Value;

    foreach (var powerSource in PowerNetworkController.Sources)
    {
      powerSource.SetFuelConsumptionRate(PowerSource_FuelConsumptionRate.Value);
      powerSource.SetFuelCapacity(PowerSource_FuelCapacity.Value);
      powerSource.UpdateFuelEfficiency();
    }
  }

  public void UpdatePowerStorages()
  {
    foreach (var powerStorage in PowerNetworkController.Storages)
    {
      powerStorage.SetCapacity(PowerStorage_Capacity.Value);
    }
  }

  public override void OnBindConfig(ConfigFile config)
  {
    SwivelPowerDrain = config.Bind(SectionKey,
      "SwivelPowerDrain", 1f,
      ConfigHelpers.CreateConfigDescription(
        "How much power (watts) is consumed by a Swivel per second. Applies only if Swivels_DoNotRequirePower is false.",
        true, false,
        new AcceptableValueRange<float>(0f, 100f)));

    // sources
    PowerSource_FuelCapacity = config.Bind(SectionKey,
      "PowerSourceFuelCapacity", 100f,
      ConfigHelpers.CreateConfigDescription(
        "The maximum amount of fuel a power source can hold.",
        true, false,
        new AcceptableValueRange<float>(1f, 1000f)));
    PowerSource_BaseFuelEfficiency = config.Bind(SectionKey,
      "PowerSource_BaseEfficiency", 1f,
      ConfigHelpers.CreateConfigDescription(
        "The base efficiency of all fuel. This can be used to tweak all fuels and keep them scaling.",
        true, false,
        new AcceptableValueRange<float>(1f, 10f)));
    PowerSource_EitrEfficiency = config.Bind(SectionKey,
      "PowerSource_EitrEfficiency", 1f,
      ConfigHelpers.CreateConfigDescription(
        "The efficiency of Eitr as fuel. IE 1 eitr turns into X fuel. This will be used for balancing with other fuel types if more fuel types are added.",
        true, false,
        new AcceptableValueRange<float>(1f, 1000f)));
    PowerSource_FuelConsumptionRate = config.Bind(SectionKey,
      "PowerSource_FuelConsumptionRate", 0.1f,
      ConfigHelpers.CreateConfigDescription(
        "The amount of fuel consumed per physics update tick at full power output by a power source.",
        true, false,
        new AcceptableValueRange<float>(0.01f, 100f)));

    // storage
    PowerStorage_Capacity = config.Bind(SectionKey,
      "PowerStorageCapacity", 800f,
      ConfigHelpers.CreateConfigDescription(
        "The maximum amount of energy a power storage unit can hold.",
        true, false,
        new AcceptableValueRange<float>(10f, 2000f)));

    SwivelComponent.SwivelEnergyDrain = SwivelPowerDrain.Value;


    PowerSource_FuelCapacity.SettingChanged += (sender, args) => UpdatePowerSources();
    PowerSource_BaseFuelEfficiency.SettingChanged += (sender, args) => UpdatePowerSources();
    PowerSource_EitrEfficiency.SettingChanged += (sender, args) => UpdatePowerSources();
    PowerSource_FuelConsumptionRate.SettingChanged += (sender, args) => UpdatePowerSources();

    SwivelPowerDrain.SettingChanged += (sender, args) =>
    {
      SwivelComponent.SwivelEnergyDrain = SwivelPowerDrain.Value;
      foreach (var swivelComponent in SwivelComponent.Instances)
      {
        swivelComponent.UpdatePowerConsumer();
        swivelComponent.UpdateBasePowerConsumption();
      }
    };
    PowerStorage_Capacity.SettingChanged += (sender, args) => UpdatePowerStorages();
    PowerStorage_Capacity.SettingChanged += (sender, args) => UpdatePowerSources();

    UpdatePowerSources();
    UpdatePowerStorages();


    Swivels_DoNotRequirePower = config.Bind(SectionKey, "Swivels_DoNotRequirePower",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Allows you to use swivels without the vehicle power system.",
        true, false));

    AllowTieredMastToRotate = config.Bind(SectionKey, "AllowTieredMastToRotateInWind", true, "allows the tiered mast to rotate in wind");
    RopeLadderEjectionOffset = config.Bind(SectionKey,
      "RopeLadderEjectionPoint", Vector3.zero,
      "The place the player is placed after they leave the ladder. Defaults to Y +0.25 and Z +0.5 meaning you are placed forwards of the ladder.");

    StartingPiece = config.Bind(SectionKey, "Vehicle Hull Starting Piece",
      VehicleShipInitPiece.Hull4X8,
      ConfigHelpers.CreateConfigDescription(
        "Allows you to customize what piece the raft initializes with. Admins only as this can be overpowered.",
        true, true));
    RopeLadderRunMultiplier = config.Bind(SectionKey,
      "ropeLadderRunClimbSpeedMult", 2,
      ConfigHelpers.CreateConfigDescription(
        "Allows you to customize how fast you can climb a ladder when in run mode",
        false, true, new AcceptableValueRange<int>(1, 10)));
    RopeLadderHints = config.Bind(SectionKey, "ropeLadderHints", true,
      ConfigHelpers.CreateConfigDescription(
        "Shows the controls required to auto ascend/descend and run to speedup ladder"));

    ProtectVehiclePiecesOnErrorFromWearNTearDamage = config.Bind(
      SectionKey,
      "Protect Vehicle pieces from breaking on Error", true,
      ConfigHelpers.CreateConfigDescription(
        "Protects against crashes breaking raft/vehicle initialization causing raft/vehicles to slowly break pieces attached to it. This will make pieces attached to valid raft ZDOs unbreakable from damage, but still breakable with hammer",
        true, true));

    GlassDefaultColor = config.Bind(SectionKey,
      "GlassDefaultColor",
      new Color(0.60f, 0.60f, 0.60f, 0.05f),
      ConfigHelpers.CreateConfigDescription(
        "Set the experimental glass color for your vehicle. This will be used for most glass meshes. This is the default color. Eventually players can customize the color of the glass.",
        true, true));

    EnableLandVehicles = config.Bind(SectionKey, "enableLandVehicles", false,
      ConfigHelpers.CreateConfigDescription(
        "Vehicles land vehicle prefab will be enabled. LandVehicles will be available for all version above V3.0.0",
        true));

    VehicleStaminaHaulingCost = config.Bind(SectionKey,
      "VehicleStaminaHaulingCost",
      5f,
      ConfigHelpers.CreateConfigDescription(
        "The cost per 1 meter of hauling a vehicle. This cost is on incurred if the vehicle is being pulled towards the player. When stamina runs out, the player is damaged by this amount until they release the vehicle.", true, false, new AcceptableValueRange<float>(0, 10f)));
    VehicleHaulingSnapsOnStaminaZero = config.Bind(SectionKey,
      "VehicleHaulingSnapsOnStaminaZero", false,
      ConfigHelpers.CreateConfigDescription(
        "Instead of allowing the viking to use health. The vehicle hauling line will snap when you have zero stamina doing a single one-time damage.", true, false));

    ExperimentalTreadScaleX = config.Bind(SectionKey,
      "Experimental_TreadScaleX", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the tank per tread piece X scale (width). This will make the treads larger or smaller allowing more/less grip.", true, false, new AcceptableValueRange<float>(0.5f, 5f)));


    AdminsCanOnlyBuildRaft = config.Bind("Server config",
      "AdminsCanOnlyBuildRaft", false,
      ConfigHelpers.CreateConfigDescription(
        "ValheimRAFT hammer menu pieces are registered as disabled unless the user is an Admin, allowing only admins to create rafts. This will update automatically make sure to un-equip the hammer to see it apply (if your remove yourself as admin). Server / client does not need to restart",
        true, true));

    AllowExperimentalPrefabs = config.Bind(SectionKey,
      "AllowExperimentalPrefabs", false,
      ConfigHelpers.CreateConfigDescription(
        "Allows >=v2.0.0 experimental prefabs such as Iron variants of slabs, hulls, and ribs. They do not look great so they are disabled by default",
        true, false));



    Graphics_AllowSailsFadeInFog = config.Bind("Graphics", "Sails Fade In Fog",
      true,
      "Allow sails to fade in fog. Unchecking this will be slightly better FPS but less realistic. Should be fine to keep enabled");


    MakeAllPiecesWaterProof = config.Bind<bool>("Server config",
      "MakeAllPiecesWaterProof", true, ConfigHelpers.CreateConfigDescription(
        "Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.",
        true
      ));


    SwivelComponent.IsPoweredSwivel = !Swivels_DoNotRequirePower.Value;
    Swivels_DoNotRequirePower.SettingChanged += (sender, args) => SwivelComponent.IsPoweredSwivel = !Swivels_DoNotRequirePower.Value;

    AllowExperimentalPrefabs.SettingChanged +=
      ExperimentalPrefabRegistry.OnExperimentalPrefabSettingsChange;

    ExperimentalTreadScaleX.SettingChanged += (sender, args) => VehicleManager.UpdateAllWheelControllers();
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