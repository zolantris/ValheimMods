#region

  using System;
  using ValheimVehicles.SharedScripts.Enums;
  using UnityEngine;
  using ValheimVehicles.Shared.Constants;
#if !TEST
  using Zolantris.Shared;
#endif

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts
  {

    public static class PrefabNameHashes
    {
      public static readonly int Mechanism_Power_Pylon = PrefabNames.Mechanism_Power_Pylon.GetStableHashCode();

      public static readonly int Mechanism_Power_Source_Coal =
        PrefabNames.Mechanism_Power_Source_Coal.GetStableHashCode();

      public static readonly int Mechanism_Power_Source_Eitr =
        PrefabNames.Mechanism_Power_Source_Eitr.GetStableHashCode();

      public static readonly int Mechanism_Power_Conduit_Charge_Plate =
        PrefabNames.Mechanism_Power_Conduit_Charge_Plate.GetStableHashCode();
      public static readonly int Mechanism_Power_Conduit_Drain_Plate =
        PrefabNames.Mechanism_Power_Conduit_Drain_Plate.GetStableHashCode();

      public static readonly int Mechanism_Power_Storage_Eitr =
        PrefabNames.Mechanism_Power_Storage_Eitr.GetStableHashCode();

      public static readonly int Mechanism_Power_Consumer_Swivel = PrefabNames.SwivelPrefabName.GetStableHashCode();
      public static readonly int Mechanism_Power_Consumer_LandVehicle = PrefabNames.LandVehicle.GetStableHashCode();
      public static readonly int Mechanism_Power_Consumer_WaterVehicle = PrefabNames.WaterVehicleShip.GetStableHashCode();

      public static readonly int LandVehicle = PrefabNames.LandVehicle.GetStableHashCode();
      public static readonly int WaterVehicleShip = PrefabNames.WaterVehicleShip.GetStableHashCode();
    }

    public static class PrefabItemNameToken
    {
      public const string CannonHandHeldName = "$valheim_vehicles_item_cannon_handheld_item";
      public const string TelescopeName = "$valheim_vehicles_item_telescope_handheld";
      public const string CannonAmmoType = "$valheim_vehicles_item_ammo_cannon_ball";
      public const string CannonExplosiveAmmo = "$valheim_vehicles_cannonball_explosive";
      public const string CannonSolidAmmo = "$valheim_vehicles_cannonball_solid";
    }

    /**
     * @todo register translatable pieceName and pieceDescription based on these names for easy lookups
     *
     * @warning Do not rename prefabs, prefabs that are renamed will orphan older gameobjects and will be deleted by valheim meaning it will break/delete pieces on pre-existing ships.
     *
     * @note anything prefixed with MB should be considered a deprecated compatibility name. Changing names to have the new prefix should only be done in a major version bump. Also a name remapper should be provided (for orphaned zdo pieces).
     */
    public static class PrefabNames
    {

      public enum DirectionVariant
      {
        Left,
        Right
      }

      public enum PrefabSizeVariant
      {
        None, // for no size variant.
        // width/length
        TwoByTwo, // 2x2
        TwoByThree, // 2x3 
        TwoByFour, // 2x4 
        TwoByEight, // 2x8 
        FourByFour, // 4x4
        FourByEight, // 4x8
        // 3d
        TwoByTwoByTwo, // 2x2x2
        TwoByOneByTwo, // 2x1x2
        TwoByTwoByFour, // 2x2x4 
        TwoByOneByEight, //2x1x8
        TwoByTwoByEight //2x1x8
      }

      public const string MBRopeAnchor = "MBRopeAnchor";


      public static readonly string ValheimVehiclesPrefix = "ValheimVehicles";

      public static readonly string DockAttachpoint = $"{ValheimVehiclesPrefix}_DockAttachpoint";
      public static readonly string SailCreator = $"{ValheimVehiclesPrefix}_SailCreator";
      public static readonly string LandVehicle = $"{ValheimVehiclesPrefix}_VehicleLand";
      public static readonly string AirVehicle = $"{ValheimVehiclesPrefix}_VehicleAir";
      public static readonly string WheelSet = $"{ValheimVehiclesPrefix}_WheelSet";

      public static readonly string CustomWaterFloatation =
        $"{ValheimVehiclesPrefix}_CustomWaterFloatation";

      public static readonly string ShipChunkBoundary1x1x1 =
        $"{ValheimVehiclesPrefix}_ShipChunkBoundary1x1x1";

      public static readonly string ShipChunkBoundary4x4x4 =
        $"{ValheimVehiclesPrefix}_ShipChunkBoundary4x4x4";

      public static readonly string ShipChunkBoundary8x8x8 =
        $"{ValheimVehiclesPrefix}_ShipChunkBoundary8x8x8";

      public static readonly string ShipChunkBoundary16x16x16 =
        $"{ValheimVehiclesPrefix}_ShipChunkBoundary16x16x16";

      public static readonly string ShipChunkBoundaryEraser =
        $"{ValheimVehiclesPrefix}_ShipChunkBoundaryEraser";

      public static readonly string CustomWaterMask =
        $"{ValheimVehiclesPrefix}_CustomWaterMask";

      public static readonly string CustomWaterMaskCreator =
        $"{ValheimVehiclesPrefix}_CustomWaterMaskCreator";

      public static readonly string CustomVehicleMaxCollisionHeightCreator =
        $"{ValheimVehiclesPrefix}_CustomVehicleMaxCollisionHeightCreator";

      public static readonly string PlayerSpawnControllerObj =
        $"{ValheimVehiclesPrefix}_PlayerSpawnControllerObj";

      public static readonly string ShipAnchorWood =
        $"{ValheimVehiclesPrefix}_ShipAnchor_{HullMaterial.Wood}";

      public static readonly string MBRopeLadder = "MBRopeLadder";
      public static readonly string MBRaft = "MBRaft";

      public static readonly string Nautilus =
        $"{ValheimVehiclesPrefix}_VehiclePresets_Nautilus";
      public static readonly string Tier1RaftMastName = "MBRaftMast";
      public static readonly string Tier2RaftMastName = "MBKarveMast";
      public static readonly string Tier3RaftMastName = "MBVikingShipMast";

      public static readonly string Tier4RaftMastName =
        $"{ValheimVehiclesPrefix}_DrakkalMast";

      public static readonly string Tier1CustomSailName = "MBSail";
      public static readonly string BoardingRamp = "MBBoardingRamp";
      public static readonly string BoardingRampWide = "MBBoardingRamp_Wide";
      public static readonly string WaterVehicleFloatCollider = "VehicleShip_FloatCollider";

      public static readonly string WaterVehicleBlockingCollider =
        "VehicleShip_BlockingCollider";

      public static readonly string WaterVehicleOnboardCollider =
        "VehicleShip_OnboardTriggerCollider";

      public static readonly string DEPRECATED_ValheimRaftMenuName = "Raft";

      // Containers that are nested within a VehiclePrefab top level
      // utilize the Get<Name> methods within the LoadValheimVehiclesAssets class to get these GameObjects
      private static readonly string PiecesContainer =
        "piecesContainer";

      public static readonly string MovingPiecesContainer =
        "movingPiecesContainer";

      public static readonly string GhostContainer =
        "ghostContainer";

      public static readonly string VehicleContainer =
        "vehicleContainer";

      public static readonly string VehiclePiecesContainer =
        $"{ValheimVehiclesPrefix}_{PiecesContainer}";

      public static readonly string VehicleMovingPiecesContainer =
        $"{ValheimVehiclesPrefix}_{MovingPiecesContainer}";

      public static readonly string WaterVehicleShip =
        $"{ValheimVehiclesPrefix}_WaterVehicleShip";

      public static readonly string HullProw = $"{ValheimVehiclesPrefix}_Ship_Hull_Prow";
      public static readonly string HullProwRib = $"{ValheimVehiclesPrefix}_Ship_Hull_Prow_Rib";

      public static readonly string HullRibCorner =
        $"{ValheimVehiclesPrefix}_Ship_Hull_Rib_Corner";

      public static readonly string HullRibCornerFloor =
        $"{ValheimVehiclesPrefix}_Ship_Hull_Rib_Corner_Floor";

      public static readonly string HullRib_BaseName = $"{ValheimVehiclesPrefix}_Ship_Hull_Rib";
      // to only be used for matching with generic prefab names
      public static readonly string HullSlab = $"{ValheimVehiclesPrefix}_Hull_Slab";

      public static readonly string HullWall =
        $"{ValheimVehiclesPrefix}_Hull_Wall";

      public static readonly string HullCornerFloor =
        $"{ValheimVehiclesPrefix}_Hull_Corner_Floor";

      public static readonly string KeelColliderPrefix = "keel_collider";

      public static readonly string ShipHullPrefabName = "Ship_Hull";

      public static readonly string WindowWallPorthole2x2Prefab =
        $"{ValheimVehiclesPrefix}_ShipWindow_Wall_Porthole_2x2";

      public static readonly string WindowWallPorthole4x4Prefab =
        $"{ValheimVehiclesPrefix}_ShipWindow_Wall_Porthole_4x4";

      public static readonly string WindowWallPorthole6x4Prefab =
        $"{ValheimVehiclesPrefix}_ShipWindow_Wall_Porthole_6x4";

      public static readonly string WindowWallPorthole8x4Prefab =
        $"{ValheimVehiclesPrefix}_ShipWindow_Wall_Porthole_8x4";

      public static readonly string WindowFloorPorthole4x4Prefab =
        $"{ValheimVehiclesPrefix}_ShipWindow_Floor_Porthole_4x4";

      public static readonly string WindowWallSquareIronPrefabName =
        $"{ValheimVehiclesPrefix}_ShipWindow_Wall_Square_{HullMaterial.Iron}_2x2";

      public static readonly string WindowWallSquareWoodPrefabName =
        $"{ValheimVehiclesPrefix}_ShipWindow_Wall_Square_{HullMaterial.Wood}_2x2";

      public static readonly string WindowPortholeStandalonePrefab =
        $"{ValheimVehiclesPrefix}_ShipWindow_Porthole_standalone";

      // hull
      public static readonly string ShipHullCenterWoodPrefabName =
        $"{ValheimVehiclesPrefix}_{ShipHullPrefabName}_Wood";

      public static readonly string ShipHullCenterIronPrefabName =
        $"{ValheimVehiclesPrefix}_{ShipHullPrefabName}_Iron";

      public static readonly string ShipRudderBasic =
        $"{ValheimVehiclesPrefix}_ShipRudderBasic";

      public static readonly string ShipRudderAdvancedWood =
        $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Wood";

      public static readonly string ShipRudderAdvancedIron =
        $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Iron";

      public static readonly string ShipRudderAdvancedDoubleWood =
        $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Tail_Wood";

      public static readonly string ShipRudderAdvancedDoubleIron =
        $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Tail_Iron";

      public static readonly string ShipSteeringWheel =
        $"{ValheimVehiclesPrefix}_ShipSteeringWheel";

      public static readonly string ShipKeel = $"{ValheimVehiclesPrefix}_ShipKeel";

      public static readonly string WaterVehiclePreviewHull =
        $"{ValheimVehiclesPrefix}_WaterVehiclePreviewHull";

      public static readonly string VehicleSail = $"{ValheimVehiclesPrefix}_VehicleSail";

      public static readonly string VehicleShipTransform =
        $"{ValheimVehiclesPrefix}_VehicleShipTransform";

      public static readonly string VehicleShipEffects =
        $"{ValheimVehiclesPrefix}_VehicleShipEffects";

      public static readonly string VehicleSailMast =
        $"{ValheimVehiclesPrefix}_VehicleSailMast";

      public static readonly string VehicleSailCloth = $"{ValheimVehiclesPrefix}_SailCloth";

      public static readonly string Mechanism_ToggleSwitch =
        $"{ValheimVehiclesPrefix}_ToggleSwitch";

      public static readonly string Mechanism_Power_Pylon =
        $"{ValheimVehiclesPrefix}_Power_Pylon";

      public static readonly string Mechanism_Power_Source_Coal =
        $"{ValheimVehiclesPrefix}_Power_Source_Coal";

      public static readonly string Mechanism_Power_Source_Eitr =
        $"{ValheimVehiclesPrefix}_Power_Source_Eitr";

      public static readonly string Mechanism_Power_Conduit_Charge_Plate =
        $"{ValheimVehiclesPrefix}_Power_Conduit_Charge_Plate";
      public static readonly string Mechanism_Power_Conduit_Drain_Plate =
        $"{ValheimVehiclesPrefix}_Power_Conduit_Drain_Plate";

      public static readonly string Mechanism_Power_Storage_Eitr =
        $"{ValheimVehiclesPrefix}_Power_Storage_Eitr";

      public static readonly string PowderBarrel =
        $"{ValheimVehiclesPrefix}_Powder_Barrel";
      public static readonly string CannonFixedTier1 =
        $"{ValheimVehiclesPrefix}_Cannon_Fixed_Tier1";
      public static readonly string CannonTurretTier1 =
        $"{ValheimVehiclesPrefix}_Cannon_Turret_Tier1";

      public static readonly string CannonballSolid =
        $"{ValheimVehiclesPrefix}_Cannonball_Solid";
      public static readonly string CannonballExplosive =
        $"{ValheimVehiclesPrefix}_Cannonball_Explosive";

      public static readonly string CannonballSolidProjectile =
        $"{ValheimVehiclesPrefix}_Cannonball_Solid_Projectile";
      public static readonly string CannonballExplosiveProjectile =
        $"{ValheimVehiclesPrefix}Cannonball_Explosive_Projectile";

      public static readonly string CannonHandHeldItem =
        $"{ValheimVehiclesPrefix}_Cannon_Handheld_Item";

      public static readonly string CannonControlCenter =
        $"{ValheimVehiclesPrefix}_Cannon_Control_Center";

      public static readonly string TelescopeItem =
        $"{ValheimVehiclesPrefix}_Telescope_Item";

      public static readonly string VehicleShipMovementOrientation =
        "VehicleShip_MovementOrientation";

      public static readonly string VehicleHudAnchorIndicator =
        $"{ValheimVehiclesPrefix}_HudAnchorIndicator";

      // hammers must contain "hammer" in the string to match.
      public static readonly string VehicleHammer = $"{ValheimVehiclesPrefix}_vehicle_hammer";

      public static readonly string RamBladePrefix = $"{ValheimVehiclesPrefix}_ram_blade";
      public static readonly string RamStakePrefix = $"{ValheimVehiclesPrefix}_ram_stake";

      public static readonly string ConvexHull = $"{ValheimVehiclesPrefix}_ConvexHull";
      public static readonly string ConvexTreadCollider = $"{ValheimVehiclesPrefix}_convex_tread_collider";

      public static readonly string SwivelPrefabName = $"{ValheimVehiclesPrefix}_Swivel";
      public static readonly string SwivelAssetName = "swivel";

      public static readonly string HullRibProwSeal = $"{ValheimVehiclesPrefix}_Hull_Rib_Prow_Seal";

      /// <summary>
      /// For usage with icons and other prefab registrations
      /// </summary>
      /// <param name="variant"></param>
      public static string GetPrefabSizeVariantName(PrefabSizeVariant variant)
      {
        switch (variant)
        {
          case PrefabSizeVariant.None:
            return "";
          // width height length variants
          case PrefabSizeVariant.TwoByOneByTwo:
            return "2x1x2";
          case PrefabSizeVariant.TwoByOneByEight:
            return "2x1x8";
          case PrefabSizeVariant.TwoByTwoByTwo:
            return "2x2x2";
          case PrefabSizeVariant.TwoByTwoByFour:
            return "2x2x4";
          case PrefabSizeVariant.TwoByTwoByEight:
            return "2x2x8";
          // width by length variants
          case PrefabSizeVariant.TwoByTwo:
            return "2x2";
          case PrefabSizeVariant.TwoByThree:
            return "2x3";
          case PrefabSizeVariant.TwoByFour:
            return "2x4";
          case PrefabSizeVariant.TwoByEight:
            return "2x8";
          case PrefabSizeVariant.FourByFour:
            return "4x4";
          case PrefabSizeVariant.FourByEight:
            return "4x8";
          default:
            LoggerProvider.LogWarning($"Unhandled prefab size {variant}");
            return "";
        }
      }

      public static string GetDirectionName(DirectionVariant directionVariant)
      {
        return directionVariant.ToString().ToLower();
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
          PrefabSizeVariant.FourByEight => 4 * 8,
          PrefabSizeVariant.None => 1,
          PrefabSizeVariant.TwoByFour => 2 * 4,
          PrefabSizeVariant.TwoByEight => 2 * 8,
          PrefabSizeVariant.TwoByTwoByTwo => 2 * 2,
          PrefabSizeVariant.TwoByOneByTwo => 3,
          PrefabSizeVariant.TwoByTwoByFour => 2 * 4,
          PrefabSizeVariant.TwoByOneByEight => 2 * 8,
          PrefabSizeVariant.TwoByTwoByEight => 2 * 8,
          _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
      }

      private static string GetMaterialVariantName(string materialVariant)
      {
        return materialVariant == HullMaterial.Iron ? "Iron" : "Wood";
      }

      public static string GetHullProwVariants(string materialVariant,
        PrefabSizeVariant prefabSizeVariant)
      {
        var sizeVariant = GetPrefabSizeVariantName(prefabSizeVariant);
        var materialVariantName = GetMaterialVariantName(materialVariant);

        return $"{HullProw}_{materialVariantName}_{sizeVariant}";
      }

      /// <summary>
      /// Todo determine if we want to use full assetbundle string instead
      /// </summary>
      public static string GetHullPrefabV4Name(string baseName, string materialVariant, PrefabSizeVariant? prefabSizeVariant, DirectionVariant? directionVariant)
      {
        var prefabRegistryName = $"{baseName}";

        if (directionVariant.HasValue)
        {
          // var sizeVariant = GetPrefabSizeVariantName(prefabSizeVariant);
          var directionName = GetDirectionName(directionVariant.Value);
          prefabRegistryName += $"_{directionName}";
        }

        var materialVariantName = GetMaterialVariantName(materialVariant);
        prefabRegistryName += $"_{materialVariantName}";

        if (directionVariant.HasValue)
        {
          var directionName = GetDirectionName(directionVariant.Value);
          prefabRegistryName += $"_{directionName}";
        }

        return prefabRegistryName;
      }


      public static string GetHullProwRibVariants(string materialVariant,
        PrefabSizeVariant prefabSizeVariant, DirectionVariant? directionVariant, string prefabVariant)
      {
        var sizeVariant = GetPrefabSizeVariantName(prefabSizeVariant);
        var materialVariantName = GetMaterialVariantName(materialVariant);

        var prefabRegistryName = $"{HullProwRib}_{prefabVariant}_{sizeVariant}_{materialVariantName}";

        if (directionVariant != null)
        {
          var directionName = GetDirectionName(directionVariant.Value);
          prefabRegistryName += $"_{directionName}";
        }

        return prefabRegistryName;
      }

      public static string GetHullRibName(string materialVariant, PrefabSizeVariant sizeVariant)
      {

        var materialVariantName = GetMaterialVariantName(materialVariant);

        // cant override default otherwise it breaks players who update
        if (sizeVariant == PrefabSizeVariant.TwoByTwoByTwo)
        {
          return $"{HullRib_BaseName}_{materialVariantName}";
        }

        var prefabSizeString = GetPrefabSizeVariantName(sizeVariant);
        return $"{HullRib_BaseName}_{prefabSizeString}_{materialVariantName}";
      }

      // material names are always second to last after size names. Direction names are important so they are first
      public static string GetHullRibCornerName(string materialVariant, DirectionVariant? directionVariant, PrefabSizeVariant sizeVariant)
      {
        var materialName = GetMaterialVariantName(materialVariant);

        // would need to migrate this otherwise valheim will delete the prefab if we use the new variant names
        if (sizeVariant == PrefabSizeVariant.TwoByTwoByTwo || directionVariant == null)
        {
          return $"{HullRibCorner}_{materialName}";
        }

        var sizeVariantString = GetPrefabSizeVariantName(sizeVariant);
        return $"{HullRibCorner}_{sizeVariantString}_{directionVariant}_{materialName}";
      }

      public static string GetHullRibCornerFloorName(string materialVariant,
        DirectionVariant directionVariant, PrefabSizeVariant sizeVariant)
      {
        var directionName = GetDirectionName(directionVariant);
        var materialName = GetMaterialVariantName(materialVariant);

        if (sizeVariant == PrefabSizeVariant.None)
        {
          return $"{HullRibCornerFloor}_{directionName}_{materialName}";
        }

        var sizeVariantName = GetPrefabSizeVariantName(sizeVariant);

        if (sizeVariantName == "2x2x2")
        {
          return $"{HullRibCornerFloor}_{directionName}_{materialName}";
        }

        return $"{HullRibCornerFloor}_{sizeVariantName}_{directionName}_{materialName}";
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

      public static string GetShipHullCenterName(string hullMaterial)
      {
        if (hullMaterial == HullMaterial.Iron)
          return ShipHullCenterIronPrefabName;

        if (hullMaterial == HullMaterial.Wood)
          return ShipHullCenterWoodPrefabName;

        LoggerProvider.LogError(
          "No hull of this name, this is an error registering a hull");
        return "";
      }

      public static string GetRamBladeName(string val)
      {
        return $"{RamBladePrefix}_{val.ToLower()}_{PrefabTiers.Tier3}";
      }

      public static string GetRamStakeName(string tier, int size)
      {
        var sizeString = size == 1 ? "1x2" : "2x4";
        return $"{RamStakePrefix}_{tier}_{sizeString}";
      }

      public static void ValidateMastTypeName(string mastLevel)
      {
        // We validate the input at runtime to make sure it's a MastLevels constant.
        if (mastLevel != MastLevels.ONE && mastLevel != MastLevels.TWO && mastLevel != MastLevels.THREE)
        {
          throw new ArgumentException($"Invalid mast tier name: {mastLevel}", nameof(mastLevel));
        }
      }

      /// <summary>
      /// Does not add prefixes as this would mismatch the exported asset for both images and prefabs.
      /// </summary>
      /// <param name="mastLevel"></param>
      /// <returns></returns>
      public static string GetMastByLevelFromAssetBundle(string mastLevel)
      {
        ValidateMastTypeName(mastLevel);
        return $"mast_{mastLevel}";
      }

      public static string CustomMast = $"{ValheimVehiclesPrefix}_custom_mast";

      public static string GetMastByLevelName(string mastLevel)
      {
        ValidateMastTypeName(mastLevel);
        return $"{CustomMast}_{mastLevel}";
      }

      public static bool IsVehicleCollider(string objName)
      {
        return objName.StartsWith(WaterVehicleBlockingCollider) ||
               objName.StartsWith(WaterVehicleFloatCollider) ||
               objName.StartsWith(WaterVehicleOnboardCollider) || objName.StartsWith(ConvexHull);
      }

      public static bool IsVehiclePiecesContainer(string objName)
      {
        return objName.StartsWith(VehiclePiecesContainer);
      }

      public static bool IsVehicleTreadCollider(string objName)
      {
        return objName.StartsWith(ConvexTreadCollider);
      }

      public static bool IsHull(string goName)
      {
        return goName.StartsWith(ShipHullCenterWoodPrefabName) ||
               goName.StartsWith(ShipHullCenterIronPrefabName) ||
               goName.StartsWith(HullRib_BaseName) ||
               goName.StartsWith(HullRibCorner)
               || goName.StartsWith(HullWall) || goName.StartsWith(HullSlab) ||
               goName.StartsWith(HullProw) ||
               goName.StartsWith(HullRibCornerFloor) ||
               goName.StartsWith(WindowPortholeStandalonePrefab) ||
               goName.StartsWith(WindowWallPorthole2x2Prefab) ||
               goName.StartsWith(WindowWallPorthole4x4Prefab) ||
               goName.StartsWith(WindowWallSquareIronPrefabName) ||
               goName.StartsWith(WindowWallSquareWoodPrefabName);
      }

#if !TEST
      public static bool IsHull(GameObject go)
      {
        return IsHull(go.name);
      }
#endif

      public static bool IsVehicle(string goName)
      {
        return goName.StartsWith(LandVehicle) || goName.StartsWith(WaterVehicleShip);
      }

      public static class MastLevels
      {
        public static readonly string ONE = "level_1";
        public static readonly string TWO = "level_2";
        public static readonly string THREE = "level_3";
      }
    }
  }