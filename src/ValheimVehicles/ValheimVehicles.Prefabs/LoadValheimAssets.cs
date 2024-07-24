using Jotunn.Managers;
using UnityEngine;

namespace ValheimVehicles.Prefabs;

public class LoadValheimAssets
{
  public static readonly LoadValheimAssets Instance = new();

  public static GameObject vanillaRaftPrefab = null!;
  public static GameObject vikingShipPrefab = null!;
  public static GameObject drakkarPrefab = null!;
  public static Transform waterMask = null!;

  public static Piece woodFloorPiece = null!;
  public static WearNTear woodFloorPieceWearNTear = null!;

  public static Piece stoneFloorPiece = null!;
  public static WearNTear stoneFloorPieceWearNTear = null!;

  public static GameObject raftMast = null!;
  public static GameObject shipWaterEffects = null!;

  public static Shader CustomPieceShader = null!;

  public void Init(PrefabManager prefabManager)
  {
    // should come directly from JVLCache
    CustomPieceShader = PrefabManager.Cache.GetPrefab<Shader>("Custom/Piece");

    vanillaRaftPrefab = prefabManager.GetPrefab("Raft");
    vikingShipPrefab = prefabManager.GetPrefab("VikingShip");
    drakkarPrefab = prefabManager.GetPrefab("VikingShip_Ashlands");
    shipWaterEffects = vanillaRaftPrefab.transform.Find("WaterEffects").gameObject;

    waterMask = vikingShipPrefab.transform.Find("ship/visual/watermask");
    raftMast = vanillaRaftPrefab.transform.Find("ship/visual/mast").gameObject;

    var woodFloorPrefab = prefabManager.GetPrefab("wood_floor");
    var stoneFloorPrefab = prefabManager.GetPrefab("stone_floor");

    stoneFloorPiece = stoneFloorPrefab.GetComponent<Piece>();
    stoneFloorPieceWearNTear = stoneFloorPiece.GetComponent<WearNTear>();

    woodFloorPiece = woodFloorPrefab.GetComponent<Piece>();
    woodFloorPieceWearNTear = woodFloorPiece.GetComponent<WearNTear>();
  }
}