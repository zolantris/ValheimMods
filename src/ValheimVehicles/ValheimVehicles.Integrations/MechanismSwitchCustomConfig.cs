// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Interfaces;
using ValheimVehicles.Structs;
using ZdoWatcher;

namespace ValheimVehicles.Config
{
  public class MechanismSwitchCustomConfig : ISerializableConfig<MechanismSwitchCustomConfig, IMechanismSwitchConfig>, IPrefabConfig<MechanismSwitchCustomConfig>
  {
    public MechanismAction SelectedAction = MechanismAction.None;
    public int SwivelTargetId = 0;

    public MechanismSwitchCustomConfig Config => this;

    public void ApplyFrom(IMechanismSwitchConfig component)
    {
      SelectedAction = component.SelectedAction;
      SwivelTargetId = component.TargetSwivel != null && component.TargetSwivel.TryGetComponent(out ZNetView view) && view.GetZDO() != null
        ? ZdoWatchController.Instance.GetOrCreatePersistentID(view.GetZDO())
        : 0;
    }

    public void ApplyTo(IMechanismSwitchConfig component)
    {
      component.SelectedAction = SelectedAction;
      component.TargetSwivel = ResolveSwivel(SwivelTargetId);
    }

    public MechanismSwitchCustomConfig Load(ZDO zdo, IMechanismSwitchConfig component)
    {
      return new MechanismSwitchCustomConfig
      {
        SelectedAction = ParseAction(zdo.GetString(VehicleZdoVars.ToggleSwitchAction, nameof(MechanismAction.CommandsHud))),
        SwivelTargetId = zdo.GetInt(VehicleZdoVars.Mechanism_Swivel_TargetId, 0)
      };
    }

    public void Save(ZDO zdo, MechanismSwitchCustomConfig config)
    {
      zdo.Set(VehicleZdoVars.ToggleSwitchAction, config.SelectedAction.ToString());
      zdo.Set(VehicleZdoVars.Mechanism_Swivel_TargetId, config.SwivelTargetId);
    }

    public void Serialize(ZPackage pkg)
    {
      pkg.Write((int)SelectedAction);
      pkg.Write(SwivelTargetId);
    }

    public MechanismSwitchCustomConfig Deserialize(ZPackage pkg)
    {
      return new MechanismSwitchCustomConfig
      {
        SelectedAction = (MechanismAction)pkg.ReadInt(),
        SwivelTargetId = pkg.ReadInt()
      };
    }

    private static SwivelComponent? ResolveSwivel(int id)
    {
      var netView = ZdoWatchController.Instance.GetInstance(id);
      return netView ? netView.GetComponent<SwivelComponent>() : null;
    }

    private static MechanismAction ParseAction(string actionString)
    {
      return Enum.TryParse(actionString, out MechanismAction result) ? result : MechanismAction.CommandsHud;
    }
  }
}