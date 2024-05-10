using Jotunn;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;

namespace ValheimVehicles.Prefabs;

public class LoadValheimVehicleAssets : ILoadAssets
{
  // hull
  public static GameObject ShipHullWoodAsset = null!;
  public static GameObject ShipHullIronAsset = null!;

  // hull ribs
  public static GameObject ShipHullRibWoodAsset = null!;
  public static GameObject ShipHullRibIronAsset = null!;

  // slabs (act as hulls too)
  public static GameObject ShipHullSlabWoodAsset = null!;
  public static GameObject ShipHullSlabIronAsset = null!;

  public static GameObject ShipRudderBasicAsset = null!;
  public static GameObject ShipRudderAdvancedAsset = null!;
  public static GameObject ShipKeelAsset = null!;

  // vehicles
  public static GameObject SteeringWheel;
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
    SteeringWheel = assetBundle.LoadAsset<GameObject>("steering_wheel.prefab");
    PieceShader = assetBundle.LoadAsset<Shader>("Custom_Piece.shader");
    ShipKeelAsset = assetBundle.LoadAsset<GameObject>("vehicle_ship_keel");
    VehicleSwitchAsset = assetBundle.LoadAsset<GameObject>("vehicle_switch");
    VehicleShipAsset =
      assetBundle.LoadAsset<GameObject>("vehicle_ship.prefab");
    VehiclePiecesAsset = assetBundle.LoadAsset<GameObject>("vehicle_ship_pieces.prefab");

    // hull slabs
    ShipHullSlabWoodAsset =
      assetBundle.LoadAsset<GameObject>("hull_slab_wood.prefab");
    ShipHullSlabIronAsset =
      assetBundle.LoadAsset<GameObject>("hull_slab_iron.prefab");

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
      assetBundle.LoadAsset<GameObject>("vehicle_ship_rudder_basic.prefab");
    ShipRudderAdvancedAsset =
      assetBundle.LoadAsset<GameObject>(
        "vehicle_ship_rudder_advanced.prefab");

    HudAnchor = assetBundle.LoadAsset<GameObject>("hud_anchor.prefab");

    VehicleSprites = assetBundle.LoadAsset<SpriteAtlas>(
      "vehicle_icons.spriteatlasv2");
  }
}