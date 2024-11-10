using BepInEx.Configuration;

namespace ValheimVehicles.Config;

public class MinimapConfig
{
  public static ConfigEntry<string> VehicleNameTag =
    null!;

  public static ConfigEntry<bool> ShowAllVehiclesOnMap =
    null!;

  public static ConfigEntry<float> VisibleVehicleRadius =
    null!;

  public static ConfigEntry<bool> ShowBedsOnVehicles = null!;
  public static ConfigEntry<float> BedPinSyncInterval = null!;
  public static ConfigEntry<float> VehiclePinSyncInterval = null!;

  private const string SectionKey = "MinimapConfig";

  private static ConfigFile Config { get; set; } = null!;

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    BedPinSyncInterval = Config.Bind(SectionKey, "BedPinSyncInterval", 3f,
      ConfigHelpers.CreateConfigDescription(
        "The interval in seconds at which DynamicSpawn Player pins are synced to the client.",
        false, false, new AcceptableValueRange<float>(1f, 200f)));
    VehiclePinSyncInterval = Config.Bind(SectionKey, "VehiclePinSyncInterval",
      3f,
      ConfigHelpers.CreateConfigDescription(
        "The interval in seconds at which vehicle pins are synced to the client.",
        false, false, new AcceptableValueRange<float>(1f, 200f)));

    VehicleNameTag = Config.Bind(SectionKey, "VehicleNameTag", "Vehicle",
      "Set the name of the vehicle icon.");

    ShowAllVehiclesOnMap = Config.Bind(SectionKey, "ShowAllVehiclesOnMap", true,
      ConfigHelpers.CreateConfigDescription(
        "Shows all vehicles on global map. All vehicles will update their position.",
        true, true));

    VisibleVehicleRadius = Config.Bind(SectionKey, "VisibleVehicleRadius", 50f,
      ConfigHelpers.CreateConfigDescription(
        "A radius in which all vehicles are revealed. This is more balanced than ShowAllVehicles.",
        true, true, new AcceptableValueRange<float>(5, 1000f)));

    ShowBedsOnVehicles = Config.Bind(SectionKey, "ShowBedsOnVehicles", true,
      ConfigHelpers.CreateConfigDescription(
        "Will show your bed on you vehicle. This requires DynamicLocations to be enabled. This config may be migrated to dynamic locations.",
        true, true));
  }
}