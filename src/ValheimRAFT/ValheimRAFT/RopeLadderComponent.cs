using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;

namespace ValheimRAFT;

public class RopeLadderComponent : MonoBehaviour, Interactable, Hoverable
{
  public GameObject m_stepObject;

  public LineRenderer m_ropeLine;

  public BoxCollider m_collider;

  public Transform m_attachPoint;

  public MoveableBaseRootComponent m_mbroot;
  public VehiclePiecesController vehiclePiecesController;

  public float m_stepDistance = 0.5f;

  public float m_ladderHeight = 1f;

  public float baseLadderMoveSpeed = 2f;
  public float ladderRunSpeedMult => PrefabConfig.RopeLadderRunMultiplier.Value;


  public int m_stepOffsetUp = 1;

  public int m_stepOffsetDown = 0;

  private List<GameObject> m_steps = [];

  private bool m_ghostObject;

  private LineRenderer m_ghostAttachPoint;

  private float m_lastHitWaterDistance;

  private static readonly int INVALID_STEP = int.MaxValue;

  private static int rayMask = 0;

  internal float m_currentMoveDir;

  internal int m_currentLeft;

  internal int m_currentRight;

  internal float m_leftMoveTime;

  internal float m_rightMoveTime;

  internal int m_targetLeft;

  internal int m_targetRight;

  internal bool m_lastMovedLeft;

  public bool isRunning = false;
  public bool hasAutoClimb = false;

  private MoveDirection _autoClimbDir = MoveDirection.None;

  public string GetHoverName()
  {
    return "";
  }

  public static string WithYellowBold(string val) => $"[<color=yellow><b>{val}</b></color>]";

  public string GetHoverText()
  {
    var localizationString =
      $"{WithYellowBold("$KEY_Use")} $mb_rope_ladder_use";

    List<string> modifiers = [isRunning ? "$valheim_vehicles_fast" : "$valheim_vehicles_slow"];
    if (hasAutoClimb)
    {
      modifiers.Add("$valheim_vehicles_auto");
    }

    var modifiersString = WithYellowBold(string.Join(", ", modifiers.ToArray()));
    localizationString += $" {modifiersString}";

    if (PrefabConfig.RopeLadderHints.Value)
    {
      localizationString +=
        $"\n{WithYellowBold("$KEY_AutoRun")} $mb_rope_ladder_use $valheim_vehicles_auto";
      localizationString +=
        $"\n{WithYellowBold("$KEY_Run")} $mb_rope_ladder_use $valheim_vehicles_fast";
    }

    return Localization.instance.Localize(
      localizationString);
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    ClimbLadder(Player.m_localPlayer);
    return true;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  private void Awake()
  {
    m_stepObject = transform.Find("step").gameObject;
    m_ropeLine = GetComponent<LineRenderer>();
    m_collider = GetComponentInChildren<BoxCollider>();
    m_ghostObject = ZNetView.m_forceDisableInit;
    m_attachPoint = transform.Find("attachpoint");
    InvokeRepeating(nameof(UpdateSteps), 0.1f, m_ghostObject ? 0.1f : 5f);
  }

  private void ClimbLadder(Player player)
  {
    if (!(bool)player) return;
    if (player.IsAttached())
    {
      player.AttachStop();
      return;
    }

    m_currentLeft = INVALID_STEP;
    m_currentRight = INVALID_STEP;
    m_targetLeft = INVALID_STEP;
    m_targetRight = INVALID_STEP;
    m_attachPoint.localPosition = new Vector3(m_attachPoint.localPosition.x,
      ClampOffset(m_attachPoint.parent.InverseTransformPoint(player.transform.position).y),
      m_attachPoint.localPosition.z);
    player.AttachStart(m_attachPoint, null, hideWeapons: true, isBed: false, onShip: false,
      "Movement", Vector3.zero);
  }

  private bool IsFlyingAndNotAnchored(Vector3 hitPoint)
  {
    var targetHeight = vehiclePiecesController?.VehicleInstance?.Instance?.TargetHeight;
    if (targetHeight != null && vehiclePiecesController?.VehicleInstance?.Instance?.TargetHeight >
        0f &&
        !(vehiclePiecesController?.MovementController?.isAnchored ?? false) &&
        hitPoint.y < vehiclePiecesController?.GetColliderBottom())
    {
      return true;
    }

    if ((bool)m_mbroot && (bool)m_mbroot.shipController &&
        m_mbroot.shipController.m_targetHeight > 0f &&
        !m_mbroot.shipController.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags
          .IsAnchored) &&
        hitPoint.y < m_mbroot.GetColliderBottom())
    {
      return true;
    }

    return false;
  }

  private void UpdateSteps()
  {
    if (!m_stepObject)
    {
      return;
    }

    if (rayMask == 0)
    {
      rayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
    }

    m_ladderHeight = 200f;
    var hitpoint = new Vector3(m_attachPoint.transform.position.x, 0f,
      m_attachPoint.transform.position.z);
    var raystart = new Vector3(m_attachPoint.transform.position.x, base.transform.position.y,
      m_attachPoint.transform.position.z);
    var hits = Physics.RaycastAll(new Ray(raystart, -m_attachPoint.transform.up),
      m_ladderHeight, rayMask);
    for (int i = 0; i < hits.Length; i++)
    {
      var hit = hits[i];
      if (!(hit.collider == m_collider) && !hit.collider.GetComponentInParent<Character>() &&
          hit.distance < m_ladderHeight)
      {
        m_ladderHeight = hit.distance;
        hitpoint = hit.point;
      }
    }

    if (IsFlyingAndNotAnchored(hitpoint))
    {
      if (vehiclePiecesController)
      {
        hitpoint.y = vehiclePiecesController.GetColliderBottom();
      }
      else if (m_mbroot)
      {
        hitpoint.y = m_mbroot.GetColliderBottom();
      }

      m_ladderHeight = (hitpoint - raystart).magnitude;
      m_lastHitWaterDistance = 0f;
    }
    else if (hitpoint.y < ZoneSystem.instance.m_waterLevel)
    {
      hitpoint.y = ZoneSystem.instance.m_waterLevel;
      var waterdist = (hitpoint - raystart).magnitude + 2f;
      if (waterdist < m_ladderHeight)
      {
        if (m_lastHitWaterDistance != 0f)
        {
          waterdist = m_lastHitWaterDistance;
        }

        m_ladderHeight = waterdist;
        m_lastHitWaterDistance = waterdist;
      }
    }

    if (m_ghostObject)
    {
      if (!m_ghostAttachPoint)
      {
        var go2 = new GameObject();
        go2.transform.SetParent(m_attachPoint);
        m_ghostAttachPoint = go2.AddComponent<LineRenderer>();
        var material = new Material(LoadValheimAssets.CustomPieceShader)
        {
          color = Color.green
        };
        m_ghostAttachPoint.material = material;
        m_ghostAttachPoint.widthMultiplier = 0.1f;
      }

      m_ghostAttachPoint.SetPosition(0, m_attachPoint.transform.position);
      m_ghostAttachPoint.SetPosition(1,
        m_attachPoint.transform.position + -m_attachPoint.transform.up * m_ladderHeight);
    }

    var steps = Mathf.RoundToInt(m_ladderHeight / m_stepDistance);
    if (m_steps.Count != steps)
    {
      var wnt = GetComponent<WearNTear>();
      wnt.ResetHighlight();
      while (m_steps.Count > steps)
      {
        Destroy(m_steps[m_steps.Count - 1]);
        m_steps.RemoveAt(m_steps.Count - 1);
      }

      while (m_steps.Count < steps)
      {
        var go = Instantiate(m_stepObject, transform);
        m_steps.Add(go);
        go.transform.localPosition =
          new Vector3(0f, (0f - m_stepDistance) * (float)m_steps.Count, 0f);
      }

      m_ropeLine.useWorldSpace = false;
      m_ropeLine.SetPosition(0, new Vector3(0.4f, 0f, 0f));
      m_ropeLine.SetPosition(1,
        new Vector3(0.4f, (0f - m_stepDistance) * (float)m_steps.Count, 0f));
      m_ropeLine.SetPosition(2,
        new Vector3(-0.4f, (0f - m_stepDistance) * (float)m_steps.Count, 0f));
      m_ropeLine.SetPosition(3, new Vector3(-0.4f, 0f, 0f));
      if (!m_ghostObject)
      {
        m_collider.size = new Vector3(1f, m_ladderHeight, 0.1f);
        m_collider.transform.localPosition = new Vector3(0f, (0f - m_ladderHeight) / 2f, 0f);
      }
    }
  }

  public void UpdateIK(Animator animator)
  {
    var center = Mathf.RoundToInt(m_attachPoint.localPosition.y / m_stepDistance);
    if (m_currentRight == INVALID_STEP)
    {
      m_currentRight = center;
    }

    if (m_currentLeft == INVALID_STEP)
    {
      m_currentLeft = center;
    }

    var currentMoveDir = hasAutoClimb ? _autoClimbDir : GetMovementDir(m_currentMoveDir);

    if (m_targetLeft == INVALID_STEP && m_targetRight == INVALID_STEP &&
        currentMoveDir != MoveDirection.None)
    {
      if ((currentMoveDir == MoveDirection.Up && m_currentLeft < m_currentRight) ||
          (currentMoveDir == MoveDirection.Down && m_currentLeft > m_currentRight) ||
          !m_lastMovedLeft)
      {
        m_targetLeft = center +
                       ((currentMoveDir == MoveDirection.Up) ? m_stepOffsetUp : m_stepOffsetDown);
        m_leftMoveTime = Time.time;
        m_lastMovedLeft = true;
      }
      else
      {
        m_targetRight = center +
                        ((currentMoveDir == MoveDirection.Up) ? m_stepOffsetUp : m_stepOffsetDown);
        m_rightMoveTime = Time.time;
        m_lastMovedLeft = false;
      }
    }

    var leftHand =
      base.transform.TransformPoint(new Vector3(-0.3f, (float)(m_currentLeft + 2) * m_stepDistance,
        -0.1f));
    var leftFoot =
      base.transform.TransformPoint(
        new Vector3(-0.2f, (float)m_currentLeft * m_stepDistance, -0.3f));
    var rightHand =
      base.transform.TransformPoint(new Vector3(0.3f, (float)(m_currentRight + 2) * m_stepDistance,
        -0.1f));
    var rightFoot =
      base.transform.TransformPoint(
        new Vector3(0.2f, (float)m_currentRight * m_stepDistance, -0.3f));
    if (m_targetLeft != INVALID_STEP)
    {
      var targetLeftHand =
        base.transform.TransformPoint(new Vector3(-0.3f, (float)(m_targetLeft + 3) * m_stepDistance,
          0f));
      var targetLeftFoot =
        base.transform.TransformPoint(new Vector3(-0.2f, (float)m_targetLeft * m_stepDistance, 0f));
      var leftAlpha =
        Mathf.Clamp01((Time.time - m_leftMoveTime) * (baseLadderMoveSpeed / m_stepDistance));
      leftHand = Vector3.Lerp(leftHand, targetLeftHand, leftAlpha);
      leftFoot = Vector3.Lerp(leftFoot, targetLeftFoot, leftAlpha);
      if (Mathf.Approximately(leftAlpha, 1f))
      {
        m_currentLeft = m_targetLeft;
        m_targetLeft = INVALID_STEP;
      }
    }
    else if (m_targetRight != INVALID_STEP)
    {
      var targetRightHand =
        base.transform.TransformPoint(new Vector3(0.3f, (float)(m_targetRight + 3) * m_stepDistance,
          0f));
      var targetRightFoot =
        base.transform.TransformPoint(new Vector3(0.2f, (float)m_targetRight * m_stepDistance, 0f));
      var rightAlpha =
        Mathf.Clamp01((Time.time - m_rightMoveTime) * (baseLadderMoveSpeed / m_stepDistance));
      rightHand = Vector3.Lerp(rightHand, targetRightHand, rightAlpha);
      rightFoot = Vector3.Lerp(rightFoot, targetRightFoot, rightAlpha);
      if (Mathf.Approximately(rightAlpha, 1f))
      {
        m_currentRight = m_targetRight;
        m_targetRight = INVALID_STEP;
      }
    }

    animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHand);
    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
    animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFoot);
    animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
    animator.SetIKPosition(AvatarIKGoal.RightHand, rightHand);
    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
    animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFoot);
    animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
  }

  private float previousDir = 0;

  private float UpdateMoveOffset(MoveDirection moveDir, float offset)
  {
    var ladderMoveSpeed =
      isRunning ? baseLadderMoveSpeed * ladderRunSpeedMult : baseLadderMoveSpeed;
    switch (moveDir)
    {
      case MoveDirection.Up:
        offset += ladderMoveSpeed * Time.deltaTime;
        break;
      case MoveDirection.Down:
        offset -= ladderMoveSpeed * Time.deltaTime;
        break;
    }

    return offset;
  }

  public MoveDirection GetMovementDir(float val)
  {
    return val switch
    {
      > 0f => MoveDirection.Up,
      < 0f => MoveDirection.Down,
      _ => MoveDirection.None
    };
  }

  /// <summary>
  /// VIP for making ladders easier to use
  /// </summary>
  public void DetectInputKeys(float moveDir)
  {
    var isPressingRun = ZInput.GetButtonUp("Run") || ZInput.GetButton("JoyRun");
    var isAutoRunPressed = ZInput.GetButtonUp("AutoRun");

    if (isAutoRunPressed)
    {
      hasAutoClimb = !hasAutoClimb;
      _autoClimbDir = hasAutoClimb
        ? GetMovementDir(moveDir)
        : MoveDirection.None;
    }

    if (isPressingRun)
    {
      isRunning = !isRunning;
    }
  }

  public void MoveOnLadder(Player player, float moveDir)
  {
    DetectInputKeys(moveDir);

    var offset = m_attachPoint.localPosition.y;

    var dir = GetMovementDir(moveDir);

    if (hasAutoClimb && dir != MoveDirection.None && dir != _autoClimbDir)
    {
      _autoClimbDir = _autoClimbDir == MoveDirection.None ? dir : MoveDirection.None;
    }

    offset = UpdateMoveOffset(hasAutoClimb ? _autoClimbDir : dir, offset);

    m_attachPoint.localPosition = new Vector3(m_attachPoint.localPosition.x, ClampOffset(offset),
      m_attachPoint.localPosition.z);
    m_currentMoveDir = moveDir;
  }

  private float ClampOffset(float offset)
  {
    return Mathf.Clamp(offset, 0f - m_collider.size.y, 0.5f);
  }

  public void StepOffLadder(Player player)
  {
    player.m_attachPoint = null;
  }
}