using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimVehicles.SharedScripts
{
    public class AnchorMechanismController : MonoBehaviour
{
    public enum AnchorState
    {
        Idle,         // No action is taking place.
    
        Dropping,     // The anchor is in the process of being dropped.
        Dropped,      // The anchor is fully dropped, at the target position.
    
        Reeling,      // The anchor is being reeled back in.
        ReeledIn      // The anchor is fully reeled back in to its starting position.
    }

    public Transform externalAnchorRopeAttachmentPoint;  // Point from which the rope is attached (usually the ship)
    public Transform anchorRopeAttachmentPoint;  // Point where the rope is attached (top of the anchor)
    public Transform anchorRopeAttachStartPoint;
    private Vector3 anchorStartPositionCenterOffset;
    
    public float maxDropDistance = 10f;  // Maximum depth the anchor can go
    public float reelSpeed = 5f;  // Speed at which the anchor reels in
    
    public Transform anchorTransform;
    private Rigidbody anchorRb;
    public AnchorState currentState = AnchorState.Idle;  // Current state of the anchor (Idle, Dropping, Reeling)

    public LineRenderer ropeLine;  // LineRenderer to visualize rope

    // for animations
    public HingeJoint anchorCogJoint;                 // Hinge joint for anchor movement
    public Rigidbody anchorCogRb;
    
    // callback methods meant for valheim-raft vehicles
    public Action OnAnchorRaise = delegate { };
    public Action OnAnchorDrop = delegate { };
    public Rigidbody prefabRigidbody;

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

    void Start()
    {
        if (anchorTransform == null)
        {
            anchorTransform = transform.Find("chain_generator/anchor");
        }
        
        anchorRb = anchorTransform.GetComponent<Rigidbody>();
        anchorStartPositionCenterOffset = anchorRopeAttachStartPoint.position - anchorTransform.position;

        // Initialize gravity state
        SetGravityState(false);

        // Initialize LineRenderer for rope visualization
        if (ropeLine == null)
        {
            ropeLine = GetComponent<LineRenderer>();
        }
        UpdateRopeVisual();
        UpdateAnchorState(AnchorState.Idle);
    }

    public Vector3 GetStartPosition()
    {
        return anchorRopeAttachStartPoint.position -
               anchorStartPositionCenterOffset;
    }

    void Update()
    {
        HandleKeyInputs();
    }

    void UpdateAnchorState(AnchorState newState)
    {
        currentState = newState;
        // Execute behavior based on the current state
        switch (currentState)
        {
            case AnchorState.Dropping:
                SetGravityState(true);  // Disable gravity when reeling in
                anchorRb.isKinematic = false;
                anchorCogRb.isKinematic = false;
                anchorCogJoint.useMotor = true;
                anchorCogJoint.axis = Vector3.up * -1;
                break;
            case AnchorState.Reeling:
                SetGravityState(false);  // Disable gravity when reeling in
                anchorRb.isKinematic = false;
                anchorCogRb.isKinematic = false;
                anchorCogJoint.useMotor = true;
                anchorCogJoint.axis = Vector3.up;
                break;
            case AnchorState.Dropped:
                SetGravityState(false);  // Disable gravity when reeling in
                anchorCogRb.isKinematic = true;
                anchorCogJoint.useMotor = false;
                anchorRb.isKinematic = true;
                break;
            case AnchorState.ReeledIn:
            case AnchorState.Idle:
                SetGravityState(false);  // Disable gravity when reeling in
                // anchorRb.velocity = Vector3.zero;
                // anchorRb.angularVelocity = Vector3.zero;
                anchorCogRb.isKinematic = true;
                anchorCogJoint.useMotor = false;
                anchorRb.isKinematic = true;
                anchorRb.MovePosition(GetStartPosition());
                break;
        }
    }

    void FixedUpdate()
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
            case AnchorState.ReeledIn:
            case AnchorState.Idle:
                // anchorRb.MovePosition(GetStartPosition());
                break;
        }

        UpdateRopeVisual();
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
        }

        if (Input.GetKeyDown(KeyCode.R)) // Toggle reeling with 'R' key
        {
            if (currentState != AnchorState.Reeling)
            {
                StartReeling();
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
            OnAnchorDrop.Invoke();
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
            OnAnchorRaise.Invoke();
        }
    }

    private void SetGravityState(bool isEnabled)
    {
        anchorRb.useGravity = isEnabled;
    }

    private void DropAnchor()
    {
        if (anchorRopeAttachmentPoint.position.y > externalAnchorRopeAttachmentPoint.position.y - maxDropDistance)
        {
            // Gravity is enabled, so anchor will fall naturally
        }
        else
        {
            StopDropping();
        }
    }

    private void ReelAnchor()
    {
        var deltaPosition = anchorRopeAttachmentPoint.position.y - anchorTransform.position.y;
        
        // Only reel if the anchor is below the attachment point (on the Y-axis)
        if (anchorRopeAttachmentPoint.position.y < anchorRopeAttachStartPoint.position.y)
        {
            // springJoint.maxDistance = Mathf.Clamp(springJoint.maxDistance - Time.fixedDeltaTime * reelSpeed, 0,
                // maxDropDistance);
            // ApplyVelocityChangeToCenterAnchor();
            anchorRb.velocity = Vector3.up * reelSpeed * Mathf.Clamp01(deltaPosition);
            // anchorRb.velocity = new Vector3(anchorRb.velocity.x, reelSpeed * Mathf.Clamp01(deltaPosition), anchorRb.velocity.z);

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

        // Calculate the relative position of the external anchor rope attachment point
        Vector3 startPosition = externalAnchorRopeAttachmentPoint.position - ropeLine.transform.position;

        // Calculate the relative position of the anchor rope attachment point (where the rope ends)
        Vector3 endPosition = anchorRopeAttachmentPoint.position - ropeLine.transform.position;

        // The length of the rope (in world space, not local space)
        float ropeLength = Vector3.Distance(startPosition, endPosition);

        // Number of segments based on rope length (adjust segment density here)
        int numberOfPoints = Mathf.RoundToInt(ropeLength / 2f); // Adjust this divisor for finer or coarser rope segments

        // Ensure a minimum of 2 points (start and end) and a maximum of 40 points
        ropeLine.positionCount = Mathf.Clamp(numberOfPoints + 1, 2, 40);

        // List to store the positions of the rope segments
        List<Vector3> positions = new List<Vector3>();

        // Add the first point (relative position of the externalAnchorRopeAttachmentPoint)
        positions.Add(startPosition); // This will be the first point of the rope in local space

        // Loop to interpolate the rope segments
        for (int i = 1; i < ropeLine.positionCount; i++)
        {
            // Calculate interpolation factor (t) between 0 and 1
            float t = i / (float)(ropeLine.positionCount - 1);

            // Lerp between startPosition (first point) and endPosition (last point)
            Vector3 lerpedPosition = Vector3.Lerp(startPosition, endPosition, t);

            // Add this new point (as an offset from the LineRenderer)
            positions.Add(lerpedPosition);
        }

        // Set the calculated positions to the LineRenderer
        ropeLine.SetPositions(positions.ToArray());
    }
}
}
