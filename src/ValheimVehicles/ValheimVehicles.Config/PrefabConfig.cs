using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Prefabs;

namespace ValheimVehicles.Config;

public static class PrefabConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<VehicleShipInitPiece>? StartingPiece
  {
    get;
    private set;
  }

  public static ConfigEntry<Color> GlassDefaultColor { get; private set; } =
    null!;

  public static ConfigEntry<int> RopeLadderRunMultiplier { get; private set; } =
    null!;

  public static ConfigEntry<bool> RopeLadderHints { get; private set; } = null!;

  public static ConfigEntry<Vector3>
    RopeLadderEjectionOffset { get; private set; } = null!;

  public static ConfigEntry<bool> EnableLandVehicles { get; private set; } =
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

  public static void UpdatePrefabEnabled(string prefabName, bool enabled)
  {
    if (PieceManager.Instance == null) return;
    var prefab = PieceManager.Instance.GetPiece(prefabName);
    if (prefab == null) return;
    prefab.Piece.m_enabled = enabled;
  }

  public static void BindConfig(ConfigFile config)
  {
    Config = config;
    RopeLadderEjectionOffset = config.Bind(SectionKey,
      "RopeLadderEjectionPoint", Vector3.zero,
      "The place the player is placed after they leave the ladder. Defaults to Y +0.25 and Z +0.5 meaning you are placed forwards of the ladder.");

    StartingPiece = config.Bind(SectionKey, SectionKey,
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

    GlassDefaultColor = Config.Bind(SectionKey,
      "GlassDefaultColor",
      new Color(0.60f, 0.60f, 0.60f, 0.05f),
      ConfigHelpers.CreateConfigDescription(
        "Set the experimental glass color for your vehicle. This will be used for most glass meshes. This is the default color. Eventually players can customize the color of the glass.",
        true, true));

    EnableLandVehicles = Config.Bind(SectionKey, "enableLandVehicles", false,
      ConfigHelpers.CreateConfigDescription(
        "Vehicles land vehicle prefab will be enabled. LandVehicles will be available for all version above V3.0.0",
        true));

    EnableLandVehicles.SettingChanged += (_, __) => UpdatePrefabEnabled(PrefabNames.LandVehicle, EnableLandVehicles.Value);
  }
}