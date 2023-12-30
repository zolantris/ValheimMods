using System.Collections.Generic;
using UnityEngine;

namespace ValheimRAFT;

public class RopeLadderComponent : MonoBehaviour, Interactable, Hoverable
{
  public GameObject m_stepObject;

  public LineRenderer m_ropeLine;

  public BoxCollider m_collider;

  public Transform m_attachPoint;

  public MoveableBaseRootComponent m_mbroot;

  public float m_stepDistance = 0.5f;

  public float m_ladderHeight = 1f;

  public float m_ladderMoveSpeed = 2f;

  public int m_stepOffsetUp = 1;

  public int m_stepOffsetDown = 0;

  private List<GameObject> m_steps = new List<GameObject>();

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

  public string GetHoverName()
  {
    return "";
  }

  public string GetHoverText()
  {
    return Localization.instance.Localize(
      "[<color=yellow><b>$KEY_Use</b></color>] $mb_rope_ladder_use");
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
    m_stepObject = base.transform.Find("step").gameObject;
    m_ropeLine = GetComponent<LineRenderer>();
    m_collider = GetComponentInChildren<BoxCollider>();
    m_ghostObject = ZNetView.m_forceDisableInit;
    m_attachPoint = base.transform.Find("attachpoint");
    InvokeRepeating("UpdateSteps", 0.1f, m_ghostObject ? 0.1f : 5f);
  }

  private void ClimbLadder(Player player)
  {
    if ((bool)player)
    {
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
    Vector3 hitpoint = new Vector3(m_attachPoint.transform.position.x, 0f,
      m_attachPoint.transform.position.z);
    Vector3 raystart = new Vector3(m_attachPoint.transform.position.x, base.transform.position.y,
      m_attachPoint.transform.position.z);
    RaycastHit[] hits = Physics.RaycastAll(new Ray(raystart, -m_attachPoint.transform.up),
      m_ladderHeight, rayMask);
    for (int i = 0; i < hits.Length; i++)
    {
      RaycastHit hit = hits[i];
      if (!(hit.collider == m_collider) && !hit.collider.GetComponentInParent<Character>() &&
          hit.distance < m_ladderHeight)
      {
        m_ladderHeight = hit.distance;
        hitpoint = hit.point;
      }
    }

    if ((bool)m_mbroot && (bool)m_mbroot.MMoveableBaseShip &&
        m_mbroot.MMoveableBaseShip.m_targetHeight > 0f &&
        !m_mbroot.MMoveableBaseShip.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored) &&
        hitpoint.y < m_mbroot.GetColliderBottom())
    {
      hitpoint.y = m_mbroot.GetColliderBottom();
      m_ladderHeight = (hitpoint - raystart).magnitude;
      m_lastHitWaterDistance = 0f;
    }
    else if (hitpoint.y < ZoneSystem.instance.m_waterLevel)
    {
      hitpoint.y = ZoneSystem.instance.m_waterLevel;
      float waterdist = (hitpoint - raystart).magnitude + 2f;
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
        GameObject go2 = new GameObject();
        go2.transform.SetParent(m_attachPoint);
        m_ghostAttachPoint = go2.AddComponent<LineRenderer>();
        m_ghostAttachPoint.widthMultiplier = 0.1f;
      }

      m_ghostAttachPoint.SetPosition(0, m_attachPoint.transform.position);
      m_ghostAttachPoint.SetPosition(1,
        m_attachPoint.transform.position + -m_attachPoint.transform.up * m_ladderHeight);
    }

    int steps = Mathf.RoundToInt(m_ladderHeight / m_stepDistance);
    if (m_steps.Count != steps)
    {
      WearNTear wnt = GetComponent<WearNTear>();
      wnt.ResetHighlight();
      while (m_steps.Count > steps)
      {
        Object.Destroy(m_steps[m_steps.Count - 1]);
        m_steps.RemoveAt(m_steps.Count - 1);
      }

      while (m_steps.Count < steps)
      {
        GameObject go = Object.Instantiate(m_stepObject, base.transform);
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
    int center = Mathf.RoundToInt(m_attachPoint.localPosition.y / m_stepDistance);
    if (m_currentRight == INVALID_STEP)
    {
      m_currentRight = center;
    }

    if (m_currentLeft == INVALID_STEP)
    {
      m_currentLeft = center;
    }

    if (m_targetLeft == INVALID_STEP && m_targetRight == INVALID_STEP && m_currentMoveDir != 0f)
    {
      if ((m_currentMoveDir > 0f && m_currentLeft < m_currentRight) ||
          (m_currentMoveDir < 0f && m_currentLeft > m_currentRight) || !m_lastMovedLeft)
      {
        m_targetLeft = center + ((m_currentMoveDir > 0f) ? m_stepOffsetUp : m_stepOffsetDown);
        m_leftMoveTime = Time.time;
        m_lastMovedLeft = true;
      }
      else
      {
        m_targetRight = center + ((m_currentMoveDir > 0f) ? m_stepOffsetUp : m_stepOffsetDown);
        m_rightMoveTime = Time.time;
        m_lastMovedLeft = false;
      }
    }

    Vector3 leftHand =
      base.transform.TransformPoint(new Vector3(-0.3f, (float)(m_currentLeft + 2) * m_stepDistance,
        -0.1f));
    Vector3 leftFoot =
      base.transform.TransformPoint(
        new Vector3(-0.2f, (float)m_currentLeft * m_stepDistance, -0.3f));
    Vector3 rightHand =
      base.transform.TransformPoint(new Vector3(0.3f, (float)(m_currentRight + 2) * m_stepDistance,
        -0.1f));
    Vector3 rightFoot =
      base.transform.TransformPoint(
        new Vector3(0.2f, (float)m_currentRight * m_stepDistance, -0.3f));
    if (m_targetLeft != INVALID_STEP)
    {
      Vector3 targetLeftHand =
        base.transform.TransformPoint(new Vector3(-0.3f, (float)(m_targetLeft + 3) * m_stepDistance,
          0f));
      Vector3 targetLeftFoot =
        base.transform.TransformPoint(new Vector3(-0.2f, (float)m_targetLeft * m_stepDistance, 0f));
      float leftAlpha =
        Mathf.Clamp01((Time.time - m_leftMoveTime) * (m_ladderMoveSpeed / m_stepDistance));
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
      Vector3 targetRightHand =
        base.transform.TransformPoint(new Vector3(0.3f, (float)(m_targetRight + 3) * m_stepDistance,
          0f));
      Vector3 targetRightFoot =
        base.transform.TransformPoint(new Vector3(0.2f, (float)m_targetRight * m_stepDistance, 0f));
      float rightAlpha =
        Mathf.Clamp01((Time.time - m_rightMoveTime) * (m_ladderMoveSpeed / m_stepDistance));
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

  public void MoveOnLadder(Player player, float movedir)
  {
    float offset = m_attachPoint.localPosition.y;
    if (movedir > 0f)
    {
      offset += m_ladderMoveSpeed * Time.deltaTime;
    }
    else if (movedir < 0f)
    {
      offset -= m_ladderMoveSpeed * Time.deltaTime;
    }

    m_attachPoint.localPosition = new Vector3(m_attachPoint.localPosition.x, ClampOffset(offset),
      m_attachPoint.localPosition.z);
    m_currentMoveDir = movedir;
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