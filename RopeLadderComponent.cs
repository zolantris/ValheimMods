// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.RopeLadderComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimRAFT
{
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

    public string GetHoverName() => "";

    public string GetHoverText() =>
      Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $mb_rope_ladder_use");

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
      this.ClimbLadder(Player.m_localPlayer);
      return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    private void Awake()
    {
      this.m_stepObject = ((Component)((Component)this).transform.Find("step")).gameObject;
      this.m_ropeLine = ((Component)this).GetComponent<LineRenderer>();
      this.m_collider = ((Component)this).GetComponentInChildren<BoxCollider>();
      this.m_ghostObject = ZNetView.m_forceDisableInit;
      this.m_attachPoint = ((Component)this).transform.Find("attachpoint");
      this.InvokeRepeating("UpdateSteps", 0.1f, this.m_ghostObject ? 0.1f : 5f);
    }

    private void ClimbLadder(Player player)
    {
      if (!player)
        return;
      if (((Character)player).IsAttached())
      {
        ((Character)player).AttachStop();
      }
      else
      {
        this.m_currentLeft = RopeLadderComponent.INVALID_STEP;
        this.m_currentRight = RopeLadderComponent.INVALID_STEP;
        this.m_targetLeft = RopeLadderComponent.INVALID_STEP;
        this.m_targetRight = RopeLadderComponent.INVALID_STEP;
        this.m_attachPoint.localPosition = new Vector3(this.m_attachPoint.localPosition.x,
          this.ClampOffset(this.m_attachPoint.parent
            .InverseTransformPoint(((Component)player).transform.position).y),
          this.m_attachPoint.localPosition.z);
        ((Character)player).AttachStart(this.m_attachPoint, (GameObject)null, true, false, false,
          "Movement", Vector3.zero, (Transform)null);
      }
    }

    private void UpdateSteps()
    {
      if (!m_stepObject)
        return;
      if (RopeLadderComponent.rayMask == 0)
        RopeLadderComponent.rayMask = LayerMask.GetMask(new string[5]
        {
          "Default",
          "static_solid",
          "Default_small",
          "piece",
          "terrain"
        });
      this.m_ladderHeight = 200f;
      Vector3 point = new Vector3(((Component)this.m_attachPoint).transform.position.x, 0.0f,
        ((Component)this.m_attachPoint).transform.position.z);
      Vector3 vector3_1 = new Vector3(this.m_attachPoint.transform.position.x,
        ((Component)this).transform.position.y,
        ((Component)this.m_attachPoint).transform.position.z);
      foreach (RaycastHit raycastHit in Physics.RaycastAll(
                 new Ray(vector3_1,
                   -m_attachPoint.transform.up),
                 this.m_ladderHeight, RopeLadderComponent.rayMask))
      {
        if (!raycastHit.collider == m_collider &&
            raycastHit.collider.GetComponentInParent<Character>() &&
            raycastHit.distance < m_ladderHeight)
        {
          this.m_ladderHeight = raycastHit.distance;
          point = raycastHit.point;
        }
      }

      if (m_mbroot &&
          m_mbroot.m_moveableBaseShip &&
          (double)this.m_mbroot.m_moveableBaseShip.m_targetHeight > 0.0 &&
          !this.m_mbroot.m_moveableBaseShip.m_flags.HasFlag((Enum)MoveableBaseShipComponent.MBFlags
            .IsAnchored) && point.y < m_mbroot.GetColliderBottom())
      {
        point.y = this.m_mbroot.GetColliderBottom();
        Vector3 vector3_2 = point - vector3_1;
        this.m_ladderHeight = vector3_2.magnitude;
        this.m_lastHitWaterDistance = 0.0f;
      }
      else if ((double)point.y < (double)ZoneSystem.instance.m_waterLevel)
      {
        point.y = ZoneSystem.instance.m_waterLevel;
        Vector3 vector3_3 = Vector3.op_Subtraction(point, vector3_1);
        float num = vector3_3.magnitude + 2f;
        if ((double)num < (double)this.m_ladderHeight)
        {
          if ((double)this.m_lastHitWaterDistance != 0.0)
            num = this.m_lastHitWaterDistance;
          this.m_ladderHeight = num;
          this.m_lastHitWaterDistance = num;
        }
      }

      if (this.m_ghostObject)
      {
        if (!Object.op_Implicit((Object)this.m_ghostAttachPoint))
        {
          GameObject gameObject = new GameObject();
          gameObject.transform.SetParent(this.m_attachPoint);
          this.m_ghostAttachPoint = gameObject.AddComponent<LineRenderer>();
          this.m_ghostAttachPoint.widthMultiplier = 0.1f;
        }

        this.m_ghostAttachPoint.SetPosition(0, ((Component)this.m_attachPoint).transform.position);
        this.m_ghostAttachPoint.SetPosition(1,
          Vector3.op_Addition(((Component)this.m_attachPoint).transform.position,
            Vector3.op_Multiply(
              Vector3.op_UnaryNegation(((Component)this.m_attachPoint).transform.up),
              this.m_ladderHeight)));
      }

      int num1 = Mathf.RoundToInt(this.m_ladderHeight / this.m_stepDistance);
      if (this.m_steps.Count == num1)
        return;
      ((Component)this).GetComponent<WearNTear>().ResetHighlight();
      while (this.m_steps.Count > num1)
      {
        Object.Destroy((Object)this.m_steps[this.m_steps.Count - 1]);
        this.m_steps.RemoveAt(this.m_steps.Count - 1);
      }

      while (this.m_steps.Count < num1)
      {
        GameObject gameObject =
          Object.Instantiate<GameObject>(this.m_stepObject, ((Component)this).transform);
        this.m_steps.Add(gameObject);
        gameObject.transform.localPosition =
          new Vector3(0.0f, -this.m_stepDistance * (float)this.m_steps.Count, 0.0f);
      }

      this.m_ropeLine.useWorldSpace = false;
      this.m_ropeLine.SetPosition(0, new Vector3(0.4f, 0.0f, 0.0f));
      this.m_ropeLine.SetPosition(1,
        new Vector3(0.4f, -this.m_stepDistance * (float)this.m_steps.Count, 0.0f));
      this.m_ropeLine.SetPosition(2,
        new Vector3(-0.4f, -this.m_stepDistance * (float)this.m_steps.Count, 0.0f));
      this.m_ropeLine.SetPosition(3, new Vector3(-0.4f, 0.0f, 0.0f));
      if (this.m_ghostObject)
        return;
      this.m_collider.size = new Vector3(1f, this.m_ladderHeight, 0.1f);
      ((Component)this.m_collider).transform.localPosition =
        new Vector3(0.0f, (float)(-(double)this.m_ladderHeight / 2.0), 0.0f);
    }

    public void UpdateIK(Animator animator)
    {
      int num1 = Mathf.RoundToInt(this.m_attachPoint.localPosition.y / this.m_stepDistance);
      if (this.m_currentRight == RopeLadderComponent.INVALID_STEP)
        this.m_currentRight = num1;
      if (this.m_currentLeft == RopeLadderComponent.INVALID_STEP)
        this.m_currentLeft = num1;
      if (this.m_targetLeft == RopeLadderComponent.INVALID_STEP &&
          this.m_targetRight == RopeLadderComponent.INVALID_STEP &&
          (double)this.m_currentMoveDir != 0.0)
      {
        if ((double)this.m_currentMoveDir > 0.0 && this.m_currentLeft < this.m_currentRight ||
            (double)this.m_currentMoveDir < 0.0 && this.m_currentLeft > this.m_currentRight ||
            !this.m_lastMovedLeft)
        {
          this.m_targetLeft = num1 + ((double)this.m_currentMoveDir > 0.0
            ? this.m_stepOffsetUp
            : this.m_stepOffsetDown);
          this.m_leftMoveTime = Time.time;
          this.m_lastMovedLeft = true;
        }
        else
        {
          this.m_targetRight = num1 + ((double)this.m_currentMoveDir > 0.0
            ? this.m_stepOffsetUp
            : this.m_stepOffsetDown);
          this.m_rightMoveTime = Time.time;
          this.m_lastMovedLeft = false;
        }
      }

      Vector3 vector3_1 = ((Component)this).transform.TransformPoint(new Vector3(-0.3f,
        (float)(this.m_currentLeft + 2) * this.m_stepDistance, -0.1f));
      Vector3 vector3_2 = ((Component)this).transform.TransformPoint(new Vector3(-0.2f,
        (float)this.m_currentLeft * this.m_stepDistance, -0.3f));
      Vector3 vector3_3 = ((Component)this).transform.TransformPoint(new Vector3(0.3f,
        (float)(this.m_currentRight + 2) * this.m_stepDistance, -0.1f));
      Vector3 vector3_4 = ((Component)this).transform.TransformPoint(new Vector3(0.2f,
        (float)this.m_currentRight * this.m_stepDistance, -0.3f));
      if (this.m_targetLeft != RopeLadderComponent.INVALID_STEP)
      {
        Vector3 vector3_5 = ((Component)this).transform.TransformPoint(new Vector3(-0.3f,
          (float)(this.m_targetLeft + 3) * this.m_stepDistance, 0.0f));
        Vector3 vector3_6 = ((Component)this).transform.TransformPoint(new Vector3(-0.2f,
          (float)this.m_targetLeft * this.m_stepDistance, 0.0f));
        float num2 = Mathf.Clamp01((float)(((double)Time.time - (double)this.m_leftMoveTime) *
                                           ((double)this.m_ladderMoveSpeed /
                                            (double)this.m_stepDistance)));
        vector3_1 = Vector3.Lerp(vector3_1, vector3_5, num2);
        vector3_2 = Vector3.Lerp(vector3_2, vector3_6, num2);
        if (Mathf.Approximately(num2, 1f))
        {
          this.m_currentLeft = this.m_targetLeft;
          this.m_targetLeft = RopeLadderComponent.INVALID_STEP;
        }
      }
      else if (this.m_targetRight != RopeLadderComponent.INVALID_STEP)
      {
        Vector3 vector3_7 = ((Component)this).transform.TransformPoint(new Vector3(0.3f,
          (float)(this.m_targetRight + 3) * this.m_stepDistance, 0.0f));
        Vector3 vector3_8 = ((Component)this).transform.TransformPoint(new Vector3(0.2f,
          (float)this.m_targetRight * this.m_stepDistance, 0.0f));
        float num3 = Mathf.Clamp01((float)(((double)Time.time - (double)this.m_rightMoveTime) *
                                           ((double)this.m_ladderMoveSpeed /
                                            (double)this.m_stepDistance)));
        vector3_3 = Vector3.Lerp(vector3_3, vector3_7, num3);
        vector3_4 = Vector3.Lerp(vector3_4, vector3_8, num3);
        if (Mathf.Approximately(num3, 1f))
        {
          this.m_currentRight = this.m_targetRight;
          this.m_targetRight = RopeLadderComponent.INVALID_STEP;
        }
      }

      animator.SetIKPosition((AvatarIKGoal)2, vector3_1);
      animator.SetIKPositionWeight((AvatarIKGoal)2, 1f);
      animator.SetIKPosition((AvatarIKGoal)0, vector3_2);
      animator.SetIKPositionWeight((AvatarIKGoal)0, 1f);
      animator.SetIKPosition((AvatarIKGoal)3, vector3_3);
      animator.SetIKPositionWeight((AvatarIKGoal)3, 1f);
      animator.SetIKPosition((AvatarIKGoal)1, vector3_4);
      animator.SetIKPositionWeight((AvatarIKGoal)1, 1f);
    }

    public void MoveOnLadder(Player player, float movedir)
    {
      float y = this.m_attachPoint.localPosition.y;
      if ((double)movedir > 0.0)
        y += this.m_ladderMoveSpeed * Time.deltaTime;
      else if ((double)movedir < 0.0)
        y -= this.m_ladderMoveSpeed * Time.deltaTime;
      this.m_attachPoint.localPosition = new Vector3(this.m_attachPoint.localPosition.x,
        this.ClampOffset(y), this.m_attachPoint.localPosition.z);
      this.m_currentMoveDir = movedir;
    }

    private float ClampOffset(float offset) => Mathf.Clamp(offset, -this.m_collider.size.y, 0.5f);

    public void StepOffLadder(Player player) => player.m_attachPoint = (Transform)null;
  }
}