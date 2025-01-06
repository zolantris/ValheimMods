using UnityEngine;

public class ChainLink : MonoBehaviour
{
    public GameObject chainLinkPrefab;  // Prefab for a single chain link (e.g., a 3D model or primitive)
    public int numberOfLinks = 10;     // Number of links in the chain
    public float linkSpacing = 0.5f;   // Distance between each link

    public Transform anchorPoint;      // The point where the chain starts (could be the ship or anchor point)
    public Transform reelPoint;        // The point where the chain is reeled from (this could be the ship's anchor point)

    public GameObject anchorPrefab;    // Prefab for the anchor (the heaviest object)

    private GameObject[] chainLinks;
    private GameObject anchor;

    void Start()
    {
        CreateChain();
    }

    // Method to create the chain by connecting multiple chain links
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
            HingeJoint hinge = chainLinks[i].AddComponent<HingeJoint>();
            hinge.connectedBody = chainLinks[i - 1].GetComponent<Rigidbody>(); // Connect to previous link
            hinge.anchor = new Vector3(0, linkSpacing, 0); // Anchor position for the hinge
            hinge.axis = Vector3.right;  // Restrict rotation along the X-axis

            // Add Rigidbody to each link (lightweight)
            Rigidbody linkRb = chainLinks[i].GetComponent<Rigidbody>();
            linkRb.mass = 0.1f; // Make chain links lightweight
        }

        // Create the anchor and attach it to the bottom of the chain
        anchor = Instantiate(anchorPrefab, chainLinks[numberOfLinks - 1].transform.position - new Vector3(0, linkSpacing, 0), Quaternion.identity);
        Rigidbody anchorRb = anchor.GetComponent<Rigidbody>();
        anchorRb.mass = 10f; // Make the anchor heavy

        // Attach the anchor to the last link in the chain
        HingeJoint anchorHinge = anchor.AddComponent<HingeJoint>();
        anchorHinge.connectedBody = chainLinks[numberOfLinks - 1].GetComponent<Rigidbody>();
        anchorHinge.anchor = new Vector3(0, -linkSpacing, 0); // Attach to the bottom link
    }

    // Method to start reeling in the anchor
    public void ReelInAnchor(float reelSpeed)
    {
        // Move the anchor upwards toward the reel point
        if (anchor != null)
        {
            anchor.transform.position = Vector3.MoveTowards(anchor.transform.position, reelPoint.position, reelSpeed * Time.deltaTime);
        }
    }

    // Method to update the chain's appearance (visual rope line, etc.)
    public void UpdateChainVisual()
    {
        // Update chain link visualizations or add a rope-like appearance
    }
}
