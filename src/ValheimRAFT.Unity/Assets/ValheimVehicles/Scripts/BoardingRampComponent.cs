using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimRAFT
{

    public class BoardingRampComponent : MonoBehaviour, Interactable, Hoverable
    {
        public enum BoardingRampState
        {
            Closed,
            Closing,
            Opening,
            Open
        };

        public BoardingRampState m_state = BoardingRampState.Closed;
        public float m_stateProgress = 0;
        public float m_stateChangeDuration = 0.5f;

        public int m_segments = 15;
        public float m_segmentLength = 2f;
        public float m_segmentOverlap = 0.1f;
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
        private int m_groundRayMask;

        void Awake()
        {
            m_groundRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "blocker", "WaterVolume", "Water");

            m_ramp = transform.Find("Ramp").gameObject;
            m_segmentObject = transform.Find("Ramp/Segment").gameObject;
            m_rope1 = transform.Find("Rope1").GetComponent<LineRenderer>();
            m_rope2 = transform.Find("Rope2").GetComponent<LineRenderer>();

            m_winch1 = transform.Find("Winch1/Cylinder");
            m_winch2 = transform.Find("Winch2/Cylinder");

            m_winch1Rope = transform.Find("Winch1/Pole/RopeAttach");
            m_winch2Rope = transform.Find("Winch2/Pole/RopeAttach");

            m_segmentObject.SetActive(ZNetView.m_forceDisableInit);

            // Placement ghost dummy
            if (ZNetView.m_forceDisableInit)
            {
                m_winch1Rope.gameObject.SetActive(false);
                m_winch2Rope.gameObject.SetActive(false);
                m_segmentObject.SetActive(true);
                enabled = false;
            }
        }

        private void CreateSegments()
        {
            while (m_segments < m_segmentObjects.Count)
            {
                var l = m_segmentObjects.Count - 1;
                UnityEngine.Object.Destroy(m_segmentObjects[l]);
                UnityEngine.Object.Destroy(m_ropeAttach1[l]);
                UnityEngine.Object.Destroy(m_ropeAttach2[l]);

                m_segmentObjects.RemoveAt(l);
                m_ropeAttach1.RemoveAt(l);
                m_ropeAttach2.RemoveAt(l);
            }

            for (int i = m_segmentObjects.Count; i < m_segments; i++)
            {
                var go = Object.Instantiate(m_segmentObject, transform.position + new Vector3(0, -m_segmentHeight * i, m_segmentLength * i), Quaternion.identity, m_ramp.transform);
                go.transform.localRotation = Quaternion.identity;
                m_segmentObjects.Add(go);
                m_ropeAttach1.Add(go.transform.Find("SegmentAnchor/Pole1/RopeAttach"));
                m_ropeAttach2.Add(go.transform.Find("SegmentAnchor/Pole2/RopeAttach"));

                go.SetActive(true);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (m_segmentObjects.Count != m_segments)
            {
                CreateSegments();
            }

            CheckRampFloor();

            if (m_state == BoardingRampState.Closing)
            {
                m_stateProgress = Mathf.Clamp01(m_stateProgress - (UnityEngine.Time.deltaTime / (m_stateChangeDuration * m_segments)));
                UpdateRamp();

                if (m_stateProgress <= 0)
                    m_state = BoardingRampState.Closed;
            }
            else if (m_state == BoardingRampState.Opening)
            {
                m_stateProgress = Mathf.Clamp01(m_stateProgress + (UnityEngine.Time.deltaTime / (m_stateChangeDuration * m_segments)));
                UpdateRamp();

                if (m_stateProgress >= 1f)
                    m_state = BoardingRampState.Open;
            }
            else if (m_updateRamp)
            {
                UpdateRamp();
            }

        }

        private static RaycastHit[] m_rayHits = new RaycastHit[30];
        private float m_lastBridgeProgress;

        private bool LinecastNonSelf(Vector3 start, Vector3 end, out RaycastHit hit)
        {
            var d = (end - start);
            return RaycastNonSelf(start, d.normalized, d.magnitude, out hit);
        }

        private bool RaycastNonSelf(Vector3 start, Vector3 dir, float dist, out RaycastHit hit)
        {
            //Debug.DrawLine(start, start + (dir * dist), Color.blue);
            var hitCount = Physics.RaycastNonAlloc(start, dir, m_rayHits, dist, m_groundRayMask, QueryTriggerInteraction.Ignore);
            var hitIndex = 0;
            var found = false;
            for (int i = 0; i < hitCount; i++)
            {
                if (m_rayHits[i].transform.IsChildOf(transform.parent ?? transform))
                {
                    continue;
                }

                if (!found || m_rayHits[hitIndex].distance > m_rayHits[i].distance)
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
                m_updateRamp |= m_state == BoardingRampState.Closing || m_ramp.transform.eulerAngles.x != -90f;
            }
            else if (m_state == BoardingRampState.Opening
                || m_state == BoardingRampState.Open)
            {
                m_updateRamp = true; //  m_state == BoardingRampState.Opening;

                var dist = (m_segmentLength * m_segments);
                var lineLen = 1f * (m_segmentLength - m_segmentOverlap) * m_segments;
                var p = m_ramp.transform.TransformPoint(new Vector3(0, -m_segmentHeight * m_segments, lineLen));
                var lineStart = m_ramp.transform.position;

                var forward = (p - lineStart).normalized;
                var up = m_ramp.transform.up;

                if (LinecastNonSelf(lineStart, p, out var hitInfo))
                {
                    m_hitColor = Color.green;
                    m_hitDistance = hitInfo.distance / lineLen;

                    // Adjust if hitting the side of something
                    if (dist * 0.94f > hitInfo.distance)
                    {
                        m_hitColor = Color.black;
                        LinecastNonSelf(hitInfo.point + (forward * 0.1f) + up, hitInfo.point + (forward * 0.1f) - up, out hitInfo);
                    }

                    if (Vector3.Dot(hitInfo.normal, Vector3.up) < 0.5)
                    {
                        m_hitColor = Color.white;
                        LinecastNonSelf(hitInfo.point + up, hitInfo.point - up, out hitInfo);
                    }

                    m_lastHitRamp = true;
                    m_hitPosition = hitInfo.point;
                    m_updateRamp = true;
                }
                else if (m_lastHitRamp && LinecastNonSelf(lineStart - (up * 0.3f), p - (up * 0.3f), out hitInfo))
                {
                    m_hitColor = Color.magenta;
                }
                else if (!m_lastHitRamp && RaycastNonSelf(p + new Vector3(0, 5f, 0), Vector3.down, 1000f, out hitInfo))
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
        }

        private void UpdateRamp()
        {
            if (!m_updateRamp)
                return;

            m_updateRamp = false;

            if (m_state == BoardingRampState.Closed || m_state == BoardingRampState.Closing)
            {
                m_desiredRotation = m_stateProgress < 1f / m_segments ? Quaternion.Euler(-90f, 0, 0) : m_ramp.transform.localRotation;
                m_rotSpeed = 90f;
            }
            else
            {
                m_desiredRotation = Quaternion.LookRotation((m_hitPosition - (transform.position - new Vector3(0, m_hitDistance * m_segmentHeight * m_segments, 0))).normalized);
                m_desiredRotation = Quaternion.Euler((Quaternion.Inverse(transform.rotation) * m_desiredRotation).eulerAngles.x, 0, 0);
                m_rotSpeed = Mathf.Clamp(Quaternion.Angle(m_ramp.transform.localRotation, m_desiredRotation) * 5f, 0, 90f);
            }

            m_ramp.transform.localRotation = Quaternion.RotateTowards(m_ramp.transform.localRotation, m_desiredRotation, Time.deltaTime * m_rotSpeed);

            m_winch1.transform.localRotation = Quaternion.Euler(m_stateProgress * 1000f * m_segments, 0, -90f);
            m_winch2.transform.localRotation = Quaternion.Euler(m_stateProgress * 1000f * m_segments, 0, -90f);

            var bridgeProgress = m_stateProgress;

            if (m_lastBridgeProgress != bridgeProgress)
            {
                m_lastBridgeProgress = bridgeProgress;

                for (int i = 1; i < m_segmentObjects.Count; i++)
                {
                    var segmentProgress = Mathf.Clamp01(bridgeProgress * (float)m_segmentObjects.Count / (float)(i));
                    m_segmentObjects[i].transform.position = m_ramp.transform.TransformPoint(new Vector3(0, -m_segmentHeight * i, segmentProgress * (m_segmentLength-m_segmentOverlap) * i));
                }
            }

            UpdateRopes();
        }

        private void UpdateRopes()
        {
            m_rope1.positionCount = m_segmentObjects.Count + 1;
            m_rope2.positionCount = m_segmentObjects.Count + 1;

            m_rope1.SetPosition(m_segmentObjects.Count, m_rope1.transform.InverseTransformPoint(m_winch1Rope.position));
            m_rope2.SetPosition(m_segmentObjects.Count, m_rope2.transform.InverseTransformPoint(m_winch2Rope.position));

            for (int i = 0; i < m_segmentObjects.Count; i++)
            {
                m_rope1.SetPosition(m_segmentObjects.Count - (i + 1), m_rope1.transform.InverseTransformPoint(m_ropeAttach1[i].position));
                m_rope2.SetPosition(m_segmentObjects.Count - (i + 1), m_rope2.transform.InverseTransformPoint(m_ropeAttach2[i].position));
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
            var stateChangeDesc = (m_state == BoardingRampState.Open || m_state == BoardingRampState.Opening) ? "$mb_boarding_ramp_retract" : "$mb_boarding_ramp_extend";
            return Localization.instance.Localize($"[<color=yellow><b>$KEY_Use</b></color>] {stateChangeDesc}");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (m_state == BoardingRampState.Open || m_state == BoardingRampState.Opening)
            {
                m_state = BoardingRampState.Closing;
            }
            else if (m_state == BoardingRampState.Closing || m_state == BoardingRampState.Closed)
            {
                m_state = BoardingRampState.Opening;
            }

            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }
}