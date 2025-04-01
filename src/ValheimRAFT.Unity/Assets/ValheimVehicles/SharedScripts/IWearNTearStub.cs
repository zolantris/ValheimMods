#region

using System;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  public interface IWearNTearStub
  {
    Action m_onDestroyed { get; set; }
    public GameObject? m_new { get; }
    public GameObject? m_worn { get; }
    public GameObject? m_broken { get; }
    public GameObject? m_wet { get; }
  }
}