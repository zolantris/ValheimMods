using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ValheimVehicles.Prefabs.Registry;

public class ShipHullPrefab : IRegisterPrefab
{
  public static readonly ShipHullPrefab Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    // hulls
    RegisterHull(PrefabNames.ShipHullCenterWoodPrefabName, ShipHulls.HullMaterial.Wood, 2,
      prefabManager, pieceManager);
    RegisterHull(PrefabNames.ShipHullCenterIronPrefabName, ShipHulls.HullMaterial.Iron,
      2, prefabManager, pieceManager);

    RegisterHull(PrefabNames.ShipHullSlabWoodPrefabName, ShipHulls.HullMaterial.Wood,
      1, prefabManager, pieceManager);
    RegisterHull(PrefabNames.ShipHullSlabIronPrefabName, ShipHulls.HullMaterial.Iron,
      1, prefabManager, pieceManager);

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
        amountPerCount = 10;
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
    PrefabRegistryHelpers.AddPieceForPrefab(prefabName, prefab);

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = [GetRequirements(hullMaterial, 4)]
    }));
  }

  private static GameObject GetShipHullAssetByMaterial(string prefabName, string hullMaterial)
  {
    if (prefabName.Contains(PrefabNames.ShipHullSlabIronPrefabName) ||
        prefabName.Contains(PrefabNames.ShipHullSlabWoodPrefabName))
    {
      return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
        ? LoadValheimVehicleAssets.ShipHullSlabIronAsset
        : LoadValheimVehicleAssets.ShipHullSlabWoodAsset;
    }

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
    int materialCount,
    PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(
        prefabName, GetShipHullAssetByMaterial(prefabName, hullMaterial));

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    prefab.layer = 0;
    prefab.gameObject.layer = 0;
    var piece = PrefabRegistryHelpers.AddPieceForPrefab(prefabName, prefab);

    prefab.gameObject.transform.position = Vector3.zero;
    prefab.gameObject.transform.localPosition = Vector3.zero;
    piece.m_waterPiece = false;
    piece.m_noClipping = true;
    piece.m_noInWater = false;

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
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab,
      ["hull_slab_new", "vehicle_ship_hull_slab"]);

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = [GetRequirements(hullMaterial, materialCount)]
    }));
  }
}