#region

  using System.ComponentModel;
  using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
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

    public const string SNAPPOINT_TAG = "snappoint";

    [Header("Swivel Settings")]
    [SerializeField] private SwivelMode mode = SwivelMode.DoorMode;
    [SerializeField]
    private Transform animatedTransform;

    [SerializeField] private float maxTurnAnglePerSecond = 90f;
    [SerializeField] private float maxTurnAngle = 90f; // turn angle maximum from the original forward rotation position of the swivel.
    [SerializeField] private float maxInclineZ = 90f;
    [SerializeField] public float turningLerpSpeed = 50f;

    [Header("Enemy Tracking Settings")]
    [SerializeField] private float minTrackingRange = 5f;
    [SerializeField] private float maxTrackingRange = 50f;
    [SerializeField] internal GameObject? nearestTarget;

    [Header("Door Mode Settings")]
    [SerializeField] private bool isDoorOpen;
    [SerializeField] public float doorLerpSpeed = 100f;
    [SerializeField] private DoorHingeMode hingeMode = DoorHingeMode.YOnly;
    [SerializeField] private HingeDirection zHingeDirection = HingeDirection.Forward;
    [SerializeField] private HingeDirection yHingeDirection = HingeDirection.Forward;

    [SerializeField] private float maxYAngle = 90f; // optional Y rotation max (like a normal swing door)

    [Description("Piece component meant for containing all pieces that will be swivelled.")]
    public Transform pieceContainer;

    [Description("_connectorContainer is meant to be shown until an object is connected to the swivel component. It will also display a forward direction indicator.")]
    public Transform connectorContainer;

    private float _currentYAngle;
    private float _currentZAngle;

    [Header("Other Settings")]

    public const string AnimatedContainerName = "animated";

    private Transform _snappoint;
    private Quaternion _targetRotation;

    public DoorHingeMode CurrentHingeMode => hingeMode;
    public HingeDirection CurrentZHingeDirection => zHingeDirection;
    public HingeDirection CurrentYHingeDirection => yHingeDirection;
    public SwivelMode Mode => mode;

    public bool IsDoorOpen => isDoorOpen;

    public float MaxInclineZ => maxInclineZ;

    public float MaxYAngle => maxYAngle;
    
    public Quaternion m_startPieceRotation = Quaternion.identity;

    // protects against running an update before objects have been parented properly.
    public bool CanUpdate;
    
    public virtual void Awake()
    {
      m_startPieceRotation = transform.localRotation;
      FindSnappoint();
      animatedTransform = transform.Find("animated");
      pieceContainer = animatedTransform.Find("piece_container");
      // connectorContainer = animatedTransform.Find("connector_container");
      // temp top level. To test patch issues.
      connectorContainer = transform.Find("connector_container");
      
      if (!pieceContainer || !connectorContainer)
      {
        Debug.LogError($"{nameof(SwivelComponent)} missing pieceContainer or connectorContainer!");
      }
    }

    public virtual void FixedUpdate()
    {
      if (!CanUpdate) return;
      UpdateTargetRotation();
      ApplyRotation();
      SyncSnappoint();
    }

    /// <summary>
    /// Sync snappoint to the moving section. This will allow for building on these moving pieces without having to reload the vehicle zero the position again.
    ///
    /// use connector container as this will not be adding pieces which could mutate a position.
    /// </summary>
    public void SyncSnappoint()
    {
      if (_snappoint == null || animatedTransform == null) return;
      _snappoint.transform.position = connectorContainer.position;
    }

    public void FindSnappoint()
    {
      for (var i = 0; i < transform.childCount; i++)
      {
        var child = transform.GetChild(i);
        if (child.CompareTag(SNAPPOINT_TAG))
        {
          _snappoint = transform.GetChild(i);
          break;
        }
      }
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
      if (pieceContainer == null || mode == SwivelMode.None)
        return;
      
      animatedTransform.localRotation = Quaternion.Slerp(
        NormalizeQuaternion(animatedTransform.localRotation),
        _targetRotation,
        turningLerpSpeed * Time.fixedDeltaTime
      );
    }

    public void ResetRotation()
    {
      if (pieceContainer.localRotation != m_startPieceRotation)
      {
        pieceContainer.localRotation = m_startPieceRotation;
      }
    }

    public void SetMode(SwivelMode newMode)
    {
      ResetRotation();
      mode = newMode;
    }
    public void SetDoorOpen(bool open)
    {
      isDoorOpen = open;
    }

    public void SetHingeMode(DoorHingeMode mode)
    {
      ResetRotation();
      hingeMode = mode;
    }
    public void SetZHingeDirection(HingeDirection dir)
    {
      ResetRotation();
      zHingeDirection = dir;
    }
    public void SetYHingeDirection(HingeDirection dir)
    {
      ResetRotation();
      yHingeDirection = dir;
    }
    public void SetMaxInclineZ(float v)
    {
      ResetRotation();
      maxInclineZ = v;
    }
    public void SetMaxYAngle(float v)
    {
      ResetRotation();
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
      if (nearestTarget == null)
      {
        // No target: Face forward by default
        return transform.localRotation;
      }

      var toTarget = nearestTarget.transform.position - pieceContainer.position;
      var distance = toTarget.magnitude;

      if (distance < minTrackingRange || distance > maxTrackingRange)
      {
        // Outside tracking range: No rotation change
        return transform.localRotation;
      }

      // Calculate direction and target rotation
      var flatDirection = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
      if (flatDirection.sqrMagnitude < 0.001f)
      {
        return transform.localRotation;
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
      if (pieceContainer == null)
        return m_startPieceRotation;

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