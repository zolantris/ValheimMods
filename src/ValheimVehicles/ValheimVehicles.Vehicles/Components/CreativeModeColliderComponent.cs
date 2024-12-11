using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Vehicles.Components;

public class CreativeModeColliderComponent : MonoBehaviour
{
  public BoxCollider collider;
  private static List<CreativeModeColliderComponent> Instances = new();
  public static bool IsEditMode = false;
  private static Material? _cubeMaskMaterial;

  public Material CubeMaskMaterial => GetCubeMaskMaterial();

  public Material GetCubeMaskMaterial()
  {
    if (_cubeMaskMaterial == null)
    {
      _cubeMaskMaterial = LoadValheimVehicleAssets.TransparentDepthMaskMaterial;
    }

    return _cubeMaskMaterial;
  }

  internal void Awake()
  {
    if (ZNetView.m_forceDisableInit)
    {
      return;
    }

    Instances.Add(this);
    SetMode(IsEditMode);
    collider = GetComponent<BoxCollider>();
  }

  internal void OnDestroy()
  {
    Instances.Remove(this);
  }

  /// <summary>
  /// Enables the box collider which allows for editing the watermask, otherwise the user will not be able to interact with box/delete it.
  /// </summary>
  /// <param name="val"></param>
  public void SetMode(bool val)
  {
    if (collider == null) return;
    collider.excludeLayers = LayerHelpers.PhysicalLayers;
  }

  public virtual void OnToggleEditMode()
  {
  }

  public static void ToggleEditMode()
  {
    IsEditMode = !IsEditMode;
    foreach (var creativeModeColliderComponent in Instances)
    {
      creativeModeColliderComponent.SetMode(IsEditMode);
      creativeModeColliderComponent.OnToggleEditMode();
    }
  }
}