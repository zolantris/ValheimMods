#region

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zolantris.Shared;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  public class MovingTreadComponent : MonoBehaviour
  {
    public const float treadPointDistanceZ = 0.624670029f;
    public const float treadPointYOffset = 1.578f;
    public const float treadRadiusScale1 = 0.789f;

    private const float lastTerrainTouchTimeExpiration = 10f;
    private const float convexHullClusterThreshold = 50f;

    public static GameObject fallbackPrefab = null!;

    internal static Vector3 tread_meshScalar = new(2f, 0.0500000007f, 0.599999964f);

    internal static readonly List<LocalTransform> _treadFrontLocalPoints = new()
    {
      new LocalTransform
      {
        position = new Vector3(0f, 1.57800007f, -treadPointDistanceZ),
        rotation = Quaternion.identity
      },
      new LocalTransform
      {
        position = new Vector3(0.0f, 1.57800007f, 0f),
        rotation = Quaternion.identity
      },
      new LocalTransform
      {
        position = new Vector3(0.0f, 1.4f, 0.542955399f),
        rotation = Quaternion.Euler(45f, 0f, 0f)
      },
      new LocalTransform
      {
        position = new Vector3(0.0f, 0.782000542f, 0.756231308f),
        rotation = Quaternion.Euler(90f, 0f, 0f)
      },
      new LocalTransform
      {
        position = new Vector3(0.0f, 0.245388985f, 0.542955399f),
        rotation = Quaternion.Euler(135f, 0f, 0f)
      },
      new LocalTransform
      {
        position = new Vector3(0f, 0f, 0f),
        rotation = Quaternion.Euler(180f, 0f, 0f)
      }
    };

    internal static readonly List<LocalTransform> _treadBackLocalPoints = new()
    {
      new LocalTransform
      {
        position = new Vector3(0.0f, 0.0f, 0.0f),
        rotation = Quaternion.Euler(180f, 0f, 0f)
      },
      new LocalTransform
      {
        position = new Vector3(0.0f, 0.245388985f, -0.542955399f),
        rotation = Quaternion.Euler(225f, 0f, 0f)
      },
      new LocalTransform
      {
        position = new Vector3(0.0f, 0.782000542f, -0.756231308f),
        rotation = Quaternion.Euler(270f, 0f, 0f)
      },
      new LocalTransform
      {
        position = new Vector3(0.0f, 1.34000015f, -0.542955399f),
        rotation = Quaternion.Euler(315f, 0f, 0f)
      }
    };

    private static readonly Quaternion flippedXRotation = Quaternion.Euler(180f, 0f, 0f);

    public VehicleLandMovementController vehicleLandMovementController;
    public List<HingeJoint> _wheelRotators = new();
    public Transform rotatorParent;
    public GameObject treadPrefab;
    public float treadTopLocalPosition = treadPointYOffset;
    public float treadBottomLocalPosition;

    public Transform treadParent;
    public List<Vector3> originalTreadPositions = new();
    public Bounds localBounds = new(Vector3.zero, Vector3.one);
    public Bounds vehicleLocalBounds = new(Vector3.zero, Vector3.one);
    public GameObject CenterObj;

    public bool isForward = true;

    public ConvexHullAPI convexHullComponent;
    public float speedMultiplierOverride;
    public PhysicsMaterial treadPhysicMaterialOverride;

    internal readonly Dictionary<Rigidbody, float> _treadProgress = new();
    internal readonly List<Rigidbody> _movingTreads = new();
    internal readonly Dictionary<Rigidbody, int> _treadTargetPointMap = new();
    internal readonly List<LocalTransform> _treadTargetPoints = new();

    private readonly List<GameObject> _generatedTreadObjects = new();

    private bool _hasInitLocalBounds;
    private float _lastTerrainTouchDeltaTime = -100f;
    private float speedMultiplier = 1f;

    internal WheelCollider[] wheelColliders = { };

    public void Awake()
    {
      rotatorParent = transform.Find("rotators");

      if (!treadPrefab && fallbackPrefab)
      {
        treadPrefab = fallbackPrefab;
      }

      if (!treadParent)
      {
        treadParent = transform.Find("treads");
      }

      if (!treadParent)
      {
        treadParent = transform;
      }

      if (rotatorParent && _wheelRotators.Count == 0)
      {
        _wheelRotators = rotatorParent.GetComponentsInChildren<HingeJoint>().ToList();
      }
    }

    public void Start()
    {
      if (!vehicleLandMovementController)
      {
        vehicleLandMovementController = GetComponentInParent<VehicleLandMovementController>();
      }

      InitConvexHullComponent();
    }

    public void FixedUpdate()
    {
      if (!isActiveAndEnabled) return;
      UpdateAllTreads();
    }

    public void OnEnable()
    {
      CleanUpRuntimeState();
    }

    public void OnDisable()
    {
      FullCleanup();
    }

    public void OnDestroy()
    {
      FullCleanup();

      if (CenterObj)
      {
        Destroy(CenterObj);
      }
    }

    private void OnCollisionStay(Collision collision)
    {
      if (collision.gameObject.layer == LayerHelpers.TerrainLayer)
      {
        _lastTerrainTouchDeltaTime = Time.fixedTime;
      }
    }

    public void InitConvexHullComponent()
    {
      if (!vehicleLandMovementController) return;

      if (!convexHullComponent)
      {
        convexHullComponent = gameObject.GetComponent<ConvexHullAPI>();
      }

      if (!convexHullComponent)
      {
        convexHullComponent = gameObject.AddComponent<ConvexHullAPI>();
      }

      convexHullComponent.m_colliderParentTransform = treadParent;
      convexHullComponent.parentTransform = treadParent;
      convexHullComponent.HasPreviewGeneration = false;
      convexHullComponent.ShouldDestroyOnGenerate = true;
      convexHullComponent.IsAllowedAsHullOverride = AllowTreadsObject;

      if (treadPhysicMaterialOverride)
      {
        convexHullComponent.AddLocalPhysicMaterial(treadPhysicMaterialOverride);
      }
      else if (vehicleLandMovementController.treadPhysicMaterial)
      {
        convexHullComponent.AddLocalPhysicMaterial(vehicleLandMovementController.treadPhysicMaterial);
      }
    }

    public bool IsOnGround()
    {
      return Time.fixedTime - _lastTerrainTouchDeltaTime < lastTerrainTouchTimeExpiration;
    }

    public Bounds GetGlobalBounds()
    {
      if (!CenterObj)
      {
        return new Bounds(transform.position, localBounds.size);
      }

      return new Bounds(CenterObj.transform.position, localBounds.size);
    }

    public bool AllowTreadsObject(string value)
    {
      return value.ToLower().Contains("tread");
    }

    public void UpdateRotators()
    {
      Physics.SyncTransforms();

      _wheelRotators.ForEach(hinge =>
      {
        var centerOfMass = hinge.transform.parent.position;
        hinge.anchor = hinge.transform.InverseTransformPoint(centerOfMass);

        var motor = hinge.motor;
        motor.force = speedMultiplier * speedMultiplierOverride * 50f;
        motor.targetVelocity = speedMultiplier * speedMultiplierOverride * 50f;
        hinge.motor = motor;
      });
    }

    public void CreateCenteringObject()
    {
      if (!CenterObj)
      {
        CenterObj = new GameObject
        {
          name = "Treads_CenterObj",
          layer = LayerMask.NameToLayer("Ignore Raycast"),
          transform = { parent = transform }
        };
      }

      CenterObj.transform.localRotation = Quaternion.identity;
      CenterObj.transform.localScale = Vector3.one;
    }

    private void CleanUpGeneratedTreads()
    {
      foreach (var treadObject in _generatedTreadObjects)
      {
        if (treadObject)
        {
          Destroy(treadObject);
        }
      }

      _generatedTreadObjects.Clear();
    }

    private void CleanUpConvexMeshes()
    {
      if (!convexHullComponent) return;
      convexHullComponent.DestroyAllConvexMeshes();
    }

    private void CleanUpRuntimeState()
    {
      _wheelRotators.Clear();
      _movingTreads.Clear();
      _treadTargetPointMap.Clear();
      _treadTargetPoints.Clear();
      _treadProgress.Clear();
      originalTreadPositions.Clear();
      _hasInitLocalBounds = true;
      localBounds = new Bounds(Vector3.zero, Vector3.one);
    }

    public void FullCleanup()
    {
      CleanUpGeneratedTreads();
      CleanUpConvexMeshes();
      CleanUpRuntimeState();
    }

    private void EncapsulateLocalPoint(Vector3 localPosition)
    {
      if (_hasInitLocalBounds)
      {
        _hasInitLocalBounds = false;
        localBounds = new Bounds(localPosition, Vector3.zero);
      }

      localBounds.Encapsulate(localPosition);
      localBounds.Encapsulate(localPosition + Vector3.right * 0.5f);
      localBounds.Encapsulate(localPosition + Vector3.left * 0.5f);
    }

    public void InitSingleTread(GameObject treadGameObject)
    {
      var treadTransform = treadGameObject.transform;
      var localPosition = treadTransform.localPosition;

      EncapsulateLocalPoint(localPosition);

      var scalar = vehicleLandMovementController
        ? vehicleLandMovementController.GetWheelRadiusScalar()
        : VehicleLandMovementController.treadRadiusScalar;

      treadTransform.localScale = Vector3.one * scalar;

      var localPoint = new LocalTransform(treadTransform);
      _treadTargetPoints.Add(localPoint);
      originalTreadPositions.Add(localPosition);

      var rb = AddRigidbodyToChild(treadTransform);
      _movingTreads.Add(rb);
      _treadTargetPointMap[rb] = _movingTreads.Count - 1;
      _treadProgress[rb] = 0f;

      _generatedTreadObjects.Add(treadGameObject);
    }

    private GameObject CreateTreadInstance(string treadName, Vector3 localPosition, Quaternion localRotation)
    {
      var treadGameObject = Instantiate(treadPrefab, treadParent);
      treadGameObject.name = treadName;
      treadGameObject.transform.localPosition = localPosition;
      treadGameObject.transform.localRotation = localRotation;
      treadGameObject.transform.localScale = Vector3.one;
      return treadGameObject;
    }

    private List<GameObject> GetGeneratedTreadGameObjects()
    {
      var treadObjects = new List<GameObject>();

      foreach (Transform child in treadParent)
      {
        if (!child) continue;
        if (!AllowTreadsObject(child.name)) continue;
        treadObjects.Add(child.gameObject);
      }

      return treadObjects;
    }

    private void RecalculateCenterObject()
    {
      CreateCenteringObject();

      CenterObj.transform.localPosition = localBounds.center;
      CenterObj.transform.localRotation = Quaternion.identity;
    }

    private void GenerateConvexHullForCurrentTreads()
    {
      InitConvexHullComponent();

      if (!convexHullComponent) return;

      convexHullComponent.m_colliderParentTransform = treadParent;
      convexHullComponent.parentTransform = treadParent;
      convexHullComponent.DestroyAllConvexMeshes();

      var treadGameObjects = GetGeneratedTreadGameObjects();
      if (treadGameObjects.Count == 0)
      {
        Debug.LogError($"MovingTreadComponent on {name} could not find any generated tread objects for convex hull generation.");
        return;
      }

      Physics.SyncTransforms();

      convexHullComponent.GenerateMeshesFromChildColliders(
        treadParent.gameObject,
        Vector3.zero,
        convexHullClusterThreshold,
        treadGameObjects
      );

      foreach (var meshCollider in convexHullComponent.convexHullMeshColliders)
      {
        if (!meshCollider) continue;

        meshCollider.transform.SetParent(treadParent, true);
        meshCollider.transform.localRotation = Quaternion.identity;
        meshCollider.gameObject.layer = LayerHelpers.PieceLayer;
        meshCollider.gameObject.name = PrefabNames.ConvexTreadCollider;
        meshCollider.includeLayers = LayerMask.GetMask("terrain", "piece", "Default", "static_solid");
        meshCollider.excludeLayers = LayerHelpers.RamColliderExcludeLayers;
        meshCollider.isTrigger = false;
      }

      Physics.SyncTransforms();
      convexHullComponent.UpdateConvexHullBounds();
    }

    public void GenerateTreads(Bounds bounds)
    {
      if (!treadPrefab) return;
      if (!vehicleLandMovementController) return;
      if (!treadParent) return;

      vehicleLocalBounds = bounds;

      FullCleanup();
      CreateCenteringObject();

      treadParent.localRotation = Quaternion.identity;
      treadParent.localScale = Vector3.one;

      var scalar = 1f;
      var horizontalTreads = Mathf.Max(1, Mathf.RoundToInt(bounds.size.z / treadPointDistanceZ / scalar));
      var fullTreadLength = horizontalTreads * treadPointDistanceZ;

      var centeringOffset =
        Vector3.down * treadPointYOffset / 2f / scalar +
        Vector3.forward * -vehicleLocalBounds.extents.z / scalar -
        Vector3.forward * 0.25f;

      for (var i = 0; i < horizontalTreads; i++)
      {
        var zPos = treadPointDistanceZ * i;
        var localPosition = new Vector3(0f, treadPointYOffset, zPos) + centeringOffset;
        var treadGameObject = CreateTreadInstance($"tread_{i}_top", localPosition, Quaternion.identity);
        InitSingleTread(treadGameObject);
      }

      for (var i = 0; i < _treadFrontLocalPoints.Count; i++)
      {
        var point = _treadFrontLocalPoints[i];
        var zPos = point.position.z + fullTreadLength + treadPointDistanceZ;
        var localPosition = new Vector3(point.position.x, point.position.y, zPos) + centeringOffset;
        var treadGameObject = CreateTreadInstance($"tread_{i}_front", localPosition, point.rotation);
        InitSingleTread(treadGameObject);
      }

      for (var i = 0; i < horizontalTreads; i++)
      {
        var localPosition = new Vector3(0f, 0f, treadPointDistanceZ * (horizontalTreads - i)) + centeringOffset;
        var treadGameObject = CreateTreadInstance($"tread_{i}_bottom", localPosition, flippedXRotation);
        InitSingleTread(treadGameObject);
      }

      for (var i = 0; i < _treadBackLocalPoints.Count; i++)
      {
        var point = _treadBackLocalPoints[i];
        var localPosition = point.position + centeringOffset;
        var treadGameObject = CreateTreadInstance($"tread_{i}_back", localPosition, point.rotation);
        InitSingleTread(treadGameObject);
      }

      RecalculateCenterObject();
      GenerateConvexHullForCurrentTreads();
    }

    public void SetSpeed(float speed)
    {
#if DEBUG
      if (speedMultiplierOverride != 0)
      {
        speedMultiplier = speedMultiplierOverride;
      }
#endif

      switch (speed)
      {
        case < 0.01f:
          speed = 0f;
          break;
        case < 0.1f:
        {
          var dir = Mathf.Sign(speed);
          speed = dir * 0.01f;
          break;
        }
      }

      speedMultiplier = speed;
    }

    public void SetDirection(bool isDirectionForward)
    {
      isForward = isDirectionForward;
    }

    public static Rigidbody AddRigidbodyToChild(Transform child)
    {
      var rb = child.GetComponent<Rigidbody>();
      if (!rb)
      {
        rb = child.gameObject.AddComponent<Rigidbody>();
      }

      rb.mass = 20f;
      rb.linearDamping = 0f;
      rb.angularDamping = 0f;
      rb.useGravity = false;
      rb.isKinematic = true;
      return rb;
    }

    public void UpdateAllTreads()
    {
      if (!vehicleLandMovementController) return;
      if (_treadTargetPoints.Count != _movingTreads.Count)
      {
        Debug.LogError($"Tread TargetPoints {_treadTargetPoints.Count} larger than moving treads {_movingTreads.Count}. Exiting treads to prevent error");
        return;
      }

      var parentPosition = treadParent.position;
      var parentRotation = treadParent.rotation;

      for (var i = 0; i < _movingTreads.Count; i++)
      {
        var currentTreadRb = _movingTreads[i];
        if (!currentTreadRb) continue;

        var currentTreadTarget = _treadTargetPointMap[currentTreadRb];

        if (!isForward)
        {
          currentTreadTarget = _movingTreads.Count - 1 - currentTreadTarget;
        }

        if (currentTreadTarget >= _movingTreads.Count)
        {
          currentTreadTarget = 0;
        }
        else if (currentTreadTarget < 0)
        {
          currentTreadTarget = _movingTreads.Count - 1;
        }

        var localTransform = _treadTargetPoints[currentTreadTarget];
        var worldTargetPosition = parentRotation * localTransform.position + parentPosition;
        var worldTargetRotation = parentRotation * localTransform.rotation;

        var prevTreadTarget = isForward ? currentTreadTarget - 1 : currentTreadTarget + 1;
        if (prevTreadTarget >= _movingTreads.Count) prevTreadTarget = 0;
        if (prevTreadTarget < 0) prevTreadTarget = _movingTreads.Count - 1;

        var prevLocalTransform = _treadTargetPoints[prevTreadTarget];
        var worldPrevPosition = parentRotation * prevLocalTransform.position + parentPosition;
        var worldPrevRotation = parentRotation * prevLocalTransform.rotation;

        var progress = _treadProgress[currentTreadRb];
        progress += Time.fixedDeltaTime * speedMultiplier;

        if (progress > 1f)
        {
          progress = 0f;
          currentTreadTarget = isForward ? currentTreadTarget + 1 : currentTreadTarget - 1;

          if (currentTreadTarget >= _movingTreads.Count) currentTreadTarget = 0;
          if (currentTreadTarget < 0) currentTreadTarget = _movingTreads.Count - 1;

          _treadProgress[currentTreadRb] = progress;
        }

        _treadProgress[currentTreadRb] = progress;

        var newPosition = Vector3.Lerp(worldPrevPosition, worldTargetPosition, progress);
        var newRotation = Quaternion.Lerp(worldPrevRotation, worldTargetRotation, progress);

        currentTreadRb.Move(newPosition, newRotation);
      }
    }

    public struct LocalTransform
    {
      public Vector3 position;
      public Quaternion rotation;

      public LocalTransform(Transform objTransform)
      {
        position = Vector3.Scale(objTransform.localPosition, objTransform.localScale);
        rotation = objTransform.localRotation;
      }
    }
  }
}