using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.ZDOConfigs;

namespace ValheimVehicles.Integrations;

public class PowerSourceComponentIntegration :
  NetworkedComponentIntegration<PowerSourceComponentIntegration, PowerSourceZDOConfig>, IPowerSource
{
  private PowerSourceComponent _logic;

  protected override void Awake()
  {
    base.Awake();

    _logic = gameObject.AddComponent<PowerSourceComponent>();
    PowerNetworkControllerIntegration.Instance.RegisterNode(_logic);
  }

  public void TryRefuel(float amount)
  {
    RunIfOwnerOrServer(() =>
    {
      _logic.Refuel(amount);
      UpdateNetworkedData();
    });
  }

  public void Refuel(float amount)
  {
    _logic.Refuel(amount);
  }
  public void SetRunning(bool state)
  {
    _logic.SetRunning(state);
  }
  public float GetFuelLevel()
  {
    return _logic.GetFuelLevel();
  }
  public float GetFuelCapacity()
  {
    return _logic.GetFuelCapacity();
  }
  public bool IsRunning => _logic.isRunning;
}