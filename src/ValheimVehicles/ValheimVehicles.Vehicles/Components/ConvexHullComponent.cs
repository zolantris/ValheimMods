using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Vehicles.Components;

public class ConvexHullComponent : ConvexHullAPI
{
  public override bool IsAllowedAsHullOverride(string val) =>
    PrefabNames.IsHull(val);
}