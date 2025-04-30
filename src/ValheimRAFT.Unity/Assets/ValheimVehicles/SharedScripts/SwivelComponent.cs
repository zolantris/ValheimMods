#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  [RequireComponent(typeof(Rigidbody))]
  public class SwivelComponent : MonoBehaviour
  {

    public enum DoorHingeMode
    {
      ZOnly,
      YOnly,
      Both
    }

    public enum HingeDirection
    {
      Forward, // positive angle
      Backward // negative angle
    }

    [Header("Swivel Settings")]
    [SerializeField] private SwivelMode mode = SwivelMode.None;
    [SerializeField] private Transform planeTransform;
    [SerializeField] private float maxTurnAnglePerSecond = 90f;
    [SerializeField] private float maxInclineZ = 90f;
    [SerializeField] private float turningLerpSpeed = 5f;

    [Header("Enemy Tracking Settings")]
    [SerializeField] private float minTrackingRange = 5f;
    [SerializeField] private float maxTrackingRange = 50f;
    [SerializeField] private GameObject nearestEnemyMock;

    [Header("Door Mode Settings")]
    [SerializeField] private bool isDoorOpen;
    [SerializeField] private float doorLerpSpeed = 2f;
    [SerializeField] public DoorHingeMode hingeMode = DoorHingeMode.ZOnly;
    [SerializeField] private HingeDirection zHingeDirection = HingeDirection.Forward;
    [SerializeField] private HingeDirection yHingeDirection = HingeDirection.Forward;

    [SerializeField] private float maxYAngle = 90f; // optional Y rotation max (like a normal swing door)

    private float _currentYAngle;
    private float _currentZAngle;

    [Header("Other Settings")]
    private Rigidbody _rigidbody;
    private Quaternion _targetRotation;

    public DoorHingeMode CurrentHingeMode => hingeMode;
    public HingeDirection CurrentZHingeDirection => zHingeDirection;
    public HingeDirection CurrentYHingeDirection => yHingeDirection;
    public SwivelMode Mode => mode;

    public bool IsDoorOpen => isDoorOpen;

    public float MaxInclineZ => maxInclineZ;

    public float MaxYAngle => maxYAngle;

    private void Awake()
    {
      _rigidbody = GetComponent<Rigidbody>();
      _rigidbody.isKinematic = true;
      if (!planeTransform)
      {
        Debug.LogError($"{nameof(SwivelComponent)} missing Plane Transform!");
      }
    }

    private void FixedUpdate()
    {
      UpdateTargetRotation();
      ApplyRotation();
    }

    private void UpdateTargetRotation()
    {
      switch (mode)
      {
        case SwivelMode.None:
          break;
        case SwivelMode.Target:
          // todo add a swivel target type "enemy", "friendly", "direction", "wind", "enemies"
          // _targetRotation = CalculateTargetWindDirectionRotation();
          _targetRotation = CalculateTargetNearestEnemyRotation();
          break;
        // case SwivelMode.TargetNearestEnemy:
        //   break;
        // case SwivelMode.TargetLargestClusterOfEnemies:
        //   _targetRotation = CalculateTargetLargestClusterOfEnemies();
        //   break;
        case SwivelMode.DoorMode:
          _targetRotation = CalculateDoorModeRotation();
          break;
      }
    }

    private static Quaternion NormalizeQuaternion(Quaternion q)
    {
      var mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
      return mag > Mathf.Epsilon ? new Quaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag) : Quaternion.identity;
    }

    private void ApplyRotation()
    {
      if (_rigidbody == null || planeTransform == null || mode == SwivelMode.None)
        return;

      var currentRotation = planeTransform.rotation;
      var newRotation = Quaternion.Slerp(currentRotation, _targetRotation, turningLerpSpeed * Time.fixedDeltaTime);
      _rigidbody.MoveRotation(NormalizeQuaternion(newRotation));
    }

    public void SetMode(SwivelMode newMode)
    {
      mode = newMode;
    }
    public void SetDoorOpen(bool open)
    {
      isDoorOpen = open;
    }

    public void SetHingeMode(DoorHingeMode mode)
    {
      hingeMode = mode;
    }
    public void SetZHingeDirection(HingeDirection dir)
    {
      zHingeDirection = dir;
    }
    public void SetYHingeDirection(HingeDirection dir)
    {
      yHingeDirection = dir;
    }
    public void SetMaxInclineZ(float v)
    {
      maxInclineZ = v;
    }
    public void SetMaxYAngle(float v)
    {
      maxYAngle = v;
    }

    /// <summary>
    /// Toggle the door open/close state manually (e.g. via UI or command).
    /// </summary>
    public void ToggleDoorState()
    {
      isDoorOpen = !isDoorOpen;
    }

    // --- Functional Override Points Below ---

    protected virtual Quaternion CalculateTargetWindDirectionRotation()
    {
      var windDirection = Vector3.forward;
      var target = Quaternion.LookRotation(windDirection, Vector3.up);
      return ClampYawOnly(target);
    }

    protected virtual Quaternion CalculateTargetNearestEnemyRotation()
    {
      if (nearestEnemyMock == null)
      {
        // No target: Face forward by default
        return planeTransform.rotation;
      }

      var toTarget = nearestEnemyMock.transform.position - planeTransform.position;
      var distance = toTarget.magnitude;

      if (distance < minTrackingRange || distance > maxTrackingRange)
      {
        // Outside tracking range: No rotation change
        return planeTransform.rotation;
      }

      // Calculate direction and target rotation
      var flatDirection = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
      if (flatDirection.sqrMagnitude < 0.001f)
      {
        return planeTransform.rotation;
      }

      var targetRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
      return ClampYawOnly(targetRotation);
    }

    protected virtual Quaternion CalculateTargetLargestClusterOfEnemies()
    {
      var targetDirection = Vector3.forward;
      return ClampYawOnly(Quaternion.LookRotation(targetDirection, Vector3.up));
    }

    protected virtual Quaternion CalculateDoorModeRotation()
    {
      if (planeTransform == null)
        return Quaternion.identity;

      var targetZ = _currentZAngle;
      var targetY = _currentYAngle;

      if (hingeMode == DoorHingeMode.ZOnly || hingeMode == DoorHingeMode.Both)
      {
        var direction = zHingeDirection == HingeDirection.Forward ? 1f : -1f;
        targetZ = isDoorOpen ? 0f : direction * maxInclineZ;
        _currentZAngle = Mathf.MoveTowards(_currentZAngle, targetZ, doorLerpSpeed * Time.fixedDeltaTime);
      }

      if (hingeMode == DoorHingeMode.YOnly || hingeMode == DoorHingeMode.Both)
      {
        var direction = yHingeDirection == HingeDirection.Forward ? 1f : -1f;
        targetY = isDoorOpen ? 0f : direction * maxYAngle;
        _currentYAngle = Mathf.MoveTowards(_currentYAngle, targetY, doorLerpSpeed * Time.fixedDeltaTime);
      }

      return Quaternion.Euler(0f, _currentYAngle, _currentZAngle);
    }

    private Quaternion ClampYawOnly(Quaternion rotation)
    {
      var euler = rotation.eulerAngles;
      euler.z = 0f; // Lock Z
      euler.x = 0f; // Lock X
      return Quaternion.Euler(euler);
    }
  }
}