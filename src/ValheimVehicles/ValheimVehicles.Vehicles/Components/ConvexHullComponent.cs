using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Vehicles.Components;

public class ConvexHullComponent : ConvexHullAPI
{
  public void Awake()
  {
    BubbleMaterial = LoadValheimVehicleAssets.DoubleSidedTransparentMat;
  }

  public VehicleMovementController? MovementController;

  public void FixedUpdate()
  {
    if (MovementController == null || convexHullPreviewMeshRendererItems.Count == 0) return;
    foreach (var meshRenderer in convexHullPreviewMeshRendererItems)
    {
      meshRenderer.material.SetFloat(MaxHeightShaderId, MovementController.ShipFloatationObj.LowestWaterHeight);
    }
  }

  public static void UpdatePropertiesForAllComponents()
  {
    foreach (var convexHullComponent in Instances.ToList())
    {
      convexHullComponent.UpdatePropertiesForConvexHulls( PhysicsConfig.convexHullPreviewOffset.Value,
        GetConvexHullModeFromFlags(), PhysicsConfig.convexHullDebuggerColor
          .Value, WaterConfig.UnderwaterBubbleEffectColor.Value);
    }
  }
  
  public static ConvexHullAPI.PreviewModes GetConvexHullModeFromFlags()
  {
    return PhysicsConfig.convexHullDebuggerForceEnabled.Value
      ?
      ConvexHullAPI.PreviewModes.Debug
      : WaterConfig.HasUnderwaterHullBubbleEffect.Value
        ? ConvexHullAPI.PreviewModes.Bubble
        : ConvexHullAPI.PreviewModes.None;
  }
  
  public override bool IsAllowedAsHullOverride(string val) =>
    PrefabNames.IsHull(val);
}