// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;
namespace ValheimVehicles.SharedScripts.Interfaces
{
  public interface IMechanismActionSetter
  {
    public Transform transform { get; }
    public MechanismAction SelectedAction { get; set; }
    public SwivelComponent? TargetSwivel { get; set; }
    public List<SwivelComponent> NearestSwivels { get; set; }
    public void SetMechanismAction(MechanismAction action);
    public void SetMechanismSwivel(SwivelComponent swivel);

    // ZDO Config TODO swap this over to newer approach with a dedicated ZDO loader.
    public void Save(SwivelComponent swivel);
    public void Load();
  }
}