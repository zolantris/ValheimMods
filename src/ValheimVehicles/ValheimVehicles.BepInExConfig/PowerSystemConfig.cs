using BepInEx.Configuration;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
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
  public static ConfigEntry<float> PowerPlate_DrainRate = null!;
  public static ConfigEntry<float> PowerPlate_EitrToEnergyRatio = null!;
  public static ConfigEntry<float> PowerPlate_ChargeRate = null!;

  // sources
  public static ConfigEntry<bool> PowerSource_AllowNearbyFuelingWithEitr = null!;
  public static ConfigEntry<float> PowerSource_FuelCapacity = null!;
  public static ConfigEntry<float> PowerSource_EitrEfficiency = null!;
  public static ConfigEntry<float> PowerSource_BaseFuelEfficiency = null!;
  public static ConfigEntry<float> PowerSource_FuelConsumptionRate = null!;

  // storage
  public static ConfigEntry<float> PowerStorage_Capacity = null!;

  // mechanism
  public static ConfigEntry<MechanismAction> Mechanism_Switch_DefaultAction = null!;

  // swivels
  public static ConfigEntry<bool> Swivels_DoNotRequirePower { get; set; }
  public static ConfigEntry<float> SwivelPowerDrain = null!;


  public static void UpdatePowerConduits()
  {
    PowerConduitPlateComponent.drainRate = PowerPlate_DrainRate.Value;
    PowerConduitPlateComponent.eitrToFuelRatio = PowerPlate_EitrToEnergyRatio.Value;
    PowerConduitPlateComponent.chargeRate = PowerPlate_ChargeRate.Value;
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

  public static void UpdatePowerStorages()
  {
    foreach (var powerStorage in PowerNetworkController.Storages)
    {
      powerStorage.SetCapacity(PowerStorage_Capacity.Value);
    }
  }

  public static void UpdateSwivelPower()
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
    PowerPylonRange = config.BindUnique(SectionKey, "PowerRangePerPowerItem", 10f, ConfigHelpers.CreateConfigDescription("The power range per power pylon prefab. Large values will make huge networks. Max range is 50. But this could span entire continents as ZDOs are not limited to render distance.", true, false, new AcceptableValueRange<float>(0, 50f)));

    PowerMechanismRange = config.BindUnique(SectionKey, "PowerMechanismRange", 4f, ConfigHelpers.CreateConfigDescription("The power range per mechanism power item. This excludes pylons and is capped at a lower number. These items are meant to be connected to pylons but at higher values could connect together.", true, false, new AcceptableValueRange<float>(0, 20f)));

    PowerPlate_ShowStatus = config.BindUnique(SectionKey, "PowerDrainPlate_ShowStatus", false, ConfigHelpers.CreateConfigDescription("Shows the power drain activity and tells you what type of plate is being used when hovering over it. This flag will be ignored if the PowerNetwork inspector is enabled which allows viewing all power values.", false, false));
    PowerSource_AllowNearbyFuelingWithEitr = config.BindUnique(SectionKey, "PowerSource_AllowNearbyFuelingWithEitr", false, ConfigHelpers.CreateConfigDescription("This will allow for the player to fuel from chests when interacting with Vehicle sources. This may not be needed with chest mods.", true, false));

    PowerNetwork_ShowAdditionalPowerInformationByDefault = config.BindUnique(SectionKey, "PowerNetwork_ShowAdditionalPowerInformationByDefault", false, ConfigHelpers.CreateConfigDescription("This will show the power network information by default per prefab. This acts as a tutorial. Most power items will have a visual indicator but it may not be clear to players immediately.", false, false));

    PowerPlate_DrainRate = config.BindUnique(SectionKey,
      "PowerPlate_DrainRate", 100f,
      ConfigHelpers.CreateConfigDescription(
        "How much eitr energy is drained to convert to power system energy units. Eitr energy is renewable but should be considered less refined. To maintain balance keep this at a higher number.",
        true, false,
        new AcceptableValueRange<float>(0.001f, 10000f)));
    PowerPlate_EitrToEnergyRatio = config.BindUnique(SectionKey,
      "PowerPlate_EitrToEnergyRatio", 10f,
      ConfigHelpers.CreateConfigDescription(
        "The amount of player eitr that is required to get 1 unit of eitr energy in the system.",
        true, false,
        new AcceptableValueRange<float>(0.001f, 100f)));

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

    SwivelPowerDrain = config.BindUnique(SectionKey,
      "SwivelPowerDrain", 1f,
      ConfigHelpers.CreateConfigDescription(
        "How much power (watts) is consumed by a Swivel per second. Applies only if Swivels_DoNotRequirePower is false.",
        true, false,
        new AcceptableValueRange<float>(0f, 100f)));

    PowerPlate_ChargeRate = config.BindUnique(SectionKey,
      "PowerPlate_ChargeRate", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Converted rate when transfering from the power conduit. This rate should be 1 by default. Use drain rate to make player mana power balanced. Charge rate will eventually be applyed to eitr chargers which can transfer eitr to the player.",
        true, false,
        new AcceptableValueRange<float>(0.001f, 100f)));

    Mechanism_Switch_DefaultAction = config.BindUnique(SectionKey,
      "Mechanism_Switch_DefaultAction", MechanismAction.CommandsHud,
      ConfigHelpers.CreateConfigDescription("Default action of the mechanism switch. This will be overridden by UpdateIntendedAction if a closer matching action is detected nearby."));


    Swivels_DoNotRequirePower = config.BindUnique(SectionKey, "Swivels_DoNotRequirePower",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Allows you to use swivels without the vehicle power system.",
        true, false));

//sources
    PowerSource_FuelCapacity.SettingChanged += (sender, args) => UpdatePowerSources();
    PowerSource_BaseFuelEfficiency.SettingChanged += (sender, args) => UpdatePowerSources();
    PowerSource_EitrEfficiency.SettingChanged += (sender, args) => UpdatePowerSources();
    PowerSource_FuelConsumptionRate.SettingChanged += (sender, args) => UpdatePowerSources();
// storages
    SwivelPowerDrain.SettingChanged += (sender, args) =>
      PowerStorage_Capacity.SettingChanged += (sender, args) => UpdatePowerStorages();
    PowerStorage_Capacity.SettingChanged += (sender, args) => UpdatePowerSources();

    // trigger synchronous updates
    UpdateSwivelPower();
    UpdatePowerSources();
    UpdatePowerStorages();
    UpdatePowerConduits();

    SwivelComponent.IsPoweredSwivel = !Swivels_DoNotRequirePower.Value;
    Swivels_DoNotRequirePower.SettingChanged += (sender, args) => SwivelComponent.IsPoweredSwivel = !Swivels_DoNotRequirePower.Value;


    PowerNetwork_ShowAdditionalPowerInformationByDefault.SettingChanged += (sender, args) =>
    {
      PowerNetworkController.CanShowNetworkData = PowerNetwork_ShowAdditionalPowerInformationByDefault.Value;
    };

    PowerNetworkController.CanShowNetworkData = PowerNetwork_ShowAdditionalPowerInformationByDefault.Value;


// conduits
    PowerPlate_EitrToEnergyRatio.SettingChanged += (sender, args) => UpdatePowerConduits();
    PowerPlate_DrainRate.SettingChanged += (sender, args) => UpdatePowerConduits();
    PowerPlate_ChargeRate.SettingChanged += (sender, args) => UpdatePowerConduits();

    SwivelComponent.SwivelEnergyDrain = SwivelPowerDrain.Value;

  }
}