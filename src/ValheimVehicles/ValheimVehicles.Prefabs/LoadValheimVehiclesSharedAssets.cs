using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;

namespace ValheimVehicles.Prefabs;

public class LoadValheimVehicleSharedAssets : ILoadAssets
{
  public static readonly LoadValheimVehicleSharedAssets Instance = new();
  public static SpriteAtlas Sprites = null!;

  public void Init(AssetBundle assetBundle)
  {
    Sprites = assetBundle.LoadAsset<SpriteAtlas>("icons.spriteatlasv2");
  }
}