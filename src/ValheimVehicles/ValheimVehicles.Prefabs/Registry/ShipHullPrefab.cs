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
    RegisterHull(PrefabNames.ShipHullPrefabName, PrefabNames.ShipHullCoreWoodHorizontal,
      new Vector3(1, 1, 1),
      prefabManager, pieceManager);
  }


  // public void RegisterHulls()
  // {
  //   RegisterHull("wood_wall_log_4x0.5", ShipHulls.HullMaterial.CoreWood,
  //     ShipHulls.HullOrientation.Horizontal, new Vector3(1, 1f, 1f));
  // }

  private static void RegisterHull(
    string prefabName, string pieceName,
    Vector3 pieceScale,
    PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var raftHullPrefab =
      prefabManager.CreateClonedPrefab(
        prefabName, LoadValheimVehicleAssets.ShipHullAsset);

    if (!(bool)raftHullPrefab) return;

    PrefabRegistryHelpers.AddNetViewWithPersistence(raftHullPrefab);
    raftHullPrefab.layer = 0;
    raftHullPrefab.gameObject.layer = 0;
    var piece = raftHullPrefab.AddComponent<Piece>();

    // shifting for collider testing
    raftHullPrefab.transform.localPosition = new Vector3(0, 0, -4);


    raftHullPrefab.transform.localScale = pieceScale;
    raftHullPrefab.gameObject.transform.position = Vector3.zero;
    raftHullPrefab.gameObject.transform.localPosition = Vector3.zero;
    piece.m_waterPiece = false;
    piece.m_icon = LoadValheimVehicleAssets.Sprites.GetSprite(SpriteNames.ShipHull);
    piece.m_noClipping = false;
    piece.m_name = pieceName;

    var wnt = PrefabRegistryHelpers.SetWearNTear(raftHullPrefab);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt, WearNTear.MaterialType.HardWood);
    // wnt.m_oldMaterials = LoadValheimAssets.woodFloorPieceWearNTear.m_oldMaterials;
    // wnt.m_oldMaterials = null;
    wnt.m_onDamaged = null;
    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.m_hitEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_hitEffect;
    wnt.m_hitNoise = LoadValheimAssets.woodFloorPieceWearNTear.m_hitNoise;
    wnt.m_health = 250f;
    wnt.m_new = LoadValheimVehicleAssets.ShipHullAsset.transform.Find("new").gameObject;
    var wornHull = LoadValheimVehicleAssets.ShipHullAsset.transform.Find("worn").gameObject;
    wnt.m_worn = wornHull;
    wnt.m_broken = wornHull;


    // this will be used to hide water on the boat
    raftHullPrefab.AddComponent<ShipHullComponent>();
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(raftHullPrefab);

    pieceManager.AddPiece(new CustomPiece(raftHullPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      /*
       * @todo make the name dynamic getter from HullMaterial
       */
      Name = piece.m_name,
      Description = piece.m_description,
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = new RequirementConfig[3]
      {
        new()
        {
          Amount = 10,
          Item = "FineWood",
          Recover = true
        },
        new()
        {
          Amount = 2,
          Item = "RoundLog",
          Recover = true
        },
        new()
        {
          Amount = 6,
          Item = "WolfPelt",
          Recover = true
        }
      }
    }));
  }

  // private GameObject RegisterHull(string prefabName, string prefabMaterial,
  //   ShipHulls.HullOrientation prefabOrientation, Vector3 pieceScale, PrefabManager prefabManager,
  //   PieceManager pieceManager)
  // {
  //   // var woodHorizontalOriginalPrefab = prefabManager.GetPrefab(prefabName);
  //   // var hullPrefabName = ShipHulls.GetHullPrefabName(prefabMaterial, prefabOrientation);
  //   // RegisterHullPrefabs(<name>)
  //   // ShipHulls.GetHullTranslations(prefabMaterial, prefabOrientation)
  //   return raftHullPrefab;
  // }
}