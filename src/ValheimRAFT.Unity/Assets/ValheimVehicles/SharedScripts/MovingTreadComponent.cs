using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  public class MovingTreadComponent : MonoBehaviour
  {
    internal List<Rigidbody> _movingTreads = new();
    internal List<LocalTransform> _treadTargetPoints = new();
    internal Dictionary<Rigidbody, int> _treadTargetPointMap = new();
    internal readonly Dictionary<Rigidbody, float> _treadProgress = new(); // Stores the progress of each tread (0 to 1)
    public List<HingeJoint> _wheelRotators = new();

    internal Rigidbody treadRb;
    internal Rigidbody rootRb;
    public Transform rotatorParent;

    public static GameObject fallbackPrefab = null!;
    public GameObject treadPrefab;

    public const float treadPointDistanceZ = 0.624670029f;

    public const float treadPointYOffset = 1.578f;
    public float treadTopLocalPosition = treadPointYOffset;
    public float treadBottomLocalPosition = 0f;

    [FormerlySerializedAs("wheelRotators")]
    public Transform treadParent;
    public List<Vector3> originalTreadPositions = new();
    public Bounds localBounds = new();
    public GameObject CenterObj;

    // New flag to control direction of movement
    public bool isForward = true;

    // Speed multiplier that controls the overall speed of treads
    public float speedMultiplier = 1f;

    internal static Vector3 tread_meshScalar = new(2f, 0.0500000007f, 0.599999964f);
    internal static readonly List<LocalTransform> _treadFrontLocalPoints = new()
    {
      new LocalTransform()
      {
        position = new Vector3(0f, 1.57800007f, -treadPointDistanceZ),
        rotation = Quaternion.identity
      },
      new LocalTransform()
      {
        position = new Vector3(0.0f, 1.57800007f, 0f),
        rotation = Quaternion.identity
      },
      // top-near
      new LocalTransform()
      {
        position = new Vector3(0.0f, 1.4f, 0.542955399f),
        rotation = Quaternion.Euler(45f, 0f, 0f)
      },
      // top middle
      new LocalTransform()
      {
        position = new Vector3(0.0f, 0.782000542f, 0.756231308f),
        rotation = Quaternion.Euler(90f, 0f, 0f)
      },
      // top near bottom
      new LocalTransform()
      {
        position = new Vector3(0.0f, 0.245388985f, 0.542955399f),
        rotation = Quaternion.Euler(135f, 0f, 0f)
      },
      new LocalTransform()
      {
        position = new Vector3(0f, 0f, 0f),
        rotation = Quaternion.Euler(180f, 0f, 0f)
      }
    };
    internal static readonly List<LocalTransform> _treadBackLocalPoints = new()
    {
      // // flat top tread
      new LocalTransform()
      {
        position = Vector3.zero,
        rotation = Quaternion.Euler(180f, 0f, 0f)
      },
      // back-near bottom tread
      new LocalTransform()
      {
        position = new Vector3(0.0f, 0.245388985f, -0.542955399f),
        rotation = Quaternion.Euler(-135f, 0f, 0f)
      },
      // back middle tread
      new LocalTransform()
      {
        position = new Vector3(0.0f, 0.782000542f, -0.756231308f),
        rotation = Quaternion.Euler(270, 0, 0)
      },
      // back-near-top tread angles 315
      new LocalTransform()
      {
        position = new Vector3(0.0f, 1.34000015f, -0.542955399f),
        rotation = Quaternion.Euler(315, 0, 0)
      }
    };

    public struct LocalTransform
    {
      public Vector3 position;
      public Quaternion rotation;

      public LocalTransform(Transform objTransform)
      {
        position = objTransform.localPosition;
        rotation = objTransform.localRotation;
      }
    }

    public ConvexHullAPI convexHullComponent;

    public void Awake()
    {
      rotatorParent = transform.Find("rotators");
      treadRb = GetComponent<Rigidbody>();
      rootRb = GetComponentInParent<Rigidbody>();
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
      if (!convexHullComponent)
      {
        convexHullComponent = gameObject.AddComponent<ConvexHullAPI>();
      }

      convexHullComponent.m_colliderParentTransform = treadParent;
      convexHullComponent.HasPreviewGeneration = false;
      convexHullComponent.IsAllowedAsHullOverride = AllowTreadsObject;
    }

    /// <summary>
    /// A lower case string matcher to allow any object starting with treads to be allowed as a convexhull so we can create collisions.
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public bool AllowTreadsObject(string val)

    {
      if (val.Contains("tread"))
        return true;
      return false;
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

    public void UpdateRotators()
    {
      Physics.SyncTransforms();
      _wheelRotators.ForEach((hinge) =>
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

    public void OnDestroy()
    {
      if (CenterObj)
      {
        Destroy(CenterObj);
      }
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
        else
        {
          localBounds.Encapsulate(child.localPosition);
        }

        var localPoint = new LocalTransform(child.transform);
        _treadTargetPoints.Add(localPoint);

        var rb = AddRigidbodyToChild(child);
        _movingTreads.Add(rb);
        _treadTargetPointMap.Add(rb, i);

        // Initialize progress for each tread (0 to 1)
        _treadProgress[rb] = 0f;
      }
      CenterObj.transform.position = treadParent.position + localBounds.center;
    }

    public void CreateCenteringObject()
    {
      if (!CenterObj)
      {
        CenterObj = new GameObject()
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
      else
      {
        localBounds.Encapsulate(treadGameObject.transform.localPosition);
      }

      var localPoint = new LocalTransform(treadGameObject.transform);
      _treadTargetPoints.Add(localPoint);

      var rb = AddRigidbodyToChild(treadGameObject.transform);
      _movingTreads.Add(rb);
      _treadTargetPointMap.Add(rb, _movingTreads.Count - 1);

      // Initialize progress for each tread (0 to 1)
      _treadProgress[rb] = 0f;
    }

    private bool _hasInitLocalBounds;
    private static readonly Quaternion flippedXRotation = Quaternion.Euler(180, 0, 0);
    /// <summary>
    /// Generates dynamically all treads based on a bounds size. Must be invoked
    /// </summary>
    /// <param name="bounds"></param>
    public void GenerateTreads(Bounds bounds)
    {
      if (!treadPrefab) return;
      CleanUp();

      CreateCenteringObject();

      // var horizontalTreads = Mathf.RoundToInt(3);
      var horizontalTreads = Mathf.RoundToInt(bounds.size.z / treadPointDistanceZ);
      var fullTreadLength = horizontalTreads * treadPointDistanceZ;
      // top treads
      for (var i = 0; i < horizontalTreads; i++)
      {
        var treadGameObject = Instantiate(treadPrefab, treadParent);
        treadGameObject.name = "Treads_" + i + "top";

        var zPos = treadPointDistanceZ * i;
        var offset = new Vector3(0, treadPointYOffset, zPos);
        treadGameObject.transform.localPosition = offset;
        // Update bounds for the first time
        InitSingleTread(treadGameObject);
      }

      // front treads
      for (var i = 0; i < _treadFrontLocalPoints.Count; i++)
      {
        var x = _treadFrontLocalPoints[i];
        var treadGameObject = Instantiate(treadPrefab, treadParent);
        treadGameObject.name = $"Treads_{i}_front";

        // we offset the Z position so that it aligns with the last 
        var zPos = x.position.z + fullTreadLength + treadPointDistanceZ;
        treadGameObject.transform.localPosition = new Vector3(x.position.x, x.position.y, zPos);
        treadGameObject.transform.localRotation = x.rotation;

        InitSingleTread(treadGameObject);
      }

      for (var i = 0; i < horizontalTreads; i++)
      {
        var treadGameObject = Instantiate(treadPrefab, treadParent);
        treadGameObject.name = $"Treads_{i}_bottom";
        var offset = new Vector3(0, 0, treadPointDistanceZ * (horizontalTreads - i));
        var rotation = flippedXRotation;

        treadGameObject.transform.localPosition = offset;
        treadGameObject.transform.localRotation = rotation;

        InitSingleTread(treadGameObject);
      }

      for (var i = 0; i < _treadBackLocalPoints.Count; i++)
      {
        var x = _treadBackLocalPoints[i];
        var treadGameObject = Instantiate(treadPrefab, transform.position, Quaternion.identity, treadParent);
        treadGameObject.name = $"Treads_{i}_back";

        // since this is zero based it is actually fine to not use relative calcs for z pos.
        treadGameObject.transform.localPosition = x.position;
        treadGameObject.transform.localRotation = x.rotation;

        InitSingleTread(treadGameObject);
      }

      CenterObj.transform.position = treadParent.position + localBounds.center;

      var treadGameObjects = _movingTreads.Select(x => x.gameObject).ToList();
      convexHullComponent.HasPreviewGeneration = false;
      convexHullComponent.GenerateMeshesFromChildColliders(treadParent.gameObject, Vector3.zero, 50, treadGameObjects, null);
      convexHullComponent.convexHullMeshColliders.ForEach(x =>
      {
        if (!x) return;
        x.gameObject.name = "convex_tread_collider";
        x.includeLayers = LayerMask.GetMask("terrain");
        x.excludeLayers = -1;
      });
    }

    public void SetSpeed(float speed)
    {
      speedMultiplier = speedMultiplierOverride != 0 ? speedMultiplierOverride : speed;
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
      rb.drag = 0.05f;
      rb.angularDrag = 10f;
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
        currentTreadRb.MovePosition(newPosition);
        currentTreadRb.MoveRotation(newRotation);
      }
    }

    private float lastSpeed = 0f;
    public float speedMultiplierOverride = 0;
    // Update is called once per frame
    public void FixedUpdate()
    {
      UpdateAllTreads();

      if (_wheelRotators.Count > 0)
      {
        if (!Mathf.Approximately(lastSpeed, speedMultiplierOverride))
        {
          lastSpeed = speedMultiplierOverride;
          UpdateRotators();
        }
      }
    }
  }
}