using UnityEngine;

public class ChainLink : MonoBehaviour
{
    public GameObject chainLinkPrefab;  // Prefab for a single chain link (e.g., a 3D model or primitive)
    public int numberOfLinks = 2;     // Number of links in the chain
    public float linkSpacing = 1.2f;   // Distance between each link

    public Transform anchorPoint;      // The point where the chain starts (could be the ship or anchor point)
    public Transform reelPoint;        // The point where the chain is reeled from (this could be the ship's anchor point)

    public GameObject anchorPrefab;    // Prefab for the anchor (the heaviest object)

    private GameObject[] chainLinks;
    private GameObject anchor;
    private Transform anchorAttachmentPoint; // The internal attachment point in the anchor prefab
    
    void Start()
    {
        CreateChain();
    }
    
    public float reelSpeed = 2f;      // Speed at which to reel the anchor
    public float dropSpeed = 2f;      // Speed at which to drop the anchor

    private bool isReeling = false;
    private bool isDropping = false;

    void Update()
    {
        // Toggle reeling with the "R" key
        if (Input.GetKeyDown(KeyCode.R) && !isDropping)  // Prevent reeling and dropping at the same time
        {
            isReeling = !isReeling;
        }

        // Toggle dropping with the "D" key
        if (Input.GetKeyDown(KeyCode.D) && !isReeling)  // Prevent reeling and dropping at the same time
        {
            isDropping = !isDropping;
        }

        // Perform actions based on the state
        if (isReeling)
        {
            ReelInAnchor(reelSpeed);  // Reel in the anchor
        }

        if (isDropping)
        {
            ReleaseAnchor(dropSpeed);  // Release the anchor
        }
    }

      // Create the chain by connecting multiple chain links
    void CreateChain()
    {
        chainLinks = new GameObject[numberOfLinks];

        // Create the first chain link and position it at the anchor point
        chainLinks[0] = Instantiate(chainLinkPrefab, anchorPoint.position, Quaternion.identity);
        Rigidbody firstLinkRb = chainLinks[0].GetComponent<Rigidbody>();
        firstLinkRb.isKinematic = true; // The first link will be fixed to the anchor, so make it kinematic

        // Create the rest of the chain links
        for (int i = 1; i < numberOfLinks; i++)
        {
            Vector3 position = chainLinks[i - 1].transform.position - new Vector3(0, linkSpacing, 0);
            chainLinks[i] = Instantiate(chainLinkPrefab, position, Quaternion.identity);

            // Attach each link to the previous one using a HingeJoint
            var hinge = chainLinks[i].AddComponent<HingeJoint>();
            hinge.connectedBody = chainLinks[i - 1].GetComponent<Rigidbody>();
            hinge.anchor = new Vector3(0, linkSpacing, 0); // Position of the joint
            hinge.connectedAnchor = new Vector3(0, 1.2f, 0);
            hinge.axis = Vector3.right;  // The axis of rotation

            // Disable the spring to prevent bouncing behavior
            hinge.useSpring = false;
            hinge.useMotor = false;
            hinge.useLimits = true;

            // Set hinge limits
            // JointLimits limits = hinge.limits;
            // limits.min = 0;  // Lock the rotation
            // limits.max = 0;  // Lock the rotation
            // hinge.limits = limits;

            // Apply constraints to rigidbody to prevent rotation or movement in unwanted directions
            Rigidbody linkRb = chainLinks[i].GetComponent<Rigidbody>();
            linkRb.mass = 5f;  // Make chain links lightweight
            linkRb.drag = 10f;    // Reduce drag for a more stable chain
            linkRb.angularDrag = 10f;
        }

        // Create the anchor and attach it to the bottom of the chain
        anchor = Instantiate(anchorPrefab, chainLinks[numberOfLinks - 1].transform.position - new Vector3(0, linkSpacing, 0), Quaternion.identity);

        // Now move the anchor so the attachment point is aligned with the last chain link's position
        AlignAnchorWithLastChainLink();
        
        // Find the attachment point inside the anchor prefab and set that point as the anchor
        anchorAttachmentPoint = anchor.transform.Find("attachpoint");
        var anchorHinge = anchor.GetComponent<HingeJoint>();
        anchorHinge.connectedBody = chainLinks[numberOfLinks -1].GetComponent<Rigidbody>();
        anchorHinge.anchor = anchorAttachmentPoint.localPosition;
        anchorHinge.autoConfigureConnectedAnchor = false;
        // should not be any offsets. Besides a small amount to center the anchor
        anchorHinge.connectedAnchor = new Vector3(-0.04f, 0.3f, 0);
        anchorHinge.axis = Vector3.right;  // The axis of rotation
    }
    
    // Method to start reeling in the anchor
    public void ReelInAnchor(float reelSpeed)
    {
        // Move the anchor upwards toward the reel point
        if (anchor != null)
        {
            // Ensure the anchor doesn't pass the reel point
            if (anchor.transform.position.y < reelPoint.position.y)
            {
                anchor.transform.position = Vector3.MoveTowards(anchor.transform.position, reelPoint.position, reelSpeed * Time.deltaTime);
            }
        }
    }
    
    // Align the anchor with the last chain link
    void AlignAnchorWithLastChainLink()
    {
        if (anchor != null && anchorAttachmentPoint != null && chainLinks[numberOfLinks - 1] != null)
        {
            // Calculate the offset between the attachment point and the current position of the anchor
            Vector3 attachPointPosition = anchorAttachmentPoint.position;

            // The target position is where the anchor should go (aligned with the last chain link)
            Vector3 targetPosition = chainLinks[numberOfLinks - 1].transform.position;

            // Calculate the offset from the anchor's attachment point to the anchor's current position
            Vector3 offset = attachPointPosition - anchor.transform.position;

            // Move the entire anchor to ensure the attachment point is aligned with the target position
            anchor.transform.position = targetPosition;
        }
    }

    // Method to release the anchor (allow it to drop)
    public void ReleaseAnchor(float dropSpeed)
    {
        if (anchor != null)
        {
            // Make the anchor fall by enabling gravity
            Rigidbody anchorRb = anchor.GetComponent<Rigidbody>();
            anchorRb.useGravity = true;
            anchor.transform.position = Vector3.MoveTowards(anchor.transform.position, anchorPoint.position, dropSpeed * Time.deltaTime);
        }
    }

    // Method to update the chain's appearance (visual rope line, etc.)
    public void UpdateChainVisual()
    {
        // Update chain link visualizations or add a rope-like appearance
    }
}
