// ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ValheimRAFT.BoardingRampComponent

using System.Collections.Generic;
using UnityEngine;
using ValheimRAFT;
using ValheimRAFT.UI;

public class BoardingRampComponent : MonoBehaviour, Interactable, Hoverable
{
  public enum BoardingRampState
  {
    Closed,
    Closing,
    Opening,
    Open
  }

  public BoardingRampState m_state = BoardingRampState.Closing;

  public float m_stateProgress = 0f;

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
    m_nview = GetComponent<ZNetView>();
    m_groundRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
      "terrain", "blocker", "WaterVolume", "Water");
    m_ramp = base.transform.Find("Ramp").gameObject;
    m_segmentObject = base.transform.Find("Ramp/Segment").gameObject;
    m_rope1 = base.transform.Find("Rope1").GetComponent<LineRenderer>();
    m_rope2 = base.transform.Find("Rope2").GetComponent<LineRenderer>();
    m_winch1 = base.transform.Find("Winch1/Cylinder");
    m_winch2 = base.transform.Find("Winch2/Cylinder");
    m_winch1Rope = base.transform.Find("Winch1/Pole/RopeAttach");
    m_winch2Rope = base.transform.Find("Winch2/Pole/RopeAttach");
    m_segmentObject.SetActive(ZNetView.m_forceDisableInit);
    if (ZNetView.m_forceDisableInit)
    {
      m_rope1.gameObject.SetActive(value: false);
      m_rope2.gameObject.SetActive(value: false);
      m_segmentObject.SetActive(value: true);
      base.enabled = false;
    }
    else
    {
      m_nview.Register<byte>("RPC_SetState", RPC_SetState);
      m_nview.Register<byte>("RPC_SetSegmentCount", RPC_SetSegmentCount);
      m_updateRamp = true;
      LoadZDO();
    }
  }

  private void RPC_SetState(long sender, byte state)
  {
    if (m_nview.IsOwner())
    {
      SetState((BoardingRampState)state);
    }
  }

  private void LoadZDO()
  {
    if ((bool)m_nview && m_nview.m_zdo != null)
    {
      m_state = (BoardingRampState)m_nview.m_zdo.GetInt("MB_m_state");
      m_segments = m_nview.m_zdo.GetInt("MB_m_segments", 5);
    }
  }

  public void RPC_SetSegmentCount(long sender, byte segmentCount)
  {
    if ((bool)m_nview && m_nview.IsOwner())
    {
      SetSegmentCount(segmentCount);
    }
  }

  public void SetSegmentCount(int segmentCount)
  {
    if (segmentCount != m_segments)
    {
      if ((bool)m_nview && !m_nview.IsOwner())
      {
        m_nview.InvokeRPC("RPC_SetSegmentCount", (byte)segmentCount);
      }
      else
      {
        m_segments = segmentCount;
        m_nview.m_zdo.Set("MB_m_segments", m_segments);
      }
    }
  }

  private void CreateSegments()
  {
    while (m_segments < m_segmentObjects.Count)
    {
      int j = m_segmentObjects.Count - 1;
      Object.Destroy(m_segmentObjects[j]);
      Object.Destroy(m_ropeAttach1[j]);
      Object.Destroy(m_ropeAttach2[j]);
      m_segmentObjects.RemoveAt(j);
      m_ropeAttach1.RemoveAt(j);
      m_ropeAttach2.RemoveAt(j);
    }

    for (int i = m_segmentObjects.Count; i < m_segments; i++)
    {
      GameObject go = Object.Instantiate(m_segmentObject,
        base.transform.position + new Vector3(0f, (0f - m_segmentHeight) * (float)i,
          m_segmentLength * (float)i), Quaternion.identity, m_ramp.transform);
      go.transform.localRotation = Quaternion.identity;
      go.transform.localScale = new Vector3(1f + (float)i * 0.0001f, 1f + (float)i * 0.0001f,
        1f + (float)i * 0.0001f);
      m_segmentObjects.Add(go);
      m_ropeAttach1.Add(go.transform.Find("SegmentAnchor/Pole1/RopeAttach"));
      m_ropeAttach2.Add(go.transform.Find("SegmentAnchor/Pole2/RopeAttach"));
      go.SetActive(value: true);
    }
  }

  private void Update()
  {
    if ((bool)m_nview && !m_nview.IsOwner())
    {
      BoardingRampState newState = (BoardingRampState)m_nview.m_zdo.GetInt("MB_m_state");
      if (newState != m_state)
      {
        if (newState == BoardingRampState.Closed || newState == BoardingRampState.Closing)
        {
          m_state = BoardingRampState.Closing;
        }
        else if (newState == BoardingRampState.Open || newState == BoardingRampState.Opening)
        {
          m_state = BoardingRampState.Opening;
        }
      }

      m_segments = m_nview.m_zdo.GetInt("MB_m_segments", 5);
    }

    if (m_segmentObjects.Count != m_segments)
    {
      CreateSegments();
      ForceRampUpdate();
    }

    CheckRampFloor();
    if (m_state == BoardingRampState.Closing)
    {
      m_stateProgress = Mathf.Clamp01(m_stateProgress -
                                      Time.deltaTime / (m_stateChangeDuration * (float)m_segments));
      UpdateRamp();
      if (m_stateProgress <= 0f)
      {
        m_state = BoardingRampState.Closed;
      }
    }
    else if (m_state == BoardingRampState.Opening)
    {
      m_stateProgress = Mathf.Clamp01(m_stateProgress +
                                      Time.deltaTime / (m_stateChangeDuration * (float)m_segments));
      UpdateRamp();
      if (m_stateProgress >= 1f)
      {
        m_state = BoardingRampState.Open;
      }
    }
    else if (m_updateRamp)
    {
      UpdateRamp();
    }
  }

  private void SetState(BoardingRampState state)
  {
    if (m_state == state)
    {
      return;
    }

    m_state = state;
    if ((bool)m_nview && m_nview.m_zdo != null)
    {
      if (m_nview.IsOwner())
      {
        m_nview.m_zdo.Set("MB_m_state", (int)state);
        return;
      }

      m_nview.InvokeRPC("RPC_SetState", (byte)state);
    }
  }

  private bool LinecastNonSelf(Vector3 start, Vector3 end, out RaycastHit hit)
  {
    Vector3 d = end - start;
    return RaycastNonSelf(start, d.normalized, d.magnitude, out hit);
  }

  private bool RaycastNonSelf(Vector3 start, Vector3 dir, float dist, out RaycastHit hit)
  {
    int hitCount = Physics.RaycastNonAlloc(start, dir, m_rayHits, dist, m_groundRayMask,
      QueryTriggerInteraction.Ignore);
    int hitIndex = 0;
    bool found = false;
    for (int i = 0; i < hitCount; i++)
    {
      if (!m_rayHits[i].transform.IsChildOf(base.transform.parent ?? base.transform) &&
          (!found || m_rayHits[hitIndex].distance > m_rayHits[i].distance))
      {
        hitIndex = i;
        found = true;
      }
    }

    hit = m_rayHits[hitIndex];
    return found;
  }

  private void CheckRampFloor()
  {
    if (m_state == BoardingRampState.Closed || m_state == BoardingRampState.Closing)
    {
      m_updateRamp |= m_state == BoardingRampState.Closing ||
                      m_ramp.transform.eulerAngles.x != -90f;
    }
    else
    {
      if (m_state != BoardingRampState.Opening && m_state != BoardingRampState.Open)
      {
        return;
      }

      m_updateRamp = true;
      float dist = m_segmentLength * (float)m_segments;
      float lineLen = 1f * (m_segmentLength - m_segmentOverlap) * (float)m_segments;
      Vector3 p =
        m_ramp.transform.TransformPoint(new Vector3(0f, (0f - m_segmentHeight) * (float)m_segments,
          lineLen));
      Vector3 lineStart = m_ramp.transform.position;
      Vector3 forward = (p - lineStart).normalized;
      Vector3 up = m_ramp.transform.up;
      if (LinecastNonSelf(lineStart, p, out var hitInfo))
      {
        m_hitColor = Color.green;
        m_hitDistance = hitInfo.distance / lineLen;
        if (dist * 0.94f > hitInfo.distance)
        {
          m_hitColor = Color.black;
          LinecastNonSelf(hitInfo.point + forward * 0.1f + up, hitInfo.point + forward * 0.1f - up,
            out hitInfo);
        }

        if ((double)Vector3.Dot(hitInfo.normal, Vector3.up) < 0.5)
        {
          m_hitColor = Color.white;
          LinecastNonSelf(hitInfo.point + up, hitInfo.point - up, out hitInfo);
        }

        m_lastHitRamp = true;
        m_hitPosition = hitInfo.point;
        m_updateRamp = true;
      }
      else if (m_lastHitRamp && LinecastNonSelf(lineStart - up * 0.3f, p - up * 0.3f, out hitInfo))
      {
        m_hitColor = Color.magenta;
      }
      else if (!m_lastHitRamp &&
               RaycastNonSelf(p + new Vector3(0f, 5f, 0f), Vector3.down, 1000f, out hitInfo))
      {
        m_hitColor = Color.blue;
        m_hitPosition = hitInfo.point;
        m_hitDistance = 1f;
        m_updateRamp = true;
        m_lastHitRamp = false;
      }
      else
      {
        m_lastHitRamp = false;
      }
    }
  }

  public void ForceRampUpdate()
  {
    m_updateRamp = true;
    m_lastBridgeProgress = -1f;
  }

  private void UpdateRamp()
  {
    if (!m_updateRamp)
    {
      return;
    }

    m_updateRamp = false;
    if (m_state == BoardingRampState.Closed || m_state == BoardingRampState.Closing)
    {
      m_desiredRotation = ((m_stateProgress < 1f / (float)m_segments)
        ? Quaternion.Euler(-90f, 0f, 0f)
        : m_ramp.transform.localRotation);
      m_rotSpeed = 90f;
    }
    else
    {
      m_desiredRotation = Quaternion.LookRotation((m_hitPosition - (base.transform.position -
        new Vector3(0f, m_hitDistance * m_segmentHeight * (float)m_segments, 0f))).normalized);
      m_desiredRotation =
        Quaternion.Euler(
          (Quaternion.Inverse(base.transform.rotation) * m_desiredRotation).eulerAngles.x, 0f, 0f);
      m_rotSpeed =
        Mathf.Clamp(Quaternion.Angle(m_ramp.transform.localRotation, m_desiredRotation) * 5f, 0f,
          90f);
    }

    m_ramp.transform.localRotation = Quaternion.RotateTowards(m_ramp.transform.localRotation,
      m_desiredRotation, Time.deltaTime * m_rotSpeed);
    m_winch1.transform.localRotation =
      Quaternion.Euler(m_stateProgress * 1000f * (float)m_segments, 0f, -90f);
    m_winch2.transform.localRotation =
      Quaternion.Euler(m_stateProgress * 1000f * (float)m_segments, 0f, -90f);
    float bridgeProgress = m_stateProgress;
    if (m_lastBridgeProgress != bridgeProgress)
    {
      m_lastBridgeProgress = bridgeProgress;
      for (int i = 1; i < m_segmentObjects.Count; i++)
      {
        float segmentProgress =
          Mathf.Clamp01(bridgeProgress * (float)m_segmentObjects.Count / (float)i);
        m_segmentObjects[i].transform.position = m_ramp.transform.TransformPoint(new Vector3(0f,
          (0f - m_segmentHeight) * (float)i,
          segmentProgress * (m_segmentLength - m_segmentOverlap) * (float)i));
      }
    }

    UpdateRopes();
  }

  private void UpdateRopes()
  {
    m_rope1.positionCount = m_segmentObjects.Count + 1;
    m_rope2.positionCount = m_segmentObjects.Count + 1;
    m_rope1.SetPosition(m_segmentObjects.Count,
      m_rope1.transform.InverseTransformPoint(m_winch1Rope.position));
    m_rope2.SetPosition(m_segmentObjects.Count,
      m_rope2.transform.InverseTransformPoint(m_winch2Rope.position));
    for (int i = 0; i < m_segmentObjects.Count; i++)
    {
      m_rope1.SetPosition(m_segmentObjects.Count - (i + 1),
        m_rope1.transform.InverseTransformPoint(m_ropeAttach1[i].position));
      m_rope2.SetPosition(m_segmentObjects.Count - (i + 1),
        m_rope2.transform.InverseTransformPoint(m_ropeAttach2[i].position));
    }
  }

  private void OnDrawGizmos()
  {
    Gizmos.color = m_hitColor;
    Gizmos.DrawSphere(m_hitPosition, 0.3f);
  }

  public string GetHoverName()
  {
    return "";
  }

  public string GetHoverText()
  {
    string stateChangeDesc =
      ((m_state == BoardingRampState.Open || m_state == BoardingRampState.Opening)
        ? "$mb_boarding_ramp_retract"
        : "$mb_boarding_ramp_extend");
    return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] " +
                                          stateChangeDesc) +
           Localization.instance.Localize(
             "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $mb_boarding_ramp_edit");
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (alt)
    {
      if (m_editPanel == null)
      {
        m_editPanel = new EditRampComponent();
      }

      m_editPanel.ShowPanel(this);
      return true;
    }

    if (!hold)
    {
      if (m_state == BoardingRampState.Open || m_state == BoardingRampState.Opening)
      {
        SetState(BoardingRampState.Closing);
      }
      else if (m_state == BoardingRampState.Closing || m_state == BoardingRampState.Closed)
      {
        SetState(BoardingRampState.Opening);
      }
    }

    return true;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }
}