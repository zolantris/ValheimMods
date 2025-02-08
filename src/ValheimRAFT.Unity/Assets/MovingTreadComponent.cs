using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting; // Required for Visual Scripting components

public class MovingTreadComponent : MonoBehaviour
{
  private List<Rigidbody> _movingTreads = new();
  private List<LocalTransform> treadTargetPoints = new();
  private Dictionary<Rigidbody, int> _treadTargetPointMap = new();
  private Rigidbody treadRb;

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

      // Calculate the angular distance to rotate towards the target
      var deltaRotation = Quaternion.RotateTowards(currentTreadRb.transform.localRotation, localTransform.rotation, Time.fixedDeltaTime * 180f * speedMultiplier); // 180 degrees per second for now

      // Linear movement: Move towards the target position in world space
      var distanceToNextTread = Vector3.Distance(currentTreadRb.transform.position, worldTargetPosition);
      if (Mathf.Approximately(distanceToNextTread, 0.0f))
      {
        // Update the target index based on direction
        if (isForward)
        {
          _treadTargetPointMap[currentTreadRb] = currentTreadTarget + 1 < _movingTreads.Count ? currentTreadTarget + 1 : 0;
        }
        else
        {
          _treadTargetPointMap[currentTreadRb] = currentTreadTarget - 1 >= 0 ? currentTreadTarget - 1 : _movingTreads.Count - 1;
        }

        localTransform = treadTargetPoints[_treadTargetPointMap[currentTreadRb]];
      }

      // Convert world space target position back to local space
      var localTargetPosition = treadParent.InverseTransformPoint(worldTargetPosition);

      // Update the rigidbody transform based on local position and rotation
      currentTreadRb.transform.localPosition = Vector3.MoveTowards(currentTreadRb.transform.localPosition, localTargetPosition, Time.fixedDeltaTime * 5f * speedMultiplier); // 5f speed for smooth transition
      currentTreadRb.transform.rotation = deltaRotation;
    }
  }

  // Update is called once per frame
  public void FixedUpdate()
  {
    UpdateAllTreads();
  }
}