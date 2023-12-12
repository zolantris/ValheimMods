// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.BoardingRampComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimRAFT.UI;

namespace ValheimRAFT
{
  public class BoardingRampComponent : MonoBehaviour, Interactable, Hoverable
  {
    public BoardingRampComponent.BoardingRampState m_state =
      BoardingRampComponent.BoardingRampState.Closing;

    public float m_stateProgress = 0.0f;
    public float m_stateChangeDuration = 0.5f;
    public int m_segments = 8;
    public float m_segmentLength = 2f;
    public float m_segmentOverlap = 0.2f;
    public float m_segmentHeight = 0.05f;
    public float m_maxRampRotation;
    private GameObject m_segmentObject;
    private List<GameObject> m_segmentObjects = new List<GameObject>();
    private List<Transform> m_ropeAttach1 = new List<Transform>();
    private List<Transform> m_ropeAttach2 = new List<Transform>();
    private LineRenderer m_rope1;
    private LineRenderer m_rope2;
    private GameObject m_ramp;
    private Transform m_winch1;
    private Transform m_winch2;
    private Transform m_winch1Rope;
    private Transform m_winch2Rope;
    private bool m_lastHitRamp;
    private Vector3 m_hitPosition;
    private bool m_updateRamp;
    private Quaternion m_desiredRotation;
    private float m_rotSpeed;
    private Color m_hitColor;
    private float m_hitDistance;
    private ZNetView m_nview;
    private int m_groundRayMask;
    private static RaycastHit[] m_rayHits = new RaycastHit[30];
    private float m_lastBridgeProgress = -1f;
    private static EditRampComponent m_editPanel;

    private void Awake()
    {
      this.m_nview = ((Component)this).GetComponent<ZNetView>();
      this.m_groundRayMask = LayerMask.GetMask(new string[8]
      {
        "Default",
        "static_solid",
        "Default_small",
        "piece",
        "terrain",
        "blocker",
        "WaterVolume",
        "Water"
      });
      this.m_ramp = ((Component)((Component)this).transform.Find("Ramp")).gameObject;
      this.m_segmentObject =
        ((Component)((Component)this).transform.Find("Ramp/Segment")).gameObject;
      this.m_rope1 = ((Component)((Component)this).transform.Find("Rope1"))
        .GetComponent<LineRenderer>();
      this.m_rope2 = ((Component)((Component)this).transform.Find("Rope2"))
        .GetComponent<LineRenderer>();
      this.m_winch1 = ((Component)this).transform.Find("Winch1/Cylinder");
      this.m_winch2 = ((Component)this).transform.Find("Winch2/Cylinder");
      this.m_winch1Rope = ((Component)this).transform.Find("Winch1/Pole/RopeAttach");
      this.m_winch2Rope = ((Component)this).transform.Find("Winch2/Pole/RopeAttach");
      this.m_segmentObject.SetActive(ZNetView.m_forceDisableInit);
      if (ZNetView.m_forceDisableInit)
      {
        ((Component)this.m_rope1).gameObject.SetActive(false);
        ((Component)this.m_rope2).gameObject.SetActive(false);
        this.m_segmentObject.SetActive(true);
        ((Behaviour)this).enabled = false;
      }
      else
      {
        this.m_nview.Register<byte>("RPC_SetState", new Action<long, byte>(this.RPC_SetState));
        this.m_nview.Register<byte>("RPC_SetSegmentCount",
          new Action<long, byte>(this.RPC_SetSegmentCount));
        this.m_updateRamp = true;
        this.LoadZDO();
      }
    }

    private void RPC_SetState(long sender, byte state)
    {
      if (!this.m_nview.IsOwner())
        return;
      this.SetState((BoardingRampComponent.BoardingRampState)state);
    }

    private void LoadZDO()
    {
      if (!this.m_nview || this.m_nview.m_zdo == null)
        return;
      this.m_state =
        (BoardingRampComponent.BoardingRampState)this.m_nview.m_zdo.GetInt("MB_m_state", 0);
      this.m_segments = this.m_nview.m_zdo.GetInt("MB_m_segments", 5);
    }

    public void RPC_SetSegmentCount(long sender, byte segmentCount)
    {
      if (!m_nview || !this.m_nview.IsOwner())
        return;
      this.SetSegmentCount((int)segmentCount);
    }

    public void SetSegmentCount(int segmentCount)
    {
      if (segmentCount == this.m_segments)
        return;
      if (m_nview && !this.m_nview.IsOwner())
      {
        this.m_nview.InvokeRPC("RPC_SetSegmentCount", new object[1]
        {
          (object)(byte)segmentCount
        });
      }
      else
      {
        this.m_segments = segmentCount;
        this.m_nview.m_zdo.Set("MB_m_segments", this.m_segments);
      }
    }

    private void CreateSegments()
    {
      while (this.m_segments < this.m_segmentObjects.Count)
      {
        int index = this.m_segmentObjects.Count - 1;
        Destroy(m_segmentObjects[index]);
        Destroy(m_ropeAttach1[index]);
        Destroy(m_ropeAttach2[index]);
        this.m_segmentObjects.RemoveAt(index);
        this.m_ropeAttach1.RemoveAt(index);
        this.m_ropeAttach2.RemoveAt(index);
      }

      for (int count = this.m_segmentObjects.Count; count < this.m_segments; ++count)
      {
        GameObject gameObject = Instantiate<GameObject>(this.m_segmentObject,
          (((Component)this).transform.position +
           new Vector3(0.0f, -this.m_segmentHeight * (float)count,
             this.m_segmentLength * (float)count)), Quaternion.identity, this.m_ramp.transform);
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = new Vector3(
          (float)(1.0 + (double)count * 9.9999997473787516E-05),
          (float)(1.0 + (double)count * 9.9999997473787516E-05),
          (float)(1.0 + (double)count * 9.9999997473787516E-05));
        this.m_segmentObjects.Add(gameObject);
        this.m_ropeAttach1.Add(gameObject.transform.Find("SegmentAnchor/Pole1/RopeAttach"));
        this.m_ropeAttach2.Add(gameObject.transform.Find("SegmentAnchor/Pole2/RopeAttach"));
        gameObject.SetActive(true);
      }
    }

    private void Update()
    {
      if (m_nview && !this.m_nview.IsOwner())
      {
        BoardingRampComponent.BoardingRampState boardingRampState =
          (BoardingRampComponent.BoardingRampState)this.m_nview.m_zdo.GetInt("MB_m_state", 0);
        if (boardingRampState != this.m_state)
        {
          if (boardingRampState == BoardingRampComponent.BoardingRampState.Closed ||
              boardingRampState == BoardingRampComponent.BoardingRampState.Closing)
            this.m_state = BoardingRampComponent.BoardingRampState.Closing;
          else if (boardingRampState == BoardingRampComponent.BoardingRampState.Open ||
                   boardingRampState == BoardingRampComponent.BoardingRampState.Opening)
            this.m_state = BoardingRampComponent.BoardingRampState.Opening;
        }

        this.m_segments = this.m_nview.m_zdo.GetInt("MB_m_segments", 5);
      }

      if (this.m_segmentObjects.Count != this.m_segments)
      {
        this.CreateSegments();
        this.ForceRampUpdate();
      }

      this.CheckRampFloor();
      if (this.m_state == BoardingRampComponent.BoardingRampState.Closing)
      {
        this.m_stateProgress = Mathf.Clamp01(this.m_stateProgress -
                                             Time.deltaTime / (this.m_stateChangeDuration *
                                                               (float)this.m_segments));
        this.UpdateRamp();
        if ((double)this.m_stateProgress > 0.0)
          return;
        this.m_state = BoardingRampComponent.BoardingRampState.Closed;
      }
      else if (this.m_state == BoardingRampComponent.BoardingRampState.Opening)
      {
        this.m_stateProgress = Mathf.Clamp01(this.m_stateProgress +
                                             Time.deltaTime / (this.m_stateChangeDuration *
                                                               (float)this.m_segments));
        this.UpdateRamp();
        if ((double)this.m_stateProgress < 1.0)
          return;
        this.m_state = BoardingRampComponent.BoardingRampState.Open;
      }
      else
      {
        if (!this.m_updateRamp)
          return;
        this.UpdateRamp();
      }
    }

    private void SetState(BoardingRampComponent.BoardingRampState state)
    {
      if (this.m_state == state)
        return;
      this.m_state = state;
      if (this.m_nview && this.m_nview.m_zdo != null)
      {
        if (this.m_nview.IsOwner())
          this.m_nview.m_zdo.Set("MB_m_state", (int)state);
        else
          this.m_nview.InvokeRPC("RPC_SetState", new object[1]
          {
            (object)(byte)state
          });
      }
    }

    private bool LinecastNonSelf(Vector3 start, Vector3 end, out RaycastHit hit)
    {
      Vector3 vector3 = (end - start);
      return this.RaycastNonSelf(start, vector3.normalized, vector3.magnitude, out hit);
    }

    private bool RaycastNonSelf(Vector3 start, Vector3 dir, float dist, out RaycastHit hit)
    {
      int num = Physics.RaycastNonAlloc(start, dir, BoardingRampComponent.m_rayHits, dist,
        this.m_groundRayMask, (QueryTriggerInteraction)1);
      int index1 = 0;
      bool flag = false;
      for (int index2 = 0; index2 < num; ++index2)
      {
        if (!BoardingRampComponent.m_rayHits[index2].transform
              .IsChildOf(((Component)this).transform.parent ?? ((Component)this).transform) &&
            (!flag || BoardingRampComponent.m_rayHits[index1]
              .distance > BoardingRampComponent.m_rayHits[index2].distance))
        {
          index1 = index2;
          flag = true;
        }
      }

      hit = BoardingRampComponent.m_rayHits[index1];
      return flag;
    }

    private void CheckRampFloor()
    {
      if (this.m_state == BoardingRampComponent.BoardingRampState.Closed ||
          this.m_state == BoardingRampComponent.BoardingRampState.Closing)
      {
        this.m_updateRamp = ((this.m_updateRamp ? 1 : 0) |
                             (this.m_state == BoardingRampComponent.BoardingRampState.Closing
                               ? 1
                               : ((double)this.m_ramp.transform.eulerAngles.x != -90.0 ? 1 : 0))) !=
                            0;
      }
      else
      {
        if (this.m_state != BoardingRampComponent.BoardingRampState.Opening &&
            this.m_state != BoardingRampComponent.BoardingRampState.Open)
          return;
        this.m_updateRamp = true;
        float num1 = this.m_segmentLength * (float)this.m_segments;
        float num2 = (float)(1.0 * ((double)this.m_segmentLength - (double)this.m_segmentOverlap)) *
                     (float)this.m_segments;
        Vector3 end = this.m_ramp.transform.TransformPoint(new Vector3(0.0f,
          -this.m_segmentHeight * (float)this.m_segments, num2));
        Vector3 position = this.m_ramp.transform.position;
        Vector3 vector3 = (end - position);
        Vector3 normalized = vector3.normalized;
        Vector3 up = this.m_ramp.transform.up;
        RaycastHit hit;
        if (this.LinecastNonSelf(position, end, out hit))
        {
          this.m_hitColor = Color.green;
          this.m_hitDistance = hit.distance / num2;
          if ((double)num1 * 0.93999999761581421 > hit.distance)
          {
            this.m_hitColor = Color.black;
            this.LinecastNonSelf(
              (hit.point +
               (normalized * 0.1f) + up), (
                (hit.point +
                 (normalized * 0.1f)) - up), out hit);
          }

          if ((double)Vector3.Dot(hit.normal, Vector3.up) < 0.5)
          {
            this.m_hitColor = Color.white;
            this.LinecastNonSelf((hit.point + up), (hit.point - up), out hit);
          }

          this.m_lastHitRamp = true;
          this.m_hitPosition = hit.point;
          this.m_updateRamp = true;
        }
        else if (this.m_lastHitRamp && this.LinecastNonSelf(
                   (position - (up
                                * 0.3f)),
                   (end - (up * 0.3f)), out hit))
          this.m_hitColor = Color.magenta;
        else if (!this.m_lastHitRamp && this.RaycastNonSelf(
                   (end + new Vector3(0.0f, 5f, 0.0f)), Vector3.down, 1000f,
                   out hit))
        {
          this.m_hitColor = Color.blue;
          this.m_hitPosition = hit.point;
          this.m_hitDistance = 1f;
          this.m_updateRamp = true;
          this.m_lastHitRamp = false;
        }
        else
          this.m_lastHitRamp = false;
      }
    }

    public void ForceRampUpdate()
    {
      this.m_updateRamp = true;
      this.m_lastBridgeProgress = -1f;
    }

    private void UpdateRamp()
    {
      if (!this.m_updateRamp)
        return;
      this.m_updateRamp = false;
      if (this.m_state == BoardingRampComponent.BoardingRampState.Closed ||
          this.m_state == BoardingRampComponent.BoardingRampState.Closing)
      {
        this.m_desiredRotation = (double)this.m_stateProgress < 1.0 / (double)this.m_segments
          ? Quaternion.Euler(-90f, 0.0f, 0.0f)
          : this.m_ramp.transform.localRotation;
        this.m_rotSpeed = 90f;
      }
      else
      {
        Vector3 vector3 = (this.m_hitPosition -
                           (((Component)this).transform.position -
                            new Vector3(0.0f,
                              this.m_hitDistance * this.m_segmentHeight * (float)this.m_segments,
                              0.0f)));
        this.m_desiredRotation = Quaternion.LookRotation((vector3).normalized);
        Quaternion quaternion =
          Quaternion.Inverse(((Component)this).transform.rotation) * this.m_desiredRotation;
        this.m_desiredRotation =
          Quaternion.Euler((quaternion).eulerAngles.x, 0.0f, 0.0f);
        this.m_rotSpeed =
          Mathf.Clamp(
            Quaternion.Angle(this.m_ramp.transform.localRotation, this.m_desiredRotation) * 5f,
            0.0f, 90f);
      }

      this.m_ramp.transform.localRotation = Quaternion.RotateTowards(
        this.m_ramp.transform.localRotation, this.m_desiredRotation,
        Time.deltaTime * this.m_rotSpeed);
      ((Component)this.m_winch1).transform.localRotation =
        Quaternion.Euler(this.m_stateProgress * 1000f * (float)this.m_segments, 0.0f, -90f);
      ((Component)this.m_winch2).transform.localRotation =
        Quaternion.Euler(this.m_stateProgress * 1000f * (float)this.m_segments, 0.0f, -90f);
      float stateProgress = this.m_stateProgress;
      if ((double)this.m_lastBridgeProgress != (double)stateProgress)
      {
        this.m_lastBridgeProgress = stateProgress;
        for (int index = 1; index < this.m_segmentObjects.Count; ++index)
        {
          float num =
            Mathf.Clamp01(stateProgress * (float)this.m_segmentObjects.Count / (float)index);
          this.m_segmentObjects[index].transform.position = this.m_ramp.transform.TransformPoint(
            new Vector3(0.0f, -this.m_segmentHeight * (float)index,
              num * (this.m_segmentLength - this.m_segmentOverlap) * (float)index));
        }
      }

      this.UpdateRopes();
    }

    private void UpdateRopes()
    {
      this.m_rope1.positionCount = this.m_segmentObjects.Count + 1;
      this.m_rope2.positionCount = this.m_segmentObjects.Count + 1;
      this.m_rope1.SetPosition(this.m_segmentObjects.Count,
        ((Component)this.m_rope1).transform.InverseTransformPoint(this.m_winch1Rope.position));
      this.m_rope2.SetPosition(this.m_segmentObjects.Count,
        ((Component)this.m_rope2).transform.InverseTransformPoint(this.m_winch2Rope.position));
      for (int index = 0; index < this.m_segmentObjects.Count; ++index)
      {
        this.m_rope1.SetPosition(this.m_segmentObjects.Count - (index + 1),
          ((Component)this.m_rope1).transform.InverseTransformPoint(this.m_ropeAttach1[index]
            .position));
        this.m_rope2.SetPosition(this.m_segmentObjects.Count - (index + 1),
          ((Component)this.m_rope2).transform.InverseTransformPoint(this.m_ropeAttach2[index]
            .position));
      }
    }

    private void OnDrawGizmos()
    {
      Gizmos.color = this.m_hitColor;
      Gizmos.DrawSphere(this.m_hitPosition, 0.3f);
    }

    public string GetHoverName() => "";

    public string GetHoverText() => Localization.instance.Localize(
      "[<color=yellow><b>$KEY_Use</b></color>] " +
      (this.m_state == BoardingRampComponent.BoardingRampState.Open ||
       this.m_state == BoardingRampComponent.BoardingRampState.Opening
        ? "$mb_boarding_ramp_retract"
        : "$mb_boarding_ramp_extend")) + Localization.instance.Localize(
      "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $mb_boarding_ramp_edit");

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
      if (alt)
      {
        if (BoardingRampComponent.m_editPanel == null)
          BoardingRampComponent.m_editPanel = new EditRampComponent();
        BoardingRampComponent.m_editPanel.ShowPanel(this);
        return true;
      }

      if (!hold)
      {
        if (this.m_state == BoardingRampComponent.BoardingRampState.Open ||
            this.m_state == BoardingRampComponent.BoardingRampState.Opening)
          this.SetState(BoardingRampComponent.BoardingRampState.Closing);
        else if (this.m_state == BoardingRampComponent.BoardingRampState.Closing ||
                 this.m_state == BoardingRampComponent.BoardingRampState.Closed)
          this.SetState(BoardingRampComponent.BoardingRampState.Opening);
      }

      return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    public enum BoardingRampState
    {
      Closed,
      Closing,
      Opening,
      Open,
    }
  }
}