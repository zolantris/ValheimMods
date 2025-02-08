using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class MovingTreadComponent : MonoBehaviour
{
  private List<Rigidbody> _movingTreads = new();
  private List<LocalTransform> treadTargetPoints = new();
  private Dictionary<Rigidbody, int> _treadTargetPointMap = new();
  private List<Rigidbody> _movingTopTreads = new();
  private List<Rigidbody> _movingBottomTreads = new();
  private List<Rigidbody> _movingFrontTreads = new();
  private List<Rigidbody> _movingBackTreads = new();

  private LocalTransform backEnd;
  private LocalTransform backStart;

  private LocalTransform frontEnd;
  private LocalTransform frontStart;

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

  public Rigidbody treadRb;
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

    for (var i = 0; i < treadParent.childCount; i++)
    {
      var child = treadParent.GetChild(i);
      if (!child) continue;
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

      // if (child.name.Contains("top"))
      // {
      //   _movingTopTreads.Add(rb);
      // }
      // if (child.name.Contains("bottom"))
      // {
      //   _movingBottomTreads.Add(rb);
      // }
      // if (child.name.Contains("front"))
      // {
      //   _movingFrontTreads.Add(rb);
      // }
      // if (child.name.Contains("back"))
      // {
      //   _movingBackTreads.Add(rb);
      // }
    }

    CenterObj.transform.position = treadParent.position + localBounds.center;
    // backStart = new LocalTransform(_movingBottomTreads[^1].transform);
    // backEnd = new LocalTransform(_movingTopTreads[0].transform);
    //
    // frontStart = new LocalTransform(_movingTopTreads[^1].transform);
    // frontEnd = new LocalTransform(_movingBottomTreads[0].transform);
  }

  public Rigidbody AddRigidbodyToChild(Transform child)
  {
    var rb = child.GetComponent<Rigidbody>();
    if (!rb)
    {
      rb = child.AddComponent<Rigidbody>();
    }
    // rb.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    rb.mass = 1f;
    rb.drag = 1f;
    rb.angularDrag = 10f;
    rb.useGravity = false;
    rb.isKinematic = true;
    return rb;
  }

  public void UpdateAllTreads()
  {
    if (treadTargetPoints.Count != _movingTreads.Count)
    {
      return;
    }

    for (var i = 0; i < _movingTreads.Count; i++)
    {
      var currentTreadRb = _movingTreads[i];
      var currentTreadTarget = _treadTargetPointMap[currentTreadRb];
      if (!currentTreadRb) continue;
      if (currentTreadTarget == _movingTreads.Count - 1 && _movingTreads.Count > 1)
      {
        currentTreadTarget = 0;
      }

      var localTransform = treadTargetPoints[currentTreadTarget];

      var distanceToNextTread = Vector3.Distance(currentTreadRb.transform.localPosition, localTransform.position);

      // if nearly 0 IE at the position it needs to be, move the tread to next position
      if (Mathf.Approximately(distanceToNextTread, 0.0f))
      {
        if (currentTreadTarget == _movingTreads.Count - 1)
        {
          currentTreadTarget = 0;
        }
        else
        {
          currentTreadTarget++;
        }

        // update the DB
        _treadTargetPointMap[currentTreadRb] = currentTreadTarget;

        // set local transform to next one.
        localTransform = treadTargetPoints[currentTreadTarget];
      }
      var rbTransform = currentTreadRb.transform;

      var position = CenterObj.transform.InverseTransformPoint(rbTransform.transform.position);
      var rotation = rbTransform.localRotation;

      var deltaPosition = Vector3.Lerp(position, localTransform.position, Time.fixedDeltaTime);
      var deltaRotation = Quaternion.Lerp(rotation, localTransform.rotation, Time.fixedDeltaTime);

      rbTransform.localPosition = deltaPosition;
      rbTransform.localRotation = deltaRotation;
      // currentTreadRb.Move(currentTreadRb.transform.position + deltaPosition, currentTreadRb.transform.rotation + deltaRotation);
    }
  }

  // public void UpdateAllTreads()
  // {
  //   // if (_movingTreads.Count < 2) return;
  //   // if (_treadsRb.Count < 2) return;
  //   // // var newPositions = new List<Vector3>();
  //   // // var newRotations = new List<Quaternion>();
  //   // for (int i = 0; i < _treadsRb.Count; i++)
  //   // {
  //   //   var currentTreadRb = _treadsRb[i];
  //   //   var targetIndex = i + 1;
  //   //   if (!currentTreadRb) continue;
  //   //   if (i == _treadsRb.Count - 1 && _treadsRb.Count > 1)
  //   //   {
  //   //     targetIndex = 0;
  //   //   }
  //   //   var targetRb = _treadsRb[targetIndex];
  //   //   if (targetRb != null)
  //   //   {
  //   //     var lerpedPosition = Vector3.Lerp(currentTreadRb.position, targetRb.position, Time.fixedDeltaTime);
  //   //     var lerpedRotation = Quaternion.Lerp(currentTreadRb.rotation, targetRb.rotation, Time.fixedDeltaTime);
  //   //     // if (i == _treadsRb.Count - 1)
  //   //     // {
  //   //     // currentTreadRb.position = lerpedPosition;
  //   //     // currentTreadRb.rotation = lerpedRotation;
  //   //     // }
  //   //     // else
  //   //     // {
  //   //     currentTreadRb.Move(lerpedPosition, lerpedRotation);
  //   //     // }
  //   //   }
  //   //   else
  //   //   {
  //   //     Debug.Log("Error targetRB is null");
  //   //   }
  //   //
  //   //   // var targetTread = _treadJoints[targetIndex];
  //   //   // if (!targetTread) continue;
  //   //   // // todo might want to get the joint distance and vector between each.
  //   //   // treadJoint.connectedBody = targetTread.GetComponent<Rigidbody>();
  //   //   // treadJoint.useSpring = true;
  //   //   // var spring = targetTread.spring;
  //   //   // spring.damper = 0.5f;
  //   //   // spring.spring = 100f;
  //   //   // treadJoint.spring = spring;
  //   //   // treadJoint.autoConfigureConnectedAnchor = false;
  //   //   // treadJoint.connectedAnchor = targetTread.transform.localPosition;
  //   //   // treadJoint.connectedAnchor = targetTread.transform.position;
  //   // }
  //   //
  //   // var lastTread = _treadsRb[^1];
  //   // var firstTread = _treadsRb[0];
  //   // var lerpedLastPosition = Vector3.Lerp(lastTread.position, firstTread.position, Time.fixedDeltaTime);
  //   // var lerpedLastRotation = Quaternion.Lerp(lastTread.rotation, firstTread.rotation, Time.fixedDeltaTime);
  //   // lastTread.Move(lerpedLastPosition, lerpedLastRotation);
  // }

  public float lastUpdateTime = 0f;

  // Update is called once per frame
  public void FixedUpdate()
  {
    UpdateAllTreads();
  }
}