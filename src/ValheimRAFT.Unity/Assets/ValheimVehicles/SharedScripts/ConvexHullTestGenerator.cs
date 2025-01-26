using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

    public bool hasFixedUpdate = true;

    public bool useWorld;

    public GameObject convexHullParentGameObject;
    public GameObject PiecesParentObj;
    public bool debugOriginalMesh;

    public Transform forwardTransform;

    public ConvexHullAPI _convexHullAPI;
    private MeshBoundsVisualizer _meshBoundsVisualizer;
    public VehicleWheelController vehicleWheelController;
    private float lastUpdate;
    private Bounds _cachedDebugBounds = new Bounds(Vector3.zero, Vector3.zero);

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

      EnableCollisionBetweenLayers(10, 28);
      EnableCollisionBetweenLayers(28, 10);

      EnableCollisionBetweenLayers(10, 29);
      EnableCollisionBetweenLayers(29, 10);
    }

    private void Start()
    {
      SyncAPIProperties();
      Cleanup();
      Generate();
    }

    public float DebouncedUpdateInterval = 2f;
    /// <summary>
    ///   For seeing the colliders update live. This should not be used in game for
    ///   performance reasons.
    /// </summary>
    public void FixedUpdate()
    {
      if (!hasFixedUpdate) return;
      if (lastUpdate > DebouncedUpdateInterval)
      {
        lastUpdate = 0f;
      }
      else
      {
        lastUpdate += Time.deltaTime;
        return;
      }

      SyncAPIProperties();

      Generate();
    }

    public void OnEnable()
    {
      Generate();
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

    public void EnableCollisionBetweenLayers(int layerA, int layerB)
    {
      Physics.IgnoreLayerCollision(layerA, layerB, false);
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

      _convexHullAPI.GenerateMeshesFromChildColliders(
        convexHullParentGameObject,
        distanceThreshold, childGameObjects.ToList());

      _convexHullAPI.UpdateConvexHullBounds();

      if (vehicleWheelController && convexHullParentGameObject != null)
      {
        var bounds = _convexHullAPI.GetConvexHullBounds(true);
        _cachedDebugBounds = bounds;
        vehicleWheelController.InitializeWheels(bounds);
      }
    }

    private void DrawSphere(Vector3 center, float radius)
    {
      // Visualize the sphere in the Scene view
      Gizmos.color = Color.green;
      Gizmos.DrawWireSphere(center, radius);
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
          Debug.Log($"Deleted GameObject: {obj.name}");
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
    [ContextMenu("Generate Mesh From Points")]
    private void TestGenerateMeshFromPoint()
    {
      // copy & paste points here to see the result.
      var points = new Vector3[] {}.ToList();

      _convexHullAPI.GenerateConvexHullMesh(points, transform);
    }
  }
}