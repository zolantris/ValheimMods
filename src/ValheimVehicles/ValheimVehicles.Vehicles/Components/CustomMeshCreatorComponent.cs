using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Vehicles.Components;

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
      Object.Destroy(customMeshCreatorComponent.meshRenderer);
      Object.Destroy(customMeshCreatorComponent);
    }

    instance.Clear();
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


    // early exit, we just add the component
    if (expectedCount > list.Count)
    {
      return false;
    }

    // should never exceed expected count
    if (expectedCount < list.Count)
    {
      DestroyAll(selectedCreatorType);
      return false;
    }

    return true;
  }

  private void Awake()
  {
    if (ZNetView.m_forceDisableInit)
    {
      canRunInit = false;
      return;
    }

    meshRenderer = GetComponent<MeshRenderer>();

    canRunInit = true;
  }

  /// <summary>
  /// Must call in start, as value assignment will not work in Awake
  /// </summary>
  public void Start()
  {
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
    DestroyAll(selectedCreatorType);
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

  // public byte[] VectorPointsToBytes(List<Vector3> list)
  // {
  //   byte[] byteArray = [];
  //   foreach (var vector3 in list)
  //   {
  //     byteArray.Append(vector3.toByte)
  //   }
  // }
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
    var bounds = new Bounds(activeMeshList[0].transform.position, Vector3.zero);
    foreach (var customMeshCreatorComponent in activeMeshList)
    {
      bounds.Encapsulate(customMeshCreatorComponent.transform.position);
    }

    var prefabToCreate = GetMeshPrefab();
    if (prefabToCreate == null) return;


    // Needs testing...
    // var rotation = GetRotationFromBounds(bounds);
    var rotation = transform.parent.rotation * RotationOffset;

    Logger.LogDebug(
      $"Creating water mask at {bounds.center} size: {bounds.size} rotation: {transform.parent.rotation.eulerAngles}");
    var meshComponent = Instantiate(prefabToCreate, bounds.center,
      rotation);

    Logger.LogDebug(
      $"Created: water mask position: {meshComponent.transform.position} rotation: {meshComponent.transform.rotation}");

    var netView = meshComponent.GetComponent<ZNetView>();
    var zdo = netView.GetZDO();
    zdo.Set(VehicleZdoVars.CustomMeshId, (int)selectedCreatorType);
    zdo.Set(VehicleZdoVars.CustomMeshScale, bounds.size);

    AddToVehicle(netView);

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