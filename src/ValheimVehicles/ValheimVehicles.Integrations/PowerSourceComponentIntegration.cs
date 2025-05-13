using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.ZDOConfigs;
using ValheimVehicles.SharedScripts.PowerSystem;

namespace ValheimVehicles.Integrations;

public class PowerSourceComponentIntegration :
  NetworkedComponentIntegration<PowerSourceComponentIntegration, PowerSourceComponent, PowerSourceZDOConfig>, IPowerSource
{
  protected override void Awake()
  {
    base.Awake();
    PowerNetworkController.RegisterPowerComponent(this);
  }

  public void TryRefuel(float amount)
  {
    RunIfOwnerOrServer(() =>
    {
      Logic.Refuel(amount);
      UpdateNetworkedData();
    });
  }

  protected override void RegisterDefaultRPCs()
  {
    RegisterRPC<float>(nameof(RPC_Refuel), RPC_Refuel);
  }

  private void RPC_Refuel(long sender, float amount)
  {
    Logic.Refuel(amount);
    UpdateNetworkedData();
  }

  public void RefuelOrRPC(float amount)
  {
    if (this.IsNetViewValid(out var netView) && (netView.IsOwner() || ZNet.instance.IsServer()))
    {
      Logic.Refuel(amount);
      UpdateNetworkedData();
    }
    else
    {
      InvokeRPC(nameof(RPC_Refuel), amount);
    }
  }

  public void Refuel(float amount)
  {
    Logic.Refuel(amount);
  }
  public void SetRunning(bool state)
  {
    Logic.SetRunning(state);
  }
  public float GetFuelLevel()
  {
    return Logic.GetFuelLevel();
  }
  public float GetFuelCapacity()
  {
    return Logic.GetFuelCapacity();
  }
  public bool IsRunning => Logic.isRunning;

  public string NetworkId => Logic.NetworkId;
  public Vector3 Position => Logic.Position;
  public bool IsActive => Logic.IsActive;
  public Transform ConnectorPoint => Logic.ConnectorPoint;

  public void SetNetworkId(string id)
  {
    Logic.SetNetworkId(id);
  }
}