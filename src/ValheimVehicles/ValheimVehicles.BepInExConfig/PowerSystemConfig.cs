using BepInEx.Configuration;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using Zolantris.Shared;
namespace ValheimVehicles.BepInExConfig;

public class PowerSystemConfig : BepInExBaseConfig<PowerSystemConfig>
{
  // connectivity / range
  public static ConfigEntry<float> PowerPylonRange = null!;
  public static ConfigEntry<float> PowerMechanismRange = null!;


  // generic
  public static ConfigEntry<bool> PowerNetwork_ShowAdditionalPowerInformationByDefault = null!;

  // plates/conduits
  public static ConfigEntry<bool> PowerPlate_ShowStatus = null!;
  public static ConfigEntry<float> PowerPlate_TransferRate = null!;
  public static ConfigEntry<float> PowerPlate_EitrDrainCostPerSecond = null!;
  public static ConfigEntry<float> PowerPlate_EnergyGainPerSecond = null!;

  // sources
  public static ConfigEntry<bool> PowerSource_AllowNearbyFuelingWithEitr = null!;
  public static ConfigEntry<float> PowerSource_FuelCapacity = null!;
  public static ConfigEntry<float> PowerSource_EitrEfficiency = null!;
  public static ConfigEntry<float> PowerSource_BaseFuelEfficiency = null!;
  public static ConfigEntry<float> PowerSource_FuelConsumptionRate = null!;

  public static ConfigEntry<float> PowerSimulationDistanceThreshold = null!;

  // storage
  public static ConfigEntry<float> PowerStorage_Capacity = null!;

  // mechanism
  public static ConfigEntry<MechanismAction> Mechanism_Switch_DefaultAction = null!;

  // swivels
  public static ConfigEntry<bool> Swivels_DoNotRequirePower { get; set; }
  public static ConfigEntry<float> SwivelPowerDrain = null!;

  // vehicles
  public static ConfigEntry<bool> LandVehicle_DoNotRequirePower { get; set; }
  public static ConfigEntry<float> LandVehicle_PowerDrain = null!;


  public static void UpdatePowerConduits()
  {
    PowerConduitData.RechargeRate = PowerPlate_TransferRate.Value;
    PowerConduitData.EitrRegenCostPerInterval = PowerPlate_EitrDrainCostPerSecond.Value;
    PowerConduitData.EnergyChargePerInterval = PowerPlate_EnergyGainPerSecond.Value;
  }

  public static void UpdatePowerRanges()
  {
    PowerSystemComputeData.PowerRangeDefault = PowerMechanismRange.Value;
    PowerSystemComputeData.PowerRangePylonDefault = PowerPylonRange.Value;
  }

  public void UpdatePowerSources()
  {
    PowerSourceData.FuelConsumptionRateDefault = PowerSource_FuelConsumptionRate.Value;
    PowerSourceData.FuelCapacityDefault = PowerSource_FuelCapacity.Value;

    PowerSourceData.FuelEfficiencyDefault = PowerSource_BaseFuelEfficiency.Value;
    PowerSourceData.EitrFuelEfficiency = PowerSource_EitrEfficiency.Value;

    foreach (var powerSource in PowerSystemRegistry._sources)
    {
      powerSource.BaseFuelEfficiency = PowerSource_BaseFuelEfficiency.Value;
      powerSource.OnPropertiesUpdate();
    }
  }

  public static void UpdatePowerStorages()
  {
    foreach (var powerStorage in PowerSystemRegistry._storages)
    {
      powerStorage.SetEnergy(PowerStorage_Capacity.Value);
    }
  }

  public static void UpdatePowerConsumers()
  {
    PowerConsumerData.PowerConsumerBaseValues.Swivel = SwivelPowerDrain.Value;
    PowerConsumerData.PowerConsumerBaseValues.LandVehicleEngine = LandVehicle_PowerDrain.Value;

    foreach (var powerConsumerData in PowerSystemRegistry._consumers)
    {
      powerConsumerData.UpdateBasePowerConsumption();
    }

    // base power intensity gets increased if above specific values of lerp per swivel.
    foreach (var powerConsumerData in SwivelComponent.Instances)
    {
      powerConsumerData.UpdateBasePowerConsumption();
    }
  }

  public static void UpdateVehiclePower()
  {
    SwivelComponent.SwivelEnergyDrain = SwivelPowerDrain.Value;
    foreach (var swivelComponent in SwivelComponent.Instances)
    {
      swivelComponent.UpdatePowerConsumer();
      swivelComponent.UpdateBasePowerConsumption();
    }
  }

  public const string SectionKey = "PowerSystem";

  public override void OnBindConfig(ConfigFile config)
  {

#if DEBUG
    var acceptableRange = new AcceptableValueRange<float>(0.000001f, 10000f);
#else
    var acceptableRange = new AcceptableValueRange<float>(1f, 100f);
#endif

    PowerPylonRange = config.BindUnique(SectionKey, "PowerPylonRange", 10f, ConfigHelpers.CreateConfigDescription("The power range per power pylon prefab. Large values will make huge networks. Max range is 50. But this could span entire continents as ZDOs are not limited to render distance.", true, false, new AcceptableValueRange<float>(10f, 50f)));
    PowerSimulationDistanceThreshold = config.BindUnique(SectionKey, nameof(PowerSimulationDistanceThreshold), 50f, ConfigHelpers.CreateConfigDescription("The maximum threshold in which to simulate networks. This means if a player or client/peer is nearby the power system will continue to simulate. Keeping this value lower will make running powersystems much faster at the cost of power not running while away from an area.", true, false, new AcceptableValueRange<float>(25f, 10000f)));

    PowerMechanismRange = config.BindUnique(SectionKey, "PowerMechanismRange", 4f, ConfigHelpers.CreateConfigDescription("The power range per mechanism power item. This excludes pylons and is capped at a lower number. These items are meant to be connected to pylons but at higher values could connect together.", true, false, new AcceptableValueRange<float>(4f, 10f)));

    PowerPlate_ShowStatus = config.BindUnique(SectionKey, "PowerDrainPlate_ShowStatus", false, ConfigHelpers.CreateConfigDescription("Shows the power drain activity and tells you what type of plate is being used when hovering over it. This flag will be ignored if the PowerNetwork inspector is enabled which allows viewing all power values.", false, false));
    PowerSource_AllowNearbyFuelingWithEitr = config.BindUnique(SectionKey, "PowerSource_AllowNearbyFuelingWithEitr", false, ConfigHelpers.CreateConfigDescription("This will allow for the player to fuel from chests when interacting with Vehicle sources. This may not be needed with chest mods.", true, false));

    PowerNetwork_ShowAdditionalPowerInformationByDefault = config.BindUnique(SectionKey, "PowerNetwork_ShowAdditionalPowerInformationByDefault", false, ConfigHelpers.CreateConfigDescription("This will show the power network information by default per prefab. This acts as a tutorial. Most power items will have a visual indicator but it may not be clear to players immediately.", false, false));

    PowerPlate_TransferRate = config.BindUnique(SectionKey,
      "PowerPlate_TransferRate", 0.05f,
      ConfigHelpers.CreateConfigDescription(
        "How much eitr energy is charged/drained per time to convert to power system energy units. Eitr energy is renewable but should be considered less refined. To maintain balance keep this at a higher number.",
        true, false,
        new AcceptableValueRange<float>(0.001f, 1f)));

    PowerPlate_EitrDrainCostPerSecond = config.BindUnique(SectionKey,
      nameof(PowerPlate_EitrDrainCostPerSecond), 10f,
      ConfigHelpers.CreateConfigDescription(
        "The amount of player eitr that is required per second to power a system.",
        true, false,
        acceptableRange));
    PowerPlate_EnergyGainPerSecond = config.BindUnique(SectionKey,
      nameof(PowerPlate_EnergyGainPerSecond), 1f,
      ConfigHelpers.CreateConfigDescription(
        "The amount of energy gained when draining player eitr per second.",
        true, false,
        acceptableRange));

    // sources
    PowerSource_FuelCapacity = config.BindUnique(SectionKey,
      "PowerSourceFuelCapacity", 100f,
      ConfigHelpers.CreateConfigDescription(
        "The maximum amount of fuel a power source can hold.",
        true, false,
        new AcceptableValueRange<float>(1f, 1000f)));
    PowerSource_BaseFuelEfficiency = config.BindUnique(SectionKey,
      "PowerSource_BaseEfficiency", 1f,
      ConfigHelpers.CreateConfigDescription(
        "The base efficiency of all fuel. This can be used to tweak all fuels and keep them scaling.",
        true, false,
        new AcceptableValueRange<float>(1f, 10f)));
    PowerSource_EitrEfficiency = config.BindUnique(SectionKey,
      "PowerSource_EitrEfficiency", 10f,
      ConfigHelpers.CreateConfigDescription(
        "The efficiency of Eitr as fuel. IE 1 eitr turns into X fuel. This will be used for balancing with other fuel types if more fuel types are added.",
        true, false,
        new AcceptableValueRange<float>(1f, 1000f)));
    PowerSource_FuelConsumptionRate = config.BindUnique(SectionKey,
      "PowerSource_FuelConsumptionRate", 0.1f,
      ConfigHelpers.CreateConfigDescription(
        "The amount of fuel consumed per physics update tick at full power output by a power source.",
        true, false,
        new AcceptableValueRange<float>(0.0001f, 100f)));

    // storage
    PowerStorage_Capacity = config.BindUnique(SectionKey,
      "PowerStorageCapacity", 800f,
      ConfigHelpers.CreateConfigDescription(
        "The maximum amount of energy a power storage unit can hold.",
        true, false,
        new AcceptableValueRange<float>(10f, 2000f)));

    LandVehicle_DoNotRequirePower = config.BindUnique(SectionKey,
      "LandVehicle_DoNotRequirePower", false,
      ConfigHelpers.CreateConfigDescription(
        $"Allows for free usage of land-vehicles without power system. Very unbalanced.",
        true));

    Swivels_DoNotRequirePower = config.BindUnique(SectionKey, "Swivels_DoNotRequirePower",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Allows you to use swivels without the vehicle power system.",
        true));

    LandVehicle_PowerDrain = config.BindUnique(SectionKey,
      "LandVehicle_PowerDrain", 1f,
      ConfigHelpers.CreateConfigDescription(
        $"How much power (watts) is consumed by a LandVehicle per second. This is a base value. Each additional mode will ramp up power. Applies only if {LandVehicle_DoNotRequirePower.Definition.Key} is false.",
        true, false,
        new AcceptableValueRange<float>(0f, 100f)));

    SwivelPowerDrain = config.BindUnique(SectionKey,
      "SwivelPowerDrain", 1f,
      ConfigHelpers.CreateConfigDescription(
        $"How much power (watts) is consumed by a Swivel per second. Swivels have 1 power mode but swivel lerp speed will affect power cost. Applies only if {Swivels_DoNotRequirePower.Definition.Key} is false.",
        true, false,
        new AcceptableValueRange<float>(0f, 100f)));

    Mechanism_Switch_DefaultAction = config.BindUnique(SectionKey,
      "Mechanism_Switch_DefaultAction", MechanismAction.CommandsHud,
      ConfigHelpers.CreateConfigDescription("Default action of the mechanism switch. This will be overridden by UpdateIntendedAction if a closer matching action is detected nearby."));

    PowerMechanismRange.SettingChanged += (sender, args) => UpdatePowerRanges();
    PowerPylonRange.SettingChanged += (sender, args) => UpdatePowerRanges();

    //sources
    PowerSource_FuelCapacity.SettingChanged += (sender, args) => UpdatePowerSources();
    PowerSource_BaseFuelEfficiency.SettingChanged += (sender, args) => UpdatePowerSources();
    PowerSource_EitrEfficiency.SettingChanged += (sender, args) => UpdatePowerSources();
    PowerSource_FuelConsumptionRate.SettingChanged += (sender, args) => UpdatePowerSources();
    // storages
    PowerStorage_Capacity.SettingChanged += (sender, args) => UpdatePowerSources();

    // consumers (swivels, vehicles)
    SwivelPowerDrain.SettingChanged += (sender, args) => UpdatePowerConsumers();
    LandVehicle_PowerDrain.SettingChanged += (sender, args) => UpdatePowerConsumers();

    SwivelComponent.IsPoweredSwivel = !Swivels_DoNotRequirePower.Value;
    Swivels_DoNotRequirePower.SettingChanged += (sender, args) => SwivelComponent.IsPoweredSwivel = !Swivels_DoNotRequirePower.Value;


    PowerNetwork_ShowAdditionalPowerInformationByDefault.SettingChanged += (sender, args) =>
    {
      PowerNetworkController.CanShowNetworkData = PowerNetwork_ShowAdditionalPowerInformationByDefault.Value;
    };

    PowerNetworkController.CanShowNetworkData = PowerNetwork_ShowAdditionalPowerInformationByDefault.Value;


    // conduits
    PowerPlate_EnergyGainPerSecond.SettingChanged += (sender, args) => UpdatePowerConduits();
    PowerPlate_EitrDrainCostPerSecond.SettingChanged += (sender, args) => UpdatePowerConduits();
    PowerPlate_TransferRate.SettingChanged += (sender, args) => UpdatePowerConduits();

    SwivelComponent.SwivelEnergyDrain = SwivelPowerDrain.Value;

    // trigger synchronous updates
    UpdatePowerRanges();
    UpdatePowerSources();
    UpdatePowerStorages();
    UpdatePowerConduits();
    UpdatePowerConsumers();
  }
}