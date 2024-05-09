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
  public const string WaterVehicleFloatCollider = "VehicleShip_FloatCollider";
  public const string WaterVehicleBlockingCollider = "VehicleShip_BlockingCollider";
  public const string WaterVehicleOnboardCollider = "VehicleShip_OnboardTriggerCollider";

  public const string ValheimRaftMenuName = "Raft";

  // Containers that are nested within a VehiclePrefab top level
  // utilize the Get<Name> methods within the LoadValheimVehiclesAssets class to get these GameObjects
  public const string PiecesContainer =
    "piecesContainer";

  public const string GhostContainer =
    "ghostContainer";

  public const string VehicleContainer =
    "vehicleContainer";

  public const string VehiclePiecesContainer = $"{ValheimVehiclesPrefix}_{PiecesContainer}";

  private const string ValheimVehiclesPrefix = "ValheimVehicles";
  public const string WaterVehicleShip = $"{ValheimVehiclesPrefix}_WaterVehicleShip";

  public const string ShipHullRibWoodPrefabName = $"{ValheimVehiclesPrefix}_Ship_Hull_Rib_Wood";
  public const string ShipHullRibIronPrefabName = $"{ValheimVehiclesPrefix}_Ship_Hull_Rib_Iron";

  public const string ShipHullCenterWoodPrefabName =
    $"{ValheimVehiclesPrefix}_Ship_Hull_Center_Wood";

  public const string ShipHullCenterIronPrefabName =
    $"{ValheimVehiclesPrefix}_Ship_Hull_Center_Iron";

  public const string SailBoxColliderName = $"{ValheimVehiclesPrefix}_SailBoxCollider";
  public const string ShipRudderBasic = $"{ValheimVehiclesPrefix}_ShipRudderBasic";
  public const string ShipRudderAdvanced = $"{ValheimVehiclesPrefix}_ShipRudderAdvanced";
  public const string ShipSteeringWheel = $"{ValheimVehiclesPrefix}_ShipSteeringWheel";
  public const string ShipKeel = $"{ValheimVehiclesPrefix}_ShipKeel";
  public const string WaterVehiclePreviewHull = $"{ValheimVehiclesPrefix}_WaterVehiclePreviewHull";

  public const string VehicleSail = $"{ValheimVehiclesPrefix}_VehicleSail";
  public const string VehicleShipTransform = $"{ValheimVehiclesPrefix}_VehicleShipTransform";
  public const string VehicleShipEffects = $"{ValheimVehiclesPrefix}_VehicleShipEffects";
  public const string VehicleSailMast = $"{ValheimVehiclesPrefix}_VehicleSailMast";
  public const string VehicleSailCloth = $"{ValheimVehiclesPrefix}_SailCloth";
  public const string VehicleToggleSwitch = $"{ValheimVehiclesPrefix}_VehicleToggleSwitch";
  public const string VehicleShipMovementOrientation = "VehicleShip_MovementOrientation";
}