using UnityEngine;

namespace ValheimVehicles.Prefabs;

public class LoadValheimRaftAssets : ILoadAssets
{
  public static readonly LoadValheimRaftAssets Instance = new();

  public static GameObject boardingRampAsset;
  public static GameObject ropeLadder;
  public static GameObject dirtFloor;
  public static Material sailMat;
  public static Texture sailTextureNormal;
  public static Texture sailTexture;
  public static GameObject editPanel;
  public static GameObject editTexturePanel;

  public static GameObject? rope_anchor;

  public void Init(AssetBundle assetBundle)
  {
    editPanel = assetBundle.LoadAsset<GameObject>("edit_sail_panel");
    editTexturePanel = assetBundle.LoadAsset<GameObject>("edit_texture_panel");
    sailTextureNormal = assetBundle.LoadAsset<Texture>("sail_normal.png");
    sailTexture = assetBundle.LoadAsset<Texture>("sail.png");
    rope_anchor =
      assetBundle.LoadAsset<GameObject>("rope_anchor.prefab");
    boardingRampAsset =
      assetBundle.LoadAsset<GameObject>("boarding_ramp.prefab");
    ropeLadder =
      assetBundle.LoadAsset<GameObject>("rope_ladder.prefab");
    sailMat = assetBundle.LoadAsset<Material>("SailMat.mat");
    dirtFloor = assetBundle.LoadAsset<GameObject>("dirt_floor.prefab");
  }
}