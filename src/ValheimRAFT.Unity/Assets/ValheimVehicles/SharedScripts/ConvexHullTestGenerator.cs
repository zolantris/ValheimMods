#region

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody

// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  /// <summary>
  ///   both unity and valheim so this can be tested easily. This file is meant to be
  ///   run inside unity.
  /// </summary>
  // [ExecuteInEditMode]
  public class ConvexHullTestGenerator : MonoBehaviour
  {
    public float distanceThreshold = 0.2f;
    public Vector3 transformPreviewOffset = new(0, -2f, 0);
    public List<GameObject> GeneratedMeshGameObjects = new();

    [Header("FixedUpdate Logic")]
    public bool HasResyncFixedUpdate = true;
    public bool HasTestPieceGeneration;
    public float DebouncedUpdateInterval = 2f;
    public float PieceGeneratorInterval = 0.05f;
    public int maxPiecesToAdd = 50;
    public int BatchAddSize = 100;
    public int MaxXGeneration = 8;
    public int MaxZGeneration = 20;
    public int MaxYGeneration = 20;

    [Header("Mesh Generation Logic")]
    public bool useWorld;
    public GameObject convexHullParentGameObject;
    public GameObject PiecesParentObj;
    public bool debugOriginalMesh;

    public Transform forwardTransform;

    public ConvexHullAPI _convexHullAPI;
    public VehicleWheelController vehicleWheelController;
    public MovementPiecesController movementPiecesController;

    public float centerOfMassOffset = -4f;

    public Transform cameraTransform;

    public bool hasCalledFirstGenerate;

    public GameObject prefabFloorPiece;
    public GameObject prefabWallPiece;

    public Vector3 lastPieceOffset = Vector3.zero;


    private Bounds _cachedDebugBounds = new(Vector3.zero, Vector3.zero);
    private MeshBoundsVisualizer _meshBoundsVisualizer;

    private Coroutine AddPieceRoutine;

    private bool CanRunGenerate;

    private float lastPieceGeneratorUpdate;
    private float lastUpdate;

    private Vector3 startPosition;
    private int xOffset;
    private int yOffset;

    private int zOffset;
    private void Awake()
    {
      GetOrAddMeshGeneratorApi();
      Cleanup();

      if (vehicleWheelController != null)
      {
        if (vehicleWheelController.wheelParent == null)
        {
          vehicleWheelController.wheelParent = transform.Find("vehicle_land/wheels");
        }
      }

      if (!cameraTransform)
      {
        cameraTransform = transform.Find("camera");
      }

      startPosition = vehicleWheelController.transform.root.position;

      _convexHullAPI.AddLocalPhysicMaterial(0.01f, 0.01f);

      vehicleWheelController.SetBrake(false);
      // EnableCollisionBetweenLayers(10, 28);
      // EnableCollisionBetweenLayers(28, 10);
      //
      // EnableCollisionBetweenLayers(10, 29);
      // EnableCollisionBetweenLayers(29, 10);
    }

    private void Start()
    {
      SyncAPIProperties();
      Cleanup();
    }
    /// <summary>
    ///   For seeing the colliders update live. This should not be used in game for
    ///   performance reasons.
    /// </summary>
    public void FixedUpdate()
    {
      CanRunGenerate = RunFixedUpdateDebounce();
      var CanUpdatePieceGenerator = RunFixedUpdateDebounceGenerator();
      SyncAPIProperties();

      // clamping offset of center of mass to prevent issues
      UpdateCenterOfMass();

      if (vehicleWheelController.transform.position.y < 0f)
      {
        vehicleWheelController.vehicleRootBody.MovePosition(startPosition);
        vehicleWheelController.vehicleRootBody.velocity = Vector3.zero;
        vehicleWheelController.vehicleRootBody.angularVelocity = Vector3.zero;
      }

      if (cameraTransform)
      {
        var wheelControllerTransform = vehicleWheelController.transform;
        cameraTransform.position = wheelControllerTransform.position + Vector3.up * 5f;
        cameraTransform.localRotation = wheelControllerTransform.localRotation;
      }

      if (HasTestPieceGeneration && CanUpdatePieceGenerator)
      {
        if (AddPieceRoutine != null)
        {
          StopCoroutine(AddPieceRoutine);
        }

        AddPieceRoutine = StartCoroutine(TestAddPieceToVehicleChild());
      }

      if (!hasCalledFirstGenerate || HasResyncFixedUpdate && CanRunGenerate)
      {
        Generate();
        hasCalledFirstGenerate = true;
      }
    }

    public void OnEnable()
    {
      // Generate();
    }

    public void OnDisable()
    {
      Cleanup();
    }


    private void OnDrawGizmos()
    {
      Gizmos.color = Color.green;
      if (_convexHullAPI == null || _convexHullAPI.convexHullMeshes == null) return;
      foreach (var convexHullMesh in _convexHullAPI.convexHullMeshes)
      {
        if (convexHullMesh == null) continue;

        var meshCollider = convexHullMesh.GetComponent<MeshCollider>();
        // EncapsulateMeshCollider(meshCollider, sphereEncapsulationBuffer,
        //   out var center,
        //   out var radius);
        // Gizmos.color = Color.green;
        // Gizmos.DrawWireSphere(center, radius);

        if (meshCollider != null && meshCollider.sharedMesh != null)
          // Calculate bounds
          _meshBoundsVisualizer.CalculateAndVisualizeBounds(meshCollider,
            forwardTransform);
      }
    }

    public void UpdateCenterOfMass()
    {
      var hullBounds = _convexHullAPI.GetConvexHullBounds(true);
      var offset = Mathf.Lerp(hullBounds.extents.y, -hullBounds.extents.y, centerOfMassOffset);
      var localCenterOfMassOffset = Mathf.Min(offset, -5f);

      PhysicUtils.UpdateRelativeCenterOfMass(vehicleWheelController.vehicleRootBody, localCenterOfMassOffset);
    }

    public bool RunFixedUpdateDebounce()
    {
      if (lastUpdate > DebouncedUpdateInterval)
      {
        lastUpdate = 0f;
        return true;
      }
      lastUpdate += Time.deltaTime;
      return false;
    }
    public bool RunFixedUpdateDebounceGenerator()
    {
      if (lastPieceGeneratorUpdate > PieceGeneratorInterval)
      {
        lastPieceGeneratorUpdate = 0f;
        return true;
      }
      lastPieceGeneratorUpdate += Time.deltaTime;
      return false;
    }

    private void GetOrAddMeshGeneratorApi()
    {
      _meshBoundsVisualizer = GetComponent<MeshBoundsVisualizer>();
      ConvexHullAPI.BubbleMaterialColor = new Color(0, 1f, 1f, 0.8f);
      ConvexHullAPI.DebugMaterialColor = new Color(1f, 1f, 0f, 0.8f);
      SyncAPIProperties();
    }

    private void SyncAPIProperties()
    {
      // static setters
      ConvexHullAPI.UseWorld = useWorld;

      if (_convexHullAPI == null) return;
      // _convexHullAPI.PreviewMode = ConvexHullAPI.PreviewModes.Debug;

      // local setters
      _convexHullAPI.transformPreviewOffset =
        transformPreviewOffset;

      // for physics this should be pieceParent, but then we need to ignore it when iterating
      _convexHullAPI.PreviewParent = PiecesParentObj.transform;
    }


    private void OnScriptHotReload()
    {
      //do whatever you want to do with access to instance via 'this'
      Generate();
    }

    /// <summary>
    ///   For GameObjects
    /// </summary>
    private void Generate()
    {
      var childGameObjects =
        ConvexHullAPI.GetAllChildGameObjects(PiecesParentObj).Where(go =>
          !go.name.Contains(ConvexHullAPI.MeshNamePrefix) &&
          !go.name.StartsWith("VehicleShip") && go.activeInHierarchy);

      // VIP confirm that rotation of roots are aligned otherwise bail due to problems with rigidbodies not matching and requiring lots of complicated transforms around pivots to align points
      if (PiecesParentObj.transform.root.rotation != vehicleWheelController.transform.root.rotation)
      {
        Debug.LogWarning("PiecesParentObj.transform.root.rotation != vehicleWheelController.transform.root.rotation force aligning them in worldspace before this causes big issues with transforms all needing to be re-aligned. ");
        PiecesParentObj.transform.root.rotation = vehicleWheelController.transform.root.rotation;
      }

      _convexHullAPI.GenerateMeshesFromChildColliders(
        convexHullParentGameObject, convexHullParentGameObject.transform.position - PiecesParentObj.GetComponent<Rigidbody>().worldCenterOfMass,
        distanceThreshold, childGameObjects.ToList());

      if (vehicleWheelController && convexHullParentGameObject != null)
      {
        var bounds = _convexHullAPI.GetConvexHullBounds(true);
        _cachedDebugBounds = bounds;
        vehicleWheelController.Initialize(bounds);
      }

      // RigidbodyUtils.RecenterRigidbodyPivot(PiecesParentObj.gameObject);
      // RigidbodyUtils.RecenterRigidbodyPivot(vehicleWheelController.gameObject);
    }
    //
    // public void IgnoreAllCollidersBetweenWheelsAndPieces()
    // {
    //   var childColliders = PhysicsHelpers.GetAllChildColliders(transform);
    //   PhysicsHelpers.IgnoreCollidersForLists(vehicleWheelController.colliders, childColliders);
    // }
    //
    // public void IgnoreAllCollidersBetweenTreadsAndPieces()
    // {
    //   var childColliders = PhysicsHelpers.GetAllChildColliders(transform);
    //   PhysicsHelpers.IgnoreCollidersForLists(vehicleWheelController.colliders, childColliders);
    // }

    private void DrawSphere(Vector3 center, float radius)
    {
      // Visualize the sphere in the Scene view
      Gizmos.color = Color.green;
      Gizmos.DrawWireSphere(center, radius);
    }
    /// <summary>
    /// Todo to detect meshRenderBounds and get the offset from there.
    /// </summary>
    /// <returns></returns>
    public Vector3? GetPieceOffset()
    {
      var pieceCount = movementPiecesController.vehiclePieces.Count;
      var currentVector = new Vector3(4 * xOffset, 4 * yOffset, 4 * zOffset);

      if (yOffset > MaxYGeneration)
      {
        return null;
      }

      if (zOffset > MaxZGeneration)
      {
        zOffset = 0;
        xOffset = 0;
        yOffset += 1;
      }

      if (xOffset > MaxXGeneration)
      {
        xOffset = 0;
        zOffset += 1;
      }
      else
      {
        xOffset += 1;
      }
      return currentVector;
    }

    private void InstantiatePrefab(GameObject prefab)
    {
      var localPosition = GetPieceOffset();
      if (localPosition == null) return;

      var piece = Instantiate(prefab, transform);
      piece.transform.localPosition = localPosition.Value;
      movementPiecesController.OnPieceAdded(piece);
    }

    private IEnumerator TestAddPieceToVehicleChild()
    {
      if (!prefabFloorPiece) yield break;
      if (movementPiecesController.vehiclePieces.Count > maxPiecesToAdd) yield break;
      var currentPieces = movementPiecesController.vehiclePieces.Count;

      var current = 0;
      while (BatchAddSize > current && movementPiecesController.vehiclePieces.Count < maxPiecesToAdd)
      {
        InstantiatePrefab(prefabFloorPiece);
        InstantiatePrefab(prefabWallPiece);
        current++;
      }

      if (currentPieces != movementPiecesController.vehiclePieces.Count)
      {
        Generate();
      }
    }

    public void Cleanup()
    {
      ConvexHullAPI.DeleteMeshesFromChildColliders(
        GeneratedMeshGameObjects);
      DeleteObjects();
    }

    // Method to delete all objects with the specified name in the scene
    public static void DeleteObjects()
    {
      // Find all GameObjects in the scene
      var allObjects = FindObjectsOfType<GameObject>();

      foreach (var obj in allObjects)
        // If the GameObject's name matches the target name, delete it
        if (obj.name.StartsWith(ConvexHullAPI.MeshNamePrefix))
        {
#if DEBUG
          // Debug.Log($"Deleted GameObject: {obj.name}");
#endif
          DebugUnityHelpers.AdaptiveDestroy(obj);
        }
    }

    [UsedImplicitly]
    public void TriggerBuildConvexHullFromColliders()
    {
      Generate();
    }

    [UsedImplicitly]
    public void TriggerDeleteConvexHullObjects()
    {
      if (GeneratedMeshGameObjects.Count > 0) Cleanup();
    }

    // Test the method with a sample list of points
    [ContextMenu("Generate Mesh")]
    private void TestGenerateMesh()
    {
      Generate();
    }

    // Test the method with a sample list of points
    // [ContextMenu("Generate Mesh From Points")]
    // private void TestGenerateMeshFromPoint()
    // {
    //   // copy & paste points here to see the result.
    //   var points = new Vector3[] {}.ToList();
    //
    //   _convexHullAPI.GenerateConvexHullMesh(points, transform);
    // }
  }
}