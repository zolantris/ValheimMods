using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.ZDOConfigs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations;

public class PowerSourceComponentIntegration :
  NetworkedComponentIntegration<PowerSourceComponentIntegration, PowerSourceComponent, PowerSourceZDOConfig>, IPowerSource
{
  public PowerSourceData Data = new();

  public static List<PowerSourceComponentIntegration> Instances = new();
  public static ZDOID? Zdoid = null;

  public void OnEnable()
  {
    if (!Instances.Contains(this))
    {
      Instances.Add(this);
    }
  }

  public void OnDisable()
  {
    if (Zdoid.HasValue)
    {
      PowerZDONetworkManager.RemovePowerComponentUpdater(Zdoid.Value);
    }
    Instances.Remove(this);
    Instances.RemoveAll(x => !x);
    PowerNetworkController.UnregisterPowerComponent(this);
  }

  protected override void Awake()
  {
    base.Awake();
    // don't do anything when we aren't initialized.
  }

  protected override void Start()
  {
    this.WaitForZNetView((netView) =>
    {
      var zdo = netView.GetZDO();
      if (!PowerZDONetworkManager.TryGetData(zdo, out PowerSourceData data, true))
      {
        LoggerProvider.LogWarning("[PowerSourceComponentIntegration] Failed to get PowerSourceData from PowerZDONetworkManager.");
        return;
      }
      Zdoid = zdo.m_uid;
      Data = data;
      Data.Load();

      if (ZNet.instance.IsDedicated())
      {
        netView.m_zdo.SetOwner(ZDOMan.GetSessionID());
      }
      PowerNetworkController.RegisterPowerComponent(this);
      PowerZDONetworkManager.RegisterPowerComponentUpdater(zdo.m_uid, data);
      base.Start();
    });
  }


  protected override void RegisterDefaultRPCs()
  {
    RegisterRPC<float>(nameof(RPC_AddFuel), RPC_AddFuel);
    RegisterRPC<float>(nameof(RPC_SetFuel), RPC_SetFuel);
  }

  private void RPC_AddFuel(long sender, float amount)
  {
    Data.AddFuel(amount);
    UpdateNetworkedData();
  }

  private void RPC_SetFuel(long sender, float amount)
  {
    Data.SetFuel(amount);
    UpdateNetworkedData();
  }

  public void SetFuelOrRPC(float amount)
  {
    if (!this.IsNetViewValid(out var netView)) return;

    if (netView.IsOwner() || ZNet.instance.IsServer())
    {
      Data.SetFuel(amount);
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
      Data.AddFuel(amount);
      UpdateNetworkedData();
    }
    else
    {
      InvokeRPC(nameof(RPC_AddFuel), amount);
    }
  }

  // keep for data model.
  public void AddFuel(float amount)
  {
    AddFuelOrRPC(amount);
  }
  public float RequestAvailablePower(float deltaTime, float supplyFromSources, float totalDemand, bool isDemanding)
  {
    return Data.GetMaxPotentialOutput(deltaTime);
    // return Logic.RequestAvailablePower(deltaTime, supplyFromSources, totalDemand, isDemanding);
  }
  public void CommitEnergyUsed(float energyUsed)
  {
    Data.CommitEnergyUsed(energyUsed);
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
    LoggerProvider.LogWarning("Method not implemented");
    // Logic.SetFuelCapacity(val);
  }
  public void SetFuelConsumptionRate(float val)
  {
    LoggerProvider.LogWarning("Method not implemented");

    // Logic.SetFuelConsumptionRate(val);
  }
  public void UpdateFuelEfficiency()
  {
    LoggerProvider.LogWarning("Method not implemented");

    // Logic.UpdateFuelEfficiency();
  }
  public void SetFuelLevel(float amount)
  {
    SetFuelOrRPC(amount);
  }
  public float GetFuelLevel()
  {
    return Data.Fuel;
  }
  public float GetFuelCapacity()
  {
    return Data.MaxFuel;
  }
  public bool IsRunning => Logic.isRunning;
  public bool IsActive => Logic.IsActive;
  public string NetworkId => Data.NetworkId;
  public Vector3 Position => Logic.Position;
  public Vector3 ConnectorPoint => Logic.ConnectorPoint;

  public void SetNetworkId(string id)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    var idFromZdo = netView.GetZDO().GetString(VehicleZdoVars.Power_NetworkId);
    Logic.SetNetworkId(idFromZdo);
  }
}