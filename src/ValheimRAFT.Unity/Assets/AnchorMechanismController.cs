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
    public Mesh LinearRenderMesh;
    public float AnchorRopeWidth = 0.1f;
    public Vector3 RopeStartPosition = new Vector3(0f, 0.7f, 0f);

    void Awake()
    {
    }
    void Start()
    {
        if (anchorTransform == null)
        {
            anchorTransform = transform.Find("chain_generator/anchor");
        }
        anchorRb = anchorTransform.GetComponent<Rigidbody>();
        rbStartPosition = anchorRb.position;

        // Initialize gravity state
        SetGravityState(false);

        // Initialize LineRenderer for rope visualization
        if (ropeLine == null)
        {
            ropeLine = GetComponent<LineRenderer>();
        }
        
        // ropeLine.startWidth = AnchorRopeWidth;  // Rope thickness at the start
        // ropeLine.endWidth = AnchorRopeWidth;  // Rope thickness at the end
        ropeLine.positionCount = 2;  // Only two points (attachment point and anchor)

        UpdateRopeVisual();
    }

    void Update()
    {
        HandleKeyInputs();
    }

    void FixedUpdate()
    {
        UpdateRopeVisual();
        // Handle key inputs to toggle dropping and reeling
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
                anchorRb.useGravity = false;
                anchorRb.velocity = Vector3.zero;
                anchorRb.angularVelocity = Vector3.zero;
                // Nothing happens when the anchor is idle
                break;
        }
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
        }

        if (Input.GetKeyDown(KeyCode.R)) // Toggle reeling with 'R' key
        {
            if (currentState != AnchorState.Reeling)
            {
                StartReeling();
            }
        }
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
            anchorRb.velocity = Vector3.zero;
            anchorRb.angularVelocity = Vector3.zero;
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
            anchorRb.velocity = Vector3.zero;
            anchorRb.angularVelocity = Vector3.zero;
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
        if (anchorRopeAttachmentPoint.position.y > externalAnchorRopeAttachmentPoint.position.y - maxDropDistance)
        {
            // Gravity is enabled, so anchor will fall naturally
        }
        else
        {
            StopDropping();
        }
    }

    // Reeling the anchor back up logic
    private void ReelAnchor()
    {
        var deltaPosition = anchorRopeAttachmentPoint.position.y - anchorTransform.position.y;

        if (deltaPosition < 0f)
        {
            anchorRb.velocity = Vector3.zero;
            anchorRb.angularVelocity = Vector3.zero;
        }
        
        // Only reel if the anchor is below the attachment point (on the Y-axis)
        if (anchorRopeAttachmentPoint.position.y < anchorRopeAttachStartPoint.position.y)
        {
            // Smoothly move the anchor towards the target position (externalAnchorRopeAttachmentPoint.position)
            // anchorRb.velocity = Vector3.zero;  // Reset velocity to prevent unnatural movement
            // anchorRb.angularVelocity = Vector3.zero;
            anchorRb.velocity = Vector3.up * reelSpeed * Mathf.Clamp01(deltaPosition);

            // Check if the anchor is close enough to the attachment point
            if (anchorRopeAttachmentPoint.position.y > anchorRopeAttachStartPoint.position.y)
            {
                anchorRb.position = rbStartPosition;
                // Stop reeling once the anchor reaches or is very close to the attachment point
                StopReeling();
            }
        }
        else
        {
            anchorRb.position = rbStartPosition;
            // Stop reeling if the anchor has reached or passed the attachment point
            StopReeling();
        }
    }

    // Update the rope visualization
    private void UpdateRopeVisual()
    {
        if (ropeLine == null) return;
        // offset from the width. Half of the width to center it.
        // var offset = new Vector3(0f,0f,0f);
        var numberOfPoints = Mathf.RoundToInt(externalAnchorRopeAttachmentPoint.position.y - anchorRopeAttachmentPoint.position.y / 0.5f);

        ropeLine.positionCount = Mathf.Clamp(numberOfPoints, 2, 40);
        var positions = new List<Vector3>();
        for (var i = 0; i <  ropeLine.positionCount; i++)
        {
            var topOfAnchorPosition = anchorRopeAttachmentPoint.position;
            if (i == 0)
            {
                positions.Add(RopeStartPosition);
            }
            else if (i == ropeLine.positionCount - 1)
            {
                positions.Add(Vector3.up * (topOfAnchorPosition.y - ropeLine.transform.position.y));
            }
            else
            {
                positions.Add(Vector3.up * -1 * i);
                // var offset = Vector3.up * -0.5f * i;
                // positions.Add();
            }
        }
        ropeLine.SetPositions(positions.ToArray());
    }
}
