using System;
using System.Collections;
using UnityEngine;

public class CircularShutter : MonoBehaviour
{
    public int sliceCount = 12;          // Number of slices
    public float radius = 5f;            // Radius of the circle
    public float shutterSpeed = 3f;      // Speed of the shutter animation
    public float maxSliceWidth = 0.5f;   // Maximum width of each slice (controls how far the slice expands radially)
    public float overlapFactor = 0.05f;  // Amount of overlap between slices
    public Material sliceMaterial;       // Material to apply to each slice (set in the Inspector)

    private Transform[] slices;          // Array of slice transforms

    public bool isShutterOpen = true;  // Track if the shutter is open or closed
    private bool wasShutterOpen = true;

    void Start()
    {
        if (sliceMaterial == null)
        {
            Debug.LogWarning("No material assigned. Please assign a material to the shutter slices.");
        }

        CreateCircleMesh();
        StartCoroutine(AnimateShutterClose());  // Start with shutter closed initially
    }

    private void FixedUpdate()
    {
        if (wasShutterOpen != isShutterOpen)
        {
            ToggleShutter();
        }
    }

    // Create the circle with slices
    void CreateCircleMesh()
    {
        slices = new Transform[sliceCount];
        
        // Create a container object for the slices
        for (int i = 0; i < sliceCount; i++)
        {
            GameObject sliceObj = new GameObject("Slice_" + i);
            sliceObj.transform.SetParent(transform);
            
            // Create slice shape (mesh for each slice)
            MeshRenderer renderer = sliceObj.AddComponent<MeshRenderer>();
            MeshFilter filter = sliceObj.AddComponent<MeshFilter>();
            filter.mesh = CreateSliceMesh(i);

            // Apply the material to the slice
            if (sliceMaterial != null)
            {
                renderer.material = sliceMaterial;
            }

            // Position each slice in a circular pattern
            float angle = Mathf.Deg2Rad * (360f / sliceCount) * i;
            
            // Vector3 position = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            sliceObj.transform.localPosition = Vector3.zero;

            // Store the slice for animation purposes
            slices[i] = sliceObj.transform;
        }
    }

    // Create the slice mesh (pie slice sector)
    Mesh CreateSliceMesh(int index)
    {
        Mesh mesh = new Mesh();
        int segmentCount = 3; // Triangle mesh: 3 points (one at the center and two for the edges)
        Vector3[] vertices = new Vector3[segmentCount];
        int[] triangles = new int[3];

        // Define the center point of the slice (at the origin)
        vertices[0] = Vector3.zero;

        // Calculate the angle range for this slice
        float angle = Mathf.Deg2Rad * (360f / sliceCount) * index;
        float nextAngle = Mathf.Deg2Rad * (360f / sliceCount) * (index + 1);

        // Define the two points at the outer edge of the slice, based on the radius
        vertices[1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        vertices[2] = new Vector3(Mathf.Cos(nextAngle) * radius, Mathf.Sin(nextAngle) * radius, 0);

        // Define the triangle that makes up the slice
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }

    // Coroutine to animate the shutter opening (from center outward)
    IEnumerator AnimateShutterOpen()
    {
        isShutterOpen = true;
        wasShutterOpen = isShutterOpen;
        float stepTime = 1f / shutterSpeed;
        
        // Iterate over each slice and animate them expanding outward
        for (int i = 0; i < sliceCount; i++)
        {
            Transform slice = slices[i];
            float angle = Mathf.Deg2Rad * (360f / sliceCount) * i;
            // Vector3 targetPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Vector3 targetPosition = Vector3.zero;

            // Set initial scale to 0 (shutter closed)
            slice.localScale = new Vector3(0, 1, 1);
            slice.localPosition = targetPosition;

            // Animate the slice growing outward
            float time = 0f;
            while (time < 1f)
            {
                time += Time.deltaTime * shutterSpeed;

                // Grow the slice's width (overlap the adjacent slice)
                float width = Mathf.Lerp(0, maxSliceWidth, time);
                slice.localScale = new Vector3(width + (i > 0 ? overlapFactor : 0), 1, 1);  // Apply overlap

                yield return null;
            }
        }
    }

    // Coroutine to animate the shutter closing (shrinking back to center)
    IEnumerator AnimateShutterClose()
    {
        isShutterOpen = false;
        wasShutterOpen = isShutterOpen;
        float stepTime = 1f / shutterSpeed;

        // // Iterate over each slice and animate them shrinking back to the center
        for (int i = 0; i < sliceCount; i++)
        {
            Transform slice = slices[i];
            
            // Animate the slice shrinking back
            float time = 0f;
            while (time < 1f)
            {
                time += Time.deltaTime * shutterSpeed;
        
                // Shrink the slice back to zero (and hide it when outside the radius)
                float scale = Mathf.Lerp(maxSliceWidth, 0, time);
                slice.localScale = new Vector3(scale, 1, 1);
        
                // Make slice invisible when scale is zero
                // if (scale < 0.05f)  // Small threshold to hide slices
                // {
                //     slice.gameObject.SetActive(false);  // Hide slice
                // }
                // else
                // {
                //     slice.gameObject.SetActive(true); // Show slice
                // }
        
                yield return null;
            }
        }
    }

    // Public method to toggle shutter animation
    public void ToggleShutter()
    {
        if (wasShutterOpen)
        {
            StartCoroutine(AnimateShutterClose());
        }
        else
        {
            StartCoroutine(AnimateShutterOpen());
        }
    }
}
