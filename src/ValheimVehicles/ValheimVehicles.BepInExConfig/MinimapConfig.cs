using BepInEx.Configuration;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using Zolantris.Shared;

namespace ValheimVehicles.BepInExConfig;

public class MinimapConfig : BepInExBaseConfig<MinimapConfig>
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


  public override void OnBindConfig(ConfigFile config)
  {
    BedPinSyncInterval = config.BindUnique(SectionKey, "BedPinSyncInterval", 3f,
      ConfigHelpers.CreateConfigDescription(
        "The interval in seconds at which DynamicSpawn Player pins are synced to the client.",
        false, false, new AcceptableValueRange<float>(1f, 200f)));
    VehiclePinSyncInterval = config.BindUnique(SectionKey, "VehiclePinSyncInterval",
      3f,
      ConfigHelpers.CreateConfigDescription(
        "The interval in seconds at which vehicle pins are synced to the client.",
        false, false, new AcceptableValueRange<float>(1f, 200f)));

    VehiclePinSyncInterval.SettingChanged += (sender, args) =>
    {
      MapPinSync.Instance.StartVehiclePinSync();
    };

    BedPinSyncInterval.SettingChanged += (sender, args) =>
    {
      MapPinSync.Instance.StartSpawnPinSync();
    };

    VehicleNameTag = config.BindUnique(SectionKey, "VehicleNameTag", "Vehicle",
      ConfigHelpers.CreateConfigDescription("Set the name of the vehicle icon."));

    ShowAllVehiclesOnMap = config.BindUnique(SectionKey, "ShowAllVehiclesOnMap",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Shows all vehicles on global map. All vehicles will update their position.",
        true, true));

    VisibleVehicleRadius = config.BindUnique(SectionKey, "VisibleVehicleRadius", 50f,
      ConfigHelpers.CreateConfigDescription(
        "A radius in which all vehicles are revealed. This is more balanced than ShowAllVehicles.",
        true, true, new AcceptableValueRange<float>(5, 1000f)));

    ShowBedsOnVehicles = config.BindUnique(SectionKey, "ShowBedsOnVehicles", true,
      ConfigHelpers.CreateConfigDescription(
        "Will show your bed on you vehicle. This requires DynamicLocations to be enabled. This config may be migrated to dynamic locations.",
        true, true));
  }
}