using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting; // Required for Visual Scripting components

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  public class MovingTreadComponent : MonoBehaviour
  {
    internal List<Rigidbody> _movingTreads = new();
    internal List<LocalTransform> treadTargetPoints = new();
    internal Dictionary<Rigidbody, int> _treadTargetPointMap = new();
    internal Rigidbody treadRb;
    public Transform rotatorParent;

    public GameObject treadPrefab;

    public const float treadPointDistanceZ = 0.624670029f;

    public const float treadPointYOffset = 1.578f;
    public float treadTopLocalPosition = treadPointYOffset;
    public float treadBottomLocalPosition = 0f;

    public List<HingeJoint> wheelRotators = new();

    public Transform treadParent;
    public List<Vector3> originalTreadPositions = new();
    public Bounds localBounds = new();
    public GameObject CenterObj;

    // New flag to control direction of movement
    public bool isForward = true;

    // Speed multiplier that controls the overall speed of treads
    public float speedMultiplier = 1f;

    internal Dictionary<Rigidbody, float> treadProgress = new(); // Stores the progress of each tread (0 to 1)

    internal static Vector3 tread_meshScalar = new Vector3(2f, 0.0500000007f, 0.599999964f);
    internal static List<LocalTransform> _treadFrontLocalPoints = new()
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
      },
    };
    internal static List<LocalTransform> _treadBackLocalPoints = new()
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
      },
      // // top tread
      // new LocalTransform()
      // {
      //   position = new Vector3(0, 1.57800007f, 0),
      //   rotation = Quaternion.identity
      // },
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

    public void Awake()
    {
      rotatorParent = transform.Find("rotators");
      treadRb = GetComponent<Rigidbody>();
      if (!treadParent)
      {
        treadParent = transform.Find("treads");
      }
      if (!treadParent)
      {
        treadParent = transform;
      }

      InitTreads(new Bounds(Vector3.zero, Vector3.one * 4));

      wheelRotators.Clear();
      if (rotatorParent && wheelRotators.Count == 0)
      {
        wheelRotators = rotatorParent.GetComponentsInChildren<HingeJoint>().ToList();
      }
      UpdateRotators();
    }
    public void Start()
    {
      UpdateRotators();
    }

    public void UpdateRotators()
    {
      Physics.SyncTransforms();
      wheelRotators.ForEach((hinge) =>
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
        Vector3 centerOfMass = hinge.transform.parent.position; // Or use a different method to calculate the center

// Set the hinge joint's anchor to the center of mass
        hinge.anchor = hinge.transform.InverseTransformPoint(centerOfMass);

// Optional: Set the pivot to match the anchor point
        // hinge.transform.position = centerOfMass; // Make sure the object is centered around the hinge


        var motor = hinge.motor;
        motor.force = speedMultiplier * currentSpeed * 50f;
        motor.targetVelocity = speedMultiplier * currentSpeed * 50f;
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
        treadTargetPoints.Add(localPoint);

        var rb = AddRigidbodyToChild(child);
        _movingTreads.Add(rb);
        _treadTargetPointMap.Add(rb, i);

        // Initialize progress for each tread (0 to 1)
        treadProgress[rb] = 0f;
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

    public void CleanUp()
    {
      _movingTreads.Clear();
      _treadTargetPointMap.Clear();
      treadProgress.Clear();
    }

    /// <summary>
    /// Generates dynamically all treads based on a bounds size. Must be invoked
    /// </summary>
    /// <param name="bounds"></param>
    public void InitTreads(Bounds bounds)
    {
      if (!treadPrefab) return;
      CleanUp();

      var hasInitLocalBounds = true;
      CreateCenteringObject();

      // var horizontalTreads = Mathf.RoundToInt(3);
      var horizontalTreads = Mathf.RoundToInt(bounds.size.z / treadPointDistanceZ);

      // top treads
      for (var i = 0; i < horizontalTreads; i++)
      {
        var treadGameObject = Instantiate(treadPrefab, treadParent);
        treadGameObject.name = "Treads_" + i + "top";
        var offset = new Vector3(0, treadPointYOffset, treadPointDistanceZ * _movingTreads.Count);
        treadGameObject.transform.localPosition = offset;
        // Update bounds for the first time
        if (hasInitLocalBounds)
        {
          hasInitLocalBounds = false;
          localBounds = new Bounds(treadGameObject.transform.localPosition, Vector3.zero);
        }
        else
        {
          localBounds.Encapsulate(treadGameObject.transform.localPosition);
        }

        var localPoint = new LocalTransform(treadGameObject.transform);
        treadTargetPoints.Add(localPoint);

        var rb = AddRigidbodyToChild(treadGameObject.transform);
        _movingTreads.Add(rb);
        _treadTargetPointMap.Add(rb, _movingTreads.Count - 1);

        // Initialize progress for each tread (0 to 1)
        treadProgress[rb] = 0f;
      }

      // front treads
      for (var i = 0; i < _treadFrontLocalPoints.Count; i++)
      {
        var x = _treadFrontLocalPoints[i];
        var treadGameObject = Instantiate(treadPrefab, treadParent);
        treadGameObject.name = "Treads_" + i + "front";
        treadGameObject.transform.localPosition = new Vector3(x.position.x, x.position.y, x.position.z + horizontalTreads * treadPointDistanceZ + treadPointDistanceZ);
        treadGameObject.transform.localRotation = x.rotation;

        if (hasInitLocalBounds)
        {
          hasInitLocalBounds = false;
          localBounds = new Bounds(treadGameObject.transform.localPosition, Vector3.zero);
        }
        else
        {
          localBounds.Encapsulate(treadGameObject.transform.localPosition);
        }

        var localPoint = new LocalTransform(treadGameObject.transform);
        treadTargetPoints.Add(localPoint);

        var rb = AddRigidbodyToChild(treadGameObject.transform);
        _movingTreads.Add(rb);
        _treadTargetPointMap.Add(rb, _movingTreads.Count - 1);

        // Initialize progress for each tread (0 to 1)
        treadProgress[rb] = 0f;
      }

      for (var i = 0; i < horizontalTreads; i++)
      {
        var treadGameObject = Instantiate(treadPrefab, treadParent);
        treadGameObject.name = "Treads_" + i + "bottom";
        var offset = new Vector3(0, 0, treadPointDistanceZ * (horizontalTreads - i));
        var rotation = Quaternion.Euler(180f, 0f, 0f);

        treadGameObject.transform.localPosition = offset;
        treadGameObject.transform.localRotation = rotation;

        // Update bounds for the first time
        if (hasInitLocalBounds)
        {
          hasInitLocalBounds = false;
          localBounds = new Bounds(treadGameObject.transform.localPosition, Vector3.zero);
        }
        else
        {
          localBounds.Encapsulate(treadGameObject.transform.localPosition);
        }

        var localPoint = new LocalTransform(treadGameObject.transform);
        treadTargetPoints.Add(localPoint);

        var rb = AddRigidbodyToChild(treadGameObject.transform);
        _movingTreads.Add(rb);
        _treadTargetPointMap.Add(rb, _movingTreads.Count - 1);

        // Initialize progress for each tread (0 to 1)
        treadProgress[rb] = 0f;
      }

      for (var i = 0; i < _treadBackLocalPoints.Count; i++)
      {
        var x = _treadBackLocalPoints[i];
        var treadGameObject = Instantiate(treadPrefab, transform.position, Quaternion.identity, treadParent);
        treadGameObject.name = "Treads_" + i + "back";
        treadGameObject.transform.localPosition = x.position;
        treadGameObject.transform.localRotation = x.rotation;

        if (hasInitLocalBounds)
        {
          hasInitLocalBounds = false;
          localBounds = new Bounds(treadGameObject.transform.localPosition, Vector3.zero);
        }
        else
        {
          localBounds.Encapsulate(treadGameObject.transform.localPosition);
        }

        var localPoint = new LocalTransform(treadGameObject.transform);
        treadTargetPoints.Add(localPoint);

        var rb = AddRigidbodyToChild(treadGameObject.transform);
        _movingTreads.Add(rb);
        _treadTargetPointMap.Add(rb, _movingTreads.Count - 1);

        // Initialize progress for each tread (0 to 1)
        treadProgress[rb] = 0f;
      }

      CenterObj.transform.position = treadParent.position + localBounds.center;
    }

    public Rigidbody AddRigidbodyToChild(Transform child)
    {
      var rb = child.GetComponent<Rigidbody>();
      if (!rb)
      {
        rb = child.AddComponent<Rigidbody>();
      }
      rb.mass = 1f;
      rb.drag = 1f;
      rb.angularDrag = 10f;
      rb.useGravity = false;
      rb.isKinematic = true;
      return rb;
    }

    public void UpdateAllTreads()
    {
      if (treadTargetPoints.Count != _movingTreads.Count) return;

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

        var localTransform = treadTargetPoints[currentTreadTarget];

        // Calculate world space position and rotation for the target
        var worldTargetPosition = parentRotation * localTransform.position + parentPosition;
        var worldTargetRotation = parentRotation * localTransform.rotation;

        // Get the previous target position and rotation
        int prevTreadTarget = isForward ? currentTreadTarget - 1 : currentTreadTarget + 1;
        if (prevTreadTarget >= _movingTreads.Count) prevTreadTarget = 0;
        if (prevTreadTarget < 0) prevTreadTarget = _movingTreads.Count - 1;

        var prevLocalTransform = treadTargetPoints[prevTreadTarget];
        var worldPrevPosition = parentRotation * prevLocalTransform.position + parentPosition;
        var worldPrevRotation = parentRotation * prevLocalTransform.rotation;

        // Interpolation factor for smooth movement
        float progress = treadProgress[currentTreadRb];

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

          treadProgress[currentTreadRb] = progress; // Reset progress for the next point
        }

        // Update the progress value
        treadProgress[currentTreadRb] = progress;
        // _treadTargetPointMap[currentTreadRb] = currentTreadTarget;

        // Lerp position and rotation based on the progress
        var newPosition = Vector3.Lerp(worldPrevPosition, worldTargetPosition, progress);
        var newRotation = Quaternion.Lerp(worldPrevRotation, worldTargetRotation, progress);

        // Apply the calculated position and rotation to the tread's rigidbody
        currentTreadRb.MovePosition(newPosition);
        currentTreadRb.MoveRotation(newRotation);
      }
    }

    private float lastSpeed = 0f;
    public float currentSpeed = 1f;
    // Update is called once per frame
    public void FixedUpdate()
    {
      UpdateAllTreads();

      if (wheelRotators.Count > 0)
      {
        if (!Mathf.Approximately(lastSpeed, currentSpeed))
        {
          lastSpeed = currentSpeed;
          UpdateRotators();
        }
      }
    }
  }
}