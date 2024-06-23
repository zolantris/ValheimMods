using System;
using BepInEx.Configuration;
using ValheimRAFT;

namespace ValheimVehicles.Config;

public static class VehicleDebugConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<bool> AutoShowVehicleColliders { get; private set; } = null!;
  public static ConfigEntry<bool> VehicleDebugMenuEnabled { get; private set; } = null!;
  public static ConfigEntry<bool> SyncShipPhysicsOnAllClients { get; private set; } = null!;


  private const string SectionName = "Vehicle Debugging";

  private static void OnShowVehicleDebugMenuChange(object sender, EventArgs eventArgs)
  {
    ValheimRaftPlugin.Instance.AddRemoveVehicleDebugGui(VehicleDebugMenuEnabled.Value);
  }

  public static void BindConfig(ConfigFile config)
  {
    Config = config;
    AutoShowVehicleColliders = config.Bind(SectionName, "Always Show Vehicle Colliders",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Automatically shows the vehicle colliders useful for debugging the vehicle",
        true, true));
    VehicleDebugMenuEnabled = config.Bind(SectionName, "Vehicle Debug Menu",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Enable the VehicleDebugMenu. This shows a GUI menu which has a few shortcuts to debugging/controlling vehicles.",
        true, true));

    SyncShipPhysicsOnAllClients =
      Config.Bind("Debug", "SyncShipPhysicsOnAllClients", false,
        ConfigHelpers.CreateConfigDescription(
          "Makes all clients sync physics",
          true, true));

    // onChanged
    AutoShowVehicleColliders.SettingChanged += OnShowVehicleDebugMenuChange;
  }
}