using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;

namespace ValheimVehicles.Prefabs;

public class LoadValheimVehicleAssets : ILoadAssets
{
  public static GameObject ShipHullAsset = null!;
  public static GameObject ShipRudderBasicAsset = null!;
  public static GameObject ShipRudderAdvancedAsset = null!;
  public static GameObject VehicleShipAsset = null!;
  public static GameObject VehiclePiecesAsset = null!;
  public static readonly LoadValheimVehicleAssets Instance = new();
  public static SpriteAtlas Sprites = null!;
  public static Shader PieceShader = null!;


  public void Init(AssetBundle assetBundle)
  {
    PieceShader = assetBundle.LoadAsset<Shader>("Custom_Piece.shader");
    VehicleShipAsset =
      assetBundle.LoadAsset<GameObject>("vehicle_ship.prefab");
    VehiclePiecesAsset = assetBundle.LoadAsset<GameObject>("vehicle_ship_pieces.prefab");
    ShipHullAsset =
      assetBundle.LoadAsset<GameObject>("vehicle_ship_hull.prefab");
    ShipRudderBasicAsset =
      assetBundle.LoadAsset<GameObject>("vehicle_ship_rudder_basic.prefab");
    ShipRudderAdvancedAsset =
      assetBundle.LoadAsset<GameObject>(
        "vehicle_ship_rudder_advancedv2.prefab");
    Sprites = assetBundle.LoadAsset<SpriteAtlas>(
      "vehicle_icons.spriteatlasv2");
  }
}