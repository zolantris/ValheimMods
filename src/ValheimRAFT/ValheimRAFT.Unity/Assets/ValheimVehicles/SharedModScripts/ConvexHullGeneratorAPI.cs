using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.SharedModScripts;

namespace ValheimVehicles.Unity.Shared
{
  public class ConvexHullMeshGeneratorAPI : MonoBehaviour
  {
    public static Vector3 transformPreviewOffset = new(0, -3f, 0);

    public static float DistanceThreshold = 0.1f;

    // Convex hull calculator instance
    private static readonly ConvexHullCalculator
      convexHullCalculator = new();

    private static readonly int Color1 = Shader.PropertyToID("_Color");

    public static void GetCapsuleColliderPoints(CapsuleCollider capsuleCollider,
      ref List<Vector3> points, Vector3 scale)
    {
      // Get the capsule's local center and radius, then scale them based on the provided scale
      var center = Vector3.Scale(capsuleCollider.center, scale);
      var radius =
        capsuleCollider.radius *
        Mathf.Max(scale.x, scale.z); // Ensure scaling on X and Z axes
      var height = (capsuleCollider.height * 0.5f - capsuleCollider.radius) *
                   scale.y; // Scale the height along the Y axis

      const int
        segmentCount = 10; // Number of segments for spherical caps and cylinder

      var direction = capsuleCollider.direction; // 0 = X, 1 = Y, 2 = Z
      var up = Vector3.up;
      var forward = Vector3.forward;
      var right = Vector3.right;

      // Adjust up, forward, right vectors based on capsule direction
      if (direction == 0)
      {
        up = Vector3.right;
        forward = Vector3.forward;
        right = Vector3.up;
      }
      else if (direction == 1)
      {
        up = Vector3.up;
        forward = Vector3.forward;
        right = Vector3.right;
      }
      else if (direction == 2)
      {
        up = Vector3.forward;
        forward = Vector3.up;
        right = Vector3.right;
      }

      // Generate cylinder body points
      for (var i = 0; i <= segmentCount; i++)
      {
        var angle = 2 * Mathf.PI * i / segmentCount;
        var x = radius * Mathf.Cos(angle);
        var z = radius * Mathf.Sin(angle);
        var bodyPoint = new Vector3(x, 0, z);

        // Apply transformations based on capsule's direction
        points.Add(center + height * up + Vector3.Scale(bodyPoint,
          new Vector3(right.x, right.y, right.z)));
        points.Add(center - height * up + Vector3.Scale(bodyPoint,
          new Vector3(right.x, right.y, right.z)));
      }

      // Generate points for the spherical caps
      for (var i = 0; i <= segmentCount; i++)
      {
        var theta = Mathf.PI * i / segmentCount; // Polar angle
        var sinTheta = Mathf.Sin(theta);
        var cosTheta = Mathf.Cos(theta);

        for (var j = 0; j <= segmentCount; j++)
        {
          var phi = 2 * Mathf.PI * j / segmentCount; // Azimuthal angle
          var x = radius * sinTheta * Mathf.Cos(phi);
          var y = radius * cosTheta;
          var z = radius * sinTheta * Mathf.Sin(phi);

          var capPoint = new Vector3(x, y, z);

          // Add cap points to the list (top and bottom caps)
          points.Add(center + height * up + capPoint); // Top cap
          points.Add(center - height * up + capPoint); // Bottom cap
        }
      }
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
        GetCapsuleColliderPoints(capsuleCollider, ref points, scale);
      }
      else
      {
        Debug.LogWarning($"Unsupported collider type: {collider.GetType()}");
      }

      return points; // Local space, scaled
    }

    private static List<Vector3> GetColliderPointsGlobal(Collider collider)
    {
      var points = new List<Vector3>();
      var transform = collider.transform;

      // Handle BoxCollider
      if (collider is BoxCollider boxCollider)
      {
        // Calculate the center of the box in world space
        var boxCenter = transform.TransformPoint(boxCollider.center);

        // Apply lossyScale to the box size
        var boxSize = Vector3.Scale(boxCollider.size, transform.lossyScale);

        // Loop through each corner of the box
        for (var x = -1; x <= 1; x += 2)
        for (var y = -1; y <= 1; y += 2)
        for (var z = -1; z <= 1; z += 2)
        {
          // Define the local corner point
          var localCorner =
            new Vector3(x * boxSize.x, y * boxSize.y, z * boxSize.z) * 0.5f;

          // Transform the local corner to world space using rotation and position
          var worldCorner = boxCenter + transform.rotation * localCorner;
          points.Add(worldCorner);
        }
      }
      // Handle SphereCollider
      else if (collider is SphereCollider sphereCollider)
      {
        var center = transform.position +
                     transform.rotation * sphereCollider.center;
        var scaledRadius = sphereCollider.radius * Mathf.Max(
          transform.lossyScale.x, transform.lossyScale.y,
          transform.lossyScale.z);

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
      }
      // Handle CapsuleCollider
      else if (collider is CapsuleCollider capsuleCollider)
      {
        // var scale = transform.lossyScale;
        // GetCapsuleColliderPoints(capsuleCollider, ref points, scale);
      }
      // Handle MeshCollider
      else if (collider is MeshCollider meshCollider)
      {
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
        if (!isReadable) Destroy(mesh);
      }

      return points;
    }

    private static List<Vector3> GetColliderPointsGlobal1(Collider collider)
    {
      var points = new List<Vector3>();
      var transform = collider.transform;

      // Handle BoxCollider
      if (collider is BoxCollider boxCollider)
      {
        var boxCenter = transform.TransformPoint(boxCollider.center);
        var boxSize =
          Vector3.Scale(boxCollider.size,
            transform.lossyScale); // Account for scaling

        // Calculate all 8 corners of the box in world space
        for (var x = -1; x <= 1; x += 2)
        for (var y = -1; y <= 1; y += 2)
        for (var z = -1; z <= 1; z += 2)
        {
          var localCorner = boxCollider.center +
                            new Vector3(x * boxSize.x, y * boxSize.y,
                              z * boxSize.z) * 0.5f;
          var worldCorner = transform.TransformPoint(localCorner);
          points.Add(worldCorner);
        }
      }
      // Handle SphereCollider
      else if (collider is SphereCollider sphereCollider)
      {
        var center = transform.TransformPoint(sphereCollider.center);
        var scaledRadius =
          sphereCollider.radius *
          transform.lossyScale.x; // Assume uniform scaling for spheres

        // Approximate the sphere using a spherical point distribution
        var resolution = 12; // Number of points for approximation
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

            var worldPoint = center + transform.TransformVector(localPoint);
            points.Add(worldPoint);
          }
        }
      }
      // Handle CapsuleCollider
      else if (collider is CapsuleCollider capsuleCollider)
      {
        var center = transform.TransformPoint(capsuleCollider.center);
        var lossyScale = transform.lossyScale;
        var scaledRadius = capsuleCollider.radius *
                           Mathf.Max(lossyScale.x,
                             lossyScale.z); // Account for x/z scaling
        var scaledHeight = Mathf.Max(0,
          capsuleCollider.height * lossyScale.y -
          2 * scaledRadius); // Account for height scaling

        // Top and bottom sphere centers in world space
        var localUp = Vector3.up * scaledHeight * 0.5f;
        var worldTop =
          transform.TransformPoint(capsuleCollider.center + localUp);
        var worldBottom =
          transform.TransformPoint(capsuleCollider.center - localUp);

        // Approximate the capsule (top, bottom, and middle)
        var resolution = 12;
        for (var i = 0; i < resolution; i++)
        {
          var theta = i * Mathf.PI * 2 / resolution;

          // Circle points for top and bottom
          var circlePoint =
            new Vector3(Mathf.Cos(theta), 0, Mathf.Sin(theta)) * scaledRadius;
          points.Add(worldTop + transform.TransformVector(circlePoint));
          points.Add(worldBottom + transform.TransformVector(circlePoint));

          // Vertical segment (cylinder part)
          var midPoint =
            Vector3.Lerp(worldBottom + transform.TransformVector(circlePoint),
              worldTop + transform.TransformVector(circlePoint), 0.5f);
          points.Add(midPoint);
        }
      }
      // Handle MeshCollider
      else if (collider is MeshCollider meshCollider)
      {
        var mesh = meshCollider.sharedMesh;
        if (mesh != null)
          foreach (var vertex in mesh.vertices)
          {
            var worldVertex = transform.TransformPoint(vertex);
            points.Add(worldVertex);
          }
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

    /// <summary>
    ///   Allows only specific gameobjects to match
    /// </summary>
    /// TODO add ability to add additional matchers so players can add other mod items if they do not match the matchers.
    /// <param name="go"></param>
    /// <returns></returns>
    public static bool IsAllowedAsHull(GameObject go)
    {
      if (PrefabNames.IsHull(go)) return true;
      var name = go.name.ToLower();

      if (name.Contains("floor") || name.Contains("wall")) return true;

      return false;
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
            LayerHelpers.PhysicalLayers)).ToList();

      return colliders;
    }

    /// <summary>
    ///   Groups colliders by proximity, handling nested or overlapping colliders
    ///   correctly.
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
    public static List<List<Collider>> GroupCollidersByProximity(
      List<GameObject> gameObjects, float proximityThreshold)
    {
      // Collect all colliders from the provided GameObjects
      var allColliders = new List<Collider>();
      foreach (var obj in gameObjects)
      {
        if (obj == null) continue;
        // excludes gameobjects that do not fit as a collider but still need to be a piece on the ship.
        if (!IsAllowedAsHull(obj)) continue;
        var colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders == null) continue;
        var filterColliders = FilterColliders(colliders.ToList());
        if (filterColliders is { Count: > 0 }) allColliders.AddRange(colliders);
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
        var closestPoint = clusterCollider.ClosestPoint(collider.bounds.center);
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
      var instances = generateObjectList.ToList();
      generateObjectList.Clear();

      foreach (var instance in instances) AdaptiveDestroy(instance);
    }

    public static List<GameObject> GetAllChildGameObjects(GameObject parentGo)
    {
      var result = new List<GameObject>();
      foreach (Transform child in parentGo.transform)
      {
        result.Add(child.gameObject);
        result.AddRange(GetAllChildGameObjects(child.gameObject));
      }

      return result;
    }

    /// <summary>
    ///   For GameObjects
    /// </summary>
    /// <param name="parentGameObject"></param>
    /// <param name="convexHullGameObjects"></param>
    /// <param name="distanceThreshold"></param>
    public static void GenerateMeshesFromChildColliders(
      GameObject parentGameObject, List<GameObject> convexHullGameObjects,
      float distanceThreshold = 0.1f)
    {
      var childGameObjects = GetAllChildGameObjects(parentGameObject);
      GenerateMeshesFromChildColliders(parentGameObject, convexHullGameObjects,
        distanceThreshold, childGameObjects);
    }

    /// <summary>
    ///   Main generator for convex hull meshes.
    /// </summary>
    /// <param name="parentGameObject"></param>
    /// <param name="childGameObjects"></param>
    /// <param name="convexHullGameObjects"></param>
    /// <param name="distanceThreshold"></param>
    public static void GenerateMeshesFromChildColliders(
      GameObject parentGameObject,
      List<GameObject> convexHullGameObjects,
      float distanceThreshold, List<GameObject> childGameObjects)
    {
      // TODO we may need to delete these after. Deleting immediately could cause a physics jump / issue
      if (convexHullGameObjects.Count > 0)
        DeleteMeshesFromChildColliders(convexHullGameObjects);

      var hullClusters =
        GroupCollidersByProximity(childGameObjects.ToList(), distanceThreshold);

      Debug.Log($"HullCluster Count: {hullClusters.Count}");

      foreach (var hullCluster in hullClusters)
      {
        var colliderPoints = GetColliderPointsWorld(hullCluster);
        GenerateConvexHullMesh(colliderPoints, convexHullGameObjects,
          parentGameObject.transform);
      }
    }

    private static void AdaptiveDestroy(GameObject gameObject)
    {
#if UNITY_EDITOR
      DestroyImmediate(gameObject);
#else
    Destroy(gameObject);
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

    public static void GenerateConvexHullMesh(
      List<Vector3> points,
      List<GameObject> generateObjectList, Transform parentObjTransform)
    {
      if (points.Count < 3)
      {
        Debug.LogError("Not enough points to generate a convex hull.");
        return;
      }

      var localPoints = points
        .Select(parentObjTransform.InverseTransformPoint).ToList();

      Debug.Log("Generating convex hull...");
      Debug.Log("GlobalPoints:");
      Vector3Logger.LogPointsForInspector(points);
      Debug.Log("LocalPoints:");
      Vector3Logger.LogPointsForInspector(localPoints);


      // Prepare output containers
      var verts = new List<Vector3>();
      var tris = new List<int>();
      var normals = new List<Vector3>();

      // Generate convex hull and export the mesh
      convexHullCalculator.GenerateHull(localPoints, true, ref verts, ref tris,
        ref normals);

      // Create a Unity Mesh
      var mesh = new Mesh
      {
        vertices = verts.ToArray(),
        triangles = tris.ToArray(),
        normals = normals.ToArray()
      };

      // possibly necessary for performance (but a bit of overhead)
      mesh.Optimize();
      // likely all unnecessary.
      mesh.RecalculateNormals();
      mesh.RecalculateBounds();

      // Create a new GameObject to display the mesh
      var go = new GameObject(PrefabNames.ConvexHull);
      go.layer = LayerHelpers.CustomRaftLayer;

      generateObjectList.Add(go);
      var meshFilter = go.AddComponent<MeshFilter>();
      var meshRenderer = go.AddComponent<MeshRenderer>();
      var meshCollider = go.AddComponent<MeshCollider>();
      meshFilter.mesh = mesh;
      meshCollider.sharedMesh = mesh;
      meshCollider.convex = true;
      meshCollider.excludeLayers = LayerHelpers.BlockingColliderExcludeLayers;

      // var standardShader = Shader.Find("Standard");
      // Create and assign the material
      // var material =
      //   new Material(standardShader
      //     ? standardShader
      //     : LoadValheimAssets.CustomPieceShader)
      meshRenderer.material =
        LoadValheimVehicleAssets.DoubleSidedTransparentMat;

      // Optional: Adjust transform
      go.transform.position =
        parentObjTransform.position + transformPreviewOffset;
      go.transform.rotation = parentObjTransform.rotation;
      go.transform.SetParent(parentObjTransform);
    }
  }
}