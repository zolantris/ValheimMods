using UnityEngine;

public class AutoFaceCamera : MonoBehaviour
{
  public Camera mainCamera;

  private void Update()
  {
    if (mainCamera == null)
      mainCamera =
        Camera.main; // Automatically assign the main camera if not set.

    // Make the sprite face the camera
    transform.LookAt(mainCamera.transform);
  }
}