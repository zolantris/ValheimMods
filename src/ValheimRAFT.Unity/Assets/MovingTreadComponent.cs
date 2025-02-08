using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting; // Required for Visual Scripting components

public class MovingTreadComponent : MonoBehaviour
{
  internal List<Rigidbody> _movingTreads = new();
  internal List<LocalTransform> treadTargetPoints = new();
  internal Dictionary<Rigidbody, int> _treadTargetPointMap = new();
  internal Rigidbody treadRb;

  public float treadPointDistanceZ = 0.624670029f;

  public const float treadPointYOffset = 1.578f;
  public float treadTopLocalPosition = treadPointYOffset;
  public float treadBottomLocalPosition = 0f;

  internal static List<LocalTransform> _treadFrontLocalPoints = new()
  {
    // flat top
    new LocalTransform()
    {
      position = new Vector3(-0.142456055f, 1.57800007f, 4.37768173f),
      rotation = Quaternion.identity
    },
    // angles 45
    new LocalTransform()
    {
      position = new Vector3(-0.142456055f, 1.57800007f, 4.37768173f),
      rotation = Quaternion.Euler(45f, 0f, 0f)
    },
    // angles 90
    new LocalTransform()
    {
      position = new Vector3(-0.169189453f, 0.782000542f, 5.19924545f),
      rotation = Quaternion.Euler(90f, 0f, 0f)
    },
    // forward tread angles 135
    new LocalTransform()
    {
      position = new Vector3(-0.160583496f, 0.245388985f, 4.93422127f),
      rotation = Quaternion.Euler(135f, 0f, 0f)
    },
    // bottom tread IE (angles 180) 
    new LocalTransform()
    {
      position = new Vector3(-0.142456055f, 0, 4.37768173f),
      rotation = Quaternion.Euler(180f, 0f, 0f)
    },
  };
  internal static List<LocalTransform> _treadBackLocalPoints = new()
  {
    // flat top tread
    new LocalTransform()
    {
      position = Vector3.zero,
      rotation = Quaternion.Euler(180f, 0f, 0f)
    },
    // back-near bottom tread
    new LocalTransform()
    {
      position = new Vector3(0.0177001953f, 0.245388985f, -0.542955399f),
      rotation = Quaternion.Euler(-135f, 0f, 0f)
    },
    // back middle tread
    new LocalTransform()
    {
      position = new Vector3(0.0246276855f, 0.782000542f, -0.756231308f),
      rotation = Quaternion.Euler(270, 0, 0)
    },
    // back-near-top tread angles 315
    new LocalTransform()
    {
      position = new Vector3(0.0173339844f, 1.34000015f, -0.531721115f),
      rotation = Quaternion.Euler(315, 0, 0)
    },
    // top tread
    new LocalTransform()
    {
      position = new Vector3(0, 1.57800007f, 0),
      rotation = Quaternion.identity
    },
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

  public Transform treadParent;
  public List<Vector3> originalTreadPositions = new();
  public Bounds localBounds = new();
  public GameObject CenterObj;

  // New flag to control direction of movement
  public bool isForward = true;

  // Speed multiplier that controls the overall speed of treads
  public float speedMultiplier = 1f;

  internal Dictionary<Rigidbody, float> treadProgress = new(); // Stores the progress of each tread (0 to 1)

  public void Awake()
  {
    treadRb = GetComponent<Rigidbody>();
    if (!treadParent)
    {
      treadParent = transform.Find("treads");
    }
    InitTreads();
  }

  public void OnDestroy()
  {
    if (CenterObj)
    {
      Destroy(CenterObj);
    }
  }

  public void InitTreads()
  {
    _movingTreads.Clear();
    _treadTargetPointMap.Clear();
    var hasInitLocalBounds = true;

    if (!CenterObj)
    {
      CenterObj = new GameObject()
      {
        name = "Treads_CenterObj",
        layer = LayerMask.NameToLayer("Ignore Raycast"),
        transform = { parent = transform }
      };
    }

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

      int currentTreadTarget = _treadTargetPointMap[currentTreadRb];

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

      // Lerp position and rotation based on the progress
      var newPosition = Vector3.Lerp(worldPrevPosition, worldTargetPosition, progress);
      var newRotation = Quaternion.Lerp(worldPrevRotation, worldTargetRotation, progress);

      // Apply the calculated position and rotation to the tread's rigidbody
      currentTreadRb.MovePosition(newPosition);
      currentTreadRb.MoveRotation(newRotation);
    }
  }

  // Update is called once per frame
  public void FixedUpdate()
  {
    UpdateAllTreads();
  }
}