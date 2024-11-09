using Jotunn.Managers;
using UnityEngine;

namespace ValheimVehicles.Prefabs;

public class LoadValheimAssets
{
  public static readonly LoadValheimAssets Instance = new();

  public static GameObject vanillaRaftPrefab;
  public static GameObject vikingShipPrefab;
  public static GameObject drakkarPrefab;
  public static Transform waterMask;
  public static Shader waterMaskShader;

  public static Piece woodFloorPiece;
  public static WearNTear woodFloorPieceWearNTear;

  public static Piece stoneFloorPiece;
  public static WearNTear stoneFloorPieceWearNTear;

  public static GameObject raftMast;
  public static GameObject shipWaterEffects;

  public static Shader CustomPieceShader;

  public void Init(PrefabManager prefabManager)
  {
    // should come directly from JVLCache
    CustomPieceShader = PrefabManager.Cache.GetPrefab<Shader>("Custom/Piece");

    vanillaRaftPrefab = prefabManager.GetPrefab("Raft");
    vikingShipPrefab = prefabManager.GetPrefab("VikingShip");
    drakkarPrefab = prefabManager.GetPrefab("VikingShip_Ashlands");
    shipWaterEffects =
      vanillaRaftPrefab.transform.Find("WaterEffects").gameObject;

    waterMask = vikingShipPrefab.transform.Find("ship/visual/watermask");
    waterMaskShader = waterMask.GetComponent<Renderer>().sharedMaterial.shader;
    raftMast = vanillaRaftPrefab.transform.Find("ship/visual/mast").gameObject;

    var woodFloorPrefab = prefabManager.GetPrefab("wood_floor");
    var stoneFloorPrefab = prefabManager.GetPrefab("stone_floor");

    stoneFloorPiece = stoneFloorPrefab.GetComponent<Piece>();
    stoneFloorPieceWearNTear = stoneFloorPiece.GetComponent<WearNTear>();

    woodFloorPiece = woodFloorPrefab.GetComponent<Piece>();
    woodFloorPieceWearNTear = woodFloorPiece.GetComponent<WearNTear>();
  }
}