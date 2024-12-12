using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Helpers;

public class ConvexHullGeneratorIntegration : ConvexHullAPI
{
  public new Material DebugMaterial =>
    LoadValheimVehicleAssets.DoubleSidedTransparentMat;

  public new string GeneratedMeshNamePrefix = PrefabNames.ConvexHull;

  public override bool IsAllowedAsHullOverride(string val) =>
    PrefabNames.IsHull(val);

  public void ConvexHullMeshGeneratorIntegration()
  {
    instance = this;
  }
}