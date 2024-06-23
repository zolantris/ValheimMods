using BepInEx.Configuration;
using ComfyLib;
using ValheimVehicles.Helpers;

namespace ValheimVehicles.Config;

public static class PrefabConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<VehicleShipInitPiece>? StartingPiece { get; private set; }

  public enum VehicleShipInitPiece
  {
    Hull4X8,
    HullFloor2X2,
    HullFloor4X4,
    WoodFloor2X2,
    Nautilus
  }

  public static void BindConfig(ConfigFile config)
  {
    Config = config;
    StartingPiece = config.Bind("PrefabConfig", "startingPiece", VehicleShipInitPiece.Hull4X8,
      ConfigHelpers.CreateConfigDescription(
        "Allows you to customize what piece the raft initializes with. Admins only as this can be overpowered.",
        true, true));
  }
}