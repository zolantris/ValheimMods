#region

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    private const float _lastTerrainTouchTimeExpiration = 10f;

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
      // top-near
      new LocalTransform
      {
        position = new Vector3(0.0f, 1.4f, 0.542955399f),
        rotation = Quaternion.Euler(45f, 0f, 0f)
      },
      // top middle
      new LocalTransform
      {
        position = new Vector3(0.0f, 0.782000542f, 0.756231308f),
        rotation = Quaternion.Euler(90f, 0f, 0f)
      },
      // top near bottom
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
      // // flat top tread
      new LocalTransform
      {
        position = new Vector3(0.0f, 0.0f, 0.0f),
        rotation = Quaternion.Euler(180f, 0f, 0f)
      },
      // back-near bottom tread
      new LocalTransform
      {
        position = new Vector3(0.0f, 0.245388985f, -0.542955399f),
        rotation = Quaternion.Euler(225, 0f, 0f)
      },
      // back middle tread
      new LocalTransform
      {
        position = new Vector3(0.0f, 0.782000542f, -0.756231308f),
        rotation = Quaternion.Euler(270, 0, 0)
      },
      // back-near-top tread angles 315
      new LocalTransform
      {
        position = new Vector3(0.0f, 1.34000015f, -0.542955399f),
        rotation = Quaternion.Euler(315, 0, 0)
      }
    };
    private static readonly Quaternion flippedXRotation = Quaternion.Euler(180, 0, 0);

    public VehicleWheelController vehicleWheelController;
    public List<HingeJoint> _wheelRotators = new();
    public Transform rotatorParent;
    public GameObject treadPrefab;
    public float treadTopLocalPosition = treadPointYOffset;
    public float treadBottomLocalPosition;

    public Transform treadParent;
    public List<Vector3> originalTreadPositions = new();
    public Bounds localBounds;
    public Bounds vehicleLocalBounds;
    public GameObject CenterObj;

    // New flag to control direction of movement
    public bool isForward = true;

    public ConvexHullAPI convexHullComponent;
    public float speedMultiplierOverride;


    internal readonly Dictionary<Rigidbody, float> _treadProgress = new(); // Stores the progress of each tread (0 to 1)

    private bool _hasInitLocalBounds;
    private float _lastTerrainTouchDeltaTime = 10f;
    internal List<Rigidbody> _movingTreads = new();
    internal Dictionary<Rigidbody, int> _treadTargetPointMap = new();
    internal List<LocalTransform> _treadTargetPoints = new();

    private float lastSpeed;

    // Speed multiplier that controls the overall speed of treads
    private float speedMultiplier = 1f;

    internal WheelCollider[] wheelColliders = {};

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

    /// <summary>
    /// Must be called after as VehicleWheelController sets some properties / binds things.
    /// </summary>
    public void Start()
    {
      InitConvexHullComponent();
    }
    // Update is called once per frame
    public void FixedUpdate()
    {
      if (!isActiveAndEnabled) return;
      UpdateAllTreads();
    }

    public void OnEnable()
    {
      CleanUp();
      GenerateTreads(new Bounds(Vector3.zero, Vector3.one * 4));
    }


    public void OnDisable()
    {
      CleanUp();
    }

    public void OnDestroy()
    {
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
      if (!convexHullComponent)
      {
        convexHullComponent = gameObject.AddComponent<ConvexHullAPI>();
      }

      convexHullComponent.m_colliderParentTransform = treadParent;
      convexHullComponent.HasPreviewGeneration = false;
      convexHullComponent.IsAllowedAsHullOverride = AllowTreadsObject;
      convexHullComponent.AddLocalPhysicMaterial(vehicleWheelController.treadPhysicMaterial);
    }

    public bool IsOnGround()
    {
      return _lastTerrainTouchDeltaTime + _lastTerrainTouchTimeExpiration < Time.fixedTime;
    }

    public Bounds GetGlobalBounds()
    {
      return new Bounds(CenterObj.transform.position, localBounds.size);
    }

    /// <summary>
    /// A lower case string matcher to allow any object starting with treads to be allowed as a convexhull so we can create collisions.
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public bool AllowTreadsObject(string val)

    {
      if (val.ToLower().Contains("tread"))
        return true;
      return false;
    }

    public void UpdateRotators()
    {
      Physics.SyncTransforms();
      _wheelRotators.ForEach(hinge =>
      {
        // Calculate the mesh bounds (you could also use other methods for finding the center)
//       Renderer meshRenderer = hinge.GetComponentInChildren<Renderer>(); // Make sure the object has a Renderer
//       Vector3 meshCenter = meshRenderer.bounds.center; // The center of the mesh's bounds
//
// // Adjust the anchor based on the object's current position and mesh offset
// // Convert the world-space center to local space of the object
//       Vector3 localCenter = hinge.transform.InverseTransformPoint(meshCenter);
//
// // Set the hinge joint's anchor to the local center
//       hinge.anchor = localCenter;
//
// // Optionally, adjust the pivot position to match the hinge anchor if necessary
//       hinge.transform.position = meshCenter; // Optionally re-adjust the pivot position

// Assuming the hinge joint is attached to this object

// Get the center of mass of the object
//       var rb = hinge.transform.GetComponent<Rigidbody>();
// // Set the hinge joint's anchor to the center of mass
//       hinge.anchor = hinge.transform.InverseTransformPoint(rb.worldCenterOfMass);
//
// // Optional: Set the pivot to match the anchor point
//       hinge.transform.position = rb.worldCenterOfMass; // Make sure the object is centered around the hinge
// Assuming the hinge joint is attached to this object
        // HingeJoint hinge = GetComponent<HingeJoint>();

// Get the center of mass of the object
        var centerOfMass = hinge.transform.parent.position; // Or use a different method to calculate the center

// Set the hinge joint's anchor to the center of mass
        hinge.anchor = hinge.transform.InverseTransformPoint(centerOfMass);

// Optional: Set the pivot to match the anchor point
        // hinge.transform.position = centerOfMass; // Make sure the object is centered around the hinge


        var motor = hinge.motor;
        motor.force = speedMultiplier * speedMultiplierOverride * 50f;
        motor.targetVelocity = speedMultiplier * speedMultiplierOverride * 50f;
        hinge.motor = motor;
      });
    }

    public void InitTreadsFromChildren()
    {
      CreateCenteringObject();
      var hasInitLocalBounds = true;
      // Initialize tread positions and add rigidbodies
      for (var i = 0; i < treadParent.childCount; i++)
      {
        var child = treadParent.GetChild(i);
        if (!child) continue;

        // Update bounds for the first time
        if (hasInitLocalBounds)
        {
          hasInitLocalBounds = false;
          localBounds = new Bounds(child.localPosition, Vector3.zero);
        }

        localBounds.Encapsulate(child.GetComponentInChildren<MeshCollider>().bounds);


        var localPoint = new LocalTransform(child.transform);
        _treadTargetPoints.Add(localPoint);

        var rb = AddRigidbodyToChild(child);
        _movingTreads.Add(rb);
        _treadTargetPointMap.Add(rb, i);

        // Initialize progress for each tread (0 to 1)
        _treadProgress[rb] = 0f;
      }
      CenterObj.transform.position = transform.position + localBounds.center;
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
    }

    /// <summary>
    /// All objects / data related to treads must be cleared here. 
    /// </summary>
    public void CleanUp()
    {
      _movingTreads.ForEach(x =>
      {
        if (x) Destroy(x.gameObject);
      });

      _wheelRotators.Clear();
      _movingTreads.Clear();
      _treadTargetPointMap.Clear();
      _treadTargetPoints.Clear();
      _treadProgress.Clear();
      _hasInitLocalBounds = true;
    }

    public void InitSingleTread(GameObject treadGameObject)
    {
      if (_hasInitLocalBounds)
      {
        _hasInitLocalBounds = false;
        localBounds = new Bounds(treadGameObject.transform.localPosition, Vector3.zero);
      }

      localBounds.Encapsulate(treadGameObject.transform.localPosition + Vector3.right * 0.5f);
      localBounds.Encapsulate(treadGameObject.transform.localPosition);
      localBounds.Encapsulate(treadGameObject.transform.localPosition + Vector3.left * 0.5f);


      // scale the tread to the correct height.
      treadGameObject.transform.localScale = Vector3.one * vehicleWheelController.GetWheelRadiusScalar();

      var localPoint = new LocalTransform(treadGameObject.transform);
      _treadTargetPoints.Add(localPoint);

      var rb = AddRigidbodyToChild(treadGameObject.transform);
      _movingTreads.Add(rb);
      _treadTargetPointMap.Add(rb, _movingTreads.Count - 1);

      // Initialize progress for each tread (0 to 1)
      _treadProgress[rb] = 0f;
    }

    /// <summary>
    /// Generates dynamically all treads based on a bounds size. Must be invoked
    /// </summary>
    /// <param name="bounds"></param>
    public void GenerateTreads(Bounds bounds)
    {
      if (!treadPrefab) return;
      if (!vehicleWheelController) return;
      vehicleLocalBounds = bounds;
      CleanUp();

      CreateCenteringObject();
      var scalar = vehicleWheelController.GetWheelRadiusScalar();
      var horizontalTreads = Mathf.RoundToInt(bounds.size.z / treadPointDistanceZ / scalar);

      // scaled from radius of the first wheel.
      var fullTreadLength = horizontalTreads * treadPointDistanceZ;

      // makes the treads within the vehicle
      var centeringOffset = Vector3.down * treadPointYOffset / 2f / scalar + Vector3.forward * -vehicleLocalBounds.extents.z / scalar - Vector3.forward * 0.25f;

      // top treads
      for (var i = 0; i < horizontalTreads; i++)
      {
        var treadGameObject = Instantiate(treadPrefab, treadParent);
        treadGameObject.name = $"tread_{i}_top";

        var zPos = treadPointDistanceZ * i;
        var offset = new Vector3(0, treadPointYOffset, zPos) + centeringOffset;
        treadGameObject.transform.localPosition = offset;
        // Update bounds for the first time
        InitSingleTread(treadGameObject);
      }

      // front treads
      for (var i = 0; i < _treadFrontLocalPoints.Count; i++)
      {
        var x = _treadFrontLocalPoints[i];
        var treadGameObject = Instantiate(treadPrefab, treadParent);
        treadGameObject.name = $"tread_{i}_front";

        // we offset the Z position so that it aligns with the last 
        var zPos = x.position.z + fullTreadLength + treadPointDistanceZ;
        treadGameObject.transform.localPosition = new Vector3(x.position.x, x.position.y, zPos) + centeringOffset;
        treadGameObject.transform.localRotation = x.rotation;

        InitSingleTread(treadGameObject);
      }

      for (var i = 0; i < horizontalTreads; i++)
      {
        var treadGameObject = Instantiate(treadPrefab, treadParent);
        treadGameObject.name = $"tread_{i}_bottom";
        var offset = new Vector3(0, 0, treadPointDistanceZ * (horizontalTreads - i));
        var rotation = flippedXRotation;

        treadGameObject.transform.localPosition = offset + centeringOffset;
        treadGameObject.transform.localRotation = rotation;

        InitSingleTread(treadGameObject);
      }

      for (var i = 0; i < _treadBackLocalPoints.Count; i++)
      {
        var x = _treadBackLocalPoints[i];
        var treadGameObject = Instantiate(treadPrefab, transform.position, Quaternion.identity, treadParent);
        treadGameObject.name = $"tread_{i}_back";

        // since this is zero based it is actually fine to not use relative calcs for z pos.
        treadGameObject.transform.localPosition = x.position + centeringOffset;
        treadGameObject.transform.localRotation = x.rotation;

        InitSingleTread(treadGameObject);
      }

      CenterObj.transform.position = treadParent.position + localBounds.center;
      treadParent.position = CenterObj.transform.position;

      var treadGameObjects = _movingTreads.Select(x => x.gameObject).ToList();
      convexHullComponent.GenerateMeshesFromChildColliders(treadParent.gameObject, Vector3.zero, 50, treadGameObjects);
      convexHullComponent.convexHullMeshColliders.ForEach(x =>
      {
        if (!x) return;
        x.gameObject.name = "convex_tread_collider";
        x.includeLayers = LayerMask.GetMask("terrain");
        x.excludeLayers = LayerHelpers.RamColliderExcludeLayers;
        x.isTrigger = false;
      });
    }

    /// <summary>
    /// Controls visual clamping of tread speed to avoid jittery animations.
    /// </summary>
    ///
    /// This may not need a negative protection as we use direction to control this value
    /// <param name="speed"></param>
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
      // rb.constraints = RigidbodyConstraints.FreezePositionX;
      rb.drag = 0f;
      rb.angularDrag = 0f;
      rb.useGravity = false;
      rb.isKinematic = true;
      return rb;
    }

    public void UpdateAllTreads()
    {
      if (_treadTargetPoints.Count != _movingTreads.Count)
      {
        Debug.LogError($"Tread TargetPoints {_treadTargetPoints.Count} larger than moving treads {_movingTreads.Count}. Exiting treads to prevent error");
        return;
      }

      // World position of the parent object (important to factor in)
      var parentPosition = treadParent.position;
      var parentRotation = treadParent.rotation;

      // Update tread rotation and position
      for (var i = 0; i < _movingTreads.Count; i++)
      {
        var currentTreadRb = _movingTreads[i];
        if (!currentTreadRb) continue;

        var currentTreadTarget = _treadTargetPointMap[currentTreadRb];

        // If moving backward, reverse the order of treads
        if (!isForward)
        {
          currentTreadTarget = _movingTreads.Count - 1 - currentTreadTarget; // Reversed indexing
        }

        // Ensure the targets loop correctly (wrap around)
        if (currentTreadTarget >= _movingTreads.Count)
        {
          currentTreadTarget = 0;
        }
        else if (currentTreadTarget < 0)
        {
          currentTreadTarget = _movingTreads.Count - 1;
        }

        var localTransform = _treadTargetPoints[currentTreadTarget];

        // Calculate world space position and rotation for the target
        var worldTargetPosition = parentRotation * localTransform.position + parentPosition;
        var worldTargetRotation = parentRotation * localTransform.rotation;

        // Get the previous target position and rotation
        var prevTreadTarget = isForward ? currentTreadTarget - 1 : currentTreadTarget + 1;
        if (prevTreadTarget >= _movingTreads.Count) prevTreadTarget = 0;
        if (prevTreadTarget < 0) prevTreadTarget = _movingTreads.Count - 1;

        var prevLocalTransform = _treadTargetPoints[prevTreadTarget];
        var worldPrevPosition = parentRotation * prevLocalTransform.position + parentPosition;
        var worldPrevRotation = parentRotation * prevLocalTransform.rotation;

        // Interpolation factor for smooth movement
        var progress = _treadProgress[currentTreadRb];

        // Increment progress based on fixedDeltaTime and speedMultiplier
        progress += Time.fixedDeltaTime * speedMultiplier;

        // Loop progress between 0 and 1
        if (progress > 1f)
        {
          progress = 0f; // Reset progress when we reach the target point
          currentTreadTarget = isForward ? currentTreadTarget + 1 : currentTreadTarget - 1; // Update target point index

          // Ensure we stay within bounds
          if (currentTreadTarget >= _movingTreads.Count) currentTreadTarget = 0;
          if (currentTreadTarget < 0) currentTreadTarget = _movingTreads.Count - 1;

          _treadProgress[currentTreadRb] = progress; // Reset progress for the next point
        }

        // Update the progress value
        _treadProgress[currentTreadRb] = progress;
        // _treadTargetPointMap[currentTreadRb] = currentTreadTarget;

        // Lerp position and rotation based on the progress
        var newPosition = Vector3.Lerp(worldPrevPosition, worldTargetPosition, progress);
        var newRotation = Quaternion.Lerp(worldPrevRotation, worldTargetRotation, progress);

        // if (!currentTreadRb.name.Contains("top"))
        // {
        //   var rbTransform = currentTreadRb.transform;
        //   var position = rbTransform.position;
        //   // var deltaPosition = newPosition - position;
        //   // the forward direction should push backwards
        //   var forwardMultiplier = isForward ? -1f : 1f;
        //   var inverseDirectionalForce = rbTransform.forward * 450f * forwardMultiplier;
        //   var upwardForce = rbTransform.up * 50f;
        //   var force = upwardForce + inverseDirectionalForce;
        //   rootRb.AddForceAtPosition(force, position, ForceMode.Force);
        // }

        // Apply the calculated position and rotation to the tread's rigidbody
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