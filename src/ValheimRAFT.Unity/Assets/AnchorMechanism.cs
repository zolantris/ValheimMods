using UnityEngine;

public class AnchorMechanism : MonoBehaviour
{
    public enum AnchorState
    {
        Idle,     // Anchor is not moving
        Dropping, // Anchor is dropping down
        Reeling   // Anchor is being reeled back up
    }

    public Transform externalAnchorRopeAttachmentPoint;  // Point from which the rope is attached (usually the ship)
    public Transform anchorRopeAttachmentPoint;  // Point where the rope is attached (top of the anchor)
    
    public float maxDropDistance = 10f;  // Maximum depth the anchor can go
    public float reelSpeed = 5f;  // Speed at which the anchor reels in
    public float dropSpeed = 2f;  // Speed at which the anchor drops
    public SpringJoint springJoint;  // SpringJoint to simulate the rope connection
    
    private Rigidbody anchorRb;
    public AnchorState currentState = AnchorState.Idle;  // Current state of the anchor (Idle, Dropping, Reeling)

    public LineRenderer ropeLine;  // LineRenderer to visualize rope

    void Start()
    {
        anchorRb = GetComponent<Rigidbody>();

        // Ensure that the SpringJoint exists, if not create it
        EnsureSpringJoint();

        // Initialize gravity state
        SetGravityState(false);

        // Setup the spring joint if it wasn't already set up
        SetupSpringJoint();

        // Initialize LineRenderer for rope visualization
        if (ropeLine == null)
        {
            ropeLine = GetComponent<LineRenderer>();
        }

        ropeLine.startWidth = 0.1f;  // Rope thickness at the start
        ropeLine.endWidth = 0.1f;  // Rope thickness at the end
        ropeLine.positionCount = 2;  // Only two points (attachment point and anchor)
        
        UpdateRopeVisual();
    }

    void Update()
    {
        // Handle key inputs to toggle dropping and reeling
        HandleKeyInputs();

        // Execute behavior based on the current state
        switch (currentState)
        {
            case AnchorState.Dropping:
                DropAnchor();
                break;
            case AnchorState.Reeling:
                ReelAnchor();
                break;
            case AnchorState.Idle:
                // Nothing happens when the anchor is idle
                break;
        }

        UpdateRopeVisual();
    }

    // Method to handle key inputs for toggling dropping and reeling
    private void HandleKeyInputs()
    {
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
            }
            else
            {
                StopReeling();
            }
        }
    }

    // Method to ensure SpringJoint exists, creates one if not
    private void EnsureSpringJoint()
    {
        if (springJoint == null)
        {
            springJoint = gameObject.AddComponent<SpringJoint>();
        }
    }

    // Method to setup the SpringJoint, but only if it's not already configured
    private void SetupSpringJoint()
    {
        // Bail if SpringJoint already exists and is already connected
        if (springJoint != null && springJoint.connectedBody != null)
        {
            return;  // Exit if SpringJoint is already connected and configured
        }

        // Otherwise, setup the SpringJoint
        springJoint.connectedBody = externalAnchorRopeAttachmentPoint.GetComponent<Rigidbody>();
        springJoint.spring = 500f;  // Rope stiffness (adjust based on how tight you want the rope)
        springJoint.damper = 10f;   // How much damping the rope has (adjust to prevent oscillations)
        springJoint.maxDistance = maxDropDistance;  // Maximum rope length
        springJoint.autoConfigureConnectedAnchor = false;
        springJoint.anchor = Vector3.zero;  // Anchor is at the center of the object, adjust if needed
        springJoint.connectedAnchor = anchorRopeAttachmentPoint.localPosition;  // Point of attachment for the rope
    }

    // Method to start dropping the anchor
    public void StartDropping()
    {
        if (currentState != AnchorState.Dropping)
        {
            currentState = AnchorState.Dropping;
            SetGravityState(true);  // Enable gravity when the anchor is dropping
        }
    }

    public void StopDropping()
    {
        if (currentState == AnchorState.Dropping)
        {
            currentState = AnchorState.Idle;
            SetGravityState(false);  // Disable gravity when the anchor is not dropping
        }
    }

    // Method to start reeling the anchor back up
    public void StartReeling()
    {
        if (currentState != AnchorState.Reeling)
        {
            currentState = AnchorState.Reeling;
            SetGravityState(false);  // Disable gravity when reeling in
        }
    }

    public void StopReeling()
    {
        if (currentState == AnchorState.Reeling)
        {
            currentState = AnchorState.Idle;
            SetGravityState(false);  // Disable gravity when not reeling
        }
    }

    // Helper method to enable/disable gravity based on state
    private void SetGravityState(bool isEnabled)
    {
        anchorRb.useGravity = isEnabled;
    }

    // Dropping the anchor logic
    private void DropAnchor()
    {
        if (Vector3.Distance(anchorRopeAttachmentPoint.position, externalAnchorRopeAttachmentPoint.position) < maxDropDistance)
        {
            // Gravity is enabled, so anchor will fall naturally
        }
        else
        {
            // Once the anchor hits the max drop distance, stop gravity and velocity
            anchorRb.useGravity = false;
            anchorRb.velocity = Vector3.zero;
        }
    }

    // Reeling the anchor back up logic
    private void ReelAnchor()
    {
        // Only reel if the anchor is below the attachment point (on the Y-axis)
        if (anchorRopeAttachmentPoint.position.y < externalAnchorRopeAttachmentPoint.position.y)
        {
            // Smoothly move the anchor towards the target position (externalAnchorRopeAttachmentPoint.position)
            anchorRb.velocity = Vector3.zero;  // Reset velocity to prevent unnatural movement
            
            // Move anchor smoothly towards the attachment point using Lerp
            transform.position = Vector3.Lerp(transform.position, externalAnchorRopeAttachmentPoint.position, reelSpeed * Time.deltaTime);

            // Check if the anchor is close enough to the attachment point
            if (Vector3.Distance(transform.position, externalAnchorRopeAttachmentPoint.position) <= 0.1f)
            {
                // Stop reeling once the anchor reaches or is very close to the attachment point
                StopReeling();
            }
        }
        else
        {
            // Stop reeling if the anchor has reached or passed the attachment point
            StopReeling();
        }
    }

    // Update the rope visualization
    private void UpdateRopeVisual()
    {
        if (ropeLine != null)
        {
            ropeLine.SetPosition(0, externalAnchorRopeAttachmentPoint.position);  // Start of the rope (attachment point)
            ropeLine.SetPosition(1, anchorRopeAttachmentPoint.position);  // End of the rope (anchor position)
        }
    }
}
