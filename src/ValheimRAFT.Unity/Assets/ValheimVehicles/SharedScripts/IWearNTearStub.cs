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
    public GameObject m_new { get; set; }
    public GameObject m_worn { get; set; }
    public GameObject m_broken { get; set; }
    public GameObject m_wet { get; set; }
  }
}