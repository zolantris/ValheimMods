// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerConduitPlateComponent : PowerNodeComponentBase
  {
    public PowerConduitMode mode;
    public Func<float> GetPlayerEitr = () => 0f;
    public Action<float> AddPlayerEitr = _ => {};
    public Action<float> SubtractPlayerEitr = _ => {};
    public Collider m_triggerCollider;

    private bool m_hasPlayerInRange;
    public bool HasPlayerInRange => m_hasPlayerInRange;

    public bool IsDemanding => mode == PowerConduitMode.Charge && HasPlayerInRange;

    protected override void Awake()
    {
      m_triggerCollider = transform.GetComponentInChildren<Collider>();
    }

    public void SetHasPlayerInRange(bool val)
    {
      m_hasPlayerInRange = val;
    }

    public override bool IsActive
    {
      get;
    } = true;
  }
}