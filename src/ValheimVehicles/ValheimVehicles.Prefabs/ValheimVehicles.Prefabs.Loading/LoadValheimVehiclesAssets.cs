using Jotunn;
using UnityEngine;
using UnityEngine.U2D;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.Validation;

namespace ValheimVehicles.Prefabs;

/// <summary>
/// Loads all ValheimVehicles assets. All assets are strings with their type. `.material` or `.shader` or `.prefab` are optional
/// </summary>
public class LoadValheimVehicleAssets : ILoadAssets
{
  // CustomSail
  public static GameObject CustomSail = null!;

  public static Material DoubleSidedTransparentMat = null!;

  public static GameObject VehicleHammer = null!;

  public static Material TransparentDepthMaskMaterial = null!;
  public static Material WaterHeightMaterial = null!;
  public static Material GlassNautilusNoTint = null!;

  public static GameObject ShipAnchorWood = null!;

  // mechanisms/energy
  public static GameObject Mechanism_Swivel = null!;
  // public static GameObject Mechanism_PowerSource_Coal = null!;
  public static GameObject Mechanism_Power_Source_Eitr = null!;
  public static GameObject Mechanism_Power_Storage_Eitr = null!;
  public static GameObject Mechanism_Power_Activator_Plate = null!;

  // hull
  public static GameObject ShipHullWoodAsset = null!;
  public static GameObject ShipHullIronAsset = null!;

  public static GameObject RamStakeWood1X2 = null!;
  public static GameObject RamStakeWood2X4 = null!;
  public static GameObject RamStakeIron1X2 = null!;
  public static GameObject RamStakeIron2X4 = null!;

  public static GameObject RamBladeTop = null!;
  public static GameObject RamBladeBottom = null!;
  public static GameObject RamBladeRight = null!;
  public static GameObject RamBladeLeft = null!;

  // ships (like nautilus)
  public static GameObject ShipNautilus = null!;

  public static GameObject VehicleLand = null!;
  public static GameObject WheelSingle = null!;
  public static GameObject TankTreadsSingle = null!;

  // slabs (act as hulls too)
  public static GameObject ShipHullSlab2X2WoodAsset = null!;
  public static GameObject ShipHullSlab2X2IronAsset = null!;
  public static GameObject ShipHullSlab4X4WoodAsset = null!;
  public static GameObject ShipHullSlab4X4IronAsset = null!;

  public static GameObject ShipHullWall2X2WoodAsset = null!;
  public static GameObject ShipHullWall2X2IronAsset = null!;
  public static GameObject ShipHullWall4X4WoodAsset = null!;
  public static GameObject ShipHullWall4X4IronAsset = null!;

  // basic rudders/look like oars
  public static GameObject ShipRudderBasicAsset = null!;

  // advanced rudders
  public static GameObject ShipRudderAdvancedSingleWoodAsset = null!;
  public static GameObject ShipRudderAdvancedSingleIronAsset = null!;

  // advanced rudder tails (double rudder like a sub)
  public static GameObject ShipRudderAdvancedDoubleWoodAsset = null!;
  public static GameObject ShipRudderAdvancedDoubleIronAsset = null!;

  public static GameObject ShipKeelAsset = null!;

  // vehicles
  public static GameObject SteeringWheel = null!;
  public static GameObject VehicleShipAsset = null!;
  public static GameObject VehiclePiecesAsset = null!;
  public static GameObject Mechanism_Switch = null!;

  public static GameObject ShipWindowPortholeWall2x2 = null!;
  public static GameObject ShipWindowPortholeWall4x4 = null!;
  public static GameObject ShipWindowPortholeWall8x4 = null!;
  public static GameObject ShipWindowPortholeFloor4x4 = null!;
  public static GameObject ShipWindowPortholeStandalone = null!;

  // hud
  public static GameObject HudAnchor = null!;

  // generic/misc
  public static SpriteAtlas VehicleSprites = null!;

  public static readonly LoadValheimVehicleAssets Instance = new();

  public static GameObject GetGhostContainer(GameObject obj)
  {
    return obj.FindDeepChild(PrefabNames.GhostContainer).gameObject;
  }

  public static Material LightningMaterial = null!;
  public static GameObject Mechanism_PowerPylon = null!;

  public static GameObject GetPiecesContainer(GameObject obj)
  {
    return obj.FindDeepChild(PrefabNames.VehiclePiecesContainer).gameObject;
  }

  public static GameObject GetVehicleContainer(GameObject obj)
  {
    return obj.FindDeepChild(PrefabNames.VehicleContainer).gameObject;
  }

  internal static AssetBundle _bundle = null!;

  public static string GetShipProwAssetName(string materialVariant,
    PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var sizeName = PrefabNames.GetPrefabSizeVariantName(sizeVariant);
    const string baseName = "hull_prow";
    var assetName = $"{baseName}_{sizeName}_{materialVariant}";
    return assetName;
  }


  /// <summary>
  /// prow type can be either cutter|smooth
  /// </summary>
  /// <returns></returns>
  public static string GetShipProwRibSpecialVariantAssetName(string materialVariant,
    PrefabNames.PrefabSizeVariant sizeVariant, PrefabNames.DirectionVariant? directionVariant, string prefabVariant)
  {
    var sizeName = PrefabNames.GetPrefabSizeVariantName(sizeVariant);
    const string baseName = "hull_rib_prow";

    var assetName = $"{baseName}_{prefabVariant}_{sizeName}";

    if (directionVariant != null)
    {
      var directionName = PrefabNames.GetDirectionName(directionVariant.Value);
      assetName += $"_{directionName}";
    }

    assetName += $"_{materialVariant}";

    return assetName;
  }

  public static GameObject GetShipHullProw(string materialVariant,
    PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var assetName = GetShipProwAssetName(materialVariant, sizeVariant);
    var assetNameToLoad = $"{assetName}.prefab";
    return _bundle.LoadAsset<GameObject>(assetNameToLoad);
  }

  public static GameObject GetShipHullRibProwSpecialVariant(string materialVariant,
    PrefabNames.PrefabSizeVariant sizeVariant, PrefabNames.DirectionVariant? directionVariant, string prefabVariant)
  {
    var assetName = GetShipProwRibSpecialVariantAssetName(materialVariant, sizeVariant, directionVariant, prefabVariant);
    var assetNameToLoad = $"{assetName}.prefab";
    return _bundle.LoadAsset<GameObject>(assetNameToLoad);
  }

  public static GameObject GetShipHullRibCorner(string hullMaterial, PrefabNames.DirectionVariant? directionVariant, PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var assetString = GetShipHullRibCornerAssetName(hullMaterial, directionVariant, sizeVariant);
    var assetNameToLoad = $"{assetString}.prefab";
    return _bundle.LoadAsset<GameObject>(assetNameToLoad);
  }


  public static string GetShipHullCornerFloorAssetName(string materialVariant,
    PrefabNames.DirectionVariant directionVariant, PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var directionName = PrefabNames.GetDirectionName(directionVariant);
    var sizeVariantString = PrefabNames.GetPrefabSizeVariantName(sizeVariant);
    const string baseName = "hull_corner_floor";
    var assetString = $"{baseName}_{sizeVariantString}_{directionName}_{materialVariant}";

    return assetString;
  }

  public static GameObject GetShipHullCornerFloor(string materialVariant,
    PrefabNames.DirectionVariant directionVariant, PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var assetString = GetShipHullCornerFloorAssetName(materialVariant, directionVariant, sizeVariant);
    var assetNameToLoad = $"{assetString}.prefab";
    return _bundle.LoadAsset<GameObject>(assetNameToLoad);
  }

  public static string GetShipHullRibCornerAssetName(string materialVariant, PrefabNames.DirectionVariant? directionVariant, PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var sizeVariantName = PrefabNames.GetPrefabSizeVariantName(sizeVariant);
    const string baseName = "hull_rib_corner";

    var assetString = $"{baseName}_{sizeVariantName}";

    if (directionVariant != null)
    {
      var directionName = PrefabNames.GetDirectionName(directionVariant.Value);
      assetString += $"_{directionName}";
    }

    assetString += $"_{materialVariant}";

    return assetString;
  }

  public static string GetShipHullRibAssetName(string materialVariant, PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var sizeVariantName = PrefabNames.GetPrefabSizeVariantName(sizeVariant);
    const string baseName = "hull_rib";

    var assetString = $"{baseName}_{sizeVariantName}_{materialVariant}";
    return assetString;
  }

  public static GameObject GetShipHullRib(string materialVariant, PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var assetString = GetShipHullRibAssetName(materialVariant, sizeVariant);
    var assetNameToLoad = $"{assetString}.prefab";
    return _bundle.LoadAsset<GameObject>(assetNameToLoad);
  }

  public static GameObject GetMastVariant(string mastType)
  {
    PrefabNames.ValidateMastTypeName(mastType);
    const string baseName = "mast";
    var assetNameToLoad = $"{baseName}_{mastType}.prefab";
    return _bundle.LoadAsset<GameObject>(assetNameToLoad);
  }

  /// <summary>
  /// This loads all the assets
  /// </summary>
  /// todo investigate if it's cleaner to do this load within the registration process.
  /// todo this approach retains the asset in memory adding a unnecessary (small) burden to valheim. Possibly swap this out for a dynamic name generator so things do not need to be hardcoded
  /// <param name="assetBundle"></param>
  public void Init(AssetBundle assetBundle)
  {
    _bundle = assetBundle;

    DoubleSidedTransparentMat =
      assetBundle.LoadAsset<Material>(
        "double_sided_transparent.mat");

    WaterHeightMaterial =
      assetBundle.LoadAsset<Material>("WaterHeightMaterial.mat");
    GlassNautilusNoTint =
      assetBundle.LoadAsset<Material>("glass_nautilus_notint.mat");
    TransparentDepthMaskMaterial =
      assetBundle.LoadAsset<Material>("TransparentDepthMask.mat");

    CustomSail = assetBundle.LoadAsset<GameObject>("custom_sail.prefab");

    ShipNautilus = assetBundle.LoadAsset<GameObject>("nautilus.prefab");

    SteeringWheel = assetBundle.LoadAsset<GameObject>("steering_wheel.prefab");
    ShipKeelAsset = assetBundle.LoadAsset<GameObject>("keel");
    VehicleShipAsset =
      assetBundle.LoadAsset<GameObject>("vehicle_ship.prefab");
    VehiclePiecesAsset =
      assetBundle.LoadAsset<GameObject>("vehicle_ship_pieces.prefab");

    // hull slabs
    ShipHullSlab2X2WoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_slab_wood_2x2.prefab");
    ShipHullSlab2X2IronAsset =
      assetBundle.LoadAsset<GameObject>("hull_slab_iron_2x2.prefab");
    ShipHullSlab4X4WoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_slab_wood_4x4.prefab");
    ShipHullSlab4X4IronAsset =
      assetBundle.LoadAsset<GameObject>("hull_slab_iron_4x4.prefab");

    ShipHullWall2X2WoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_wall_wood_2x2.prefab");
    ShipHullWall2X2IronAsset =
      assetBundle.LoadAsset<GameObject>("hull_wall_iron_2x2.prefab");
    ShipHullWall4X4WoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_wall_wood_4x4.prefab");
    ShipHullWall4X4IronAsset =
      assetBundle.LoadAsset<GameObject>("hull_wall_iron_4x4.prefab");

    // hull center variants
    ShipHullWoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_center_wood.prefab");
    ShipHullIronAsset =
      assetBundle.LoadAsset<GameObject>("hull_center_iron.prefab");

    // rudder variants
    ShipRudderBasicAsset =
      assetBundle.LoadAsset<GameObject>("rudder_basic.prefab");
    ShipRudderAdvancedSingleWoodAsset =
      assetBundle.LoadAsset<GameObject>(
        "rudder_advanced_single_wood.prefab");
    ShipRudderAdvancedSingleIronAsset = assetBundle.LoadAsset<GameObject>(
      "rudder_advanced_single_iron.prefab");


    ShipRudderAdvancedDoubleWoodAsset =
      assetBundle.LoadAsset<GameObject>(
        "rudder_advanced_double_wood.prefab");

    ShipRudderAdvancedDoubleIronAsset =
      assetBundle.LoadAsset<GameObject>(
        "rudder_advanced_double_iron.prefab");

    HudAnchor = assetBundle.LoadAsset<GameObject>("hud_anchor.prefab");

    VehicleSprites = assetBundle.LoadAsset<SpriteAtlas>(
      "vehicle_icons.spriteatlasv2");

    RamStakeWood1X2 =
      assetBundle.LoadAsset<GameObject>($"ram_stake_{PrefabTiers.Tier1}_1x2");
    RamStakeWood2X4 =
      assetBundle.LoadAsset<GameObject>($"ram_stake_{PrefabTiers.Tier1}_2x4");
    RamStakeIron1X2 =
      assetBundle.LoadAsset<GameObject>($"ram_stake_{PrefabTiers.Tier3}_1x2");
    RamStakeIron2X4 =
      assetBundle.LoadAsset<GameObject>($"ram_stake_{PrefabTiers.Tier3}_2x4");


    ShipWindowPortholeWall2x2 =
      assetBundle.LoadAsset<GameObject>(
        $"hull_wall_window_porthole_iron_2x2.prefab");
    ShipWindowPortholeWall4x4 =
      assetBundle.LoadAsset<GameObject>(
        $"hull_wall_window_porthole_iron_4x4.prefab");
    ShipWindowPortholeWall8x4 =
      assetBundle.LoadAsset<GameObject>(
        $"hull_wall_window_porthole_iron_8x4.prefab");

    ShipWindowPortholeFloor4x4 =
      assetBundle.LoadAsset<GameObject>(
        $"hull_floor_window_porthole_iron_4x4.prefab");
    ShipWindowPortholeStandalone =
      assetBundle.LoadAsset<GameObject>($"window_porthole_standalone.prefab");

    ShipAnchorWood =
      assetBundle.LoadAsset<GameObject>($"anchor_full_wood.prefab");

    RamBladeTop = assetBundle.LoadAsset<GameObject>(
      "ram_blade_top.prefab");

    RamBladeBottom = assetBundle.LoadAsset<GameObject>(
      "ram_blade_bottom.prefab");

    RamBladeRight = assetBundle.LoadAsset<GameObject>(
      "ram_blade_right.prefab");

    RamBladeLeft = assetBundle.LoadAsset<GameObject>(
      "ram_blade_left.prefab");

    WheelSingle =
      assetBundle.LoadAsset<GameObject>(
        $"wheel_single.prefab");
    TankTreadsSingle = assetBundle.LoadAsset<GameObject>(
      $"shared_tank_tread.prefab");
    VehicleLand =
      assetBundle.LoadAsset<GameObject>(
        $"vehicle_land.prefab");

    VehicleHammer = assetBundle.LoadAsset<GameObject>("vehicle_hammer.prefab");

    // Mechanism prefabs
    Mechanism_Switch = assetBundle.LoadAsset<GameObject>("mechanism_switch");
    Mechanism_Swivel = assetBundle.LoadAsset<GameObject>("mechanism_swivel.prefab");
    // Mechanism_PowerSource_Coal = assetBundle.LoadAsset<GameObject>("mechanism_power_source_coal.prefab");
    Mechanism_Power_Source_Eitr = assetBundle.LoadAsset<GameObject>("mechanism_power_source_eitr.prefab");
    Mechanism_Power_Storage_Eitr = assetBundle.LoadAsset<GameObject>("mechanism_power_storage_eitr.prefab");
    Mechanism_Power_Activator_Plate = assetBundle.LoadAsset<GameObject>("mechanism_activator_plate.prefab");
    Mechanism_PowerPylon = assetBundle.LoadAsset<GameObject>("mechanism_power_pylon.prefab");

    // Effects Prefabs
    // from Plugin, todo rename the casing problematic asset.
    LightningMaterial = assetBundle.LoadAsset<Material>("lightning_bolt_material_animated_additive.mat");

    // Data rebinds done inline
    PowerNetworkController.WireMaterial = new Material(DoubleSidedTransparentMat)
    {
      color = Color.black
    };

    RuntimeDebugLineDrawer.DebugRayMaterial = new Material(DoubleSidedTransparentMat);

    PowerPylon.LightningMaterial = LightningMaterial;
    MovingTreadComponent.fallbackPrefab = TankTreadsSingle;

    ClassValidator.ValidateRequiredNonNullFields<LoadValheimVehicleAssets>();
  }
}