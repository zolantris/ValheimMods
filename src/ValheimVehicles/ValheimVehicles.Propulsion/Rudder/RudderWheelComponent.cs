#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Propulsion.Rudder;

public class RudderWheelComponent : MonoBehaviour, Interactable, Hoverable
{
  public IRudderControls? Controls => _controls;
  private VehicleMovementController _controls;

  public IVehicleShip ShipInstance;

  public Transform? wheelTransform;

  public List<Transform> m_spokes = new();

  public Vector3 m_leftHandPosition = new(0f, 0f, 2f);

  public Vector3 m_rightHandPosition = new(0f, 0f, -2f);

  public float m_holdWheelTime = 0.7f;

  public float m_wheelRotationFactor = 4f;

  public float m_handIKSpeed = 0.2f;

  private float m_movingLeftAlpha;

  private float m_movingRightAlpha;

  private Transform m_currentLeftHand;

  private Transform m_currentRightHand;

  private Transform m_targetLeftHand;

  private Transform m_targetRightHand;

  private Transform AttachPoint;

  /**
   * For Workaround with older ships
   */
  private class CompatVehicleShip(Ship ship) : IVehicleShip
  {
    public bool IsPlayerInBoat(ZDOID zdoId)
    {
      return ship.IsPlayerInBoat(zdoId);
    }

    public bool IsPlayerInBoat(Player zdoId)
    {
      return ship.IsPlayerInBoat(zdoId);
    }

    public bool IsPlayerInBoat(long playerID)
    {
      return ship.IsPlayerInBoat(playerID);
    }

    public GameObject RudderObject
    {
      get => ship.m_rudderObject;
      set { }
    }

    public IWaterVehicleController VehicleController { get; }

    public BoxCollider FloatCollider
    {
      get => ship.m_floatCollider;
      set { }
    }

    public Transform? ShipForwardRotation { get; }

    public Transform ControlGuiPosition
    {
      get => ship.m_controlGuiPos;
      set { }
    }

    public VehicleShip? Instance
    {
      get => (VehicleShip)(ship as object);
    }
  }

  public string GetHoverText()
  {
    return ShipInstance.Instance
      ? ShipInstance.Instance.MovementController.GetHoverText()
      : "Invalid, no ship detected";
  }

  private void Awake()
  {
    AttachPoint = transform.Find("attachpoint");
  }

  public string GetHoverName()
  {
    return Localization.instance.Localize("$mb_rudder");
  }


  /// <summary>
  /// pass-through for the single instance of VehicleMovementController on a vehicle
  /// </summary>
  /// Updates attachpoint of parent when Interacted with
  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (_controls != null)
    {
      _controls.AttachPoint = AttachPoint;

      if (ShipInstance?.Instance != null)
      {
        ShipInstance.Instance.ControlGuiPosition = transform;
      }
    }

    return _controls.Interact(user, hold, alt);
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  /**
   * @Deprecated for Older MoveableShip compatibility. Likely will just remove the ship controller from the MBRaft.
   */
  public void InitializeControls(ZNetView netView, Ship vehicleShip)
  {
    if (vehicleShip == null)
    {
      Logger.LogError("Initialized called with null vehicleShip");
      return;
    }

    ShipInstance = new CompatVehicleShip(vehicleShip);

    if (!(bool)_controls)
    {
      // _controls = gameObject.AddComponent<VehicleMovementController>();
      _controls = vehicleShip.GetComponent<VehicleMovementController>();
    }

    if (!(bool)_controls)
    {
      _controls =
        vehicleShip.gameObject.AddComponent<VehicleMovementController>();
    }

    if (!wheelTransform) wheelTransform = netView.transform.Find("controls/wheel");


    if (_controls != null)
    {
      _controls.DEPRECATED_InitializeRudderWithShip(ShipInstance,
        this, vehicleShip);
      _controls.enabled = true;
    }

    Logger.LogDebug("added rudder to BaseVehicle");
  }

  public void InitializeControls(ZNetView netView, IVehicleShip? vehicleShip)
  {
    if (vehicleShip == null)
    {
      Logger.LogError("Initialized called with null vehicleShip");
      return;
    }

    ShipInstance = vehicleShip;

    if (!(bool)_controls)
    {
      // _controls = gameObject.AddComponent<VehicleMovementController>();
      _controls = vehicleShip.Instance.m_shipControlls;
    }

    if (!(bool)_controls)
    {
      _controls =
        vehicleShip.Instance.gameObject.AddComponent<VehicleMovementController>();
      // two way binding is required for this to work.
      vehicleShip.Instance.m_shipControlls = _controls;
    }

    if (!wheelTransform) wheelTransform = netView.transform.Find("controls/wheel");

    if (Controls != null)
    {
      _controls.InitializeRudderWithShip(vehicleShip,
        this);
      ShipInstance = vehicleShip;
      _controls.enabled = true;
    }

    Logger.LogDebug("added rudder to BaseVehicle");
  }

  public void UpdateSpokes()
  {
    m_spokes.Clear();
    m_spokes.AddRange(from k in wheelTransform.GetComponentsInChildren<Transform>()
      where k.gameObject.name.StartsWith("grabpoint")
      select k);
  }

  public void UpdateIK(Animator animator)
  {
    if (!wheelTransform)
    {
      return;
    }

    if (!m_currentLeftHand)
    {
      m_currentLeftHand = GetNearestSpoke(transform.TransformPoint(m_leftHandPosition));
    }

    if (!m_currentRightHand)
    {
      m_currentRightHand = GetNearestSpoke(transform.TransformPoint(m_rightHandPosition));
    }

    if (!m_targetLeftHand && !m_targetRightHand)
    {
      Vector3 left = transform.InverseTransformPoint(m_currentLeftHand.position);
      Vector3 right = transform.InverseTransformPoint(m_currentRightHand.position);
      if (left.z < 0.2f)
      {
        Vector3 offsetY2 = new Vector3(0f, (left.y > 0.5f) ? (-2f) : 2f, 0f);
        m_targetLeftHand =
          GetNearestSpoke(transform.TransformPoint(m_leftHandPosition + offsetY2));
        m_movingLeftAlpha = Time.time;
      }
      else if (right.z > -0.2f)
      {
        Vector3 offsetY = new Vector3(0f, (right.y > 0.5f) ? (-2f) : 2f, 0f);
        m_targetRightHand =
          GetNearestSpoke(transform.TransformPoint(m_rightHandPosition + offsetY));
        m_movingRightAlpha = Time.time;
      }
    }

    float leftHandAlpha = Mathf.Clamp01((Time.time - m_movingLeftAlpha) / m_handIKSpeed);
    float rightHandAlpha = Mathf.Clamp01((Time.time - m_movingRightAlpha) / m_handIKSpeed);
    float leftHandIKWeight = Mathf.Sin(leftHandAlpha * (float)Math.PI) * (1f - m_holdWheelTime) +
                             m_holdWheelTime;
    float rightHandIKWeight = Mathf.Sin(rightHandAlpha * (float)Math.PI) * (1f - m_holdWheelTime) +
                              m_holdWheelTime;
    if ((bool)m_targetLeftHand && leftHandAlpha > 0.99f)
    {
      m_currentLeftHand = m_targetLeftHand;
      m_targetLeftHand = null;
    }

    if ((bool)m_targetRightHand && rightHandAlpha > 0.99f)
    {
      m_currentRightHand = m_targetRightHand;
      m_targetRightHand = null;
    }

    Vector3 leftHandPos = (m_targetLeftHand
      ? Vector3.Lerp(m_currentLeftHand.transform.position, m_targetLeftHand.transform.position,
        leftHandAlpha)
      : m_currentLeftHand.transform.position);
    Vector3 rightHandPos = (m_targetRightHand
      ? Vector3.Lerp(m_currentRightHand.transform.position, m_targetRightHand.transform.position,
        rightHandAlpha)
      : m_currentRightHand.transform.position);
    Vector3 rightHandRot = (m_targetLeftHand
      ? Vector3.Slerp(m_currentLeftHand.transform.rotation.eulerAngles,
        m_targetLeftHand.transform.rotation.eulerAngles, leftHandAlpha)
      : m_currentLeftHand.transform.rotation.eulerAngles);
    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandIKWeight);
    animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPos);
    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
    animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPos);
  }

  public Transform GetNearestSpoke(Vector3 position)
  {
    Transform? best = null;
    var bestDistance = 0f;
    foreach (var spoke in m_spokes)
    {
      var dist = (spoke.transform.position - position).sqrMagnitude;
      if (best != null && !(dist < bestDistance)) continue;
      best = spoke;
      bestDistance = dist;
    }

    return best;
  }
}