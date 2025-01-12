using UnityEngine;

public class RopeSpoolGenerator : MonoBehaviour
{
    public GameObject ropeSpoolPrefab; // Optional: Prefab of the spool (cylinder model)
    public Material ropeMaterial;     // Material for the rope
    public Transform ropeStartPoint;  // Point where the rope starts (attached to the spool)
    public Transform ropeEndPoint;    // Point where the rope ends (attached to an anchor or hook)

    public float spoolRadius = 0.5f;  // Radius of the spool
    public float ropeLength = 20f;    // Length of the rope
    public float ropeSegmentLength = 0.1f; // Distance between rope segments

    private GameObject spool;
    private LineRenderer ropeLineRenderer;
    private GameObject spoolModel;
    private GameObject rope;

    void Awake()
    {
        CreateRopeSpool();
    }

    public void CreateRopeSpool()
    {
        // Add spool model (cylinder)
        if (spoolModel == null)
        {
            spoolModel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spoolModel.transform.SetParent(transform);
            spoolModel.transform.localScale = new Vector3(spoolRadius, spoolRadius * 2, spoolRadius);
            spoolModel.transform.localPosition = Vector3.zero;
            spoolModel.GetComponent<MeshRenderer>().material.color = Color.gray; // Default spool color
        }

        // Add Rope
        if (rope == null)
        {
            rope = new GameObject("Rope");
            rope.transform.SetParent(transform);
        }

        // Add LineRenderer for Rope
        if (ropeLineRenderer == null)
        {
            ropeLineRenderer = rope.AddComponent<LineRenderer>();
            ropeLineRenderer.material = ropeMaterial;
            ropeLineRenderer.startWidth = 0.05f; // Width of the rope
            ropeLineRenderer.endWidth = 0.05f;
            ropeLineRenderer.positionCount = 0;
            ropeLineRenderer.useWorldSpace = true;
        }

        // Initialize Rope
        UpdateRope();
    }

    void Update()
    {
        if (ropeLineRenderer == null)
        {
            CreateRopeSpool();
        }
        // Update rope visualization in real-time
        UpdateRope();
    }

    public void UpdateRope()
    {
        // Determine rope points
        int segments = Mathf.CeilToInt(ropeLength / ropeSegmentLength);
        ropeLineRenderer.positionCount = segments + 1;

        Vector3 direction = (ropeEndPoint.position - ropeStartPoint.position).normalized;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            ropeLineRenderer.SetPosition(i, Vector3.Lerp(ropeStartPoint.position, ropeEndPoint.position, t));
        }
    }

    public void SetRopeLength(float newLength)
    {
        ropeLength = newLength;
        UpdateRope();
    }

    public void AttachAnchor(GameObject anchorPrefab)
    {
        GameObject anchor = Instantiate(anchorPrefab, ropeEndPoint.position, Quaternion.identity);
        anchor.transform.parent = ropeEndPoint;
    }
}
