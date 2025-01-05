using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ValheimVehicles.SharedScripts
{
  public class ConvexHullAPI : MonoBehaviour
  {
    public Vector3 transformPreviewOffset = Vector3.zero;

    // Convex hull calculator instance
    private static readonly ConvexHullCalculator
      convexHullCalculator = new();

    public static bool DebugMode = true;
    public static bool DebugOriginalMesh = false;

    public static bool HasInitialized = false;
    [CanBeNull] public static Material DebugMaterial;
    public static Color DebugMaterialColor = new(0, 1, 0, 0.5f);

    public static Action<string> LoggerAPI = Debug.Log;
    public static bool UseWorld = true;

    // todo move prefixes to unity so it can be shared. Possibly auto-generated too.
    public static string MeshNamePrefix = "ConvexHull";
    public static string MeshNamePreviewPrefix => $"{MeshNamePrefix}_Preview";
    public static bool ShouldOptimizeGeneratedMeshes = false;

    public Transform PreviewParent;

    /// <summary>
    /// A list of Convex Hull GameObjects. These gameobjects can be updated by the debug flags. 
    /// </summary>
    public List<GameObject> convexHullMeshes = new();

    /// <summary>
    /// List of Convex Hull Preview Meshes. These meshes must placed within a container that should not have a Rigidbody parent.
    /// </summary>
    public List<GameObject> convexHullPreviewMeshes = new();

    public static List<ConvexHullAPI> Instances = new();

    public static void InitializeConvexMeshGeneratorApi(bool debugMode,
      Material debugMaterial, Color debugMaterialColor, string meshNamePrefix,
      Action<string> loggerApi)
    {
      MeshNamePrefix = meshNamePrefix;
      LoggerAPI = loggerApi;
      DebugMode = debugMode;
      DebugMaterial = debugMaterial;
      DebugMaterialColor = debugMaterialColor;
    }

    private void Awake()
    {
      PreviewParent = transform;
    }

    private void OnDisable()
    {
      if (!Instances.Contains(this))
      {
        return;
      }

      Instances.Remove(this);
    }

    private void OnEnable()
    {
      if (Instances.Contains(this))
      {
        return;
      }

      Instances.Add(this);
    }

    /// <summary>
    /// Allows for additional overrides. This should be a function provided in any class extending ConvexHullAPI
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public virtual bool IsAllowedAsHullOverride(string val)
    {
      return false;
    }

    public static void UpdatePropertiesForConvexHulls(
      Vector3 transformPreviewOffset, bool IsDebug, Color color)
    {
      DebugMode = IsDebug;
      DebugMaterialColor = color;

      foreach (var convexHullAPI in Instances)
      {
        convexHullAPI.transformPreviewOffset = transformPreviewOffset;
        convexHullAPI.CreatePreviewConvexHullMeshes();
      }
    }

    public static List<Vector3> GetCapsuleColliderVertices(
      CapsuleCollider capsuleCollider)
    {
      var points = new List<Vector3>();

      // Get key capsule parameters
      var localCenter = capsuleCollider.center;
      var radius = capsuleCollider.radius;
      var height = capsuleCollider.height;
      var direction = capsuleCollider.direction;

      // Determine axis vectors based on direction
      Vector3 primaryAxis, secondaryAxis, tertiaryAxis;
      switch (direction)
      {
        case 0: // X-axis
          primaryAxis = Vector3.right;
          secondaryAxis = Vector3.up;
          tertiaryAxis = Vector3.forward;
          break;
        case 1: // Y-axis
          primaryAxis = Vector3.up;
          secondaryAxis = Vector3.forward;
          tertiaryAxis = Vector3.right;
          break;
        case 2: // Z-axis
          primaryAxis = Vector3.forward;
          secondaryAxis = Vector3.up;
          tertiaryAxis = Vector3.right;
          break;
        default:
          throw new ArgumentException("Invalid capsule direction");
      }

      // Precise height calculation
      var halfHeight = (height - 2 * radius) * 0.5f;

      // Generate points for a full sphere with additional cylinder section
      var horizontalSegments = 12;
      var verticalSegments = 6;

      // Generate sphere points for top and bottom caps
      for (var lat = 0; lat <= verticalSegments; lat++)
      {
        var polar = Mathf.PI * lat / verticalSegments;
        var sinPolar = Mathf.Sin(polar);
        var cosPolar = Mathf.Cos(polar);

        for (var lon = 0; lon <= horizontalSegments; lon++)
        {
          var azimuth = 2 * Mathf.PI * lon / horizontalSegments;
          var sinAzimuth = Mathf.Sin(azimuth);
          var cosAzimuth = Mathf.Cos(azimuth);

          // Calculate point on unit sphere
          var spherePoint = new Vector3(
            sinPolar * cosAzimuth,
            cosPolar,
            sinPolar * sinAzimuth
          );

          // Top and bottom cap points (considering radius and height)
          var topCapPoint = localCenter +
                            primaryAxis * (halfHeight + radius) +
                            spherePoint * radius;
          var bottomCapPoint = localCenter -
                               primaryAxis * (halfHeight + radius) +
                               spherePoint * radius;

          points.Add(topCapPoint);
          points.Add(bottomCapPoint);
        }
      }

      // Generate cylinder body points
      for (var i = 0; i <= horizontalSegments; i++)
      {
        var angle = 2 * Mathf.PI * i / horizontalSegments;
        var sinAngle = Mathf.Sin(angle);
        var cosAngle = Mathf.Cos(angle);

        // Points along the cylinder body
        var cylinderPoint = localCenter +
                            (secondaryAxis * sinAngle +
                             tertiaryAxis * cosAngle) * radius;

        // Adjust cylinder points with parent scaling
        var topCylinderPoint = cylinderPoint + primaryAxis * halfHeight;
        var bottomCylinderPoint = cylinderPoint - primaryAxis * halfHeight;

        // Add the points
        points.Add(topCylinderPoint);
        points.Add(bottomCylinderPoint);
      }

      // Transform points to world space, adapting for the object's scale
      for (var i = 0; i < points.Count; i++)
        // Adapt for the scaling of the parent object (capsuleCollider.transform.localScale)
        points[i] = capsuleCollider.transform.TransformPoint(points[i]);

      return points;
    }

    public static List<Vector3> GetMeshColliderVertices(
      MeshCollider meshCollider)
    {
      var transform = meshCollider.transform;
      var points = new List<Vector3>();
      var isReadable = meshCollider.sharedMesh.isReadable;
      var mesh = isReadable
        ? meshCollider.sharedMesh
        : CreateReadableMesh(meshCollider.sharedMesh);

      if (mesh != null)
        foreach (var vertex in mesh.vertices)
        {
          // Convert mesh vertex to world space
          var worldVertex = transform.TransformPoint(vertex);
          points.Add(worldVertex);
        }

      // immediately garbage collect the copy mesh to prevent memory problems.
      // if (!isReadable) AdaptiveDestroy(mesh);
      return points;
    }

    /// <summary>
    ///   Gets all local-space points of a collider, adjusted for the object's scale.
    /// </summary>
    /// <param name="collider">The collider to extract points from.</param>
    /// <returns>A list of Vector3 points in local space, scaled correctly.</returns>
    public static List<Vector3> GetColliderPointsLocal(Collider collider)
    {
      var points = new List<Vector3>();
      var scale = collider.transform.localScale; // Object's local scale

      if (collider is MeshCollider meshCollider &&
          meshCollider.sharedMesh != null)
      {
        // MeshCollider: Extract scaled local-space vertices
        foreach (var vertex in meshCollider.sharedMesh.vertices)
          points.Add(Vector3.Scale(vertex, scale));
      }
      else if (collider is BoxCollider boxCollider)
      {
        // BoxCollider: Calculate 8 corners in scaled local space
        var center = Vector3.Scale(boxCollider.center, scale);
        var size = Vector3.Scale(boxCollider.size * 0.5f, scale);

        var corners = new[]
        {
          center + new Vector3(-size.x, -size.y, -size.z),
          center + new Vector3(-size.x, -size.y, size.z),
          center + new Vector3(-size.x, size.y, -size.z),
          center + new Vector3(-size.x, size.y, size.z),
          center + new Vector3(size.x, -size.y, -size.z),
          center + new Vector3(size.x, -size.y, size.z),
          center + new Vector3(size.x, size.y, -size.z),
          center + new Vector3(size.x, size.y, size.z)
        };

        points.AddRange(corners);
      }
      else if (collider is SphereCollider sphereCollider)
      {
        // SphereCollider: Generate points on the sphere surface
        var center = Vector3.Scale(sphereCollider.center, scale);
        var radius = sphereCollider.radius *
                     Mathf.Max(scale.x, scale.y, scale.z);

        const int latitudeSegments = 10; // Number of latitudinal divisions
        const int longitudeSegments = 20; // Number of longitudinal divisions

        for (var lat = 0; lat <= latitudeSegments; lat++)
        {
          var theta = Mathf.PI * lat / latitudeSegments; // Latitude angle
          var sinTheta = Mathf.Sin(theta);
          var cosTheta = Mathf.Cos(theta);

          for (var lon = 0; lon <= longitudeSegments; lon++)
          {
            var phi = 2 * Mathf.PI * lon / longitudeSegments; // Longitude angle
            var x = radius * sinTheta * Mathf.Cos(phi);
            var y = radius * cosTheta;
            var z = radius * sinTheta * Mathf.Sin(phi);

            points.Add(center + new Vector3(x, y, z));
          }
        }
      }
      else if (collider is CapsuleCollider capsuleCollider)
      {
        points.AddRange(GetCapsuleColliderVertices(capsuleCollider));
      }
      else
      {
        Debug.LogWarning($"Unsupported collider type: {collider.GetType()}");
      }

      return points; // Local space, scaled
    }

    private static List<Vector3> GetSphereColliderPoints(
      SphereCollider sphereCollider)
    {
      var points = new List<Vector3>();
      var transform = sphereCollider.transform;
      var center = transform.position +
                   transform.rotation * sphereCollider.center;
      var lossyScale = transform.lossyScale;
      var scaledRadius = sphereCollider.radius * Mathf.Max(
        lossyScale.x, lossyScale.y,
        lossyScale.z);

      var resolution = 12;
      for (var i = 0; i < resolution; i++)
      {
        var theta = i * Mathf.PI * 2 / resolution;
        for (var j = 0; j <= resolution / 2; j++)
        {
          var phi = j * Mathf.PI / (resolution / 2);
          var localPoint = new Vector3(
            Mathf.Sin(phi) * Mathf.Cos(theta),
            Mathf.Sin(phi) * Mathf.Sin(theta),
            Mathf.Cos(phi)
          ) * scaledRadius;

          var worldPoint = center + localPoint;
          points.Add(worldPoint);
        }
      }

      return points;
    }

    private static List<Vector3> GetBoxColliderVertices(BoxCollider boxCollider)
    {
      var transform = boxCollider.transform;
      var points = new List<Vector3>();

      // Calculate the center of the box in world space
      var boxCenter = transform.TransformPoint(boxCollider.center);

      // Calculate the box size based on the boxCollider's size
      var boxSize = boxCollider.size;

      // Initialize a vector to store the final scale, starting with local scale of the collider
      var finalScale = Vector3.one;

      // Traverse the hierarchy and accumulate the scale from each parent
      var currentTransform = transform;
      while (currentTransform != null)
      {
        finalScale.Scale(currentTransform
          .localScale); // Apply the parent's scale
        currentTransform = currentTransform.parent;
      }

      // Apply the accumulated scale to the box size
      var scaledBoxSize = Vector3.Scale(boxSize, finalScale);

      // Loop through each corner of the box
      for (var x = -1; x <= 1; x += 2)
      for (var y = -1; y <= 1; y += 2)
      for (var z = -1; z <= 1; z += 2)
      {
        // Define the local corner point (relative to the center)
        var localCorner = new Vector3(x * scaledBoxSize.x, y * scaledBoxSize.y,
          z * scaledBoxSize.z) * 0.5f;

        // Transform the local corner to world space using the position, rotation, and scaling
        var worldCorner = boxCenter + transform.rotation * localCorner;

        // Add the world corner to the list of points
        points.Add(worldCorner);
      }

      return points;
    }


    private static List<Vector3> GetColliderPointsGlobal(Collider collider)
    {
      var points = new List<Vector3>();
      
      // filters out layer that should not be considered physical during generation
      if (!LayerHelpers.IsContainedWithinMask(collider.gameObject.layer,
            LayerHelpers.PhysicalLayers) ||
          !collider.gameObject.activeInHierarchy) return [];
      
      switch (collider)
      {
        // Handle BoxCollider
        case BoxCollider boxCollider:
          points.AddRange(GetBoxColliderVertices(boxCollider));
          break;
        // Handle SphereCollider
        case SphereCollider sphereCollider:
          points.AddRange(GetSphereColliderPoints(sphereCollider));
          break;
        // Handle CapsuleCollider
        case CapsuleCollider capsuleCollider:
          points.AddRange(GetCapsuleColliderVertices(capsuleCollider));
          break;
        // Handle MeshCollider
        case MeshCollider meshCollider:
          points.AddRange(GetMeshColliderVertices(meshCollider));
          break;
      }

      return points;
    }

    public static List<Vector3> ConvertToRelativeSpace(Collider collider,
      List<Vector3> localPoints)
    {
      var relativePoints = new List<Vector3>();
      foreach (var localPoint in localPoints)
        relativePoints.Add(localPoint + collider.transform.localPosition);

      return relativePoints;
    }

    public static List<Vector3> ConvertToWorldSpace(Collider collider,
      List<Vector3> localPoints)
    {
      var worldPoints = new List<Vector3>();
      foreach (var localPoint in localPoints)
        worldPoints.Add(collider.transform.TransformPoint(localPoint));

      return worldPoints;
    }

    public static List<Vector3> GetColliderPointsLocal(
      List<Collider> colliders)
    {
      return colliders
        .SelectMany(GetColliderPointsLocal)
        .Distinct()
        .ToList();
    }

    // Optional: Overload for world space conversion of multiple colliders
    public static List<Vector3> GetAllColliderPointsAsWorldPoints(
      List<Collider> colliders)
    {
      return colliders
        .SelectMany(GetColliderPointsGlobal)
        .Distinct()
        .ToList();
    }

    public static List<Vector3> GetColliderPointsRelative(
      List<Collider> colliders)
    {
      return colliders
        .SelectMany(collider =>
          ConvertToRelativeSpace(collider, GetColliderPointsLocal(collider)))
        .Distinct()
        .ToList();
    }

    private static bool IsAllowedAsHullDefault(string input)
    {
      if (input.Contains("floor") || input.Contains("wall") ||
          input.Contains("hull"))
        return true;

      return false;
    }

    /// <summary>
    ///   Allows only specific gameobjects to match
    /// </summary>
    /// TODO add ability to add additional matchers so players can add other mod items if they do not match the matchers.
    /// <param name="go"></param>
    /// <returns></returns>
    public bool IsAllowedAsHull(GameObject go)
    {
      var input = go.name.ToLower();
      return IsAllowedAsHullOverride(input) ||
             IsAllowedAsHullDefault(input);
    }


    /// <summary>
    ///   Used to filter out any colliders not considered in a valid layer or a
    ///   component that should not be included such as stairs
    /// </summary>
    /// <param name="colliders"></param>
    public static List<Collider> FilterColliders(List<Collider> colliders)
    {
      if (colliders is { Count: > 0 })
        colliders = colliders.Where(x =>
            LayerHelpers.IsContainedWithinMask(x.gameObject.layer,
              LayerHelpers.PhysicalLayers) && x.gameObject.activeInHierarchy)
          .ToList();

      return colliders;
    }

    /// <summary>
    ///   Groups colliders by proximity, handling nested or overlapping colliders
    ///   correctly. Allows combining colliders together into a massive mesh. Or splitting if the combination range is too large.
    /// </summary>
    /// <param name="gameObjects">List of GameObjects to process.</param>
    /// <param name="proximityThreshold">
    ///   The distance threshold to determine proximity
    ///   between colliders.
    /// </param>
    /// <returns>
    ///   A list of collider clusters, where each cluster is a list of
    ///   colliders.
    /// </returns>
    private List<List<Collider>> GroupCollidersByProximity(
      List<GameObject> gameObjects, float proximityThreshold)
    {
      // Collect all colliders from the provided GameObjects
      var allColliders = new List<Collider>();
      foreach (var obj in gameObjects)
      {
        if (obj == null) continue;
        // excludes gameobjects that do not fit as a collider but still need to be a piece on the ship.
        if (!IsAllowedAsHull(obj)) continue;
        var colliders = obj.GetComponentsInChildren<Collider>(false);
        if (colliders == null) continue;
        var filterColliders = FilterColliders(colliders.ToList());
        if (filterColliders is { Count: > 0 })
          allColliders.AddRange(colliders);
      }

      // Cluster colliders by proximity
      var clusters = new List<List<Collider>>();

      foreach (var collider in allColliders)
      {
        var addedToCluster = false;

        // Check against existing clusters
        foreach (var cluster in clusters)
          if (IsColliderNearCluster(collider, cluster, proximityThreshold))
          {
            cluster.Add(collider);
            addedToCluster = true;
            break;
          }

        // If not added to an existing cluster, create a new cluster
        if (!addedToCluster) clusters.Add(new List<Collider> { collider });
      }

      // Merge clusters that are near each other
      MergeNearbyClusters(clusters, proximityThreshold);

      return clusters;
    }

    /// <summary>
    ///   Checks if a collider is near an existing cluster of colliders.
    /// </summary>
    private static bool IsColliderNearCluster(Collider collider,
      List<Collider> cluster, float proximityThreshold)
    {
      foreach (var clusterCollider in cluster)
      {
        // Use Bounds.Intersects to handle overlap or nesting
        if (collider.bounds.Intersects(clusterCollider.bounds))
          return true;

        // Fallback to proximity calculation using ClosestPoint
        var closestPoint =
          clusterCollider.ClosestPoint(collider.bounds.center);
        var distance = Vector3.Distance(collider.bounds.center, closestPoint);
        if (distance <= proximityThreshold)
          return true;
      }

      return false;
    }

    /// <summary>
    ///   Merges clusters that are near each other based on the proximity threshold.
    /// </summary>
    private static void MergeNearbyClusters(List<List<Collider>> clusters,
      float proximityThreshold)
    {
      bool merged;
      do
      {
        merged = false;

        for (var i = 0; i < clusters.Count; i++)
        {
          for (var j = i + 1; j < clusters.Count; j++)
            if (AreClustersNearEachOther(clusters[i], clusters[j],
                  proximityThreshold))
            {
              // Merge clusters
              clusters[i].AddRange(clusters[j]);
              clusters.RemoveAt(j);
              merged = true;
              break;
            }

          if (merged) break;
        }
      } while (merged);
    }

    /// <summary>
    ///   Checks if two clusters are near each other based on the proximity threshold.
    /// </summary>
    private static bool AreClustersNearEachOther(List<Collider> cluster1,
      List<Collider> cluster2, float proximityThreshold)
    {
      foreach (var collider1 in cluster1)
      foreach (var collider2 in cluster2)
      {
        // Check overlap
        if (collider1.bounds.Intersects(collider2.bounds))
          return true;

        // Proximity check
        var closestPoint = collider1.ClosestPoint(collider2.bounds.center);
        var distance =
          Vector3.Distance(collider2.bounds.center, closestPoint);
        if (distance <= proximityThreshold)
          return true;
      }

      return false;
    }

    public static void DeleteMeshesFromChildColliders(
      List<GameObject> generateObjectList)
    {
      if (generateObjectList.Count == 0) return;
      var objects = generateObjectList.ToList();
      generateObjectList.Clear();

      foreach (var obj in objects) AdaptiveDestroy(obj);
    }

    public static List<GameObject> GetAllChildGameObjects(GameObject parentGo,
      int depth = 0, int maxDepth = 0)
    {
      var result = new List<GameObject>();
      foreach (Transform child in parentGo.transform)
      {
        result.Add(child.gameObject);
        if (depth < maxDepth)
          result.AddRange(GetAllChildGameObjects(child.gameObject, depth + 1,
            maxDepth));
      }

      return result;
    }

    /// <summary>
    ///   Main generator for convex hull meshes.
    /// </summary>
    /// <param name="childGameObjects"></param>
    /// <param name="targetParentGameObject">ConvexHullParent where all convexHull GameObjects are places</param>
    /// <param name="distanceThreshold"></param>
    public void GenerateMeshesFromChildColliders(
      GameObject targetParentGameObject,
      float distanceThreshold, List<GameObject> childGameObjects)
    {
      // TODO we may need to delete these after. Deleting immediately could cause a physics jump / issue
      if (convexHullMeshes.Count > 0)
        DeleteMeshesFromChildColliders(convexHullMeshes);

      var hullClusters =
        GroupCollidersByProximity(childGameObjects.ToList(),
          distanceThreshold);

      foreach (var hullCluster in hullClusters)
      {
        var colliderPoints = UseWorld
          ? GetAllColliderPointsAsWorldPoints(hullCluster)
          : GetColliderPointsLocal(hullCluster);
        GenerateConvexHullMesh(colliderPoints,
          targetParentGameObject.transform);
      }

      CreatePreviewConvexHullMeshes();
    }

    public static void AdaptiveDestroy(Object gameObject)
    {
#if UNITY_EDITOR
      Object.DestroyImmediate(gameObject);
#else
      Object.Destroy(gameObject);
#endif
    }

    // Create a copy of the mesh that is readable
    /// <summary>
    ///   Some meshes are not readable, this allows reading the mesh by writing it into
    ///   another mesh.
    ///   - This will allow reading the vertex data.
    /// </summary>
    /// <param name="sourceMesh"></param>
    /// <returns></returns>
    private static Mesh CreateReadableMesh(Mesh sourceMesh)
    {
      var readableMesh = new Mesh
      {
        vertices = sourceMesh.vertices,
        triangles = sourceMesh.triangles,
        normals = sourceMesh.normals,
        uv = sourceMesh.uv
      };
      readableMesh.RecalculateBounds();
      return readableMesh;
    }

    public static Material? GetMaterial()
    {
      if (DebugMaterial)
      {
        DebugMaterial.color = DebugMaterialColor;
        return DebugMaterial;
      }

      var shader = Shader.Find("Custom/DoubleSidedTransparent");
      if (!shader) return null;

      return new Material(shader)
      {
        color = DebugMaterialColor
      };
    }

    /// <summary>
    /// Renders a mesh for visualizing the hull. Not meant for casual players but worth using in creative mode.
    /// </summary>
    /// <param name="go"></param>
    public static void AddDebugMeshRenderer(GameObject go)
    {
      if (!DebugMode) return;
      var material = GetMaterial();
      if (material != null)
      {
        var meshRenderer = go.GetComponent<MeshRenderer>();
        if (!meshRenderer)
        {
          meshRenderer = go.AddComponent<MeshRenderer>();
        }

        meshRenderer.material = material;

        if (meshRenderer.material.color != DebugMaterialColor)
        {
          meshRenderer.material.color = DebugMaterialColor;
        }
      }
    }

    /// <summary>
    /// Preview meshes are to be only used for visual purposes.
    /// </summary>
    public void CreatePreviewConvexHullMeshes()
    {
      DeleteMeshesFromChildColliders(convexHullPreviewMeshes);

      if (!DebugMode) return;

      // Rigidbody must not be in preview parent otherwise it would have it's colliders applied as physics.
      var rbComponent =
        PreviewParent.GetComponent<Rigidbody>();

      if (rbComponent == null)
      {
        PreviewParent
          .GetComponentInParent<Rigidbody>();
      }

      if (rbComponent != null && rbComponent.isKinematic == false)
      {
        LoggerAPI(
          "Error this component is invalid due to preview parent containing a non-kinematic Rigidbody. using a non-kinematic rigidbody will cause this preview to desync.");
        return;
      }

      if (PreviewParent == null)
      {
        LoggerAPI("Error: PreviewParent is null.");
        return;
      }

      if (convexHullMeshes.Count > 0)
      {
        for (var index = 0; index < convexHullMeshes.Count; index++)
        {
          var convexHullMesh = convexHullMeshes[index];
          var previewInstance =
            Instantiate(convexHullMesh, PreviewParent);
          convexHullPreviewMeshes.Add(previewInstance);

          // Gets the difference between the original position and the parent. Then adds that as local position to align things.
          var positionOffsetFromDestinationOffset =
            convexHullMesh.transform.position -
            previewInstance.transform.position;

          previewInstance.transform.localPosition +=
            positionOffsetFromDestinationOffset;


          AddDebugMeshRenderer(previewInstance.gameObject);

          var previewName = $"{MeshNamePreviewPrefix}_{index}";
          previewInstance.gameObject.name = previewName;

          var previewMeshCollider =
            previewInstance.GetComponent<MeshCollider>();
          // Do not need a mesh collider for a preview instance. This can cause problems too.
          if (previewMeshCollider != null)
          {
            AdaptiveDestroy(previewMeshCollider);
          }

          LoggerAPI(
            $"Adjusting preview offset by {transformPreviewOffset}");
          previewInstance.transform.localPosition += transformPreviewOffset;
        }
      }
    }

    public void GenerateConvexHullMesh(
      List<Vector3> points, Transform parentObjTransform)
    {
      if (points.Count < 3)
      {
        Debug.LogError("Not enough points to generate a convex hull.");
        return;
      }

      var localPoints = points
        .Select(x => x - parentObjTransform.transform.position).ToList();

      // Vector3Logger.LogPointsForInspector(localPoints);


      // Prepare output containers
      var verts = new List<Vector3>();
      var tris = new List<int>();
      var normals = new List<Vector3>();

      // Generate convex hull and export the mesh
      convexHullCalculator.GenerateHull(localPoints, false, ref verts,
        ref tris,
        ref normals);

      // Create a Unity Mesh
      var mesh = new Mesh
      {
        vertices = verts.ToArray(),
        triangles = tris.ToArray(),
        normals = normals.ToArray(),
        name =
          $"{MeshNamePrefix}_{convexHullMeshes.Count}_mesh"
      };

      if (ShouldOptimizeGeneratedMeshes)
      {
        // possibly necessary for performance (but a bit of overhead)
        mesh.Optimize();
        //
        // // possibly unnecessary.
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
      }

      // Create a new GameObject to display the mesh
      var go =
        new GameObject(
          $"{MeshNamePrefix}_{convexHullMeshes.Count}")
        {
          layer = LayerHelpers.CustomRaftLayer
        };

      convexHullMeshes.Add(go);

      var meshFilter = go.AddComponent<MeshFilter>();

#if UNITY_EDITOR
      if (DebugOriginalMesh)
      {
        AddDebugMeshRenderer(go);
      }
#endif

      var meshCollider = go.AddComponent<MeshCollider>();
      meshFilter.mesh = mesh;

      meshCollider.sharedMesh = mesh;
      meshCollider.convex = true;
      meshCollider.excludeLayers = LayerHelpers.BlockingColliderExcludeLayers;

      go.transform.position = parentObjTransform.transform.position;
      go.transform.SetParent(parentObjTransform);
    }
  }
}