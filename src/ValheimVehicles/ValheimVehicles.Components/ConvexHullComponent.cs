using System;
using System.Linq;
using ValheimVehicles.Config;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Components;

public class ConvexHullComponent : ConvexHullAPI
{
  public override void Awake()
  {
    IsAllowedAsHullOverride = IsAllowedForConvexHullFn;
    BubbleMaterial = LoadValheimVehicleAssets.DoubleSidedTransparentMat;
    base.Awake();
  }

  public VehicleMovementController? MovementController;

  public void FixedUpdate()
  {
    if (MovementController == null ||
        convexHullPreviewMeshRendererItems.Count == 0) return;
    foreach (var meshRenderer in convexHullPreviewMeshRendererItems)
      meshRenderer.material.SetFloat(MaxHeightShaderId,
        MovementController.ShipFloatationObj.LowestWaterHeight - 1f);
  }

  public static void UpdatePropertiesForAllComponents()
  {
    foreach (var convexHullComponent in Instances.ToList())
      convexHullComponent.UpdatePropertiesForConvexHulls(
        PhysicsConfig.convexHullPreviewOffset.Value,
        GetConvexHullModeFromFlags(), PhysicsConfig.convexHullDebuggerColor
          .Value, WaterConfig.UnderwaterBubbleEffectColor.Value);
  }

  public static PreviewModes GetConvexHullModeFromFlags()
  {
    return PhysicsConfig.convexHullDebuggerForceEnabled.Value
      ? PreviewModes.Debug
      : WaterConfig.HasUnderwaterHullBubbleEffect.Value
        ? PreviewModes.Bubble
        : PreviewModes.None;
  }

  public bool IsAllowedForConvexHullFn(string val)
  {
    return PrefabNames.IsHull(val);
  }
}