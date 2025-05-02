using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.Config;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Structs;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Components;

public class CustomMeshCreatorComponent : MonoBehaviour
{
  private bool canRunInit = false;

  public enum MeshCreatorTypeEnum
  {
    WaterMask
  }

  MeshRenderer meshRenderer;

  private static readonly Dictionary<MeshCreatorTypeEnum,
      List<CustomMeshCreatorComponent>>
    Instances = [];

  [FormerlySerializedAs("meshCreatorType")]
  public MeshCreatorTypeEnum
    selectedCreatorType = MeshCreatorTypeEnum.WaterMask;

  public void SetCreatorType(MeshCreatorTypeEnum mCreatorType)
  {
    selectedCreatorType = mCreatorType;
  }

  public GameObject GetMeshPrefab() => selectedCreatorType switch
  {
    MeshCreatorTypeEnum.WaterMask => PrefabManager.Instance.GetPrefab(
      PrefabNames.CustomWaterMask),
    _ => throw new ArgumentOutOfRangeException(nameof(selectedCreatorType),
      selectedCreatorType, null)
  };

  public int GetRequiredNumber() => selectedCreatorType switch
  {
    MeshCreatorTypeEnum.WaterMask => 8,
    _ => throw new ArgumentOutOfRangeException()
  };

  public static void DestroyAll(MeshCreatorTypeEnum cType)
  {
    if (!Instances.TryGetValue(cType, out var instance)) return;

    foreach (var customMeshCreatorComponent in instance.ToList())
    {
      customMeshCreatorComponent.DestroySelf();
    }

    instance.Clear();
  }

  public void DestroySelf()
  {
    if (Instances.TryGetValue(selectedCreatorType, out var instance))
    {
      instance.Remove(this);
    }

    Destroy(gameObject);
  }


  public bool IsReadyToCreateMesh(List<CustomMeshCreatorComponent> list)
  {
    var expectedCount = GetRequiredNumber();

    // NREs meaning that one of the items was deleted but not by the player, likely Zone removed it.
    if (list.ToList().Any(sailCreator => sailCreator == null))
    {
      DestroyAll(selectedCreatorType);
      return false;
    }

    if (list.Count == expectedCount)
    {
      return true;
    }


    // early exit, we just add the component
    if (list.Count < expectedCount)
    {
      return false;
    }

    // should never exceed expected count
    if (list.Count > expectedCount)
    {
      DestroyAll(selectedCreatorType);
      return false;
    }

    return false;
  }

  private void Awake()
  {
    if (ZNetView.m_forceDisableInit)
    {
      canRunInit = false;
      return;
    }

    transform.rotation = Quaternion.identity;

    meshRenderer = GetComponent<MeshRenderer>();

    canRunInit = true;
  }

  /// <summary>
  /// Must call in start, as value assignment will not work in Awake
  /// </summary>
  public void Start()
  {
    transform.rotation = Quaternion.identity;
    if (ZNetView.m_forceDisableInit || !canRunInit)
    {
      return;
    }

    if (!Instances.TryGetValue(selectedCreatorType, out var activeMeshList))
    {
      Instances.Add(selectedCreatorType, [this]);
      return;
    }

    activeMeshList.Add(this);

    if (!IsReadyToCreateMesh(activeMeshList))
    {
      return;
    }

    CreateMeshComponent();
  }


  public void OnDestroy()
  {
    DestroySelf();
  }

  private void CreateMeshComponent()
  {
    switch (selectedCreatorType)
    {
      case MeshCreatorTypeEnum.WaterMask:
        CreateCustomWaterMask();
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  public Quaternion GetRotationFromBounds(Bounds bounds)
  {
    var center = bounds.center;
    var extents = bounds.extents;

    var corners = new Vector3[8];
    corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
    corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
    corners[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
    corners[3] = center + new Vector3(extents.x, extents.y, -extents.z);
    corners[4] = center + new Vector3(-extents.x, -extents.y, extents.z);
    corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
    corners[6] = center + new Vector3(-extents.x, extents.y, extents.z);
    corners[7] = center + new Vector3(extents.x, extents.y, extents.z);

    // You can then determine the rotation based on the corner points
    // For example, to find the rotation from a reference axis
    var forward = (corners[1] - corners[0]).normalized; // Forward direction
    var right = (corners[2] - corners[0]).normalized; // Right direction
    var up = Vector3.Cross(forward, right).normalized; // Up direction

    // Create the rotation from these vectors
    var rotation = Quaternion.LookRotation(forward, up);

    return rotation;
  }

  public Quaternion RotationOffset = Quaternion.Euler(0, 0, 0);

  /// <summary>
  /// Generates a WaterDisplacementPrefab which can be edited / deleted in creative mode only.
  /// - Prefab will fit a square within all points provided.
  /// - The square will match max height and width of the bounds.
  /// </summary>
  public void CreateCustomWaterMask()
  {
    if (!Instances.TryGetValue(selectedCreatorType, out var activeMeshList))
      return;
    // must be from the first coordinate otherwise bounds would start from zero and be off
    transform.rotation = Quaternion.identity;
    var localBounds = new Bounds(activeMeshList[0].transform.localPosition,
      Vector3.zero);
    foreach (var customMeshCreatorComponent in activeMeshList.Skip(0).ToArray())
    {
      localBounds.Encapsulate(
        customMeshCreatorComponent.transform.localPosition);
    }

    var prefabToCreate = GetMeshPrefab();
    if (prefabToCreate == null) return;


    // If on the vehicle it will need to have the parent's rotation, defaults to zero rotation.
    var rotation = transform.parent
      ? transform.parent.rotation
      : Quaternion.identity;

    var deltaLocalPositionToCenter =
      localBounds.center - transform.localPosition;

    var meshPosition = transform.position + deltaLocalPositionToCenter;

    Logger.LogDebug(
      $"Creating water mask at {localBounds.center} size: {localBounds.size}");
    var meshComponent = Instantiate(prefabToCreate, meshPosition,
      rotation);

    if (transform.parent != null)
    {
      meshComponent.transform.SetParent(transform.parent);
      meshComponent.transform.localPosition = localBounds.center;
    }


    Logger.LogDebug(
      $"Created: water mask position: {meshComponent.transform.position} rotation: {meshComponent.transform.rotation}");

    try
    {
      var netView = meshComponent.GetComponent<ZNetView>();
      var zdo = netView.GetZDO();
      zdo.SetPosition(transform.position);
      zdo.SetRotation(transform.rotation);
      zdo.Set(VehicleZdoVars.CustomMeshId, (int)selectedCreatorType);
      zdo.Set(VehicleZdoVars.CustomMeshScale, localBounds.size);
      zdo.Set(VehicleZdoVars.CustomMeshPrimitiveType,
        (int)WaterConfig.DEBUG_WaterDisplacementMeshPrimitive.Value);

      AddToVehicle(netView);
    }
    catch
    {
      Logger.LogError("error creating Mesh");
    }

    DestroyAll(selectedCreatorType);
  }

  /**
   * <description/> Delegates to the VehicleController that it is placed within.
   * - This avoids the additional check if possible.
   */
  public bool AddToVehicle(ZNetView netView)
  {
    var baseVehicle = GetComponentInParent<VehiclePiecesController>();
    if ((bool)baseVehicle)
    {
      baseVehicle.AddNewPiece(netView);
      return true;
    }

    return false;
  }
}