// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
    /// <summary>
    /// A base controller for rotating platforms like elevators or doors inside vehicles.
    /// This class assumes:
    /// - It is parented to a Rigidbody-driven root (e.g. a vehicle).
    /// - It has a kinematic Rigidbody and at least one collider.
    /// - Rotation is applied via localRotation only.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SwivelComponent))]
    public class SwivelPlatformController : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 90f; // degrees per second
        [SerializeField] private Vector3 maxRotationEuler = new Vector3(0f, 90f, 0f); // Target local rotation when active
        [SerializeField] private bool isActive;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private Rigidbody _rigidbody;
        private SwivelComponent _swivelComponent;
        private Quaternion _targetRotation;

        /// <summary>Whether the platform is currently active (open) or closed.</summary>
        public bool IsActive => isActive;

        public virtual void Awake()
        {
            // Configure Rigidbody for kinematic motion
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            // Cache closed and open rotations
            _closedRotation = transform.localRotation;
            _openRotation = Quaternion.Euler(maxRotationEuler);

            // Register with SwivelComponent
            _swivelComponent = GetComponent<SwivelComponent>();
            if (_swivelComponent == null)
            {
                Debug.LogWarning("SwivelPlatformController requires a SwivelComponent on the same GameObject.");
            }
            else
            {
                _swivelComponent.RegisterSwivelPlatformController(this);
            }
        }

#if UNITY_EDITOR
        public virtual void Update()
        {
            // Ensure collider transforms are synchronized for physics
            Physics.SyncTransforms();
        }
#endif

        public virtual void FixedUpdate()
        {
            // Only update if the swivel component allows
            if (_swivelComponent != null && !_swivelComponent.CanUpdate)
            {
                return;
            }

            // Determine target rotation based on active state
            _targetRotation = isActive ? _openRotation : _closedRotation;

            // Smoothly rotate towards target
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                _targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }

        /// <summary>Set whether the platform is active/open.</summary>
        public virtual void SetActive(bool value)
        {
            isActive = value;
        }

        /// <summary>Toggle active/open state.</summary>
        public virtual void ToggleActive()
        {
            SetActive(!isActive);
        }

        /// <summary>Used by SwivelComponent to directly set open/closed.</summary>
        public void SetFromSwivel(bool open)
        {
            SetActive(open);
        }
    }

    /// <summary>
    /// Extension to SwivelComponent to integrate platform controller.
    /// </summary>
    public partial class SwivelComponent : MonoBehaviour
    {
        /// <summary>Called by SwivelPlatformController to register itself.</summary>
        public void RegisterSwivelPlatformController(SwivelPlatformController controller)
        {
            _platformController = controller;
        }

        /// <summary>Sets the platform active/open state.</summary>
        public void SetPlatformActive(bool active)
        {
            if (_platformController != null)
            {
                _platformController.SetFromSwivel(active);
            }
        }

        /// <summary>Toggles the platform state.</summary>
        public void TogglePlatform()
        {
            if (_platformController != null)
            {
                _platformController.SetFromSwivel(!_platformController.IsActive);
            }
        }
    }
}
