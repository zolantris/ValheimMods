using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;

namespace ValheimVehicles.Prefabs;

public class LoadValheimVehicleSharedAssets : ILoadAssets
{
  public static readonly LoadValheimVehicleSharedAssets Instance = new();

  public void Init(AssetBundle assetBundle)
  {
    // init random
  }
}