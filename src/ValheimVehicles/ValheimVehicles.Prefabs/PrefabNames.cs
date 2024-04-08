namespace ValheimVehicles.Prefabs;

/**
 * @todo register translatable pieceName and pieceDescription based on these names for easy lookups
 */
public static class PrefabNames
{
  public static string m_raft = "MBRaft";
  public static int m_raftHash = "MBRaft".GetStableHashCode();
  public const string Tier1RaftMastName = "MBRaftMast";
  public const string Tier2RaftMastName = "MBKarveMast";
  public const string Tier3RaftMastName = "MBVikingShipMast";
  public const string Tier1CustomSailName = "MBSail";
  public const string BoardingRamp = "MBBoardingRamp";
  public const string BoardingRampWide = "MBBoardingRamp_Wide";
  public const string ValheimVehiclesShipName = "ValheimVehicles_Ship";
  public const string WaterVehicleFloatCollider = "VVFloatCollider";
  public const string VehicleBlockingCollider = "VVBlockingCollider";
  public const string VehicleOnboardCollider = "VVOnboardCollider";
  public const string ValheimRaftMenuName = "Raft";
  public const string ShipHullCoreWoodHorizontal = "$mb_ship_hull_corewood_0";
  private const string ValheimVehiclesPrefix = "ValheimVehicles";
  public const string WaterVehiclePrefabName = $"{ValheimVehiclesPrefix}_WaterVehicle";
  public const string ShipHullPrefabName = $"{ValheimVehiclesPrefix}_ShipHull_Wood";
  public const string SailBoxColliderName = $"{ValheimVehiclesPrefix}_SailBoxCollider";
  public const string ShipRudderBasic = $"{ValheimVehiclesPrefix}_ShipRudderBasic";
  public const string ShipRudderAdvanced = $"{ValheimVehiclesPrefix}_ShipRudderAdvanced";
  public const string ShipSteeringWheel = $"{ValheimVehiclesPrefix}_ShipSteeringWheel";
  public const string WaterVehiclePreviewHull = $"{ValheimVehiclesPrefix}_WaterVehiclePreviewHull";
  public const string PiecesContainer = $"{ValheimVehiclesPrefix}_PiecesContainer";
  public const string VehicleSail = $"{ValheimVehiclesPrefix}_VehicleSail";
  public const string VehicleSailMast = $"{ValheimVehiclesPrefix}_VehicleSailMast";
  public const string VehicleSailCloth = $"{ValheimVehiclesPrefix}_SailCloth";
}