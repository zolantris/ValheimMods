using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using ValheimVehicles.Scene;

namespace ValheimVehicles.Scene
{
  
  [ExecuteInEditMode]
public class ConvexHullMeshGenerator : MonoBehaviour
{

    public static void GetCapsuleColliderPoints(CapsuleCollider capsuleCollider, ref List<Vector3> points, Vector3 scale)
    {
        // CapsuleCollider: Generate points for the body and spherical caps
        var center = Vector3.Scale(capsuleCollider.center, scale);
        var height = (capsuleCollider.height * 0.5f - capsuleCollider.radius) * scale.y;
        var radius = capsuleCollider.radius * Mathf.Max(scale.x, scale.z);

        const int segmentCount = 10; // Number of segments for spherical caps and cylinder

        var direction = capsuleCollider.direction; // 0 = X, 1 = Y, 2 = Z
        Vector3 up = Vector3.up, forward = Vector3.forward, right = Vector3.right;

        if (direction == 0) { up = Vector3.right; forward = Vector3.up; right = Vector3.forward; }
        else if (direction == 2) { up = Vector3.forward; forward = Vector3.up; right = Vector3.right; }

        // Generate cylinder body points
        for (int i = 0; i <= segmentCount; i++)
        {
            var angle = 2 * Mathf.PI * i / segmentCount;
            var x = radius * Mathf.Cos(angle);
            var z = radius * Mathf.Sin(angle);
            var bodyPoint = new Vector3(x, 0, z);

            points.Add(center + height * up + Vector3.Scale(bodyPoint, new Vector3(right.x, right.y, right.z)));
            points.Add(center - height * up + Vector3.Scale(bodyPoint, new Vector3(right.x, right.y, right.z)));
        }

        // Generate points for the spherical caps
        for (int i = 0; i <= segmentCount; i++)
        {
            var theta = Mathf.PI * i / segmentCount; // Polar angle
            var sinTheta = Mathf.Sin(theta);
            var cosTheta = Mathf.Cos(theta);

            for (int j = 0; j <= segmentCount; j++)
            {
                var phi = 2 * Mathf.PI * j / segmentCount; // Azimuthal angle
                var x = radius * sinTheta * Mathf.Cos(phi);
                var y = radius * cosTheta;
                var z = radius * sinTheta * Mathf.Sin(phi);

                var capPoint = new Vector3(x, y, z);

                points.Add(center + height * up + capPoint); // Top cap
                points.Add(center - height * up + capPoint); // Bottom cap
            }
        }
    }
   /// <summary>
  /// Gets all local-space points of a collider, adjusted for the object's scale.
  /// </summary>
  /// <param name="collider">The collider to extract points from.</param>
  /// <returns>A list of Vector3 points in local space, scaled correctly.</returns>
     public static List<Vector3> GetColliderPointsLocal(Collider collider)
    {
        var points = new List<Vector3>();
        var scale = collider.transform.localScale; // Object's local scale

        if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
        {
            // MeshCollider: Extract scaled local-space vertices
            foreach (var vertex in meshCollider.sharedMesh.vertices)
            {
                points.Add(Vector3.Scale(vertex, scale));
            }
        }
        else if (collider is BoxCollider boxCollider)
        {
            // BoxCollider: Calculate 8 corners in scaled local space
            var center = Vector3.Scale(boxCollider.center, scale);
            var size = Vector3.Scale(boxCollider.size * 0.5f, scale);

            var corners = new Vector3[]
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
            var radius = sphereCollider.radius * Mathf.Max(scale.x, scale.y, scale.z);

            const int latitudeSegments = 10; // Number of latitudinal divisions
            const int longitudeSegments = 20; // Number of longitudinal divisions

            for (int lat = 0; lat <= latitudeSegments; lat++)
            {
                var theta = Mathf.PI * lat / latitudeSegments; // Latitude angle
                var sinTheta = Mathf.Sin(theta);
                var cosTheta = Mathf.Cos(theta);

                for (int lon = 0; lon <= longitudeSegments; lon++)
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
            GetCapsuleColliderPoints(capsuleCollider, ref points, scale);
        else
        {
            Debug.LogWarning($"Unsupported collider type: {collider.GetType()}");
        }

        return points; // Local space, scaled
    }
   
private static List<Vector3> GetColliderPointsGlobal(Collider collider)
{
    var points = new List<Vector3>();
    Transform transform = collider.transform;

    // Handle BoxCollider
    if (collider is BoxCollider boxCollider)
    {
        Vector3 boxCenter = transform.position + transform.rotation * boxCollider.center;
        Vector3 boxSize = Vector3.Scale(boxCollider.size, transform.lossyScale);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 localCorner = new Vector3(x * boxSize.x, y * boxSize.y, z * boxSize.z) * 0.5f;
                    Vector3 worldCorner = boxCenter + localCorner;
                    points.Add(worldCorner);
                }
            }
        }
    }
    // Handle SphereCollider
    else if (collider is SphereCollider sphereCollider)
    {
        Vector3 center = transform.position + transform.rotation * sphereCollider.center;
        float scaledRadius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

        int resolution = 12;
        for (int i = 0; i < resolution; i++)
        {
            float theta = i * Mathf.PI * 2 / resolution;
            for (int j = 0; j <= resolution / 2; j++)
            {
                float phi = j * Mathf.PI / (resolution / 2);
                Vector3 localPoint = new Vector3(
                    Mathf.Sin(phi) * Mathf.Cos(theta),
                    Mathf.Sin(phi) * Mathf.Sin(theta),
                    Mathf.Cos(phi)
                ) * scaledRadius;

                Vector3 worldPoint = center + localPoint;
                points.Add(worldPoint);
            }
        }
    }
    // Handle CapsuleCollider
    else if (collider is CapsuleCollider capsuleCollider)
    {
        var scale = transform.lossyScale;
        GetCapsuleColliderPoints(capsuleCollider, ref points, scale);
    }
    // Handle MeshCollider
    else if (collider is MeshCollider meshCollider)
    {
        Mesh mesh = meshCollider.sharedMesh;
        if (mesh != null)
        {
            foreach (var vertex in mesh.vertices)
            {
                // Convert mesh vertex to world space
                var worldVertex = transform.TransformPoint(vertex);
                points.Add(worldVertex);
            }
        }
    }

    return points;
}
   
   private static List<Vector3> GetColliderPointsGlobal1(Collider collider)
    {
        var points = new List<Vector3>();
        Transform transform = collider.transform;

        // Handle BoxCollider
        if (collider is BoxCollider boxCollider)
        {
            Vector3 boxCenter = transform.TransformPoint(boxCollider.center);
            Vector3 boxSize = Vector3.Scale(boxCollider.size, transform.lossyScale); // Account for scaling

            // Calculate all 8 corners of the box in world space
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 localCorner = boxCollider.center + new Vector3(x * boxSize.x, y * boxSize.y, z * boxSize.z) * 0.5f;
                        Vector3 worldCorner = transform.TransformPoint(localCorner);
                        points.Add(worldCorner);
                    }
                }
            }
        }
        // Handle SphereCollider
        else if (collider is SphereCollider sphereCollider)
        {
            Vector3 center = transform.TransformPoint(sphereCollider.center);
            float scaledRadius = sphereCollider.radius * transform.lossyScale.x; // Assume uniform scaling for spheres

            // Approximate the sphere using a spherical point distribution
            int resolution = 12; // Number of points for approximation
            for (int i = 0; i < resolution; i++)
            {
                float theta = i * Mathf.PI * 2 / resolution;
                for (int j = 0; j <= resolution / 2; j++)
                {
                    float phi = j * Mathf.PI / (resolution / 2);
                    Vector3 localPoint = new Vector3(
                        Mathf.Sin(phi) * Mathf.Cos(theta),
                        Mathf.Sin(phi) * Mathf.Sin(theta),
                        Mathf.Cos(phi)
                    ) * scaledRadius;

                    Vector3 worldPoint = center + transform.TransformVector(localPoint);
                    points.Add(worldPoint);
                }
            }
        }
        // Handle CapsuleCollider
        else if (collider is CapsuleCollider capsuleCollider)
        {
            Vector3 center = transform.TransformPoint(capsuleCollider.center);
            var lossyScale = transform.lossyScale;
            float scaledRadius = capsuleCollider.radius * Mathf.Max(lossyScale.x, lossyScale.z); // Account for x/z scaling
            float scaledHeight = Mathf.Max(0, capsuleCollider.height * lossyScale.y - 2 * scaledRadius); // Account for height scaling

            // Top and bottom sphere centers in world space
            Vector3 localUp = Vector3.up * scaledHeight * 0.5f;
            Vector3 worldTop = transform.TransformPoint(capsuleCollider.center + localUp);
            Vector3 worldBottom = transform.TransformPoint(capsuleCollider.center - localUp);

            // Approximate the capsule (top, bottom, and middle)
            int resolution = 12;
            for (int i = 0; i < resolution; i++)
            {
                float theta = i * Mathf.PI * 2 / resolution;

                // Circle points for top and bottom
                Vector3 circlePoint = new Vector3(Mathf.Cos(theta), 0, Mathf.Sin(theta)) * scaledRadius;
                points.Add(worldTop + transform.TransformVector(circlePoint));
                points.Add(worldBottom + transform.TransformVector(circlePoint));

                // Vertical segment (cylinder part)
                Vector3 midPoint = Vector3.Lerp(worldBottom + transform.TransformVector(circlePoint), worldTop + transform.TransformVector(circlePoint), 0.5f);
                points.Add(midPoint);
            }
        }
        // Handle MeshCollider
        else if (collider is MeshCollider meshCollider)
        {
            Mesh mesh = meshCollider.sharedMesh;
            if (mesh != null)
            {
                foreach (Vector3 vertex in mesh.vertices)
                {
                    Vector3 worldVertex = transform.TransformPoint(vertex);
                    points.Add(worldVertex);
                }
            }
        }

        return points;
    }
   
  public static List<Vector3> ConvertToRelativeSpace(Collider collider,
    List<Vector3> localPoints)
  {
    var relativePoints = new List<Vector3>();
    foreach (var localPoint in localPoints)
    {
      relativePoints.Add(localPoint + collider.transform.localPosition);
    }

    return relativePoints;
  }

  public static List<Vector3> ConvertToWorldSpace(Collider collider,
    List<Vector3> localPoints)
  {
    var worldPoints = new List<Vector3>();
    foreach (var localPoint in localPoints)
    {
      worldPoints.Add(collider.transform.TransformPoint(localPoint));
    }

    return worldPoints;
  }

  public static List<Vector3> GetColliderPointsLocal(List<Collider> colliders)
  {
    return colliders
      .SelectMany(GetColliderPointsLocal)
      .Distinct()
      .ToList();
  }

  // Optional: Overload for world space conversion of multiple colliders
  public static List<Vector3> GetColliderPointsWorld(List<Collider> colliders)
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
  
  public Vector3 transformPreviewOffset = new Vector3(1, 0, 0);

  private void Start()
  {
      GenerateMeshesFromChildColliders();
  }

  public void OnEnable()
  {
    GenerateMeshesFromChildColliders();
  }

  public void OnDisable()
  {
      DeleteMeshesFromChildColliders();
  }

  public void FixedUpdate()
  {
    GenerateMeshesFromChildColliders();
  }
  
  
      /// <summary>
    /// Groups colliders by proximity, handling nested or overlapping colliders correctly.
    /// </summary>
    /// <param name="gameObjects">List of GameObjects to process.</param>
    /// <param name="proximityThreshold">The distance threshold to determine proximity between colliders.</param>
    /// <returns>A list of collider clusters, where each cluster is a list of colliders.</returns>
    public static List<List<Collider>> GroupCollidersByProximity(List<GameObject> gameObjects, float proximityThreshold)
    {
        // Collect all colliders from the provided GameObjects
        var allColliders = new List<Collider>();
        foreach (var obj in gameObjects)
        {
            if (obj == null) continue;
            allColliders.AddRange(obj.GetComponentsInChildren<Collider>());
        }

        // Cluster colliders by proximity
        var clusters = new List<List<Collider>>();

        foreach (var collider in allColliders)
        {
            bool addedToCluster = false;

            // Check against existing clusters
            foreach (var cluster in clusters)
            {
                if (IsColliderNearCluster(collider, cluster, proximityThreshold))
                {
                    cluster.Add(collider);
                    addedToCluster = true;
                    break;
                }
            }

            // If not added to an existing cluster, create a new cluster
            if (!addedToCluster)
            {
                clusters.Add(new List<Collider> { collider });
            }
        }

        // Merge clusters that are near each other
        MergeNearbyClusters(clusters, proximityThreshold);

        return clusters;
    }

    /// <summary>
    /// Checks if a collider is near an existing cluster of colliders.
    /// </summary>
    private static bool IsColliderNearCluster(Collider collider, List<Collider> cluster, float proximityThreshold)
    {
        foreach (var clusterCollider in cluster)
        {
            // Use Bounds.Intersects to handle overlap or nesting
            if (collider.bounds.Intersects(clusterCollider.bounds))
                return true;

            // Fallback to proximity calculation using ClosestPoint
            var closestPoint = clusterCollider.ClosestPoint(collider.bounds.center);
            var distance = Vector3.Distance(collider.bounds.center, closestPoint);
            if (distance <= proximityThreshold)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Merges clusters that are near each other based on the proximity threshold.
    /// </summary>
    private static void MergeNearbyClusters(List<List<Collider>> clusters, float proximityThreshold)
    {
        bool merged;
        do
        {
            merged = false;

            for (int i = 0; i < clusters.Count; i++)
            {
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    if (AreClustersNearEachOther(clusters[i], clusters[j], proximityThreshold))
                    {
                        // Merge clusters
                        clusters[i].AddRange(clusters[j]);
                        clusters.RemoveAt(j);
                        merged = true;
                        break;
                    }
                }
                if (merged) break;
            }
        } while (merged);
    }

    /// <summary>
    /// Checks if two clusters are near each other based on the proximity threshold.
    /// </summary>
    private static bool AreClustersNearEachOther(List<Collider> cluster1, List<Collider> cluster2, float proximityThreshold)
    {
        foreach (var collider1 in cluster1)
        {
            foreach (var collider2 in cluster2)
            {
                // Check overlap
                if (collider1.bounds.Intersects(collider2.bounds))
                    return true;

                // Proximity check
                Vector3 closestPoint = collider1.ClosestPoint(collider2.bounds.center);
                float distance = Vector3.Distance(collider2.bounds.center, closestPoint);
                if (distance <= proximityThreshold)
                    return true;
            }
        }
        return false;
    }

  public float DistanceThreshold = 0.1f;
  
  public List<GameObject> GeneratedMeshGameObjects = new();

  public void DeleteMeshesFromChildColliders()
  {
      var instances = GeneratedMeshGameObjects.ToList();
      GeneratedMeshGameObjects.Clear();
      
      foreach (var instance in instances)
      {
          AdaptiveDestroy(instance);
      }
  }
  
  public static List<GameObject> GetAllChildGameObjects(GameObject parentGo)
  {
      var result = new List<GameObject>();
      foreach (Transform child in parentGo.transform)
      {
          if (child.name.StartsWith("ConvexHullPreview")) continue;
          result.Add(child.gameObject);
          result.AddRange(GetAllChildGameObjects(child.gameObject));
      }
      return result;
  }
  
  public void GenerateMeshesFromChildColliders()
  {
      if (GeneratedMeshGameObjects.Count > 0)
      {
          DeleteMeshesFromChildColliders();
      }

      var childGameObjects = GetAllChildGameObjects(gameObject);
      var hullClusters = GroupCollidersByProximity(childGameObjects.ToList(), DistanceThreshold);
      Debug.Log($"HullCluster Count: {hullClusters.Count}");

      foreach (var hullCluster in hullClusters)
      {
          var colliderPoints = GetColliderPointsWorld(hullCluster);
          GenerateConvexHullMesh(colliderPoints);
      }
  }

  // Convex hull calculator instance
  private ConvexHullCalculator
    convexHullCalculator = new ConvexHullCalculator();

  private static void AdaptiveDestroy(GameObject gameObject)
  {
#if UNITY_EDITOR
    UnityEngine.Object.DestroyImmediate(gameObject);
#else
            UnityEngine.Object.Destroy(gameObject);
#endif
  }

  void GenerateConvexHullMesh(List<Vector3> points)
  {
    if (points.Count < 3)
    {
      Debug.LogError("Not enough points to generate a convex hull.");
      return;
    }
    
    Debug.Log("Generating convex hull...");

    // Step 1: Prepare output containers
    var verts = new List<Vector3>();
    var tris = new List<int>();
    var normals = new List<Vector3>();

    // Step 2: Generate convex hull and export the mesh
    convexHullCalculator.GenerateHull(points, true, ref verts, ref tris,
      ref normals);

    // Step 3: Create a Unity Mesh
    var mesh = new Mesh
    {
      vertices = verts.ToArray(),
      triangles = tris.ToArray(),
      normals = normals.ToArray()
    };

    // Step 5: Create a new GameObject to display the mesh
    var go = new GameObject($"ConvexHullPreview{GeneratedMeshGameObjects.Count}");
    GeneratedMeshGameObjects.Add(go);
    var meshFilter = go.AddComponent<MeshFilter>();
    var meshRenderer = go.AddComponent<MeshRenderer>();

    meshFilter.mesh = mesh;

    // Step 6: Create and assign the material
    var material = new Material(Shader.Find("Standard"))
    {
      color = Color.green
    };
    meshRenderer.material = material;

    // Optional: Adjust transform
    var transform1 = transform;
    go.transform.position += transformPreviewOffset;
    go.transform.rotation = transform1.rotation;
    go.transform.SetParent(null);
  }

  // Test the method with a sample list of points
  [ContextMenu("Generate Mesh")]
  private void TestGenerateMesh()
  {
    GenerateMeshesFromChildColliders();
  }
}
}