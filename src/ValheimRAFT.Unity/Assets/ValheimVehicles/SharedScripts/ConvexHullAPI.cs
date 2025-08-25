#region

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using JetBrains.Annotations;
  using UnityEngine;
  using UnityEngine.Rendering;
  using UnityEngine.Serialization;
  using Zolantris.Shared;
  using Debug = UnityEngine.Debug;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts
  {
    public class ConvexHullAPI : MonoBehaviour
    {
      public enum PreviewModes
      {
        None,
        Debug,
        Bubble
      }

      private const string colliderParentNameDefault = "vehicle_movement/colliders";
      public static bool CanRenderBubble = true;

      public static bool HasInitialized = false;
      [CanBeNull] public static Material DebugMaterial;
      [CanBeNull] public static Material BubbleMaterial;

      public static Color DebugMaterialColor = new(0.10f, 0.23f, 0.5f, 0.2f);

      public static Color BubbleMaterialColor = new(0f, 0.4f, 0.4f, 0.8f);

      public static Action<string> LoggerAPI = Debug.Log;
      public static bool UseWorld = true;

      // todo move prefixes to unity so it can be shared. Possibly auto-generated too.
      public static string MeshNamePrefix = "ConvexHull";
      public static bool ShouldOptimizeGeneratedMeshes = true;

      public static List<ConvexHullAPI> Instances = new();

      public static readonly int MaxHeightShaderId =
        Shader.PropertyToID("_MaxHeight");

      public static readonly string MeshNamePreviewPrefix =
        $"{MeshNamePrefix}_Preview";

      public static string MeshNameTriggerPrefix = $"{MeshNamePrefix}_Preview";

      // shared across all instances, but not created immediately
      public PhysicsMaterial localPhysicMaterial;

      public PreviewModes PreviewMode = PreviewModes.Bubble;

      [FormerlySerializedAs("sphereEncapsulationBuffer")]
      public float wrapperBuffer = 0.5f;

      public Vector3 transformPreviewOffset = Vector3.zero;

      [FormerlySerializedAs("ConvexHullMaterialOverride")] [CanBeNull]
      public Material m_fallbackMaterial;

      public bool CanLog;
      public Vector3 previewScale = Vector3.one;

      public Transform PreviewParent;
      public Transform? TriggerParent;

      public int convexMeshLayer = 29;

      public bool HasPreviewGeneration = true;

      public Transform m_colliderParentTransform;
      public Rigidbody m_rigidbody;

      public bool ShouldDestroyOnGenerate;
      [FormerlySerializedAs("HasFrictionlessMaterial")]
      public bool HasPhysicMaterial;

      public float PhysicMaterialStaticFriction = 0.01f;
      public float PhysicMaterialDynamicFriction = 0.02f;

      public string colliderParentName = colliderParentNameDefault;

      public Transform parentTransform = null!;

      private Bounds _cachedConvexHullBounds = new(Vector3.zero, Vector3.one * 4);

      private List<Vector3> _cachedPoints = new();

      // Convex hull calculator instance
      /// <summary>
      ///   With the current implementation we need multiple ConvexHullApi components
      ///   otherwise the hullcalculator can collide with itself.
      ///   todo Would be worth profiling to see if calling new ConvexHullCalculator
      ///   would benefit per invocation.
      /// </summary>
      private ConvexHullCalculator
        convexHullCalculator = new();

      [NonSerialized] public List<MeshCollider> convexHullMeshColliders = new();

      /// <summary>
      ///   A list of Convex Hull GameObjects. These gameobjects can be updated by the
      ///   debug flags.
      /// </summary>
      [NonSerialized] public List<GameObject> convexHullMeshes = new();

      /// <summary>
      ///   List of Convex Hull Preview Meshes. These meshes must placed within a
      ///   container that should not have a Rigidbody parent.
      /// </summary>
      [NonSerialized] public List<GameObject> convexHullPreviewMeshes = new();

      [NonSerialized]
      public List<MeshRenderer> convexHullPreviewMeshRendererItems = new();

      [NonSerialized] public List<GameObject> convexHullTriggerMeshes = new();

      /// <summary>
      ///   Allows for additional overrides. This should be a function provided in any
      ///   class extending ConvexHullAPI
      /// </summary>
      /// <param name="val"></param>
      /// <returns></returns>
      public Func<string, bool> IsAllowedAsHullOverride = s => false;

      [NonSerialized] public List<MeshCollider> newMeshColliders = new();

      public virtual void Awake()
      {
        if (m_fallbackMaterial != null && BubbleMaterial == null)
          BubbleMaterial = m_fallbackMaterial;

        if (m_fallbackMaterial != null && DebugMaterial == null)
          DebugMaterial = m_fallbackMaterial;

        PreviewParent = transform;
        m_rigidbody = GetComponent<Rigidbody>();
      }

      public virtual void Start()
      {
        if (!m_colliderParentTransform)
        {
          m_colliderParentTransform = transform.Find(colliderParentName);
        }

        // if (convexMeshLayer == 29)
        // {
        //   convexMeshLayer = LayerHelpers.CustomRaftLayer;
        // }

        if (!parentTransform)
        {
          parentTransform = transform;
        }
      }

      private void OnEnable()
      {
        if (Instances.Contains(this)) return;

        Instances.Add(this);
      }

      private void OnDisable()
      {
        if (!Instances.Contains(this)) return;

        Instances.Remove(this);

        DestroyAllConvexMeshes();
      }

      public void OnDrawGizmos()
      {
        if (_cachedConvexHullBounds != null)
        {
          Gizmos.color = Color.green;
          // Gizmos.DrawWireCube(_cachedConvexHullBounds.center + transform.position, _cachedConvexHullBounds.size);
          Gizmos.DrawWireCube(_cachedConvexHullBounds.center + m_colliderParentTransform.position, Vector3.one);
        }

        if (!m_rigidbody)
        {
          m_rigidbody = GetComponent<Rigidbody>();

        }

        if (m_rigidbody)
        {
          Gizmos.color = Color.white;
          Gizmos.DrawWireCube(m_rigidbody.worldCenterOfMass, Vector3.one);
        }
      }

      public void InitializeConvexMeshGeneratorApi(PreviewModes mode,
        Material debugMaterial, Material bubbleMaterial, Color debugMaterialColor,
        Color bubbleMaterialColor, string meshNamePrefix,
        Action<string> loggerApi)
      {
        MeshNamePrefix = meshNamePrefix;
        LoggerAPI = loggerApi;
        PreviewMode = mode;
        DebugMaterial = debugMaterial;
        DebugMaterialColor = debugMaterialColor;
        BubbleMaterial = bubbleMaterial;
        BubbleMaterialColor = bubbleMaterialColor;
      }

      public void UpdatePropertiesForConvexHulls(
        Vector3 transformPreviewOffset, PreviewModes mode, Color debugColor,
        Color bubbleColor)
      {
        PreviewMode = mode;
        DebugMaterialColor = debugColor;
        BubbleMaterialColor = bubbleColor;

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

        if (!collider.enabled) return points;
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


      public static List<Vector3> GetColliderPointsGlobal(Collider collider)
      {
        var points = new List<Vector3>();

        if (!collider.enabled) return points;
        // filters out layer that should not be considered physical during generation
        if (!LayerHelpers.IsContainedWithinLayerMask(collider.gameObject.layer,
              LayerHelpers.PhysicalLayerMask) ||
            !collider.gameObject.activeInHierarchy) return points;


        switch (collider)
        {
          // Handle BoxColliedr
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

      public void AddLocalPhysicMaterial(PhysicsMaterial material)
      {
        localPhysicMaterial = material;
        HasPhysicMaterial = true;
      }
      /// <summary>
      ///   Applies a PhysicMaterial on to any new convexHullApi generated collider.
      ///   Useful for making things frictionless
      /// </summary>
      public void AddLocalPhysicMaterial(float dynamicFriction, float staticFriction)
      {
        PhysicMaterialDynamicFriction = dynamicFriction;
        PhysicMaterialStaticFriction = staticFriction;

        AddLocalPhysicMaterial(CreatePhysicMaterial());
      }

      private PhysicsMaterial CreatePhysicMaterial()
      {
        if (localPhysicMaterial != null) return localPhysicMaterial;
        var physicsMaterial = new PhysicsMaterial("ConvexHullAPI_local_PhysicMaterial")
        {
          dynamicFriction = PhysicMaterialDynamicFriction, // Low friction so it slides over objects
          staticFriction = PhysicMaterialStaticFriction, // Prevents excessive sticking
          bounciness = 0.0f, // No bounce effect
          frictionCombine = PhysicsMaterialCombine.Multiply // Ensures friction rapidly increases as we slow down.
        };
        return physicsMaterial;
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

      public void GenerateUnderwaterBoxWrapper()
      {
        foreach (var meshCollider in convexHullMeshColliders.ToList())
        {
          if (meshCollider == null)
          {
            convexHullMeshColliders.Remove(meshCollider);
            continue;
          }

          // Create the box GameObject
          CreateEncapsulationBox(_cachedConvexHullBounds.center, _cachedConvexHullBounds.size);
        }
      }

      private void EncapsulateMeshCollider(MeshCollider collider, float buffer,
        out Vector3 boxCenter, out Vector3 boxSize)
      {
        // Access the mesh
        var mesh = collider.sharedMesh;
        if (mesh == null)
        {
          Debug.LogError("MeshCollider does not have a valid mesh.");
          boxCenter = Vector3.zero;
          boxSize = Vector3.zero;
          return;
        }

        // Get the vertices of the mesh in world space
        var vertices = mesh.vertices;
        var colliderTransform = collider.transform;

        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;

        // Compute world-space bounds of all vertices
        foreach (var localVertex in vertices)
        {
          var worldVertex = colliderTransform.TransformPoint(localVertex);
          min = Vector3.Min(min, worldVertex);
          max = Vector3.Max(max, worldVertex);
        }

        // Calculate the center and size of the box
        boxCenter = (min + max) / 2;
        boxSize = max - min;

        // Add buffer to the size
        boxSize +=
          new Vector3(buffer, buffer,
            buffer); // Add a small buffer to ensure it encapsulates the mesh
      }

      private void CreateEncapsulationBox(Vector3 center, Vector3 size)
      {
        // Create a new GameObject for the box
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "VehicleShip_HullUnderwaterBox";
        box.gameObject.layer = LayerHelpers.IgnoreRaycastLayer;

        // Set the box's position to the calculated center

        // Scale the box to match the calculated size

        // Optionally, assign it as a child of the MeshCollider's GameObject for better organization
        box.transform.SetParent(PreviewParent);
        box.transform.localScale = size;
        box.transform.localPosition = center;
        box.transform.localRotation = Quaternion.identity;

        // Set a transparent material for visualization (optional)
        var boxRenderer = box.GetComponent<Renderer>();
        if (boxRenderer != null)
        {
          boxRenderer.material = GetMaterial();
          boxRenderer.shadowCastingMode = ShadowCastingMode.Off;
          boxRenderer.receiveShadows = false;
          boxRenderer.lightProbeUsage = LightProbeUsage.Off;
          boxRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        // Prevent the box from interfering with physics by removing its collider
        Destroy(box.GetComponent<Collider>());

        convexHullPreviewMeshes.Add(box);
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
              LayerHelpers.IsContainedWithinLayerMask(x.gameObject.layer,
                LayerHelpers.PhysicalLayerMask) && x.gameObject.activeInHierarchy)
            .ToList();

        return colliders;
      }

      /// <summary>
      ///   Groups colliders by proximity, handling nested or overlapping colliders
      ///   correctly. Allows combining colliders together into a massive mesh. Or
      ///   splitting if the combination range is too large.
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
      ///   Groups colliders by proximity and returns separate clusters for convex hull
      ///   generation.
      /// </summary>
      public List<List<Collider>> GroupCollidersByProximityForPieceData(List<PrefabPieceData> items, float proximityThreshold)
      {
        var clusters = new List<List<Collider>>();

        foreach (var prefabPieceData in items)
        {
          var addedToCluster = false;

          foreach (var cluster in clusters)
          {
            foreach (var collider in prefabPieceData.HullColliders)
            {
              if (IsColliderNearCluster(collider, cluster, proximityThreshold))
              {
                cluster.Add(collider);
                addedToCluster = true;
                break;
              }
            }

            if (addedToCluster) break;
          }

          if (!addedToCluster)
          {
            clusters.Add(new List<Collider>(prefabPieceData.HullColliders));
          }
        }

        MergeNearbyClusters(clusters, proximityThreshold);
        return clusters;
      }

      /// <summary>
      ///   Determines if a collider is near an existing cluster.
      /// </summary>
      public static bool IsColliderNearCluster(Collider collider, List<Collider> cluster, float threshold)
      {
        foreach (var clusterCollider in cluster)
        {
          if (Vector3.Distance(collider.bounds.ClosestPoint(clusterCollider.bounds.center), clusterCollider.bounds.ClosestPoint(collider.bounds.center)) < threshold)
          {
            return true;
          }
        }
        return false;
      }

      /// <summary>
      ///   Determines if a collider is near an existing cluster.
      /// </summary>
      public static bool IsBoundsNearCluster(PrefabColliderPointData targetColliderPointData, List<PrefabColliderPointData> data, float threshold)
      {
        foreach (var item in data)
        {
          if (Vector3.Distance(item.LocalBounds.ClosestPoint(targetColliderPointData.LocalBounds.center), targetColliderPointData.LocalBounds.ClosestPoint(item.LocalBounds.center)) < threshold)
          {
            return true;
          }
        }
        return false;
      }

      /// <summary>
      ///   Merges clusters that are too close to each other.
      /// </summary>
      private static void MergeNearbyClusters(List<List<Collider>> clusters, float proximityThreshold)
      {
        for (var i = 0; i < clusters.Count; i++)
        {
          for (var j = i + 1; j < clusters.Count; j++)
          {
            foreach (var collider in clusters[i])
            {
              if (clusters[j].Any(otherCollider =>
                    Vector3.Distance(collider.bounds.center, otherCollider.bounds.center) < proximityThreshold))
              {
                clusters[i].AddRange(clusters[j]);
                clusters.RemoveAt(j);
                j--;
                break;
              }
            }
          }
        }
      }

      public static void DeleteMeshesFromChildColliders(
        List<GameObject> generateObjectList)
      {
        if (generateObjectList.Count == 0) return;
        var objects = generateObjectList.ToList();
        generateObjectList.Clear();
        foreach (var obj in objects) DebugUnityHelpers.AdaptiveDestroy(obj);
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
      ///   TODO refactor this so it does not destroy, only mutates the already existing
      ///   mesh object. This way the colliders do not have to be rebound to physics
      ///   ignores.
      /// </summary>
      public void DestroyAllConvexMeshes()
      {
        DeleteMeshesFromChildColliders(convexHullMeshes);
        convexHullMeshColliders.Clear();

        DeleteMeshesFromChildColliders(convexHullPreviewMeshes);
        DeleteMeshesFromChildColliders(convexHullTriggerMeshes);
      }

      public void DestroyRemainingColliders(int clusterCount)
      {
        // If we have MORE meshes than required clusters, delete the extras.
        var extras = convexHullMeshes.Count - clusterCount;
        if (extras <= 0) return;

        for (var i = convexHullMeshes.Count - 1; i >= clusterCount; i--)
        {
          var go = convexHullMeshes[i];
          if (go == null)
          {
            convexHullMeshes.RemoveAt(i);
            continue;
          }

          var meshCollider = go.GetComponent<MeshCollider>();
          if (meshCollider && convexHullMeshColliders.Contains(meshCollider))
          {
            convexHullMeshColliders.Remove(meshCollider);
          }

          Destroy(go);
          convexHullMeshes.RemoveAt(i);
        }
      }

      /// <summary>
      ///   Main generator for convex hull meshes.
      /// </summary>
      /// <param name="childGameObjects"></param>
      /// <param name="targetParentGameObject">
      ///   ConvexHullParent where all convexHull
      ///   GameObjects are places
      /// </param>
      /// <param name="distanceThreshold"></param>
      /// <param name="triggerParent"></param>
      public void GenerateMeshesFromChildColliders(
        GameObject targetParentGameObject, Vector3 offset,
        float distanceThreshold, List<GameObject> childGameObjects,
        Transform? triggerParent = null)
      {
        if (ShouldDestroyOnGenerate)
        {
          DestroyAllConvexMeshes();
        }


        var hullClusters =
          GroupCollidersByProximity(childGameObjects.ToList(),
            distanceThreshold);

        DestroyRemainingColliders(hullClusters.Count);

        for (var index = 0; index < hullClusters.Count; index++)
        {
          var hullCluster = hullClusters[index];
          var colliderPoints = UseWorld
            ? GetAllColliderPointsAsWorldPoints(hullCluster)
            : GetColliderPointsLocal(hullCluster);

          GenerateConvexHullMesh(colliderPoints,
            targetParentGameObject.transform, offset, index);
        }

        PostGenerateConvexMeshes();
      }

      public void PostGenerateConvexMeshes()
      {
        CreatePreviewConvexHullMeshes();
        if (TriggerParent != null) CreateTriggerConvexHullMeshes(TriggerParent);
        UpdateConvexHullBounds();
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
          uv = sourceMesh.uv,
          indexFormat = sourceMesh.indexFormat
        };
        readableMesh.RecalculateBounds();
        return readableMesh;
      }

      public Material? GetMaterial()
      {
        if (PreviewMode == PreviewModes.Bubble && BubbleMaterial != null)
        {
          BubbleMaterial.color = BubbleMaterialColor;
          BubbleMaterial.SetFloat(MaxHeightShaderId, 29f);
          return BubbleMaterial;
        }

        if (PreviewMode == PreviewModes.Debug && DebugMaterial != null)
        {
          DebugMaterial.color = DebugMaterialColor;
          return DebugMaterial;
        }

        return null;
      }

      /// <summary>
      ///   Renders a mesh for visualizing the hull. Not meant for casual players but
      ///   worth using in creative mode.
      /// </summary>
      /// <param name="go"></param>
      public void AddConvexHullMeshRenderer(GameObject go, bool isPreview = false)
      {
        if (PreviewMode == PreviewModes.None) return;
        var material = GetMaterial();
        if (material != null)
        {
          var meshRenderer = go.GetComponent<MeshRenderer>();
          if (!meshRenderer) meshRenderer = go.AddComponent<MeshRenderer>();

          if (!convexHullPreviewMeshRendererItems.Contains(meshRenderer))
          {
            convexHullPreviewMeshRendererItems.Add(meshRenderer);
          }

          meshRenderer.shadowCastingMode =
            ShadowCastingMode.Off;
          meshRenderer.receiveShadows = false;

          meshRenderer.lightProbeUsage =
            LightProbeUsage.Off; // Disable Light Probes
          meshRenderer.reflectionProbeUsage =
            ReflectionProbeUsage.Off; // Disable Reflection Probes

          meshRenderer.sharedMaterial = material;

          if (PreviewMode == PreviewModes.Debug)
            if (meshRenderer.sharedMaterial.color != DebugMaterialColor)
              meshRenderer.sharedMaterial.color = DebugMaterialColor;

          if (PreviewMode == PreviewModes.Bubble)
            if (meshRenderer.sharedMaterial.color != BubbleMaterialColor)
              meshRenderer.sharedMaterial.color = BubbleMaterialColor;
        }
      }

      public void CreateTriggerConvexHullMeshes(Transform triggerParent)
      {
        DeleteMeshesFromChildColliders(convexHullTriggerMeshes);
        convexHullTriggerMeshes.Clear();

        // Rigidbody must not be in preview parent otherwise it would have its colliders applied as physics.
        var rbComponent = PreviewParent.GetComponent<Rigidbody>();

        if (rbComponent == null)
          rbComponent = PreviewParent.GetComponentInParent<Rigidbody>();

        // if (rbComponent != null && !rbComponent.isKinematic)
        // {
        //   LoggerAPI(
        //     "Error: This component is invalid due to preview parent containing a non-kinematic Rigidbody. Using a non-kinematic rigidbody will cause this preview to desync.");
        //   return;
        // }

        if (rbComponent != null)
        {
          rbComponent.includeLayers = LayerHelpers.PhysicalLayerMask;
          rbComponent.excludeLayers =
            LayerHelpers.RamColliderExcludeLayers;
        }

        if (triggerParent == null)
        {
          LoggerAPI("Error: triggerParent is null.");
          return;
        }

        if (convexHullMeshes.Count > 0)
          for (var index = 0; index < convexHullMeshes.Count; index++)
          {
            var convexHullMesh = convexHullMeshes[index];

            var triggerInstance = GetOrInstantiateMesh(convexHullMesh, index, triggerParent, convexHullTriggerMeshes);

            triggerInstance.transform.localScale = Vector3.one;

            // It must remain at the parent position (world position but it will be 0,0,0 local position)
            // triggerInstance.transform.position = transform.position;

            var triggerObjName = $"{MeshNameTriggerPrefix}_damage_trigger_{index}";
            triggerInstance.gameObject.name = triggerObjName;

            var meshRenderer = triggerInstance.GetComponent<MeshRenderer>();
            if (meshRenderer != null) Destroy(meshRenderer);

            // Handle the MeshCollider for the preview instance
            var triggerMeshCollider =
              triggerInstance.GetComponent<MeshCollider>();
            triggerMeshCollider.isTrigger = true;
            triggerMeshCollider.includeLayers = LayerHelpers.PhysicalLayerMask;
            triggerMeshCollider.excludeLayers =
              LayerHelpers.RamColliderExcludeLayers;
          }
      }

      /// <summary>
      ///   Todo this might need to be refactored to determine the origin point it needs
      ///   to move to. Scale seems inaccurate when resizing a complex mesh.
      /// </summary>
      /// <param name="meshCollider"></param>
      /// <returns></returns>
      public Vector3 GetPreviewScale(MeshCollider meshCollider)
      {
        if (meshCollider.bounds.size.x > 30 || meshCollider.bounds.size.z > 30)
          return new Vector3(1.03f, 1.1f, 1.03f);

        if (meshCollider.bounds.size.x > 10 || meshCollider.bounds.size.z > 10)
          return Vector3.one * 1.05f;

        return Vector3.one * 1.1f;
      }

      public GameObject GetOrInstantiateMesh(GameObject convexHullMesh, int index, Transform parent, List<GameObject> meshList)
      {
        var instance = meshList.Count > index ? meshList[index] : null;

        if (instance == null)
        {
          instance =
            Instantiate(convexHullMesh, parent);
          if (meshList.Count > index)
          {
            meshList[index] = instance;
          }
          else
          {
            meshList.Add(instance);
          }
        }

        return instance;
      }

      /// <summary>
      ///   Preview meshes are to be only used for visual purposes.
      /// </summary>
      public void CreatePreviewConvexHullMeshes()
      {
        if (!HasPreviewGeneration) return;
        // if (ShouldDestroyOnGenerate)
        // {
        DeleteMeshesFromChildColliders(convexHullPreviewMeshes);
        // must clear previews too
        convexHullPreviewMeshRendererItems.Clear();
        // }

        if (PreviewMode == PreviewModes.None) return;
        if (PreviewMode == PreviewModes.Bubble)
        {
          GenerateUnderwaterBoxWrapper();
          return;
        }

        // Rigidbody must not be in preview parent otherwise it would have its colliders applied as physics.
        var rbComponent = PreviewParent.GetComponent<Rigidbody>();

        if (rbComponent == null)
          rbComponent = PreviewParent.GetComponentInParent<Rigidbody>();

        // if (rbComponent != null && !rbComponent.isKinematic)
        // {
        //   LoggerAPI(
        //     "Error: This component is invalid due to preview parent containing a non-kinematic Rigidbody. Using a non-kinematic rigidbody will cause this preview to desync.");
        //   return;
        // }

        if (PreviewParent == null)
        {
          LoggerAPI("Error: PreviewParent is null.");
          return;
        }

        if (convexHullMeshes.Count > 0)
          for (var index = 0; index < convexHullMeshes.Count; index++)
          {
            var convexHullMesh = convexHullMeshes[index];
            var previewInstance = GetOrInstantiateMesh(convexHullMesh, index, PreviewParent, convexHullPreviewMeshes);

            var previewMeshCollider =
              previewInstance.GetComponent<MeshCollider>();

            previewInstance.transform.localScale =
              PreviewMode == PreviewModes.Debug
                ? previewScale
                : Vector3.one;
            // World-space position offset calculation
            // var parentOffset =
            //   convexHullMesh.transform.TransformPoint(previewInstance
            //     .transform
            //     .position);
            //
            // previewInstance.transform.position =
            //   parentOffset - PreviewParent.transform.position +
            //   transformPreviewOffset;
            previewInstance.transform.localPosition += transformPreviewOffset;


            var meshFilter = previewInstance.AddComponent<MeshFilter>();
            meshFilter.mesh = previewMeshCollider.sharedMesh;
            AddConvexHullMeshRenderer(previewInstance.gameObject);

            var previewName = $"{MeshNamePreviewPrefix}_{index}";
            previewInstance.gameObject.name = previewName;

            // Handle the MeshCollider for the preview instance
            if (previewMeshCollider != null)
              DebugUnityHelpers.AdaptiveDestroy(previewMeshCollider);
          }
      }

      public void GenerateConvexHullMesh(
        List<Vector3> points, Transform parentObjTransform, Vector3 offset, int meshIndex)
      {
        if (points.Count < 3)
        {
          Debug.LogError($"Not enough points to generate a convex hull. This is for parentObjTransform: {parentObjTransform.name}");
          return;
        }

        if (parentTransform != parentObjTransform)
        {
          parentTransform = parentObjTransform;
          convexHullCalculator = new ConvexHullCalculator();
        }


        var localPoints = points
          .Select(x => parentObjTransform.InverseTransformPoint(x) + offset).ToList();

        _cachedPoints = localPoints;

        // Prepare output containers
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var normals = new List<Vector3>();

        // Generate convex hull and export the mesh
        // let this calculator garbage collect if the parentTransform is different.
        convexHullCalculator.GenerateHull(localPoints, false, ref verts,
          ref tris,
          ref normals, out var hasBailed);

        GenerateMeshFromConvexOutput(verts.ToArray(), tris.ToArray(), normals.ToArray(), meshIndex);
      }

      public void GenerateMeshFromConvexOutput(Vector3[] vertices, int[] triangles, Vector3[] normals, int meshIndex)
      {
        var meshObject = convexHullMeshes.Count > meshIndex ? convexHullMeshes[meshIndex] : null;

        // First step is to either update previous mesh or generate a new one.
        Mesh mesh;

        if (meshObject != null)
        {
          var existingCollider = meshObject.GetComponent<MeshCollider>();
          mesh = existingCollider && existingCollider.sharedMesh != null
            ? existingCollider.sharedMesh
            : new Mesh { name = $"{MeshNamePrefix}_{meshIndex}_mesh" };
        }
        else
        {
          mesh = new Mesh { name = $"{MeshNamePrefix}_{meshIndex}_mesh" };
        }

        // Ensure index format handles large meshes
        mesh.indexFormat = vertices != null && vertices.Length > 65535
          ? IndexFormat.UInt32
          : IndexFormat.UInt16;

        // Clear previous data to avoid stale geometry when counts shrink/grow
        mesh.Clear(false);

        // Defensive: triangles must be a multiple of 3
        if (triangles != null && triangles.Length % 3 != 0)
        {
          var valid = triangles.Length / 3 * 3;
          if (valid == 0)
          {
            // Avoid setting any triangles; let it be empty
            triangles = Array.Empty<int>();
          }
          else
          {
            Array.Resize(ref triangles, valid);
          }
        }

        // Assign data
        if (vertices != null && vertices.Length > 0)
        {
          // Using SetVertices/SetNormals is generally safer for dynamic data,
          // but array properties are fine too; keep it simple:
          mesh.SetVertices(vertices);

          if (normals != null && normals.Length == vertices.Length)
            mesh.SetNormals(normals);
          else
            mesh.RecalculateNormals(); // fallback if normals missing/mismatched
        }
        else
        {
          mesh.SetVertices(Array.Empty<Vector3>());
          mesh.SetNormals(Array.Empty<Vector3>());
        }

        if (triangles != null && triangles.Length > 0)
        {
          mesh.SetTriangles(triangles, 0, true);
        }
        else
        {
          mesh.SetTriangles(Array.Empty<int>(), 0, true);
        }

        mesh.RecalculateBounds();

        // Do not create another mesh instance if it already exists. Doing this require every collider to be ignored.
        if (meshObject == null)
        {
          // Create a new GameObject to display the mesh
          meshObject =
            new GameObject($"{MeshNamePrefix}_{meshIndex}")
            {
              layer = LayerHelpers.CustomRaftLayer,
              transform =
              {
                parent = parentTransform,
                position = parentTransform.transform.position
              }
            };

          // this mesh will be updated frequently
          mesh.MarkDynamic();

          convexHullMeshes.Add(meshObject);
        }

        // Add/Find the MeshCollider
        var meshCollider = meshObject.GetComponent<MeshCollider>();
        if (!meshCollider)
        {
          meshCollider = meshObject.AddComponent<MeshCollider>();
        }

        if (HasPhysicMaterial)
        {
          meshCollider.material = localPhysicMaterial;
        }

        if (!convexHullMeshColliders.Contains(meshCollider))
        {
          convexHullMeshColliders.Add(meshCollider);
        }

        // Cooking options that help stability when geometry changes a lot
        try
        {
          meshCollider.cookingOptions =
            MeshColliderCookingOptions.EnableMeshCleaning
            | MeshColliderCookingOptions.WeldColocatedVertices
            | MeshColliderCookingOptions.UseFastMidphase; // Unity 2022+
        }
        catch
        {
          // Some platforms may not support all flags; ignore
        }

        meshCollider.convex = true;
        meshCollider.excludeLayers = LayerHelpers.BlockingColliderExcludeLayers;
        meshCollider.includeLayers = LayerHelpers.PhysicalLayerMask;
        meshCollider.transform.localRotation = Quaternion.identity;

        // Force a clean recook: reassign sharedMesh
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
      }

      /// <summary>
      ///   Returns the sum of all convexHulls as a bounds, this can be non-relative but
      ///   requires a transform.
      /// </summary>
      /// <param name="isRelative"></param>
      /// <param name="worldPosition"></param>
      /// <returns></returns>
      public Bounds GetConvexHullBounds(bool isRelative, Vector3? worldPosition = null)
      {
        if (isRelative) return _cachedConvexHullBounds;
        worldPosition ??= transform.position;
        // todo may need to add x and z with world position too.

        var centerWithWorldY = new Vector3(worldPosition.Value.x + _cachedConvexHullBounds.center.x, worldPosition.Value.y, worldPosition.Value.z + _cachedConvexHullBounds.center.z);
        return new Bounds(centerWithWorldY, _cachedConvexHullBounds.size);
      }

      /// <summary>
      ///   This needs to be manually called after GenerateMeshesFromChildColliders is
      ///   invoked in order to regenerate the accurate colliders.
      ///   This is heavier than using local points, but it should prevent issues with
      ///   transform mutating the local meshcollider actual point position.
      /// </summary>
      public void UpdateConvexHullBounds()
      {
        var localPoints = convexHullMeshColliders.SelectMany(x => x.sharedMesh.vertices).Select(x => transform.TransformPoint(x)).Select(x => transform.InverseTransformPoint(x)).ToList();

        if (localPoints.Count > 0)
        {
          var convexHullBounds = new Bounds(localPoints[0], Vector3.zero);
          localPoints.ForEach(x => convexHullBounds.Encapsulate(x));
          _cachedConvexHullBounds = convexHullBounds;
        }
      }
    }
  }