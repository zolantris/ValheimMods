// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.Integrations.ZDOConfigs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations
{
  public class PowerStorageBridge :
    PowerNetworkDataEntity<PowerStorageBridge, PowerStorageComponent, PowerStorageData>,
    IPowerStorage
  {
    public static List<PowerStorageBridge> Instances = new();

    public void OnEnable()
    {
      if (!Instances.Contains(this))
      {
        Instances.Add(this);
      }
    }

    public void OnDisable()
    {
      Instances.Remove(this);
      Instances.RemoveAll(x => !x);
    }

    protected override void RegisterDefaultRPCs()
    {
      RegisterRPC<float>(nameof(RPC_Discharge), RPC_Discharge);
      RegisterRPC<float>(nameof(RPC_Charge), RPC_Charge);
    }

    public void ChargeOrRPC(float amount)
    {
      if (!this.IsNetViewValid(out var netView)) return;
      if (netView.IsOwner() || ZNet.instance.IsServer())
      {
        Logic.Charge(amount);
        UpdateNetworkedData();
      }
      else
      {
        InvokeRPC(nameof(RPC_Charge), amount);
      }
    }

    public void DischargeOrRPC(float amount)
    {
      if (!this.IsNetViewValid(out var netView)) return;
      if (netView.IsOwner() || ZNet.instance.IsServer())
      {
        Logic.Discharge(amount);
        UpdateNetworkedData();
      }
      else
      {
        InvokeRPC(nameof(RPC_Discharge), amount);
      }
    }

    private void RPC_Charge(long sender, float amount)
    {
      this.RunIfOwnerOrServerOrNoOwner(_ =>
      {
        Logic.Charge(amount);
        UpdateNetworkedData();
      });
    }

    private void RPC_Discharge(long sender, float amount)
    {
      this.RunIfOwnerOrServerOrNoOwner(_ =>
      {
        Logic.Discharge(amount);
        UpdateNetworkedData();
      });
    }

    public string NetworkId => Data.NetworkId;
    public Vector3 Position => Logic.Position;
    public bool IsActive => Logic.IsActive;
    public Vector3 ConnectorPoint => Logic.ConnectorPoint;
    public bool IsCharging => Logic.IsCharging;
    public void SetCapacity(float val)
    {
      // Logic.SetCapacity(val);
    }
    public void SetActive(bool val)
    {
      Logic.SetActive(val);
    }

    public float PeekDischarge(float amount)
    {
      // todo might want this in Data.
      return 0f;
      // return Logic.PeekDischarge(amount);
    }

    public void CommitDischarge(float amount)
    {
      Logic.CommitDischarge(amount);
    }

    public float ChargeLevel => Data.Energy;
    public float Energy => Data.Energy;
    public float CapacityRemaining => Data.EnergyCapacityRemaining;

    public float Charge(float amount)
    {
      return Logic.Charge(amount);
    }
    public float Discharge(float amount)
    {
      return Logic.Discharge(amount);
    }

    public void SetNetworkId(string id)
    {
      if (!this.IsNetViewValid(out var netView)) return;
      var idFromZdo = netView.GetZDO().GetString(VehicleZdoVars.PowerSystem_NetworkId);
      Logic.SetNetworkId(idFromZdo);
    }
  }
}