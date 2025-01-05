using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ValheimVehicles.SharedScripts
{
  /// <summary>
  ///   both unity and valheim so this can be tested easily. This file is meant to be
  ///   run inside unity.
  /// </summary>
  [ExecuteInEditMode]
  public class ConvexHullTestGenerator : MonoBehaviour
  {
    public float distanceThreshold = 0.2f;
    public Vector3 transformPreviewOffset = new(0, -2f, 0);
    public List<GameObject> GeneratedMeshGameObjects = new();

    public bool hasFixedUpdate = true;

    public bool useWorld;

    public GameObject parentGameObject;

    private ConvexHullAPI _convexHullAPI;

    private float lastUpdate;
    public bool debugOriginalMesh = false;

    private void GetOrAddMeshGeneratorApi()
    {
      _convexHullAPI =
        gameObject.GetComponent<ConvexHullAPI>();

      if (_convexHullAPI == null)
      {
        _convexHullAPI =
          gameObject.AddComponent<ConvexHullAPI>();
      }

      SyncAPIProperties();
    }

    private void SyncAPIProperties()
    {
      // static setters
      ConvexHullAPI.UseWorld = useWorld;
      ConvexHullAPI.DebugMode = true;
      ConvexHullAPI.DebugOriginalMesh = debugOriginalMesh;

      if (_convexHullAPI == null) return;

      // local setters
      _convexHullAPI.transformPreviewOffset =
        transformPreviewOffset;
      _convexHullAPI.PreviewParent = transform;
    }

    private void Awake()
    {
      GetOrAddMeshGeneratorApi();
      Cleanup();
    }

    private void Start()
    {
      SyncAPIProperties();
      Cleanup();
      Generate();
    }

    /// <summary>
    ///   For GameObjects
    /// </summary>
    private void Generate()
    {
      var childGameObjects =
        ConvexHullAPI.GetAllChildGameObjects(gameObject);

      _convexHullAPI.GenerateMeshesFromChildColliders(
        parentGameObject,
        distanceThreshold, childGameObjects);
    }

    /// <summary>
    ///   For seeing the colliders update live. This should not be used in game for
    ///   performance reasons.
    /// </summary>
    public void FixedUpdate()
    {
      if (!hasFixedUpdate) return;
      if (lastUpdate > 0.2f)
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
          ConvexHullAPI.AdaptiveDestroy(obj);
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
      var points = new Vector3[] { }.ToList();

      _convexHullAPI.GenerateConvexHullMesh(points, transform);
    }
  }
}