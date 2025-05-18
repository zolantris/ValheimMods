using System;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.ZDOConfigs;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.Structs;

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
    RegisterRPC<float>(nameof(RPC_SetFuel), RPC_SetFuel);
  }

  private void RPC_AddFuel(long sender, float amount)
  {
    Logic.AddFuel(amount);
    UpdateNetworkedData();
  }

  private void RPC_SetFuel(long sender, float amount)
  {
    Logic.SetFuelLevel(amount);
    UpdateNetworkedData();
  }

  public void SetFuelOrRPC(float amount)
  {
    if (!this.IsNetViewValid(out var netView)) return;

    if (netView.IsOwner() || ZNet.instance.IsServer())
    {
      Logic.SetFuelLevel(amount);
      UpdateNetworkedData();
    }
    else
    {
      InvokeRPC(nameof(RPC_SetFuel), amount);
    }
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
    AddFuelOrRPC(amount);
  }
  public float RequestAvailablePower(float deltaTime, float supplyFromSources, float totalDemand, bool isDemanding)
  {
    return Logic.RequestAvailablePower(deltaTime, supplyFromSources, totalDemand, isDemanding);
  }
  public void CommitEnergyUsed(float energyUsed)
  {
    Logic.CommitEnergyUsed(energyUsed);
    UpdateNetworkedData();
  }
  public void SetRunning(bool state)
  {
    if (!this.IsNetViewValid(out var netView))
    {
      return;
    }
    Logic.SetRunning(state);
  }
  public void SetFuelCapacity(float val)
  {
    Logic.SetFuelCapacity(val);
  }
  public void SetFuelConsumptionRate(float val)
  {
    Logic.SetFuelConsumptionRate(val);
  }
  public void UpdateFuelEfficiency()
  {
    Logic.UpdateFuelEfficiency();
  }
  public void SetFuelLevel(float amount)
  {
    SetFuelOrRPC(amount);
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