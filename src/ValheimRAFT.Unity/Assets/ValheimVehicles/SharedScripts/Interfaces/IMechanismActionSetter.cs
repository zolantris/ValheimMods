// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;

namespace ValheimVehicles.SharedScripts.Interfaces
{
  public interface IMechanismActionSetter : IMechanismSwitchConfig
  {
    public Transform transform { get; }
    public GameObject gameObject { get; }
    public List<SwivelComponent> NearestSwivels { get; set; }
    public void SetMechanismAction(MechanismAction action);
    public void SetMechanismSwivel(SwivelComponent swivel);
    public List<SwivelComponent> GetNearestSwivels();
  }
}