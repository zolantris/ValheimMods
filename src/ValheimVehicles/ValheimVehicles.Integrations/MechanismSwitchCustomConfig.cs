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
  public class MechanismSwitchCustomConfig : ISerializableConfig<MechanismSwitchCustomConfig, IMechanismSwitchConfig>, IPrefabConfig<MechanismSwitchCustomConfig>, IMechanismSwitchConfig
  {
    public MechanismAction SelectedAction = MechanismAction.None;
    public int TargetSwivelId = 0;
    public MechanismSwitchCustomConfig Config => this;

    public void ApplyFrom(IMechanismSwitchConfig component)
    {
      SelectedAction = component.SelectedAction;
      TargetSwivelId = component.TargetSwivel != null && component.TargetSwivel.TryGetComponent(out ZNetView view) && view.GetZDO() != null
        ? ZdoWatchController.Instance.GetOrCreatePersistentID(view.GetZDO())
        : 0;
    }

    public int GetStableHashCode()
    {
      unchecked
      {
        var hash = 17;
        hash = hash * 31 + SelectedAction.GetHashCode();
        hash = hash * 31 + TargetSwivelId;
        return hash;
      }
    }

    public void ApplyTo(IMechanismSwitchConfig component)
    {
      component.SelectedAction = SelectedAction;
      component.TargetSwivelId = TargetSwivelId;
      component.TargetSwivel = ResolveSwivel(TargetSwivelId);
    }

    public MechanismSwitchCustomConfig Load(ZDO zdo, IMechanismSwitchConfig component)
    {
      return new MechanismSwitchCustomConfig
      {
        SelectedAction = ParseAction(zdo.GetString(VehicleZdoVars.ToggleSwitchAction, nameof(MechanismAction.CommandsHud))),
        TargetSwivelId = zdo.GetInt(VehicleZdoVars.Mechanism_Swivel_TargetId, 0)
      };
    }

    public void Save(ZDO zdo, MechanismSwitchCustomConfig config)
    {
      zdo.Set(VehicleZdoVars.ToggleSwitchAction, config.SelectedAction.ToString());
      zdo.Set(VehicleZdoVars.Mechanism_Swivel_TargetId, config.TargetSwivelId);
    }

    public void Serialize(ZPackage pkg)
    {
      pkg.Write((int)SelectedAction);
      pkg.Write(TargetSwivelId);
    }

    public MechanismSwitchCustomConfig Deserialize(ZPackage pkg)
    {
      return new MechanismSwitchCustomConfig
      {
        SelectedAction = (MechanismAction)pkg.ReadInt(),
        TargetSwivelId = pkg.ReadInt()
      };
    }

    public static SwivelComponent? ResolveSwivel(int id)
    {
      var netView = ZdoWatchController.Instance.GetInstance(id);
      return netView ? netView.GetComponent<SwivelComponent>() : null;
    }

    private static MechanismAction ParseAction(string actionString)
    {
      return Enum.TryParse(actionString, out MechanismAction result) ? result : MechanismAction.CommandsHud;
    }

    MechanismAction IMechanismSwitchConfig.SelectedAction
    {
      get => SelectedAction;
      set => SelectedAction = value;
    }

    int IMechanismSwitchConfig.TargetSwivelId
    {
      get => TargetSwivelId;
      set => TargetSwivelId = value;
    }

    SwivelComponent? IMechanismSwitchConfig.TargetSwivel
    {
      get => ResolveSwivel(TargetSwivelId); // optional caching if needed
      set => TargetSwivelId = value != null && value.TryGetComponent(out ZNetView view) && view.GetZDO() != null
        ? ZdoWatchController.Instance.GetOrCreatePersistentID(view.GetZDO())
        : 0;
    }
  }
}