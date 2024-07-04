using System;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs;

/**
 * @todo register translatable pieceName and pieceDescription based on these names for easy lookups
 *
 * @warning Do not rename prefabs, prefabs that are renamed will orphan older gameobjects and will be deleted by valheim meaning it will break/delete pieces on pre-existing ships.
 */
public static class PrefabNames
{
  public enum PrefabSizeVariant
  {
    TwoByTwo,
    TwoByThree,
    FourByFour,
  }

  public enum DirectionVariant
  {
    Left,
    Right,
  }

  /// <summary>
  /// For usage with icons and other prefab registrations
  /// </summary>
  /// <param name="variant"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentOutOfRangeException"></exception>
  public static string GetPrefabSizeName(PrefabSizeVariant variant)
  {
    return variant switch
    {
      PrefabSizeVariant.TwoByTwo => "2x2",
      PrefabSizeVariant.TwoByThree => "2x3",
      PrefabSizeVariant.FourByFour => "4x4",
      _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };
  }

  public static string GetDirectionName(DirectionVariant directionVariant)
  {
    return directionVariant switch
    {
      DirectionVariant.Left => "left",
      DirectionVariant.Right => "right",
      _ => throw new ArgumentOutOfRangeException(nameof(directionVariant), directionVariant, null)
    };
  }

  /// <summary> 
  /// Calculated from the variant name
  /// </summary>
  /// <param name="variant"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentOutOfRangeException"></exception>
  public static int GetPrefabSizeArea(PrefabSizeVariant variant)
  {
    return variant switch
    {
      PrefabSizeVariant.TwoByTwo => 2 * 2,
      PrefabSizeVariant.TwoByThree => 2 * 3,
      PrefabSizeVariant.FourByFour => 4 * 4,
      _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };
  }

  private const string ValheimVehiclesPrefix = "ValheimVehicles";


  public const string PlayerSpawnControllerObj =
    $"{ValheimVehiclesPrefix}_PlayerSpawnControllerObj";


  public const string MBRaft = "MBRaft";
  public const string Nautilus = $"{ValheimVehiclesPrefix}_VehiclePresets_Nautilus";
  public static int m_raftHash = MBRaft.GetStableHashCode();
  public const string Tier1RaftMastName = "MBRaftMast";
  public const string Tier2RaftMastName = "MBKarveMast";
  public const string Tier3RaftMastName = "MBVikingShipMast";
  public const string Tier4RaftMastName = $"{ValheimVehiclesPrefix}_DrakkalMast";
  public const string Tier1CustomSailName = "MBSail";
  public const string BoardingRamp = "MBBoardingRamp";
  public const string BoardingRampWide = "MBBoardingRamp_Wide";
  public const string WaterVehicleFloatCollider = "VehicleShip_FloatCollider";
  public const string WaterVehicleBlockingCollider = "VehicleShip_BlockingCollider";
  public const string WaterVehicleOnboardCollider = "VehicleShip_OnboardTriggerCollider";

  public const string ValheimRaftMenuName = "Raft";

  // Containers that are nested within a VehiclePrefab top level
  // utilize the Get<Name> methods within the LoadValheimVehiclesAssets class to get these GameObjects
  public const string PiecesContainer =
    "piecesContainer";

  public const string MovingPiecesContainer =
    "movingPiecesContainer";

  public const string GhostContainer =
    "ghostContainer";

  public const string VehicleContainer =
    "vehicleContainer";

  public const string VehiclePiecesContainer = $"{ValheimVehiclesPrefix}_{PiecesContainer}";

  public const string VehicleMovingPiecesContainer =
    $"{ValheimVehiclesPrefix}_{MovingPiecesContainer}";

  public const string WaterVehicleShip = $"{ValheimVehiclesPrefix}_WaterVehicleShip";

  public const string HullProw = $"{ValheimVehiclesPrefix}_Ship_Hull_Prow";
  public const string HullRibCorner = $"{ValheimVehiclesPrefix}_Ship_Hull_Rib_Corner";
  public const string HullRib = $"{ValheimVehiclesPrefix}_Ship_Hull_Rib";

  // to only be used for matching with generic prefab names
  public const string HullSlab = $"{ValheimVehiclesPrefix}_Hull_Slab";

  public const string HullWall =
    $"{ValheimVehiclesPrefix}_Hull_Wall";

  private static string GetMaterialVariantName(string materialVariant)
  {
    return materialVariant == ShipHulls.HullMaterial.Iron ? "Iron" : "Wood";
  }

  private static string GetPrefabSizeVariantName(PrefabSizeVariant prefabSizeVariant)
  {
    return prefabSizeVariant == PrefabSizeVariant.FourByFour ? "4x4" : "2x2";
  }

  public static string GetHullProwVariants(string materialVariant,
    PrefabSizeVariant prefabSizeVariant)
  {
    var sizeVariant = GetPrefabSizeVariantName(prefabSizeVariant);
    var materialVariantName = GetMaterialVariantName(materialVariant);

    return $"{HullProw}_{materialVariantName}_{sizeVariant}";
  }

  public static string GetHullRibName(string materialVariant)
  {
    var materialVariantName = GetMaterialVariantName(materialVariant);

    return $"{HullRib}_{materialVariantName}";
  }

  public static string GetHullRibCornerName(string materialVariant,
    DirectionVariant directionVariant)
  {
    var directionName = GetDirectionName(directionVariant);
    var materialName = GetMaterialVariantName(materialVariant);

    return $"{HullRibCorner}_{materialName}_{directionName}";
  }

  public static string GetHullSlabName(string materialVariant,
    PrefabSizeVariant prefabSizeVariant)
  {
    var sizeVariant = GetPrefabSizeVariantName(prefabSizeVariant);
    var materialVariantName = GetMaterialVariantName(materialVariant);

    return $"{HullSlab}_{materialVariantName}_{sizeVariant}";
  }

  public static string GetHullWallName(string materialVariant,
    PrefabSizeVariant prefabSizeVariant)
  {
    var sizeVariant = GetPrefabSizeVariantName(prefabSizeVariant);
    var materialVariantName = GetMaterialVariantName(materialVariant);

    return $"{HullWall}_{materialVariantName}_{sizeVariant}";
  }

  public const string ShipHullPrefabName = "Ship_Hull";

  // hull
  public const string ShipHullCenterWoodPrefabName =
    $"{ValheimVehiclesPrefix}_{ShipHullPrefabName}_Wood";

  public const string ShipHullCenterIronPrefabName =
    $"{ValheimVehiclesPrefix}_{ShipHullPrefabName}_Iron";

  public static string GetShipHullCenterName(string hullMaterial)
  {
    if (hullMaterial == ShipHulls.HullMaterial.Iron)
    {
      return ShipHullCenterIronPrefabName;
    }

    if (hullMaterial == ShipHulls.HullMaterial.Wood)
    {
      return ShipHullCenterWoodPrefabName;
    }

    Logger.LogError("No hull of this name, this is an error registering a hull");
    return "";
  }

  public const string ShipRudderBasic = $"{ValheimVehiclesPrefix}_ShipRudderBasic";

  public const string ShipRudderAdvancedWood = $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Wood";
  public const string ShipRudderAdvancedIron = $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Iron";

  public const string ShipRudderAdvancedDoubleWood =
    $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Tail_Wood";

  public const string ShipRudderAdvancedDoubleIron =
    $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Tail_Iron";

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
  public const string VehicleHudAnchorIndicator = $"{ValheimVehiclesPrefix}_HudAnchorIndicator";

  public const string RamBladePrefix = $"{ValheimVehiclesPrefix}_ram_blade";
  public const string RamStakePrefix = $"{ValheimVehiclesPrefix}_ram_stake";

  public static string GetRamBladeName(string val)
  {
    return $"{RamBladePrefix}_{val.ToLower()}_{PrefabTiers.Tier3}";
  }

  public static string GetRamStakeName(string tier, int size)
  {
    var sizeString = size == 1 ? "1x2" : "2x4";
    return $"{RamStakePrefix}_{tier}_{sizeString}";
  }

  public static bool IsVehicleCollider(string objName)
  {
    return objName.StartsWith(WaterVehicleBlockingCollider) ||
           objName.StartsWith(WaterVehicleFloatCollider) ||
           objName.StartsWith(WaterVehicleOnboardCollider);
  }

  public static bool IsHull(GameObject go)
  {
    var goName = go.name;
    return goName.StartsWith(ShipHullCenterWoodPrefabName) ||
           goName.StartsWith(ShipHullCenterIronPrefabName) ||
           goName.StartsWith(HullRib) ||
           goName.StartsWith(HullRibCorner)
           || goName.StartsWith(HullWall) || goName.StartsWith(HullSlab) ||
           goName.StartsWith(HullProw);
  }
}