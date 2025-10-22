#region

using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts.Magic
{
  /// <summary>
  /// Simple mouse-look for testing in the Unity editor.
  /// Attach to your Camera. Uses Mouse X/Y, RMB to freely look while pressed.
  /// </summary>
  public class TestMouseLook : MonoBehaviour
  {
    [SerializeField] private float sensitivity = 120f; // deg/sec per mouse unit
    [SerializeField] private float pitchMin = -80f;
    [SerializeField] private float pitchMax = 80f;
    [SerializeField] private bool requireRightMouse = true;

    private float _yaw;
    private float _pitch;

    private void Start()
    {
      var e = transform.eulerAngles;
      _yaw = e.y;
      _pitch = e.x > 180 ? e.x - 360f : e.x;
    }

    private void Update()
    {
      if (requireRightMouse && !Input.GetMouseButton(1)) return;

      var dx = Input.GetAxis("Mouse X");
      var dy = Input.GetAxis("Mouse Y");

      _yaw += dx * sensitivity * Time.unscaledDeltaTime;
      _pitch -= dy * sensitivity * Time.unscaledDeltaTime;
      _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

      transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
  }
}