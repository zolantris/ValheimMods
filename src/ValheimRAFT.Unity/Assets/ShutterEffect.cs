using System.Collections;
using UnityEngine;

public class ShutterEffect : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed of the shutter effect
    public bool isShutterOpen = false; // Whether the shutter is open or closed

    private MeshRenderer[] sliceRenderers;

    void Start()
    {
        sliceRenderers = GetComponentsInChildren<MeshRenderer>(); // Assuming each slice is a separate child object with a mesh renderer
    }

    void Update()
    {
        if (isShutterOpen)
        {
            OpenShutter();
        }
        else
        {
            CloseShutter();
        }
    }

    void OpenShutter()
    {
        // Move the slices up to open the shutter
        foreach (var renderer in sliceRenderers)
        {
            renderer.transform.localPosition = Vector3.Lerp(renderer.transform.localPosition, new Vector3(0, 10, 0), Time.deltaTime * moveSpeed);
        }
    }

    void CloseShutter()
    {
        // Move the slices back to the original position to close the shutter
        foreach (var renderer in sliceRenderers)
        {
            renderer.transform.localPosition = Vector3.Lerp(renderer.transform.localPosition, Vector3.zero, Time.deltaTime * moveSpeed);
        }
    }

    // Call this function to trigger the effect
    public void ToggleShutter()
    {
        isShutterOpen = !isShutterOpen;
    }
}

