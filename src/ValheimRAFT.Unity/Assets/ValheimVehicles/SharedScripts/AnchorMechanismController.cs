using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ValheimVehicles.SharedScripts
{
    public enum AnchorState
    {
        Idle,         // No action is taking place.
    
        Dropping,     // The anchor is in the process of being dropped.
        Dropped,      // The anchor is fully dropped, at the target position.
    
        Reeling,      // The anchor is being reeled back in.
        ReeledIn      // The anchor is fully reeled back in to its starting position.
    }
    
    public class AnchorMechanismController : ParentCollisionListener
{

    public Transform rotationAnchorRopeAttachpoint;  // Point from which the rope is attached (usually the ship)
    public Transform anchorRopeAttachmentPoint;  // Point where the rope is attached (top of the anchor)
    public Transform anchorRopeAttachStartPoint;
    public Transform anchorReelTransform;
    public Transform anchorReelCogsTransform;
    private Vector3 anchorStartLocalPosition;
    
    public float anchorDropDistance = 10f;  // Maximum depth the anchor can go
    public float reelSpeed = 5f;  // Speed at which the anchor reels in
    public float reelCogAngleMult = 100f;  // Speed at which the anchor reels in
    
    public Transform anchorTransform;
    private Rigidbody anchorRb;
    public AnchorState currentState = AnchorState.ReeledIn;  // Current state of the anchor (Idle, Dropping, Reeling)

    public LineRenderer ropeLine;  // LineRenderer to visualize rope
    
    public Rigidbody prefabRigidbody;

    public bool CanUseHotkeys = true;
    
    public override void OnChildCollisionEnter(Collision collision)
    {
        if (currentState == AnchorState.Dropping && collision.gameObject.layer == LayerMask.NameToLayer("terrain"))
        {
            UpdateAnchorState(AnchorState.Dropped);
        }
    }

    /// <summary>
    /// This rigidbody should not start awakened to prevent collision problems on placement
    /// </summary>
    private void Awake()
    {
        if (prefabRigidbody == null)
        {
            prefabRigidbody = transform.GetComponent<Rigidbody>();
            if (prefabRigidbody)
            {
                prefabRigidbody.Sleep();
            }
        }
    }

    private void Start()
    {
        if (anchorTransform == null)
        {
            anchorTransform = transform.Find("chain_generator/anchor");
        }
        
        anchorTransform.Find("scalar/colliders").gameObject.AddComponent<ChildCollisionDetector>();
        anchorReelTransform = transform.Find("anchor_reel");
        anchorReelCogsTransform = transform.Find("anchor_reel/cogs");
        anchorRb = anchorTransform.GetComponent<Rigidbody>();
        anchorStartLocalPosition = anchorRb.transform.localPosition;

        // Initialize LineRenderer for rope visualization
        if (ropeLine == null)
        {
            ropeLine = GetComponent<LineRenderer>();
        }
        UpdateRopeVisual();
        UpdateAnchorState(AnchorState.ReeledIn);
    }

    private void Update()
    {
        if (CanUseHotkeys)
        {
            HandleKeyInputs();
        }
    }
    
    public virtual void FixedUpdate()
    {
        if (anchorRb == null) return;
        // Execute behavior based on the current state
        switch (currentState)
        {
            case AnchorState.Dropping:
                DropAnchor();
                break;
            case AnchorState.Reeling:
                ReelAnchor();
                break;
            case AnchorState.Dropped:
                break;
            case AnchorState.ReeledIn:
            case AnchorState.Idle:
                anchorRb.transform.localPosition = anchorStartLocalPosition;
                anchorRb.transform.localRotation = Quaternion.identity;
                break;
        }

        UpdateRopeVisual();
    }

    public virtual void OnAnchorStateChange(AnchorState newState) {}

    internal void UpdateAnchorState(AnchorState newState)
    {
        // Do nothing if state is equivalent
        if (newState == currentState)
        {
            return;
        }
        
        currentState = newState;
        // Execute behavior based on the current state
        switch (currentState)
        {
            case AnchorState.Dropping:
            case AnchorState.Reeling:
            case AnchorState.Dropped:
            case AnchorState.ReeledIn:
            case AnchorState.Idle:
                anchorRb.isKinematic = true;
                break;
        }
        
        OnAnchorStateChange(currentState);
    }

    private void HandleKeyInputs()
    {
        if (anchorRb == null) return;
        if (Input.GetKeyDown(KeyCode.D)) // Toggle dropping with 'D' key
        {
            if (currentState != AnchorState.Dropping)
            {
                StartDropping();
            }
            else
            {
                StopDropping();
            }
        }

        if (Input.GetKeyDown(KeyCode.R)) // Toggle reeling with 'R' key
        {
            if (currentState != AnchorState.Reeling)
            {
                StartReeling();
            }else
            {
                StopReeling();
            }
        }
    }

    public void StartDropping()
    {
        if (anchorRb == null) return;
        if (currentState != AnchorState.Dropping)
        {
            UpdateAnchorState(AnchorState.Dropping);
        }
    }

    public void StopDropping()
    {
        if (anchorRb == null) return;
        if (currentState == AnchorState.Dropping)
        {
            UpdateAnchorState(AnchorState.Dropped);
        }
    }

    public void StartReeling()
    {
        if (anchorRb == null) return;
        if (currentState != AnchorState.Reeling)
        {
            UpdateAnchorState(AnchorState.Reeling);
        }
    }

    public void StopReeling()
    {
        if (anchorRb == null) return;
        
        if (currentState == AnchorState.Reeling)
        {
            UpdateAnchorState(AnchorState.ReeledIn);
        }
    }
    
    private void DropAnchor()
    {
        // Check if the anchor is still above the maximum drop distance
        if (anchorRopeAttachmentPoint.position.y > rotationAnchorRopeAttachpoint.position.y - anchorDropDistance)
        {
            var deltaReelSpeed = reelSpeed * 1.5f * Time.fixedDeltaTime;

            // Move the anchor downward
            anchorRb.MovePosition(anchorRb.position + Vector3.down * deltaReelSpeed);

            // Rotate the cogs using quaternions
            var rotationStep = Quaternion.Euler(-reelCogAngleMult * deltaReelSpeed, 0, 0);
            anchorReelCogsTransform.localRotation *= rotationStep;
        }
        else
        {
            // Stop dropping when the anchor reaches the max drop distance
            StopDropping();
        }
    }


    private void ReelAnchor()
    {
        // Only reel if the anchor is below the attachment point (on the Y-axis)
        if (anchorRopeAttachmentPoint.position.y < anchorRopeAttachStartPoint.position.y)
        {
            var deltaReelSpeed = reelSpeed * Time.fixedDeltaTime;

            // Move the anchor upward
            anchorRb.MovePosition(anchorRb.position + Vector3.up * deltaReelSpeed);

            // Rotate the cogs using quaternions
            var rotationStep = Quaternion.Euler(-reelCogAngleMult * deltaReelSpeed, 0, 0);
            anchorReelCogsTransform.localRotation *= rotationStep;

            // Check if the anchor is close enough to the attachment point
            if (anchorRopeAttachmentPoint.position.y > anchorRopeAttachStartPoint.position.y)
            {
                StopReeling();
            }
        }
        else
        {
            // Stop reeling if the anchor has reached or passed the attachment point
            StopReeling();
        }
    }

  private void UpdateRopeVisual()
{
    if (ropeLine == null) return;

    ropeLine.useWorldSpace = false;  // Ensure the rope uses local space.
    ropeLine.startWidth = 0.2f;
    ropeLine.endWidth = 0.2f;

    // Get the transform of the LineRenderer and the relative positions of the points in local space.
    var ropelineTransform = ropeLine.transform;
    
    // Calculate the relative local positions from the RopeLine to the anchor points (ignore world space)
    var rotationAttachpoint = ropelineTransform.InverseTransformPoint(rotationAnchorRopeAttachpoint.position);
    var localStartPosition = ropelineTransform.InverseTransformPoint(anchorRopeAttachStartPoint.position);
    var localEndPosition = ropelineTransform.InverseTransformPoint(anchorRopeAttachmentPoint.position);

    // The length of the rope in world space (but it's easier to use the local difference here)
    var ropeLength = Vector3.Distance(localStartPosition, localEndPosition);

    // Number of segments based on rope length (adjust this divisor for finer or coarser rope segments)
    int numberOfPoints = Mathf.RoundToInt(ropeLength / 2f); // Adjust this divisor for finer or coarser rope segments

    // Ensure a minimum of 2 points (start and end) and a maximum of 40 points
    var lerpedPositionCount = Mathf.Clamp(numberOfPoints + 1, 3, 40);

    // List to store the positions of the rope segments in local space
    var positions = new List<Vector3>();

    // rotation attachpoint
    positions.Add(rotationAttachpoint);
    // Add the first point (local position of the start point)
    positions.Add(localStartPosition); // This will be the first point of the rope in local space

    // Loop to interpolate the rope segments in local space
    for (var i = 1; i < lerpedPositionCount; i++)
    {
        // Calculate interpolation factor (t) between 0 and 1
        var t = i / (float)(lerpedPositionCount - 1);

        // Lerp between startPosition (first point) and endPosition (last point) in local space
        Vector3 lerpedPosition = Vector3.Lerp(localStartPosition, localEndPosition, t);

        // Add this new point (as an offset from the LineRenderer in local space)
        positions.Add(lerpedPosition);
    }
    
    // Add the last point (local position of the end point)
    positions.Add(localEndPosition);  // This will be the last point of the rope in local space
    
    ropeLine.positionCount = positions.Count;
    // Set the calculated positions to the LineRenderer in local space
    ropeLine.SetPositions(positions.ToArray());
}

    /// <summary>
    /// For validating anchor state is in safe range. Likely not needed in favor of GetSafeAnchorState
    /// </summary>
    /// <param name="anchorState"></param>
    /// <returns></returns>
    public static bool IsAnchorStateValid(AnchorState anchorState)
    {
        switch (anchorState)
        {
            case AnchorState.Idle:
            case AnchorState.Dropping:
            case AnchorState.Dropped:
            case AnchorState.Reeling:
            case AnchorState.ReeledIn:
                return true;
            default:
                // Logger.LogError("Invalid anchor state. Enum out of range");
                return false;
        }
    }
    
    /// <summary>
    /// Returns a safe range for anchor state
    /// </summary>
    /// <param name="anchorStateInt"></param>
    /// <returns></returns>
    public static AnchorState GetSafeAnchorState(int anchorStateInt)
    {
        switch (anchorStateInt)
        {
            case (int)AnchorState.Idle:
            case (int)AnchorState.Dropping:
            case (int)AnchorState.Dropped:
            case (int)AnchorState.Reeling:
            case (int)AnchorState.ReeledIn:
                return (AnchorState)anchorStateInt;
            default:
                // Logger.LogError("Invalid anchor state. Enum out of range");
                return AnchorState.ReeledIn;
        }
    }
}
}
