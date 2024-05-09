using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

public class ShipHullPrefab : IRegisterPrefab
{
  public static readonly ShipHullPrefab Instance = new();

  public static GameObject? HullWoodPrefabInstance =>
    PrefabManager.Instance.GetPrefab(PrefabNames.ShipHullCenterWoodPrefabName);

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    // hulls
    RegisterHull(PrefabNames.ShipHullCenterWoodPrefabName, ShipHulls.HullMaterial.Wood,
      prefabManager, pieceManager);
    RegisterHull(PrefabNames.ShipHullCenterIronPrefabName, ShipHulls.HullMaterial.Iron,
      prefabManager, pieceManager);

    // hull-ribs
    RegisterHullRib(PrefabNames.ShipHullRibWoodPrefabName, ShipHulls.HullMaterial.Wood,
      prefabManager, pieceManager);
    RegisterHullRib(PrefabNames.ShipHullRibIronPrefabName, ShipHulls.HullMaterial.Iron,
      prefabManager, pieceManager);
  }

  public static RequirementConfig GetRequirements(string material, int materialCount)
  {
    var item = "Wood";
    var amountPerCount = 20;

    switch (material)
    {
      case ShipHulls.HullMaterial.Iron:
        item = "Iron";
        amountPerCount = 2;
        break;
      case ShipHulls.HullMaterial.Wood:
        item = "Wood";
        amountPerCount = 20;
        break;
    }

    return new RequirementConfig
    {
      Amount = amountPerCount * materialCount,
      Item = item,
      Recover = true
    };
  }

  /// <summary>
  /// Experimental not ready
  /// </summary>
  private static void RegisterHullRib(
    string prefabName,
    string hullMaterial,
    PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(
        prefabName, GetShipHullRibAssetByMaterial(hullMaterial));

    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt, WearNTear.MaterialType.Iron);

    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.m_hitEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_hitEffect;
    wnt.m_switchEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_switchEffect;
    wnt.m_hitNoise = LoadValheimAssets.woodFloorPieceWearNTear.m_hitNoise;

    ShipHulls.SetMaterialValues(hullMaterial, wnt, 9);
    PrefabRegistryHelpers.AddNewOldPiecesToWearNTear(prefab, wnt);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    prefab.layer = 0;
    prefab.gameObject.layer = 0;
    var piece = PrefabRegistryHelpers.AddPieceForPrefab(prefabName, prefab);

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = [GetRequirements(hullMaterial, 9)]
    }));
  }

  private static GameObject GetShipHullAssetByMaterial(string hullMaterial)
  {
    return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
      ? LoadValheimVehicleAssets.ShipHullIronAsset
      : LoadValheimVehicleAssets.ShipHullWoodAsset;
  }

  private static GameObject GetShipHullRibAssetByMaterial(string hullMaterial)
  {
    return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
      ? LoadValheimVehicleAssets.ShipHullRibIronAsset
      : LoadValheimVehicleAssets.ShipHullRibWoodAsset;
  }

  private static void RegisterHull(
    string prefabName,
    string hullMaterial,
    PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(
        prefabName, GetShipHullAssetByMaterial(hullMaterial));

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    prefab.layer = 0;
    prefab.gameObject.layer = 0;
    var piece = PrefabRegistryHelpers.AddPieceForPrefab(prefabName, prefab);

    // shifting for collider testing
    prefab.transform.localPosition = new Vector3(0, 0, -4);

    prefab.gameObject.transform.position = Vector3.zero;
    prefab.gameObject.transform.localPosition = Vector3.zero;
    piece.m_waterPiece = false;
    piece.m_noClipping = false;

    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab);
    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.m_hitEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_hitEffect;
    wnt.m_switchEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_switchEffect;
    wnt.m_hitNoise = LoadValheimAssets.woodFloorPieceWearNTear.m_hitNoise;

    ShipHulls.SetMaterialValues(hullMaterial, wnt, 1);
    PrefabRegistryHelpers.AddNewOldPiecesToWearNTear(prefab, wnt);
    // this will be used to hide water on the boat
    prefab.AddComponent<ShipHullComponent>();
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab,
      ["hull_slab_new", "vehicle_ship_hull_slab"]);

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = [GetRequirements(hullMaterial, 1)]
    }));
  }
}