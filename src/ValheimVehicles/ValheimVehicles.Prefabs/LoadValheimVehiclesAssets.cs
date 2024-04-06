using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;

namespace ValheimVehicles.Prefabs;

public class LoadValheimVehicleAssets : ILoadAssets
{
  public static GameObject? ShipHullAsset;
  public static GameObject? ShipRudderBasicAsset;
  public static GameObject? ShipRudderAdvancedAsset;
  public static readonly LoadValheimVehicleAssets Instance = new();

  public void Init(AssetBundle assetBundle)
  {
    ShipHullAsset =
      assetBundle.LoadAsset<GameObject>("Assets/ValheimVehicles/vehicle_ship_hull.prefab");
    ShipRudderBasicAsset =
      assetBundle.LoadAsset<GameObject>("Assets/ValheimVehicles/vehicle_ship_rudder_basic.prefab");
    ShipRudderAdvancedAsset =
      assetBundle.LoadAsset<GameObject>(
        "Assets/ValheimVehicles/vehicle_ship_rudder_advanced.prefab");
  }
}