using System;
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
    // don't do anything when we aren't initialized.
  }

  protected override void Start()
  {
    if (!this.IsNetViewValid()) return;
    base.Start();
    PowerNetworkController.RegisterPowerComponent(this);
  }

  protected override void OnDestroy()
  {
    PowerNetworkController.UnregisterPowerComponent(this);
  }

  protected override void RegisterDefaultRPCs()
  {
    RegisterRPC<float>(nameof(RPC_AddFuel), RPC_AddFuel);
  }

  private void RPC_AddFuel(long sender, float amount)
  {
    Logic.AddFuel(amount);
    UpdateNetworkedData();
  }

  public void AddFuelOrRPC(float amount)
  {
    if (!this.IsNetViewValid(out var netView)) return;

    if (netView.IsOwner() || ZNet.instance.IsServer())
    {
      Logic.AddFuel(amount);
      UpdateNetworkedData();
    }
    else
    {
      InvokeRPC(nameof(RPC_AddFuel), amount);
    }
  }

  public void AddFuel(float amount)
  {
    Logic.AddFuel(amount);
  }
  public float RequestAvailablePower(float deltaTime, float supplyFromSources, float totalDemand, bool isDemanding)
  {
    return Logic.RequestAvailablePower(deltaTime, supplyFromSources, totalDemand, isDemanding);
  }
  public void SetRunning(bool state)
  {
    Logic.SetRunning(state);
  }
  public void SetFuelLevel(float amount)
  {
    Logic.SetFuelLevel(amount);
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