using Jotunn;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;

namespace ValheimVehicles.Prefabs;

public class LoadValheimVehicleAssets : ILoadAssets
{
  // ships
  public static GameObject ShipHullAsset = null!;
  public static GameObject ShipRudderBasicAsset = null!;
  public static GameObject ShipRudderAdvancedAsset = null!;
  public static GameObject ShipKeelAsset = null!;

  // vehicles
  public static GameObject VehicleShipAsset = null!;
  public static GameObject VehiclePiecesAsset = null!;

  // generic/misc
  public static SpriteAtlas Sprites = null!;
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
    PieceShader = assetBundle.LoadAsset<Shader>("Custom_Piece.shader");
    ShipKeelAsset = assetBundle.LoadAsset<GameObject>("vehicle_ship_keel");
    VehicleShipAsset =
      assetBundle.LoadAsset<GameObject>("vehicle_ship.prefab");
    VehiclePiecesAsset = assetBundle.LoadAsset<GameObject>("vehicle_ship_pieces.prefab");
    ShipHullAsset =
      assetBundle.LoadAsset<GameObject>("vehicle_ship_hull.prefab");
    ShipRudderBasicAsset =
      assetBundle.LoadAsset<GameObject>("vehicle_ship_rudder_basic.prefab");
    ShipRudderAdvancedAsset =
      assetBundle.LoadAsset<GameObject>(
        "vehicle_ship_rudder_advanced.prefab");
    Sprites = assetBundle.LoadAsset<SpriteAtlas>(
      "vehicle_icons.spriteatlasv2");
  }
}