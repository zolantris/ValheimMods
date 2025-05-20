// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerConduitPlateComponent : PowerNodeComponentBase
  {
    public PowerConduitMode mode;
    public static float chargeRate = 1f;
    public static float drainRate = 10f;
    public static float eitrToFuelRatio = 40f;
    public float fuelStored = 0f;
    public float maxFuelCapacity = 100f;

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