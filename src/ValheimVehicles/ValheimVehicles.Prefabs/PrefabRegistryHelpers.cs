using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HarmonyLib;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Enums;
using ValheimVehicles.SharedScripts.Prefabs;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Prefabs;

public abstract class PrefabRegistryHelpers
{
  public const string SnappointTag = "snappoint";
  public static int PieceLayer;

  public struct PieceData
  {
    private string _name;
    private string _description;

    public string Name
    {
      get => _name;
      set => _name = NormalizeTranslationKeys(value);
    }

    public string Description
    {
      get => _description;
      set => _description = NormalizeTranslationKeys(value);
    }

    public Sprite Icon;
  }

  public static string NormalizeTranslationKeys(string localizableString)
  {
    return new Regex(@"((?<!\$)\b\w+_\w+\b)").Replace(localizableString,
      match => "$" + match.Value);
  }

  // may use for complex shared variant prefabs
  // idea is to send in the keys and then register the PieceData
  public static void RegisterPieceWithVariant(string prefabName,
    string translationKey,
    string hullMaterials, PrefabNames.PrefabSizeVariant sizeVariant)
  {
  }

  public static readonly Dictionary<string, PieceData> PieceDataDictionary =
    new();

  public static VehicleZSyncTransform GetOrAddMovementZSyncTransform(
    GameObject obj)
  {
    var zSyncTransform = obj.GetComponent<VehicleZSyncTransform>();
    if (zSyncTransform == null)
      zSyncTransform = obj.AddComponent<VehicleZSyncTransform>();

    zSyncTransform.m_syncPosition = true;
    zSyncTransform.m_syncBodyVelocity = true;
    zSyncTransform.m_syncRotation = true;
    return zSyncTransform;
  }

  public static ZNetView AddTempNetView(GameObject obj,
    bool prioritized = false)
  {
    var netView = obj.GetComponent<ZNetView>();
    if (netView == null)
      netView = obj.AddComponent<ZNetView>();
    if (prioritized) netView.m_type = ZDO.ObjectType.Prioritized;

    netView.m_persistent = false;
    return netView;
  }

  private static void RegisterCustomMastPieces()
  {
    PieceDataDictionary.Add(PrefabNames.GetMastByLevelName(PrefabNames.MastLevels.ONE),
      new PieceData
      {
        Name = "$valheim_vehicles_custom_mast $valheim_vehicles_level 1",
        Description = "$valheim_vehicles_custom_mast_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(PrefabNames.GetMastByLevelFromAssetBundle(PrefabNames.MastLevels.ONE))
      });

    PieceDataDictionary.Add(PrefabNames.GetMastByLevelName(PrefabNames.MastLevels.TWO),
      new PieceData
      {
        Name = "$valheim_vehicles_custom_mast $valheim_vehicles_level 2",
        Description = "$valheim_vehicles_custom_mast_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(PrefabNames.GetMastByLevelFromAssetBundle(PrefabNames.MastLevels.TWO))
      });

    PieceDataDictionary.Add(PrefabNames.GetMastByLevelName(PrefabNames.MastLevels.THREE),
      new PieceData
      {
        Name = "$valheim_vehicles_custom_mast $valheim_vehicles_level 3",
        Description = "$valheim_vehicles_custom_mast_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(PrefabNames.GetMastByLevelFromAssetBundle(PrefabNames.MastLevels.THREE))
      });
  }
  private static void RegisterCustomMeshPieces()
  {
    PieceDataDictionary.Add(PrefabNames.CustomWaterFloatation,
      new PieceData
      {
        Name = "$valheim_vehicles_custom_floatation_tool",
        Description = "$valheim_vehicles_custom_floatation_tool_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WaterFloatationHeight)
      });

    PieceDataDictionary.Add(PrefabNames.CustomWaterMaskCreator,
      new PieceData
      {
        Name = "$valheim_vehicles_water_mask",
        Description = "$valheim_vehicles_water_mask_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WaterOpacityBucket)
      });
  }

  /// <summary>
  /// Another cube like controller for bounds setting for a vehicle
  /// </summary>
  private static void RegisterVehicleBoundsControllerPieces()
  {
    PieceDataDictionary.Add(PrefabNames.CustomVehicleMaxCollisionHeightCreator,
      new PieceData
      {
        Name = "$valheim_vehicles_tread_distance",
        Description = "$valheim_vehicles_tread_distance_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .TankTreadIcon)
      });
  }

  private static void RegisterRamPieces()
  {
    int[] ramSizes = [1, 2];
    string[] ramMaterials = [PrefabTiers.Tier1, PrefabTiers.Tier3];
    foreach (var ramMaterial in ramMaterials)
    {
      var materialTranslation =
        PrefabTiers.GetTierMaterialTranslation(ramMaterial);
      foreach (var ramSize in ramSizes)
        PieceDataDictionary.Add(
          PrefabNames.GetRamStakeName(ramMaterial, ramSize), new PieceData
          {
            Name =
              $"$valheim_vehicles_ram_stake  {materialTranslation}",
            Description = "$valheim_vehicles_ram_stake_desc",
            Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(
              SpriteNames.GetRamStakeName(ramMaterial, ramSize))
          });
    }

    string[] bladeDirs = ["top", "bottom", "left", "right"];

    foreach (var bladeDir in bladeDirs)
      PieceDataDictionary.Add(
        PrefabNames.GetRamBladeName(bladeDir), new PieceData
        {
          Name =
            $"$valheim_vehicles_ram_blade $valheim_vehicles_direction_{bladeDir} $valheim_vehicles_material_bronze",
          Description = "$valheim_vehicles_ram_blade_desc",
          Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(
            SpriteNames.GetRamBladeName(bladeDir))
        });
  }

  private static void RegisterExternalShips()
  {
    if (!PrefabConfig.AllowExperimentalPrefabs.Value) return;

    const string prefabName = "Nautilus Submarine";
    const string description =
      $"Experimental Nautilus technology discovered. Have Fun!";
    PieceDataDictionary.Add(
      PrefabNames.Nautilus, new PieceData
      {
        Name = prefabName,
        Description = description,
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .Nautilus)
      });
  }

  private static void RegisterHullSlabIcons()
  {
    var spriteAtlas = LoadValheimVehicleAssets.VehicleSprites;

    const string pieceBaseName = "valheim_vehicles_hull_slab";

    const string pieceName = $"${pieceBaseName}";
    const string pieceDescription = $"${pieceBaseName}_desc";
    const string iconBaseName = "hull_slab";

    List<PrefabNames.PrefabSizeVariant> sizeVariants =
    [
      PrefabNames.PrefabSizeVariant.TwoByTwo,
      PrefabNames.PrefabSizeVariant.FourByFour
    ];
    List<string> materialVariants =
      [HullMaterial.Wood, HullMaterial.Iron];

    foreach (var sizeVariant in sizeVariants)
    {
      var sizeName = PrefabNames.GetPrefabSizeName(sizeVariant);
      foreach (var materialVariant in materialVariants)
      {
        var materialName = materialVariant.ToLower();
        PieceDataDictionary.Add(
          PrefabNames.GetHullSlabName(materialVariant,
            sizeVariant), new PieceData
          {
            Name =
              $"{pieceName} $valheim_vehicles_material_{materialName} {sizeName}",
            Description = pieceDescription,
            Icon = spriteAtlas.GetSprite(
              $"{iconBaseName}_{materialName}_{sizeName}")
          });
      }
    }
  }

  private static void RegisterHullWalls()
  {
    var spriteAtlas = LoadValheimVehicleAssets.VehicleSprites;
    const string pieceBaseName = "valheim_vehicles_hull_wall";
    const string pieceName = $"${pieceBaseName}";
    const string pieceDescription = $"${pieceBaseName}_desc";
    const string iconBaseName = "hull_wall";

    List<PrefabNames.PrefabSizeVariant> sizeVariants =
    [
      PrefabNames.PrefabSizeVariant.TwoByTwo,
      PrefabNames.PrefabSizeVariant.FourByFour
    ];
    List<string> materialVariants =
      [HullMaterial.Wood, HullMaterial.Iron];

    foreach (var sizeVariant in sizeVariants)
    {
      var sizeName = PrefabNames.GetPrefabSizeName(sizeVariant);
      foreach (var materialVariant in materialVariants)
      {
        var materialName = materialVariant.ToLower();
        PieceDataDictionary.Add(
          PrefabNames.GetHullWallName(materialVariant,
            sizeVariant), new PieceData
          {
            Name =
              $"{pieceName} $valheim_vehicles_material_{materialName} {sizeName}",
            Description = pieceDescription,
            Icon = spriteAtlas.GetSprite(
              $"{iconBaseName}_{materialName}_{sizeName}")
          });
      }
    }
  }

  public static void RegisterHullRibCornerFloors()
  {
    var spriteAtlas = LoadValheimVehicleAssets.VehicleSprites;
    const string pieceBaseName = "valheim_vehicles_hull_rib_corner_floor";
    const string pieceName = $"${pieceBaseName}";
    const string pieceDescription = $"${pieceBaseName}_desc";
    const string iconBaseName = "hull_corner_floor";

    List<PrefabNames.DirectionVariant> directionVariants =
      [PrefabNames.DirectionVariant.Left, PrefabNames.DirectionVariant.Right];
    List<string> materialVariants =
      [HullMaterial.Wood, HullMaterial.Iron];

    foreach (var directionVariant in directionVariants)
    {
      var directionName = PrefabNames.GetDirectionName(directionVariant);
      foreach (var materialVariant in materialVariants)
      {
        var materialName = materialVariant.ToLower();
        var prefabName = PrefabNames.GetHullRibCornerFloorName(materialVariant,
          directionVariant);
        var pieceData = new PieceData
        {
          Name =
            $"{pieceName} $valheim_vehicles_material_{materialName} $valheim_vehicles_direction_{directionName}",
          Description = pieceDescription,
          Icon = spriteAtlas.GetSprite(
            $"{iconBaseName}_{directionName}_{materialName}")
        };

        PieceDataDictionary.Add(prefabName, pieceData);
      }
    }
  }

  public static void RegisterHullRibCornerWalls()
  {
    var spriteAtlas = LoadValheimVehicleAssets.VehicleSprites;
    const string pieceBaseName = "valheim_vehicles_hull_rib_corner";
    const string pieceName = $"${pieceBaseName}";
    const string pieceDescription = $"${pieceBaseName}_desc";
    const string iconBaseName = "hull_rib_corner";

    List<string> materialVariants =
      [HullMaterial.Wood, HullMaterial.Iron];

    foreach (var materialVariant in materialVariants)
    {
      var materialName = materialVariant.ToLower();
      var prefabName = PrefabNames.GetHullRibCornerName(materialVariant);
      var pieceData = new PieceData
      {
        Name =
          $"{pieceName} $valheim_vehicles_material_{materialName}",
        Description = pieceDescription,
        Icon = spriteAtlas.GetSprite(
          $"{iconBaseName}_{materialName}")
      };

      PieceDataDictionary.Add(prefabName, pieceData);
    }
  }

  public static void RegisterHullProws()
  {
    var spriteAtlas = LoadValheimVehicleAssets.VehicleSprites;
    const string pieceBaseName = "valheim_vehicles_hull_rib_prow";
    const string pieceName = $"${pieceBaseName}";
    const string pieceDescription = $"${pieceBaseName}_desc";
    const string iconBaseName = "hull_rib_prow";

    List<PrefabNames.PrefabSizeVariant> sizeVariants =
    [
      PrefabNames.PrefabSizeVariant.TwoByTwo,
      PrefabNames.PrefabSizeVariant.FourByFour
    ];
    List<string> materialVariants =
      [HullMaterial.Wood, HullMaterial.Iron];

    foreach (var sizeVariant in sizeVariants)
    {
      var sizeName = PrefabNames.GetPrefabSizeName(sizeVariant);
      foreach (var materialVariant in materialVariants)
      {
        var prefabName = PrefabNames.GetHullProwVariants(materialVariant,
          sizeVariant);
        var materialName = materialVariant.ToLower();
        var materialDescription =
          ShipHulls.GetHullMaterialDescription(materialVariant);

        var variantDescription =
          $"{pieceDescription} {materialDescription}";

        var pieceData = new PieceData
        {
          Name =
            $"{pieceName} $valheim_vehicles_material_{materialName} {sizeName}",
          Description = variantDescription,
          Icon = spriteAtlas.GetSprite(
            $"{iconBaseName}_{materialName}_{sizeName}")
        };
        PieceDataDictionary.Add(prefabName, pieceData);
      }
    }
  }

  /// todo consider using Jotunn.Manager.RenderManager for these Icon generation
  /// todo auto generate this from the translations json
  /// 4x4 and 2x2 icons look similar, may remove 4x4
  public static void Init()
  {
    PieceLayer = LayerMask.NameToLayer("piece");

    RegisterCustomMastPieces();
    RegisterCustomMeshPieces();
    RegisterVehicleBoundsControllerPieces();

    RegisterRamPieces();
    RegisterExternalShips();

    RegisterHullSlabIcons();
    RegisterHullWalls();
    RegisterHullProws();
    RegisterHullRibCornerWalls();
    RegisterHullRibCornerFloors();

    PieceDataDictionary.Add(PrefabNames.WaterVehicleShip, new PieceData
    {
      Name = "$valheim_vehicles_water_vehicle",
      Description = "$valheim_vehicles_water_vehicle_desc",
      Icon = LoadValheimAssets.vanillaRaftPrefab.GetComponent<Piece>().m_icon
    });

    var woodMatDesc =
      ShipHulls.GetHullMaterialDescription(HullMaterial.Wood);
    var ironMatDesc =
      ShipHulls.GetHullMaterialDescription(HullMaterial.Iron);

    // hull rib variants
    PieceDataDictionary.Add(
      PrefabNames.GetHullRibName(HullMaterial.Wood), new PieceData
      {
        Name =
          "$valheim_vehicles_hull_rib_side $valheim_vehicles_material_wood",
        Description = $"$valheim_vehicles_hull_rib_side_desc {woodMatDesc}",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .HullRibWood)
      });
    PieceDataDictionary.Add(
      PrefabNames.GetHullRibName(HullMaterial.Iron), new PieceData
      {
        Name =
          "$valheim_vehicles_hull_rib_side $valheim_vehicles_material_iron",
        Description =
          $"$valheim_vehicles_hull_rib_side_desc {ironMatDesc}",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .HullRibIron)
      });

    // hull center variants
    PieceDataDictionary.Add(PrefabNames.ShipHullCenterWoodPrefabName,
      new PieceData
      {
        Name = "$valheim_vehicles_hull_center $valheim_vehicles_material_wood",
        Description = $"$valheim_vehicles_hull_center_desc {woodMatDesc}",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .HullCenterWood)
      });

    PieceDataDictionary.Add(PrefabNames.ShipHullCenterIronPrefabName,
      new PieceData
      {
        Name = "$valheim_vehicles_hull_center $valheim_vehicles_material_iron",
        Description = $"valheim_vehicles_hull_center_desc {ironMatDesc}",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .HullCenterIron)
      });


    PieceDataDictionary.Add(PrefabNames.ShipSteeringWheel, new PieceData
    {
      Name = "$valheim_vehicles_wheel",
      Description = "$valheim_vehicles_wheel_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .ShipSteeringWheel)
    });

    PieceDataDictionary.Add(PrefabNames.ShipKeel, new PieceData
    {
      Name = "$valheim_vehicles_keel",
      Description = "$valheim_vehicles_keel_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .ShipKeel)
    });

    PieceDataDictionary.Add(PrefabNames.ShipRudderBasic, new PieceData
    {
      Name = "$valheim_vehicles_rudder_basic",
      Description = "$valheim_vehicles_rudder_basic_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .ShipRudderBasic)
    });

    PieceDataDictionary.Add(PrefabNames.ShipRudderAdvancedWood, new PieceData
    {
      Name =
        "$valheim_vehicles_rudder_advanced $valheim_vehicles_material_wood",
      Description = $"$valheim_vehicles_rudder_advanced_desc {woodMatDesc}",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .ShipRudderAdvancedWood)
    });
    PieceDataDictionary.Add(PrefabNames.ShipRudderAdvancedIron, new PieceData
    {
      Name =
        "$valheim_vehicles_rudder_advanced $valheim_vehicles_material_iron",
      Description = $"$valheim_vehicles_rudder_advanced_desc {ironMatDesc}",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .ShipRudderAdvancedIron)
    });

    PieceDataDictionary.Add(PrefabNames.ShipRudderAdvancedDoubleWood,
      new PieceData
      {
        Name =
          "$valheim_vehicles_rudder_advanced_double $valheim_vehicles_material_wood",
        Description = $"$valheim_vehicles_rudder_advanced_desc {woodMatDesc}",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .ShipRudderAdvancedDoubleWood)
      });
    PieceDataDictionary.Add(PrefabNames.ShipRudderAdvancedDoubleIron,
      new PieceData
      {
        Name =
          "$valheim_vehicles_rudder_advanced_double $valheim_vehicles_material_iron",
        Description = $"$valheim_vehicles_rudder_advanced_desc {ironMatDesc}",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .ShipRudderAdvancedDoubleIron)
      });

    PieceDataDictionary.Add(PrefabNames.ToggleSwitch, new PieceData
    {
      Name = "$valheim_vehicles_toggle_switch",
      Description = "$valheim_vehicles_toggle_switch_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .VehicleSwitch)
    });

    PieceDataDictionary.Add(PrefabNames.WindowWallPorthole2x2Prefab,
      new PieceData
      {
        Name = "$valheim_vehicles_window_wall_porthole 2x2",
        Description = "$valheim_vehicles_window_wall_porthole_desc 2x2",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WindowWallPorthole2x2)
      });

    PieceDataDictionary.Add(PrefabNames.WindowWallPorthole4x4Prefab,
      new PieceData
      {
        Name = "$valheim_vehicles_window_wall_porthole 4x4",
        Description = "$valheim_vehicles_window_wall_porthole_desc 4x4",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WindowWallPorthole4x4)
      });

    PieceDataDictionary.Add(PrefabNames.WindowWallPorthole8x4Prefab,
      new PieceData
      {
        Name = "$valheim_vehicles_window_wall_porthole 8x4",
        Description = "$valheim_vehicles_window_wall_porthole_desc 8x4",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WindowWallPorthole8x4)
      });

    PieceDataDictionary.Add(PrefabNames.WindowFloorPorthole4x4Prefab,
      new PieceData
      {
        Name = "$valheim_vehicles_window_floor_porthole 4x4",
        Description = "$valheim_vehicles_window_floor_porthole_desc 4x4",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WindowFloorPorthole4x4Prefab)
      });

    PieceDataDictionary.Add(PrefabNames.WindowPortholeStandalonePrefab,
      new PieceData
      {
        Name = "$valheim_vehicles_window_porthole_standalone",
        Description = "$valheim_vehicles_window_porthole_standalone_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WindowPortholeStandalone)
      });

    PieceDataDictionary.Add(PrefabNames.WindowWallSquareWoodPrefabName,
      new PieceData
      {
        Name =
          "$valheim_vehicles_window_wall_square $valheim_vehicles_material_wood",
        Description =
          "$valheim_vehicles_window_wall_square $valheim_vehicles_material_wood",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WindowWallSquareWood)
      });


    PieceDataDictionary.Add(PrefabNames.WindowWallSquareIronPrefabName,
      new PieceData
      {
        Name =
          "$valheim_vehicles_window_wall_square $valheim_vehicles_material_iron",
        Description =
          "$valheim_vehicles_window_wall_square $valheim_vehicles_material_iron",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WindowWallSquareIron)
      });

    PieceDataDictionary.Add(PrefabNames.ShipAnchorWood,
      new PieceData
      {
        Name =
          "$valheim_vehicles_ship_anchor $valheim_vehicles_material_wood",
        Description =
          "$valheim_vehicles_ship_anchor_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .Anchor)
      });

    PieceDataDictionary.Add(PrefabNames.LandVehicle,
      new PieceData
      {
        Name =
          "$valheim_vehicles_land_vehicle",
        Description =
          "$valheim_vehicles_land_vehicle_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .LandVehicle)
      });
  }

  public static void IgnoreCameraCollisions(GameObject go)
  {
    var cameraMask = GameCamera.instance.m_blockCameraMask;
    if (cameraMask == 0L) return;
    var colliders = go.GetComponentsInChildren<Collider>();

    foreach (var collider in colliders) collider.excludeLayers = cameraMask;
  }

  /// <summary>
  /// Auto sets up new, worn, broken for wnt
  /// </summary>
  /// <param name="prefab"></param>
  /// <param name="wnt"></param>
  public static void AddNewOldPiecesToWearNTear(GameObject prefab,
    WearNTear wnt)
  {
    var wntNew = prefab.transform.FindDeepChild("new");
    var wntWorn = prefab.transform.FindDeepChild("worn");
    var wntBroken = prefab.transform.FindDeepChild("broken");

    // do not assign objects if worn and wnt broken do not exist
    if (!(bool)wntWorn && !(bool)wntBroken) return;
    if (!(bool)wntNew) return;
    wnt.m_new = wntNew.gameObject;
    if (!(bool)wntWorn) return;
    wnt.m_worn = wntWorn.gameObject;
    wnt.m_broken = wntWorn.gameObject;
    if (!(bool)wntBroken) return;
    wnt.m_broken = wntBroken.gameObject;
  }

  public static string GetPieceNameFromPrefab(string name)
  {
    return Localization.instance.Localize(PieceDataDictionary.GetValueSafe(name)
      .Name);
  }

  public static Piece AddPieceForPrefab(string prefabName, GameObject prefab,
    bool isInverse = false)
  {
    var pieceInformation = PieceDataDictionary.GetValueSafe(prefabName);

    var piece = prefab.AddComponent<Piece>();

    piece.m_name = pieceInformation.Name;
    piece.m_description = pieceInformation.Description;
    piece.m_icon = pieceInformation.Icon;

    // todo yet another helper might be needed.
    if (isInverse)
    {
      piece.m_name = $"$valheim_vehicles_inverse {piece.m_name}";
      piece.m_description =
        $"$valheim_vehicles_inverse_desc {piece.m_description}";
    }

    return piece;
  }

  public static ZNetView AddNetViewWithPersistence(GameObject prefab,
    bool prioritized = false)
  {
    var netView = prefab.GetComponent<ZNetView>();
    if (!(bool)netView) netView = prefab.AddComponent<ZNetView>();

    if (!netView)
    {
      Logger.LogError(
        "Unable to register NetView, ValheimRAFT could be broken without netview");
      return netView;
    }

    if (prioritized) netView.m_type = ZDO.ObjectType.Prioritized;

    netView.m_persistent = true;

    return netView;
  }

  public static WearNTear GetWearNTearSafe(GameObject prefabComponent)
  {
    var wearNTearComponent = prefabComponent.GetComponent<WearNTear>();
    if (wearNTearComponent == null)
    {
      // Many components do not have WearNTear so they must be added to the prefabPiece
      wearNTearComponent = prefabComponent.AddComponent<WearNTear>();
      if (!wearNTearComponent)
        Logger.LogError(
          $"error setting WearNTear for RAFT prefab {prefabComponent.name}, the ValheimRAFT mod may be unstable without WearNTear working properly");
    }

    return wearNTearComponent;
  }

  public static WearNTear SetWearNTear(GameObject prefabComponent,
    int tierMultiplier = 1,
    bool canFloat = false)
  {
    var wearNTearComponent = GetWearNTearSafe(prefabComponent);

    wearNTearComponent.m_noSupportWear = canFloat;
    wearNTearComponent.m_destroyedEffect =
      LoadValheimAssets.woodFloorPieceWearNTear.m_destroyedEffect;
    wearNTearComponent.m_hitEffect =
      LoadValheimAssets.woodFloorPieceWearNTear.m_hitEffect;

    if (tierMultiplier == 1)
    {
      wearNTearComponent.m_materialType = WearNTear.MaterialType.Wood;
      wearNTearComponent.m_destroyedEffect =
        LoadValheimAssets.woodFloorPieceWearNTear.m_destroyedEffect;
    }
    else if (tierMultiplier == 2)
    {
      wearNTearComponent.m_materialType = WearNTear.MaterialType.Stone;
      wearNTearComponent.m_destroyedEffect =
        LoadValheimAssets.stoneFloorPieceWearNTear.m_destroyedEffect;
      wearNTearComponent.m_hitEffect =
        LoadValheimAssets.stoneFloorPieceWearNTear.m_hitEffect;
    }
    else if (tierMultiplier == 3)
    {
      // todo add different hit effect and destroy effect
      wearNTearComponent.m_materialType = WearNTear.MaterialType.Iron;
    }

    wearNTearComponent.m_health = PrefabRegistryController.wearNTearBaseHealth *
                                  tierMultiplier;
    wearNTearComponent.m_noRoofWear = false;

    return wearNTearComponent;
  }

  /**
   * experimentally add snappoints
   */
  public static void AddSnapPoint(string name, GameObject parentObj)
  {
    var snappointObj = new GameObject
    {
      name = name,
      tag = SnappointTag
    };
    Object.Instantiate(snappointObj, parentObj.transform);
  }

  public static void FixCollisionLayers(GameObject r)
  {
    var piece = r.layer = LayerMask.NameToLayer("piece");
    var comps = r.transform.GetComponentsInChildren<Transform>(true);
    for (var i = 0; i < comps.Length; i++) comps[i].gameObject.layer = piece;
  }

  public static WearNTear SetWearNTearSupport(WearNTear wntComponent,
    WearNTear.MaterialType materialType)
  {
    // this will use the base material support provided by valheim for support. This should be balanced for wood. Stone may need some tweaks for buoyancy and other balancing concerns
    wntComponent.m_materialType = materialType;

    return wntComponent;
  }


  /**
  * todo this needs to be fixed so the mast blocks only with the mast part and ignores the non-sail area.
  * if the collider is too big it also pushes the rigidbody system underwater (IE Raft sinks)
  *
  * May be easier to just get the game object structure for each sail and do a search for the sail and master parts.
  */
  public static void AddBoundsToAllChildren(string colliderName,
    GameObject parent,
    GameObject componentToEncapsulate)
  {
    var boxCol = parent.GetComponent<BoxCollider>();
    if (boxCol == null) boxCol = parent.AddComponent<BoxCollider>();

    boxCol.name = colliderName;

    var bounds = new Bounds(parent.transform.position, Vector3.zero);

    var allDescendants =
      componentToEncapsulate.GetComponentsInChildren<Transform>();
    foreach (var desc in allDescendants)
    {
      var childRenderer = desc.GetComponent<Renderer>();
      if (childRenderer != null) bounds.Encapsulate(childRenderer.bounds);

      boxCol.center = new Vector3(0, bounds.max.y,
        0);
      boxCol.size = boxCol.center * 2;
    }
  }

  public static void FixRopes(GameObject r)
  {
    var ropes = r.GetComponentsInChildren<LineAttach>();
    for (var i = 0; i < ropes.Length; i++)
    {
      ropes[i].GetComponent<LineRenderer>().positionCount = 2;
      ropes[i].m_attachments.Clear();
      ropes[i].m_attachments.Add(r.transform);
    }
  }

  /**
   * Deprecated...but still needed for a few older raft components
   */
  public static void FixSnapPoints(GameObject r)
  {
    var t = r.GetComponentsInChildren<Transform>(true);
    foreach (var t1 in t)
      if (t1.name.StartsWith($"_{SnappointTag}"))
        t1.tag = SnappointTag;
  }


  public static void HoistSnapPointsToPrefab(GameObject prefab)
  {
    HoistSnapPointsToPrefab(prefab, prefab.transform);
  }

  /// <summary>
  /// Recursive.
  /// </summary>
  /// TODO might want to just use a GetComponentsInChildren<Transform> and then iterate through that.
  ///
  public static void HoistSnappointChildren(Transform prefabRoot, Transform currentParent)
  {
    if (currentParent.childCount == 0) return;
    for (var i = 0; i < currentParent.childCount; i++)
    {
      var transformObj = currentParent.GetChild(i);
      if (transformObj.tag != SnappointTag && transformObj.childCount == 0)
      {
        continue;
      }
      if (transformObj.childCount > 0)
      {
        HoistSnappointChildren(prefabRoot, transformObj);
        continue;
      }

      // changing parent makes the current childCount immediately drop.
      transformObj.SetParent(prefabRoot);
      transformObj.gameObject.SetActive(false);
      i--;
    }
  }

// Use this to work around object resizing requiring repeated movement of child snappoints. This way snappoints can stay in the relative object without issue
  public static void HoistSnapPointsToPrefab(GameObject prefab,
    Transform parent,
    string[]? hoistParentNameFilters = null)
  {
    var snappointParent = prefab.transform.Find("snappoints");
    if (snappointParent == null)
    {
      snappointParent = parent.transform.Find("snappoints");
    }

    if (snappointParent != null)
    {
      HoistSnappointChildren(prefab.transform, snappointParent);
      return;
    }

    var transformObjs = parent.GetComponentsInChildren<Transform>(true);
    foreach (var transformObj in transformObjs)
    {
      if (transformObj.tag != SnappointTag)
        continue;
      if (hoistParentNameFilters != null)
      {
        foreach (var hoistName in hoistParentNameFilters)
        {
          if (!transformObj.parent.name.StartsWith(hoistName)) continue;
          transformObj.SetParent(prefab.transform);
          transformObj.gameObject.SetActive(false);
        }
      }
      else
      {
        transformObj.SetParent(prefab.transform);
        transformObj.gameObject.SetActive(false);
      }
    }
  }
}