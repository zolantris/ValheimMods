using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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

  public float rotationTimeMultiplier = 1f;
  public float movementTimeMultiplier = 1f;

  public void UpdateAllTreads()
  {
    if (treadTargetPoints.Count != _movingTreads.Count) return;

    // World position of the parent object (important to factor in)
    var parentPosition = treadParent.position;
    var parentRotation = treadParent.rotation;

    for (var i = 0; i < _movingTreads.Count; i++)
    {
      var currentTreadRb = _movingTreads[i];
      if (!currentTreadRb) continue;

      int currentTreadTarget = _treadTargetPointMap[currentTreadRb];

      // Wrap target points to avoid out-of-bounds access
      if (currentTreadTarget == _movingTreads.Count - 1 && _movingTreads.Count > 1)
      {
        currentTreadTarget = 0;
      }
      else
      {
        currentTreadTarget++;
      }

      var localTransform = treadTargetPoints[currentTreadTarget];

      // Calculate world space position and rotation for the target
      var worldTargetPosition = parentRotation * localTransform.position + parentPosition;
      var worldTargetRotation = parentRotation * localTransform.rotation;

      // Calculate distance and handle position update
      var distanceToNextTread = Vector3.Distance(currentTreadRb.transform.position, worldTargetPosition);
      if (Mathf.Approximately(distanceToNextTread, 0.0f))
      {
        // Update the DB and set new target point
        _treadTargetPointMap[currentTreadRb] = currentTreadTarget;
        localTransform = treadTargetPoints[currentTreadTarget];
      }

      // Update the rigidbody transform based on the world space position and rotation
      var rbTransform = currentTreadRb.transform;

      // Convert world space target position back to local space
      var localTargetPosition = treadParent.InverseTransformPoint(worldTargetPosition);
      var localTargetRotation = Quaternion.Inverse(treadParent.rotation) * worldTargetRotation;

      // Update the tread's position and rotation, considering parent changes
      rbTransform.localPosition = Vector3.MoveTowards(rbTransform.localPosition, localTargetPosition, Time.fixedDeltaTime * movementTimeMultiplier); // 5f speed for smooth transition
      rbTransform.localRotation = Quaternion.Lerp(rbTransform.localRotation, localTargetRotation, Time.fixedDeltaTime * rotationTimeMultiplier);
      // rbTransform.localRotation = Quaternion.RotateTowards(rbTransform.localRotation, localTargetRotation, Time.fixedDeltaTime * rotationTimeMultiplier); // 180 degrees per second
    }
  }

  // Update is called once per frame
  public void FixedUpdate()
  {
    UpdateAllTreads();
  }
}