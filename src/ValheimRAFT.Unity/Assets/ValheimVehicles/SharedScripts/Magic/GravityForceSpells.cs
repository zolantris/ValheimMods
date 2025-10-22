#region

using System.Collections.Generic;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts.Magic
{
  /// <summary>
  /// Force / Gravity combat spells that work on any Rigidbody targets.
  /// - All inputs are virtual; override later for Valheim bindings.
  /// - Safe defaults for mouse+keyboard testing in the editor.
  /// </summary>
  public class GravityForceSpells : MonoBehaviour
  {
    // ---------- Serialized (private) ----------
    [Header("Common")]
    [SerializeField] private float scanRadius = 10f;
    [SerializeField] private LayerMask targetMask = ~0; // everything
    [SerializeField] private float maxAffectMass = 500f;

    [Header("Blast (Q)")]
    [SerializeField] private float blastRadius = 6f;
    [SerializeField] private float blastForce = 1200f;

    [Header("Hold (F)")]
    [SerializeField] private float holdRadius = 5f;
    [SerializeField] private float holdForce = 2000f;
    [SerializeField] private float holdDuration = 2.0f;

    [Header("Crush (C)")]
    [SerializeField] private float crushRadius = 5f;
    [SerializeField] private float crushDownForce = 2800f;
    [SerializeField] private float crushStunSeconds = 1.0f; // left as a hook (no stun system here)

    [Header("Pull/Push (E/R)")]
    [SerializeField] private float pullPushRadius = 8f;
    [SerializeField] private float pullForce = 1500f;
    [SerializeField] private float pushForce = 1500f;

    [Header("Throw (T)")]
    [SerializeField] private float throwRadius = 6f;
    [SerializeField] private float throwUpForce = 2200f;

    [Header("QoL")]
    [SerializeField] private ForceMode forceMode = ForceMode.Impulse;
    [SerializeField] private float lineOfSightPadding = 0.25f;

    // ---------- Private ----------
    private readonly List<Rigidbody> _buffer = new();

    // ---------- Unity ----------
    private void Update()
    {
      if (GetBlastPressed()) ForceBlast();
      if (GetHoldPressed()) ForceHold();
      if (GetCrushPressed()) ForceCrush();
      if (GetPullPressed()) ForcePull();
      if (GetPushPressed()) ForcePush();
      if (GetThrowPressed()) ForceThrow();
    }

    // ---------- Spells ----------
    public void ForceBlast()
    {
      var origin = transform.position + Vector3.up * 1.0f;
      var cols = Physics.OverlapSphere(origin, blastRadius, targetMask, QueryTriggerInteraction.Ignore);
      foreach (var col in cols)
      {
        if (!TryGetBody(col, out var rb)) continue;
        var dir = (rb.worldCenterOfMass - origin).normalized;
        rb.AddForce(dir * blastForce, forceMode);
      }
    }

    public void ForceHold()
    {
      // Locks targets toward a sphere center (simple positional hold using opposing force)
      var center = transform.position + Vector3.up * 1.0f;
      GatherBodies(center, holdRadius, _buffer);
      StartCoroutine(HoldRoutine(_buffer, center, holdDuration));
    }

    private System.Collections.IEnumerator HoldRoutine(List<Rigidbody> bodies, Vector3 center, float duration)
    {
      var time = 0f;
      while (time < duration)
      {
        var dt = Time.deltaTime;
        foreach (var rb in bodies)
        {
          if (!rb) continue;
          var toCenter = center - rb.worldCenterOfMass;
          var dist = toCenter.magnitude + 0.001f;
          var dir = toCenter / dist;
          rb.AddForce(dir * holdForce * dt, ForceMode.Acceleration);
          rb.linearVelocity *= 0.96f; // mild damping while held
        }
        time += dt;
        yield return null;
      }
    }

    public void ForceCrush()
    {
      // Slam down nearby bodies
      var center = transform.position + Vector3.up * 1.0f;
      GatherBodies(center, crushRadius, _buffer);
      foreach (var rb in _buffer)
      {
        if (!rb) continue;
        rb.AddForce(Vector3.down * crushDownForce, forceMode);
      }
      // Stun hook left for game integration layer
      OnCrushApplied(_buffer, crushStunSeconds);
    }

    public void ForcePull()
    {
      var center = transform.position + Vector3.up * 1.0f;
      GatherBodies(center, pullPushRadius, _buffer);
      foreach (var rb in _buffer)
      {
        if (!rb) continue;
        var dir = (center - rb.worldCenterOfMass).normalized;
        rb.AddForce(dir * pullForce, forceMode);
      }
    }

    public void ForcePush()
    {
      var center = transform.position + Vector3.up * 1.0f;
      GatherBodies(center, pullPushRadius, _buffer);
      foreach (var rb in _buffer)
      {
        if (!rb) continue;
        var dir = (rb.worldCenterOfMass - center).normalized;
        rb.AddForce(dir * pushForce, forceMode);
      }
    }

    public void ForceThrow()
    {
      var center = transform.position + Vector3.up * 1.0f;
      GatherBodies(center, throwRadius, _buffer);
      foreach (var rb in _buffer)
      {
        if (!rb) continue;
        rb.AddForce(Vector3.up * throwUpForce, forceMode);
      }
    }

    // ---------- Helpers ----------
    private void GatherBodies(Vector3 center, float radius, List<Rigidbody> outList)
    {
      outList.Clear();
      var cols = Physics.OverlapSphere(center, radius, targetMask, QueryTriggerInteraction.Ignore);
      foreach (var col in cols)
      {
        if (!TryGetBody(col, out var rb)) continue;
        if (rb.mass > maxAffectMass) continue;
        if (!HasLoS(center, rb)) continue;
        outList.Add(rb);
      }
    }

    private static bool TryGetBody(Collider c, out Rigidbody rb)
    {
      rb = c.attachedRigidbody ? c.attachedRigidbody : c.GetComponentInParent<Rigidbody>();
      return rb;
    }

    private bool HasLoS(Vector3 origin, Rigidbody rb)
    {
      var to = rb.worldCenterOfMass - origin;
      var dist = to.magnitude;
      var dir = to / (dist + 0.0001f);
      return !Physics.Raycast(origin, dir, dist - lineOfSightPadding, ~0, QueryTriggerInteraction.Ignore);
    }

    // ---------- Input (virtual) ----------
    protected virtual bool GetBlastPressed()
    {
      return Input.GetKeyDown(KeyCode.Q);
    }
    protected virtual bool GetHoldPressed()
    {
      return Input.GetKeyDown(KeyCode.F);
    }
    protected virtual bool GetCrushPressed()
    {
      return Input.GetKeyDown(KeyCode.C);
    }
    protected virtual bool GetPullPressed()
    {
      return Input.GetKeyDown(KeyCode.E);
    }
    protected virtual bool GetPushPressed()
    {
      return Input.GetKeyDown(KeyCode.R);
    }
    protected virtual bool GetThrowPressed()
    {
      return Input.GetKeyDown(KeyCode.T);
    }

    // ---------- Hooks for integration ----------
    protected virtual void OnCrushApplied(List<Rigidbody> affected, float stunSeconds) {}
  }
}