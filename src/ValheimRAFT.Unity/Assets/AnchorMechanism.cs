using UnityEngine;

public class AnchorMechanism : MonoBehaviour
{
    public Transform externalAnchorRopeAttachmentPoint;  // Point from which the rope is attached (usually the ship)
    public Transform anchorRopeAttachmentPoint;  // Point from which the rope is attached (usually the ship)
    public float maxDropDistance = 10f;  // Maximum depth the anchor can go
    public float reelSpeed = 5f;  // Speed at which the anchor reels in
    public float dropSpeed = 2f;  // Speed at which the anchor drops
    public LineRenderer ropeLine;  // LineRenderer to visualize rope
    
    private Rigidbody anchorRb;
    public bool isReeling = false;
    public bool isDropping = false;

    void Start()
    {
        anchorRb = GetComponent<Rigidbody>();
        anchorRb.useGravity = true;
        
        if (ropeLine == null)
        {
            ropeLine = GetComponent<LineRenderer>();
        }

        UpdateRopeVisual();
    }

    void Update()
    {
        if (isDropping)
        {
            DropAnchor();
        }
        else if (isReeling)
        {
            ReelAnchor();
        }

        UpdateRopeVisual();
    }

    // Method to drop the anchor
    public void StartDropping()
    {
        isDropping = true;
    }

    public void StopDropping()
    {
        isDropping = false;
    }

    // Method to reel the anchor back up
    public void StartReeling()
    {
        isReeling = true;
    }

    public void StopReeling()
    {
        isReeling = false;
    }

    // Dropping the anchor
    private void DropAnchor()
    {
        if (Vector3.Distance(anchorRopeAttachmentPoint.position, externalAnchorRopeAttachmentPoint.position) < maxDropDistance)
        {
            anchorRb.useGravity = true;
            // anchorRb.velocity = new Vector3(0, -dropSpeed, 0);
        }
        else
        {
            anchorRb.useGravity = false;
            anchorRb.velocity = Vector3.zero;
        }
    }

    // Reeling the anchor back up
    private void ReelAnchor()
    {
        anchorRb.useGravity = false;
        if (anchorRopeAttachmentPoint.position.y > externalAnchorRopeAttachmentPoint.position.y)
        {
            anchorRb.velocity = Vector3.zero;
            isReeling = false;
            return;
        }
        
        if (Vector3.Distance(anchorRopeAttachmentPoint.position, externalAnchorRopeAttachmentPoint.position) > 0.1f)
        {
            anchorRb.velocity = new Vector3(0, reelSpeed, 0);
        }
        else
        {
            anchorRb.velocity = Vector3.zero;
            isReeling = false;
        }
    }

    // Update the rope visualization
    private void UpdateRopeVisual()
    {
        if (ropeLine != null)
        {
            ropeLine.SetPosition(0, externalAnchorRopeAttachmentPoint.position);  // Start of the rope (attachment point)
            ropeLine.SetPosition(1, transform.position);  // End of the rope (anchor position)
        }
    }
}