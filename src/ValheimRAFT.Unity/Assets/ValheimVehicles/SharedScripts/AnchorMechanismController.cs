using System;
using System.Collections.Generic;
using UnityEngine;

public class AnchorMechanismController : MonoBehaviour
{
    public enum AnchorState
    {
        Idle,     // Anchor is not moving
        Dropping, // Anchor is dropping down
        Reeling   // Anchor is being reeled back up
    }

    public Transform externalAnchorRopeAttachmentPoint;  // Point from which the rope is attached (usually the ship)
    public Transform anchorRopeAttachmentPoint;  // Point where the rope is attached (top of the anchor)
    public Transform anchorRopeAttachStartPoint;
    private Vector3 rbStartPosition;
    
    public float maxDropDistance = 10f;  // Maximum depth the anchor can go
    public float reelSpeed = 5f;  // Speed at which the anchor reels in
    
    public Transform anchorTransform;
    private Rigidbody anchorRb;
    public AnchorState currentState = AnchorState.Idle;  // Current state of the anchor (Idle, Dropping, Reeling)

    public LineRenderer ropeLine;  // LineRenderer to visualize rope
    public Vector3 RopeStartPosition = new Vector3(0f, 0.7f, 0f);

    // for animations
    public HingeJoint anchorCogJoint;                 // Hinge joint for anchor movement
    public Rigidbody anchorCogRb;

    public SpringJoint springJoint; // Spring Joint to attach the anchor to the ship

    // callback methods meant for valheim-raft vehicles
    public Action OnAnchorRaise = delegate { };
    public Action OnAnchorDrop = delegate { };

    void Start()
    {
        if (anchorTransform == null)
        {
            anchorTransform = transform.Find("chain_generator/anchor");
        }
        anchorRb = anchorTransform.GetComponent<Rigidbody>();
        rbStartPosition = anchorRb.position - Vector3.up * 0.05f;

        // Initialize gravity state
        SetGravityState(false);

        // Initialize LineRenderer for rope visualization
        if (ropeLine == null)
        {
            ropeLine = GetComponent<LineRenderer>();
        }
        
        ropeLine.positionCount = 2;  // Only two points (attachment point and anchor)

        UpdateRopeVisual();

        // Add Spring Joint to simulate attachment to the ship
        // springJoint = anchorTransform.gameObject.AddComponent<SpringJoint>();
        // springJoint.connectedBody = externalAnchorRopeAttachmentPoint.GetComponent<Rigidbody>();
        // springJoint.spring = 500f; // Adjust spring force to fit the needed attachment
        // springJoint.damper = 50f; // Adjust damper to prevent oscillations
        // springJoint.maxDistance = maxDropDistance;  // Control how far the anchor can move
    }

    void Update()
    {
        HandleKeyInputs();
    }

    void FixedUpdate()
    {
        if (anchorRb == null) return;
        UpdateRopeVisual();

        // Execute behavior based on the current state
        switch (currentState)
        {
            case AnchorState.Dropping:
                anchorRb.isKinematic = false;
                anchorCogRb.isKinematic = false;
                anchorCogJoint.useMotor = true;
                anchorCogJoint.axis = Vector3.up * -1;
                DropAnchor();
                break;
            case AnchorState.Reeling:
                anchorRb.isKinematic = false;
                anchorCogRb.isKinematic = false;
                anchorCogJoint.useMotor = true;
                anchorCogJoint.axis = Vector3.up;
                ReelAnchor();
                break;
            case AnchorState.Idle:
                anchorRb.useGravity = false;
                // anchorRb.velocity = Vector3.zero;
                // anchorRb.angularVelocity = Vector3.zero;
                anchorCogRb.isKinematic = true;
                anchorCogJoint.useMotor = false;
                anchorRb.isKinematic = true;
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
            currentState = AnchorState.Dropping;
            SetGravityState(true);  // Enable gravity when the anchor is dropping
        }
    }

    public void StopDropping()
    {
        if (anchorRb == null) return;
        if (currentState == AnchorState.Dropping)
        {
            OnAnchorDrop.Invoke();
            currentState = AnchorState.Idle;
            // anchorRb.velocity = Vector3.zero;
            // anchorRb.angularVelocity = Vector3.zero;
            SetGravityState(false);
        }
    }

    public void StartReeling()
    {
        if (anchorRb == null) return;
        if (currentState != AnchorState.Reeling)
        {
            currentState = AnchorState.Reeling;
            SetGravityState(false);  // Disable gravity when reeling in
        }
    }

    public void StopReeling()
    {
        if (anchorRb == null) return;
        
        if (currentState == AnchorState.Reeling)
        {
            // anchorRb.position = rbStartPosition;
            // currentState = AnchorState.Idle;
            anchorRb.velocity = new Vector3(anchorRb.velocity.x, 0f,anchorRb.velocity.z);
            // anchorRb.angularVelocity = Vector3.zero;
            SetGravityState(false);
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

    public float maxSpeed = 20f;
    void ApplyVelocityChangeToCenterAnchor()
    {
        // Calculate the difference in position (Vector3)
        Vector3 directionToTarget = anchorRopeAttachStartPoint.position - anchorRb.position;

        // Calculate the desired velocity change (velocity needed to move towards the target)
        // We ignore the Y-axis for now since we're focusing on X and Z movement
        directionToTarget.y = 0f;

        // If the distance is very small, don't apply any velocity
        if (directionToTarget.sqrMagnitude < 0.1f)
        {
            anchorRb.velocity = Vector3.zero;  // Stop the movement if close enough
            return;
        }

        // Normalize the direction to get a unit vector (direction only)
        directionToTarget.Normalize();

        // Multiply by maxSpeed to get the velocity change
        Vector3 velocityChange = directionToTarget * maxSpeed;

        // Apply the calculated velocity change to the Rigidbody
        // We use `velocity` to directly set the velocity, as we're using a "velocity change" approach
        anchorRb.velocity = new Vector3(velocityChange.x, anchorRb.velocity.y, velocityChange.z);

        // Optionally, apply a "softening" factor (e.g., reduce velocity when close to target)
        // You could also apply a damping factor to gradually slow down as you approach the target
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
