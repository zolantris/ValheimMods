using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;

namespace ValheimVehicles.Prefabs;

public class LoadValheimVehicleSharedAssets : ILoadAssets
{
  public static readonly LoadValheimVehicleSharedAssets Instance = new();
  public static SpriteAtlas SharedSprites = null!;

  public void Init(AssetBundle assetBundle)
  {
    SharedSprites = assetBundle.LoadAsset<SpriteAtlas>("shared_icons.spriteatlasv2");
  }
}