using BepInEx.Configuration;

namespace ValheimVehicles.Config;

public static class PrefabConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<VehicleShipInitPiece>? StartingPiece
  {
    get;
    private set;
  }

  public static ConfigEntry<int> RopeLadderRunMultiplier { get; private set; } =
    null!;

  public static ConfigEntry<bool> RopeLadderHints { get; private set; } = null!;

  public static ConfigEntry<int> MaxVehicleSize { get; private set; } =
    null!;

  public enum VehicleShipInitPiece
  {
    Hull4X8,
    HullFloor2X2,
    HullFloor4X4,
    WoodFloor2X2,
    Nautilus
  }

  private const string SectionKey = "PrefabConfig";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    MaxVehicleSize = config.Bind(SectionKey, "MaxVehicleSize",
      150,
      ConfigHelpers.CreateConfigDescription(
        "Adds guards to prevent building vehicles larger than the current size. Will prevent massive player ships from breaking the game and will prevent ships from escaping their render bounds for ZDO. The upper limit is there for a reason",
        true, true, new AcceptableValueRange<int>(10, 500)));

    StartingPiece = config.Bind(SectionKey, "startingPiece",
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
  }
}