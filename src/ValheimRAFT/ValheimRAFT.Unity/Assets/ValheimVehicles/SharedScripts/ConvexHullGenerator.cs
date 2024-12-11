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
  public class ConvexHullMeshGenerator : MonoBehaviour
  {
    public float distanceThreshold = 0.2f;
    public Vector3 transformPreviewOffset = new(0, -2f, 0);
    public List<GameObject> GeneratedMeshGameObjects = new();

    public bool hasFixedUpdate = true;

    public bool useWorld;

    // Convex hull calculator instance
    private readonly ConvexHullCalculator
      convexHullCalculator = new();

    private float lastUpdate;

    private void Awake()
    {
      SyncDebugConfig();
      Cleanup();
    }

    private void Start()
    {
      Cleanup();
      ConvexHullMeshGeneratorAPI.GenerateMeshesFromChildColliders(
        transform.root.gameObject,
        GeneratedMeshGameObjects, distanceThreshold);
    }

    /// <summary>
    ///   For seeing the colliders update live. This should not be used in game for
    ///   performance reasons.
    /// </summary>
    public void FixedUpdate()
    {
      if (useWorld != ConvexHullMeshGeneratorAPI.UseWorld)
        ConvexHullMeshGeneratorAPI.UseWorld = useWorld;

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

      ConvexHullMeshGeneratorAPI.GenerateMeshesFromChildColliders(
        transform.root.gameObject,
        GeneratedMeshGameObjects, distanceThreshold);
    }

    public void OnEnable()
    {
      ConvexHullMeshGeneratorAPI.GenerateMeshesFromChildColliders(
        transform.root.gameObject,
        GeneratedMeshGameObjects, distanceThreshold);
    }

    public void OnDisable()
    {
      Cleanup();
    }

    [Conditional("UNITY_EDITOR")]
    private void SyncDebugConfig()
    {
      // overrides this method allowing all strings.
      ConvexHullMeshGeneratorAPI.IsAllowedAsHullOverride = s => true;

      ConvexHullMeshGeneratorAPI.transformPreviewOffset =
        transformPreviewOffset;
      ConvexHullMeshGeneratorAPI.DebugMode = true;
    }

    public void Cleanup()
    {
      ConvexHullMeshGeneratorAPI.DeleteMeshesFromChildColliders(
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
        if (obj.name.StartsWith(ConvexHullMeshGeneratorAPI
              .GeneratedMeshNamePrefix))
        {
          Destroy(obj);
          Debug.Log($"Deleted GameObject: {obj.name}");
        }
    }

    [UsedImplicitly]
    public void TriggerBuildConvexHullFromColliders()
    {
      ConvexHullMeshGeneratorAPI.GenerateMeshesFromChildColliders(
        transform.root.gameObject,
        GeneratedMeshGameObjects, distanceThreshold);
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
      ConvexHullMeshGeneratorAPI.GenerateMeshesFromChildColliders(
        transform.root.gameObject,
        GeneratedMeshGameObjects, distanceThreshold);
    }

    // Test the method with a sample list of points
    [ContextMenu("Generate Mesh From Points")]
    private void TestGenerateMeshFromPoint()
    {
      var points = new Vector3[]
      {
        new(453.0362f, 29.7599f, 5362.737f),
        new(453.0362f, 29.7599f, 5370.771f),
        new(453.0362f, 30.39231f, 5362.737f),
        new(453.0362f, 30.39231f, 5370.771f),
        new(457.0362f, 29.7599f, 5362.737f),
        new(457.0362f, 29.7599f, 5370.771f),
        new(457.0362f, 30.39231f, 5362.737f),
        new(457.0362f, 30.39231f, 5370.771f),
        new(454.9006f, 29.9016f, 5365.494f),
        new(454.9006f, 29.9016f, 5369.494f),
        new(454.9006f, 30.4016f, 5365.494f),
        new(454.9006f, 30.4016f, 5369.494f),
        new(458.9006f, 29.9016f, 5365.494f),
        new(458.9006f, 29.9016f, 5369.494f),
        new(458.9006f, 30.4016f, 5365.494f),
        new(458.9006f, 30.4016f, 5369.494f),
        new(451.2051f, 29.883f, 5363.963f),
        new(451.2051f, 29.883f, 5367.963f),
        new(451.2051f, 30.383f, 5363.963f),
        new(451.2051f, 30.383f, 5367.963f),
        new(455.2051f, 29.883f, 5363.963f),
        new(455.2051f, 29.883f, 5367.963f),
        new(455.2051f, 30.383f, 5363.963f),
        new(455.2051f, 30.383f, 5367.963f),
        new(447.5082f, 30.1555f, 5362.463f),
        new(447.5082f, 30.1555f, 5366.415f),
        new(447.5082f, 30.38369f, 5362.463f),
        new(447.5082f, 30.38369f, 5366.415f),
        new(451.5243f, 30.1555f, 5362.463f),
        new(451.5243f, 30.1555f, 5366.415f),
        new(451.5243f, 30.38369f, 5362.463f),
        new(451.5243f, 30.38369f, 5366.415f),
        new(454.2187f, 30.39941f, 5368.808f),
        new(454.2187f, 30.39941f, 5369.107f),
        new(454.2187f, 32.39941f, 5368.808f),
        new(454.2187f, 32.39941f, 5369.107f),
        new(456.2187f, 30.39941f, 5368.808f),
        new(456.2187f, 30.39941f, 5369.107f),
        new(456.2187f, 32.39941f, 5368.808f),
        new(456.2187f, 32.39941f, 5369.107f),
        new(456.0665f, 30.40871f, 5369.573f),
        new(456.0665f, 30.40871f, 5369.873f),
        new(456.0665f, 32.40871f, 5369.573f),
        new(456.0665f, 32.40871f, 5369.873f),
        new(458.0665f, 30.40871f, 5369.573f),
        new(458.0665f, 30.40871f, 5369.873f),
        new(458.0665f, 32.40871f, 5369.573f),
        new(458.0665f, 32.40871f, 5369.873f),
        new(457.5971f, 30.40385f, 5365.877f),
        new(457.5971f, 30.40385f, 5366.177f),
        new(457.5971f, 32.40385f, 5365.877f),
        new(457.5971f, 32.40385f, 5366.177f),
        new(459.5971f, 30.40385f, 5365.877f),
        new(459.5971f, 30.40385f, 5366.177f),
        new(459.5971f, 32.40385f, 5365.877f),
        new(459.5971f, 32.40385f, 5366.177f),
        new(457.373f, 30.41214f, 5369.032f),
        new(457.373f, 30.41214f, 5369.332f),
        new(457.373f, 32.41214f, 5369.032f),
        new(457.373f, 32.41214f, 5369.332f),
        new(459.373f, 30.41214f, 5369.032f),
        new(459.373f, 30.41214f, 5369.332f),
        new(459.373f, 32.41214f, 5369.032f),
        new(459.373f, 32.41214f, 5369.332f),
        new(458.1383f, 30.40972f, 5367.184f),
        new(458.1383f, 30.40972f, 5367.483f),
        new(458.1383f, 32.40972f, 5367.184f),
        new(458.1383f, 32.40972f, 5367.483f),
        new(460.1383f, 30.40972f, 5367.184f),
        new(460.1383f, 30.40972f, 5367.483f),
        new(460.1383f, 32.40972f, 5367.184f),
        new(460.1383f, 32.40972f, 5367.483f),
        new(447.2122f, 30.36102f, 5363.974f),
        new(447.2122f, 30.36102f, 5365.974f),
        new(447.2122f, 31.36102f, 5363.974f),
        new(447.2122f, 31.36102f, 5365.974f),
        new(449.2122f, 30.36102f, 5363.974f),
        new(449.2122f, 30.36102f, 5365.974f),
        new(449.2122f, 31.36102f, 5363.974f),
        new(449.2122f, 31.36102f, 5365.974f),
        new(457.3654f, 32.41212f, 5369.026f),
        new(457.3654f, 32.41212f, 5369.326f),
        new(457.3654f, 34.41212f, 5369.026f),
        new(457.3654f, 34.41212f, 5369.326f),
        new(459.3654f, 32.41212f, 5369.026f),
        new(459.3654f, 32.41212f, 5369.326f),
        new(459.3654f, 34.41212f, 5369.026f),
        new(459.3654f, 34.41212f, 5369.326f),
        new(458.1307f, 32.40969f, 5367.178f),
        new(458.1307f, 32.40969f, 5367.478f),
        new(458.1307f, 34.40969f, 5367.178f),
        new(458.1307f, 34.40969f, 5367.478f),
        new(460.1307f, 32.40969f, 5367.178f),
        new(460.1307f, 32.40969f, 5367.478f),
        new(460.1307f, 34.40969f, 5367.178f),
        new(460.1307f, 34.40969f, 5367.478f),
        new(448.3583f, 30.35737f, 5362.051f),
        new(448.3583f, 30.35737f, 5362.351f),
        new(448.3583f, 32.35737f, 5362.051f),
        new(448.3583f, 32.35737f, 5362.351f),
        new(450.3583f, 30.35737f, 5362.051f),
        new(450.3583f, 30.35737f, 5362.351f),
        new(450.3583f, 32.35737f, 5362.051f),
        new(450.3583f, 32.35737f, 5362.351f),
        new(446.2864f, 30.35637f, 5364.44f),
        new(446.2864f, 30.35637f, 5364.74f),
        new(446.2864f, 32.35637f, 5364.44f),
        new(446.2864f, 32.35637f, 5364.74f),
        new(448.2864f, 30.35637f, 5364.44f),
        new(448.2864f, 30.35637f, 5364.74f),
        new(448.2864f, 32.35637f, 5364.44f),
        new(448.2864f, 32.35637f, 5364.74f),
        new(447.0518f, 30.35394f, 5362.592f),
        new(447.0518f, 30.35394f, 5362.892f),
        new(447.0518f, 32.35394f, 5362.592f),
        new(447.0518f, 32.35394f, 5362.892f),
        new(449.0518f, 30.35394f, 5362.592f),
        new(449.0518f, 30.35394f, 5362.892f),
        new(449.0518f, 32.35394f, 5362.592f),
        new(449.0518f, 32.35394f, 5362.892f),
        new(448.3506f, 32.35735f, 5362.045f),
        new(448.3506f, 32.35735f, 5362.345f),
        new(448.3506f, 34.35735f, 5362.045f),
        new(448.3506f, 34.35735f, 5362.345f),
        new(450.3506f, 32.35735f, 5362.045f),
        new(450.3506f, 32.35735f, 5362.345f),
        new(450.3506f, 34.35735f, 5362.045f),
        new(450.3506f, 34.35735f, 5362.345f)
      }.ToList();

      ConvexHullMeshGeneratorAPI.GenerateConvexHullMesh(points,
        GeneratedMeshGameObjects, transform);
    }
  }
}