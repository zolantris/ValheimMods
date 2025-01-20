using System.Linq;
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

  public override bool IsAllowedAsHullOverride(string val)
  {
    return PrefabNames.IsHull(val);
  }
}