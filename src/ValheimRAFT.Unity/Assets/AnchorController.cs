using System.Collections.Generic;
using UnityEngine;

public class AnchorController : MonoBehaviour
{
    public enum AnchorState
    {
        Idle,     // Anchor is not moving
        Dropping, // Anchor is dropping down
        Reeling   // Anchor is being reeled back up
    }

    public Transform externalAnchorRopeAttachmentPoint;  // Point from which the rope is attached (usually the ship)
    public Transform anchorRopeAttachmentPoint;          // Point where the rope is attached (top of the anchor)
    public GameObject chainLinkPrefab;                   // Prefab for the chain links
    public Transform chainBox;                           // Parent object to hold chain links

    public float maxDropDistance = 10f;                  // Maximum depth the anchor can go
    public float chainLinkHeight = 2f;                   // Height of each chain link
    public float reelSpeed = 5f;                         // Speed at which the anchor reels in
    public float dropSpeed = 2f;                         // Speed at which the anchor drops
    public HingeJoint anchorCogJoint;                 // Hinge joint for anchor movement
    public Rigidbody anchorCogRb;                 // Hinge joint for anchor movement

    private Rigidbody anchorRb;
    private Transform anchorTransform;
    private AnchorState currentState = AnchorState.Idle; // Current state of the anchor
    private List<GameObject> chainLinks = new List<GameObject>(); // List to manage spawned chain links

    void Start()
    {
        anchorTransform = transform.Find("chain_generator/anchor");
        anchorRb = anchorTransform.GetComponent<Rigidbody>();
        SetGravityState(false);
        UpdateRenderedChains();
    }

    void Update()
    {
        HandleKeyInputs();

        switch (currentState)
        {
            case AnchorState.Dropping:
                DropAnchor();
                anchorCogRb.isKinematic = false;
                anchorCogJoint.useMotor = true;
                break;
            case AnchorState.Reeling:
                anchorCogRb.isKinematic = false;
                ReelAnchor();
                anchorCogJoint.useMotor = true;
                break;
            case AnchorState.Idle:
                anchorCogRb.isKinematic = true;
                anchorCogJoint.useMotor = false;
                break;
        }

        UpdateRenderedChains();
    }

    private void HandleKeyInputs()
    {
        if (Input.GetKeyDown(KeyCode.D)) // Toggle dropping with 'D' key
        {
            if (currentState != AnchorState.Dropping)
                StartDropping();
            else
                StopDropping();
        }

        if (Input.GetKeyDown(KeyCode.R)) // Toggle reeling with 'R' key
        {
            if (currentState != AnchorState.Reeling)
                StartReeling();
            else
                StopReeling();
        }
    }

    public void StartDropping()
    {
        currentState = AnchorState.Dropping;
        SetGravityState(true);
    }

    public void StopDropping()
    {
        currentState = AnchorState.Idle;
        SetGravityState(false);
    }

    public void StartReeling()
    {
        currentState = AnchorState.Reeling;
        SetGravityState(false);
    }

    public void StopReeling()
    {
        currentState = AnchorState.Idle;
    }

    private void SetGravityState(bool isEnabled)
    {
        anchorRb.useGravity = isEnabled;
    }

    private void DropAnchor()
    {
        if (Vector3.Distance(anchorRopeAttachmentPoint.position, externalAnchorRopeAttachmentPoint.position) < maxDropDistance)
        {
            // Anchor drops naturally with gravity
        }
        else
        {
            StopDropping();
        }
    }

    private void ReelAnchor()
    {
        if (anchorRopeAttachmentPoint.position.y < externalAnchorRopeAttachmentPoint.position.y)
        {
            anchorTransform.position = Vector3.Lerp(
                anchorTransform.position,
                externalAnchorRopeAttachmentPoint.position,
                reelSpeed * Time.deltaTime
            );

            if (Vector3.Distance(anchorTransform.position, externalAnchorRopeAttachmentPoint.position) <= 0.1f)
                StopReeling();
        }
        else
        {
            StopReeling();
        }
    }

    private void UpdateRenderedChains()
    {
        int requiredChainLinks = Mathf.CeilToInt(Vector3.Distance(
            anchorRopeAttachmentPoint.position,
            externalAnchorRopeAttachmentPoint.position
        ) / chainLinkHeight);

        // Spawn or remove chain links as needed
        while (chainLinks.Count < requiredChainLinks)
        {
            SpawnChainLink();
        }

        while (chainLinks.Count > requiredChainLinks)
        {
            RemoveChainLink();
        }
    }

    private void SpawnChainLink()
    {
        // Calculate the position for the new chain link
        Vector3 chainLinkPosition = chainBox.position - Vector3.up * (chainLinks.Count * chainLinkHeight);

        // Instantiate the new chain link and parent it to the chainBox
        GameObject newChainLink = Instantiate(chainLinkPrefab, chainLinkPosition, Quaternion.identity, chainBox);

        // Add the new chain link to the list
        chainLinks.Add(newChainLink);
    }

    private void RemoveChainLink()
    {
        if (chainLinks.Count > 0)
        {
            // Safely remove the last chain link
            GameObject lastChainLink = chainLinks[chainLinks.Count - 1];
            chainLinks.RemoveAt(chainLinks.Count - 1);

            // Destroy the chain link game object
            Destroy(lastChainLink);
        }
    }
}
