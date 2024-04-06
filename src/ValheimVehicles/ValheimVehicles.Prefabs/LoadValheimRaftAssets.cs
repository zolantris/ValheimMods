using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;

namespace ValheimVehicles.Prefabs;

public class LoadValheimRaftAssets : ILoadAssets
{
  public static readonly LoadValheimRaftAssets Instance = new();

  public static SpriteAtlas sprites;
  public static GameObject boarding_ramp;
  public static GameObject mbBoardingRamp;
  public static GameObject steering_wheel;
  public static GameObject rope_ladder;
  public static GameObject rope_anchor;
  public static GameObject ship_hull;
  public static GameObject raftMast;
  public static GameObject dirtFloor;
  public static Material sailMat;
  public static Piece woodFloorPiece;
  public static WearNTear woodFloorPieceWearNTear;
  public static GameObject vanillaRaftPrefab;
  public static GameObject vikingShipPrefab;

  public void Init(AssetBundle assetBundle)
  {
    boarding_ramp =
      assetBundle.LoadAsset<GameObject>("Assets/boarding_ramp.prefab");
    ship_hull =
      assetBundle.LoadAsset<GameObject>("Assets/ship_hull.prefab");
    steering_wheel =
      assetBundle.LoadAsset<GameObject>("Assets/steering_wheel.prefab");
    rope_ladder =
      assetBundle.LoadAsset<GameObject>("Assets/rope_ladder.prefab");
    rope_anchor =
      assetBundle.LoadAsset<GameObject>("Assets/rope_anchor.prefab");
    sprites =
      assetBundle.LoadAsset<SpriteAtlas>("Assets/icons.spriteatlas");
    sailMat = assetBundle.LoadAsset<Material>("Assets/SailMat.mat");
    dirtFloor = assetBundle.LoadAsset<GameObject>("dirt_floor.prefab");
    raftMast = vanillaRaftPrefab.transform.Find("ship/visual/mast").gameObject;
  }
}