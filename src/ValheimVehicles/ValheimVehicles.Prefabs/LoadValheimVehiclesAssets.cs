using Jotunn;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;

namespace ValheimVehicles.Prefabs;

public class LoadValheimVehicleAssets : ILoadAssets
{
  // CustomSail
  public static GameObject CustomSail = null!;

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

  // hull ribs
  public static GameObject ShipHullRibWoodAsset = null!;
  public static GameObject ShipHullRibIronAsset = null!;

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
  public static GameObject VehicleSwitchAsset = null!;

  // hud
  public static GameObject HudAnchor = null!;

  // generic/misc
  public static SpriteAtlas VehicleSprites = null!;
  public static Shader PieceShader = null!;

  public static readonly LoadValheimVehicleAssets Instance = new();

  public static GameObject GetGhostContainer(GameObject obj) =>
    obj.FindDeepChild(PrefabNames.GhostContainer).gameObject;

  public static GameObject GetPiecesContainer(GameObject obj) =>
    obj.FindDeepChild(PrefabNames.PiecesContainer).gameObject;

  public static GameObject GetVehicleContainer(GameObject obj) =>
    obj.FindDeepChild(PrefabNames.VehicleContainer).gameObject;

  public void Init(AssetBundle assetBundle)
  {
    CustomSail = assetBundle.LoadAsset<GameObject>("custom_sail.prefab");

    ShipNautilus = assetBundle.LoadAsset<GameObject>("nautilus.prefab");

    SteeringWheel = assetBundle.LoadAsset<GameObject>("steering_wheel.prefab");
    PieceShader = assetBundle.LoadAsset<Shader>("Custom_Piece.shader");
    ShipKeelAsset = assetBundle.LoadAsset<GameObject>("keel");
    VehicleSwitchAsset = assetBundle.LoadAsset<GameObject>("vehicle_switch");
    VehicleShipAsset =
      assetBundle.LoadAsset<GameObject>("vehicle_ship.prefab");
    VehiclePiecesAsset = assetBundle.LoadAsset<GameObject>("vehicle_ship_pieces.prefab");

    // hull slabs
    ShipHullSlab2X2WoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_slab_2x2_wood.prefab");
    ShipHullSlab2X2IronAsset =
      assetBundle.LoadAsset<GameObject>("hull_slab_2x2_iron.prefab");
    ShipHullSlab4X4WoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_slab_4x4_wood.prefab");
    ShipHullSlab4X4IronAsset =
      assetBundle.LoadAsset<GameObject>("hull_slab_4x4_iron.prefab");

    ShipHullWall2X2WoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_wall_2x2_wood.prefab");
    ShipHullWall2X2IronAsset =
      assetBundle.LoadAsset<GameObject>("hull_wall_2x2_iron.prefab");
    ShipHullWall4X4WoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_wall_4x4_wood.prefab");
    ShipHullWall4X4IronAsset =
      assetBundle.LoadAsset<GameObject>("hull_wall_4x4_iron.prefab");

    // hull center variants
    ShipHullWoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_center_wood.prefab");
    ShipHullIronAsset =
      assetBundle.LoadAsset<GameObject>("hull_center_iron.prefab");

    // hull rib variants
    ShipHullRibWoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_rib_wood.prefab");
    ShipHullRibIronAsset =
      assetBundle.LoadAsset<GameObject>("hull_rib_iron.prefab");

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

    RamStakeWood1X2 = assetBundle.LoadAsset<GameObject>("ram_stake_wood_1x2");
    RamStakeWood2X4 = assetBundle.LoadAsset<GameObject>("ram_stake_wood_2x4");
    RamStakeIron1X2 = assetBundle.LoadAsset<GameObject>("ram_stake_iron_1x2");
    RamStakeIron2X4 = assetBundle.LoadAsset<GameObject>("ram_stake_iron_2x4");


    RamBladeTop = assetBundle.LoadAsset<GameObject>(
      "ram_blade_top.prefab");
    ;
    RamBladeBottom = assetBundle.LoadAsset<GameObject>(
      "ram_blade_bottom.prefab");
    ;
    RamBladeRight = assetBundle.LoadAsset<GameObject>(
      "ram_blade_right.prefab");
    ;
    RamBladeLeft = assetBundle.LoadAsset<GameObject>(
      "ram_blade_left.prefab");
    ;
  }
}