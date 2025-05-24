// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerConsumerComponent : PowerNodeComponentBase
  {

    [Header("Power Consumption Settings")]
    [SerializeField] private PowerConsumerData m_data = new();
    public PowerConsumerData Data => m_data;
    public bool IsDemanding => Data.IsDemanding;
    public override bool IsActive => Data.IsActive;
    public bool IsPowerDenied => !IsActive && IsDemanding;
    public event Action<float>? OnPowerSupplied;
    public event Action? OnPowerDenied;
    public void SetData(PowerConsumerData data)
    {
      m_data = data;
    }
    public void SetDemandState(bool val)
    {
      Data.IsDemanding = val;
    }
  }
}