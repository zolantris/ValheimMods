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

  public static GameObject? RaftHullPrefabInstance =>
    PrefabManager.Instance.GetPrefab(PrefabNames.ShipHullPrefabName);

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterHull(PrefabNames.ShipHullPrefabName,
      new Vector3(1, 1, 1),
      prefabManager, pieceManager);
  }

  private static void RegisterHull(
    string prefabName,
    Vector3 pieceScale,
    PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(
        prefabName, LoadValheimVehicleAssets.ShipHullAdvancedCenterAsset);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    prefab.layer = 0;
    prefab.gameObject.layer = 0;
    var piece = PrefabRegistryHelpers.AddPieceForPrefab(prefabName, prefab);

    // shifting for collider testing
    prefab.transform.localPosition = new Vector3(0, 0, -4);


    prefab.transform.localScale = pieceScale;
    prefab.gameObject.transform.position = Vector3.zero;
    prefab.gameObject.transform.localPosition = Vector3.zero;
    piece.m_waterPiece = false;
    piece.m_noClipping = false;

    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt, WearNTear.MaterialType.HardWood);
    // wnt.m_oldMaterials = LoadValheimAssets.woodFloorPieceWearNTear.m_oldMaterials;
    // wnt.m_oldMaterials = null;
    // wnt.m_onDamaged = null;
    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.m_hitEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_hitEffect;
    wnt.m_switchEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_switchEffect;
    wnt.m_hitNoise = LoadValheimAssets.woodFloorPieceWearNTear.m_hitNoise;
    // todo set back to 1600f after debugging
    wnt.m_health = 600f;
    // wnt.m_health = 1600f;
    wnt.m_new = prefab.transform.Find("new").gameObject;
    var wornHull = prefab.transform.Find("worn").gameObject;
    wnt.m_worn = wornHull;
    wnt.m_broken = wornHull;

    // this will be used to hide water on the boat
    prefab.AddComponent<ShipHullComponent>();
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Name = piece.m_name,
      Description = piece.m_description,
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 20,
          Item = "Wood",
          Recover = true
        }
      ]
    }));
  }
}