using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Prefabs;
using Object = UnityEngine.Object;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

public class ShipHullPrefab : IRegisterPrefab
{
  public static readonly ShipHullPrefab Instance = new();

  public static GameObject? RaftHullPrefabInstance;

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
    raftHullPrefab.layer = 0;
    raftHullPrefab.gameObject.layer = 0;
    var piece = raftHullPrefab.AddComponent<Piece>();

    raftHullPrefab.transform.localScale = pieceScale;
    raftHullPrefab.gameObject.transform.position = Vector3.zero;
    raftHullPrefab.gameObject.transform.localPosition = Vector3.zero;
    piece.m_waterPiece = false;
    piece.m_icon = LoadValheimAssets.vanillaRaftPrefab.GetComponent<Piece>().m_icon;
    piece.m_noClipping = false;
    piece.m_name = pieceName;

    var wntComponent = PrefabRegistryHelpers.SetWearNTear(raftHullPrefab);
    PrefabRegistryHelpers.SetWearNTearSupport(wntComponent, WearNTear.MaterialType.HardWood);
    wntComponent.m_onDamaged = null;
    wntComponent.m_supports = true;
    wntComponent.m_support = 2000f;
    wntComponent.m_noSupportWear = true;
    wntComponent.m_noRoofWear = true;
    wntComponent.m_hitEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_hitEffect;
    wntComponent.m_hitNoise = LoadValheimAssets.woodFloorPieceWearNTear.m_hitNoise;
    wntComponent.m_health = 25000f;

    // this may need to send in Piece instead
    PrefabRegistryHelpers.AddNetViewWithPersistence(raftHullPrefab);

    // this will be used to hide water on the boat
    raftHullPrefab.AddComponent<ShipHullComponent>();

    RaftHullPrefabInstance = raftHullPrefab;

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