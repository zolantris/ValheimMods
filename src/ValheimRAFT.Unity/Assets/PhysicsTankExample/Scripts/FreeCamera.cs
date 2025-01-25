using UnityEngine;

namespace PhysicsTankExample.Scripts
{
  public class FreeCamera : MonoBehaviour
  {
    public bool useFixedUpdate;

    public bool invert = true;

    public float sensitivity = 20f;

    public float speed = 20f;
    public float modSpeed = 60f;
    public float zoomFov = 20.0f;

    public bool visible = true;

    private Camera cam;
    private float lastX, lastY;

    private float shift = 1f;
    private float startingFov;

    private bool useModSpeed;
    private float x, y;

    private void Start()
    {
      cam = GetComponent<Camera>();
      startingFov = cam.fieldOfView;
    }

    private void Update()
    {
      if (Input.GetMouseButtonDown(0))
      {
        visible = !visible;

        if (visible)
        {
          Cursor.lockState = CursorLockMode.None;
          Cursor.visible = true;
        }
        else
        {
          Cursor.lockState = CursorLockMode.Locked;
          Cursor.visible = false;
        }
      }

      if (Input.GetMouseButtonDown(1)) useModSpeed = !useModSpeed;

      if (cam != null)
      {
        var targetFov = startingFov;
        if (Input.GetMouseButton(2))
          targetFov = zoomFov;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov,
          10.0f * Time.deltaTime);
      }

      if (!useFixedUpdate)
        MouseLookMovement();
    }

    private void FixedUpdate()
    {
      if (useFixedUpdate)
        MouseLookMovement();
    }

    private void MouseLookMovement()
    {
      var trueSpeed = useModSpeed ? modSpeed : speed;

      if (!visible)
      {
        x = Input.GetAxis("Mouse X") + lastX * 0.9f;

        if (invert)
          y = -Input.GetAxis("Mouse Y") + lastY * 0.9f;
        else
          y = Input.GetAxis("Mouse Y") + lastY * 0.9f;

        var targetShift = Input.GetKey(KeyCode.LeftShift) ? 2f : 1f;
        shift = Mathf.Lerp(shift, targetShift, trueSpeed * Time.deltaTime);

        lastX = x;
        lastY = y;

        transform.Rotate(sensitivity * y * Time.deltaTime,
          sensitivity * x * Time.deltaTime, 0f);
      }

      var upright = transform.eulerAngles;
      upright.z = 0f;
      transform.eulerAngles = upright;

      transform.Translate(
        trueSpeed * shift * Input.GetAxis("Horizontal") * Time.deltaTime,
        0f,
        trueSpeed * shift * Input.GetAxis("Vertical") * Time.deltaTime);
    }
  }
}