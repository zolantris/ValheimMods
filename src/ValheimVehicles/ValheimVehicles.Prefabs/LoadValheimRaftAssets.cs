using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs;

public class LoadValheimRaftAssets : ILoadAssets
{
  public static readonly LoadValheimRaftAssets Instance = new();

  public static SpriteAtlas sprites;
  public static GameObject boardingRampAsset;
  public static GameObject steeringWheel;
  public static GameObject ropeLadder;
  public static GameObject dirtFloor;
  public static Material sailMat;

  public static GameObject? rope_anchor;

  public void Init(AssetBundle assetBundle)
  {
    rope_anchor =
      assetBundle.LoadAsset<GameObject>("rope_anchor.prefab");
    boardingRampAsset =
      assetBundle.LoadAsset<GameObject>("boarding_ramp.prefab");
    steeringWheel =
      assetBundle.LoadAsset<GameObject>("steering_wheel.prefab");
    ropeLadder =
      assetBundle.LoadAsset<GameObject>("rope_ladder.prefab");
    sprites =
      assetBundle.LoadAsset<SpriteAtlas>("icons.spriteatlas");
    sailMat = assetBundle.LoadAsset<Material>("SailMat.mat");
    dirtFloor = assetBundle.LoadAsset<GameObject>("dirt_floor.prefab");
  }
}