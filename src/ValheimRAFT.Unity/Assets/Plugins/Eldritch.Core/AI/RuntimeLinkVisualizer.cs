// RuntimeLinkVisualizer.cs (replace your scanner with this)

using UnityEngine;
using System.Collections.Generic;
using Eldritch.Core;

[ExecuteAlways]
public class RuntimeLinkVisualizer : MonoBehaviour
{
  public ValheimPathfinding.AgentType agent = ValheimPathfinding.AgentType.HumanoidBig;

  [Header("Sampling")]
  public float ringRadius = 40f; // default; override per-call if you like
  public float ringStep = 0.8f; // radial stride
  public float angleStep = 15f; // angular stride
  public float navSnap = 0.6f; // snap-to-nav radius

  [Header("Body")]
  public float bodyRadius = 0.5f;
  public float chestHeight = 1.2f;

  [Header("Mantle")]
  public float minLedge = 0.35f;
  public float maxLedge = 3.0f;
  public float mantleUp = 2.0f;
  public float stepOver = 0.35f;

  [Header("Ingress (hole)")]
  public float dropMin = 1.0f;
  public float dropMax = 6.0f;
  public float holeRimOffset = 0.35f; // step slightly inward toward the opening
  public float holeProbeLift = 2.5f; // cast from above to see through thin floors
  public float holeClearAbove = 0.3f; // consider it a hole if no hit within this

  [Header("Layers")]
  public LayerMask solid; // set this in Inspector to PF.m_layers | PF.m_waterLayers

  [Header("Debug Draw")]
  public bool drawProbes = false;
  public bool drawHits = true;

  public readonly List<(Vector3 from, Vector3 to, bool isMantle)> links = new();

  // stats
  public int samplesTried, onNav, wallHits, mantlesAccepted, holeCandidates, holesAccepted;

  public void ScanFrom(Vector3 center)
  {
    ScanFrom(center, ringRadius);
  }

  public void ScanFrom(Vector3 center, float radius)
  {
    links.Clear();
    samplesTried = onNav = wallHits = mantlesAccepted = holeCandidates = holesAccepted = 0;

    var pf = ValheimPathfinding.instance;
    if (!pf) return;

    // Early: ensure nav exists near center
    if (!pf.TrySnapToNav(center, agent, out var centerOnNav)) return;

    for (float ang = 0; ang < 360f; ang += angleStep)
    {
      var dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;

      for (var r = ringStep; r <= radius; r += ringStep)
      {
        samplesTried++;
        var probe = centerOnNav + dir * r;

        // Snap each probe to runtime nav
        if (!pf.TrySnapToNav(probe, agent, out probe)) continue;
        onNav++;

        if (drawProbes)
        {
          Debug.DrawLine(centerOnNav + new Vector3(ringRadius, 0, 0), centerOnNav + new Vector3(-ringRadius, 0, 0), Color.gray, 0, false);
          Debug.DrawLine(centerOnNav + new Vector3(0, 0, ringRadius), centerOnNav + new Vector3(0, 0, -ringRadius), Color.gray, 0, false);
        }

        // --- Mantle: spherecast at chest height in the direction of scan
        var chest = probe + Vector3.up * chestHeight;
        if (Physics.SphereCast(chest, bodyRadius * 0.9f, dir, out var face, 1.2f, solid, QueryTriggerInteraction.Ignore))
        {
          wallHits++;

          var h = face.point.y - probe.y;
          if (h >= minLedge && h <= maxLedge)
          {
            var stepOverPos = face.point + Vector3.up * mantleUp + face.normal * stepOver;

            if (RaycastDown(stepOverPos + Vector3.up * 0.25f, out var down))
            {
              if (down.point.y > probe.y + minLedge)
              {
                links.Add((probe, down.point, true));
                mantlesAccepted++;
                if (drawHits)
                {
                  Debug.DrawLine(probe, down.point, Color.yellow, 0, false);
                  Debug.DrawRay(down.point, Vector3.up * 0.2f, Color.yellow, 0, false);
                }
                break; // don’t spam multiple along same ray
              }
            }
          }
        }

        // --- Hole/Ingress:
        // Move slightly *forward* (toward interior) and cast from above, collect all hits.
        var rim = probe + dir * holeRimOffset;
        var topCastFrom = new Vector3(rim.x, rim.y + holeProbeLift, rim.z);

        var hits = Physics.RaycastAll(topCastFrom, Vector3.down, holeProbeLift + dropMax + 2f, solid, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
          System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
          holeCandidates++;

          // pick first hit that is at least dropMin below the probe floor and <= dropMax
          RaycastHit? landingHit = null;
          for (var i = 0; i < hits.Length; i++)
          {
            var dy = probe.y - hits[i].point.y;
            if (dy >= dropMin && dy <= dropMax && Vector3.Angle(hits[i].normal, Vector3.up) <= 45f)
            {
              landingHit = hits[i];
              break;
            }
          }

          if (landingHit.HasValue)
          {
            var landing = landingHit.Value.point;
            if (pf.FindValidPoint(out var snapped, landing, 1.0f, agent))
              landing = snapped;

            links.Add((probe, landing, false));
            holesAccepted++;
            if (drawHits)
            {
              Debug.DrawLine(probe, landing, Color.magenta, 0, false);
              Debug.DrawRay(landing, Vector3.up * 0.25f, Color.magenta, 0, false);
            }
            break;
          }
        }
      }
    }

    // Quick console note (only in Editor playmode to avoid spam in build)
#if UNITY_EDITOR
    Debug.Log($"[PortalScan] samples={samplesTried} onNav={onNav} wallHits={wallHits} mantleOK={mantlesAccepted} holeCand={holeCandidates} holeOK={holesAccepted} totalLinks={links.Count}");
#endif
  }

  private bool RaycastDown(Vector3 from, out RaycastHit hit)
  {
    if (Physics.Raycast(from, Vector3.down, out hit, dropMax + 4f, solid, QueryTriggerInteraction.Ignore))
    {
      if (Vector3.Angle(hit.normal, Vector3.up) <= 45f) return true;
    }
    return false;
  }

  private void OnDrawGizmosSelected()
  {
    foreach (var link in links)
    {
      Gizmos.color = link.isMantle ? Color.yellow : Color.magenta;
      Gizmos.DrawLine(link.from, link.to);
      Gizmos.DrawSphere(link.from, 0.08f);
      Gizmos.DrawSphere(link.to, 0.08f);
    }
  }
}