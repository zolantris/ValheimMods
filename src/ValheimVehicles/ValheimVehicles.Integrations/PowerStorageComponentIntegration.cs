// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.Integrations.ZDOConfigs;

namespace ValheimVehicles.Integrations
{
  public class PowerStorageComponentIntegration :
    NetworkedComponentIntegration<PowerStorageComponentIntegration, PowerStorageComponent, PowerStorageZDOConfig>,
    IPowerStorage
  {
    protected override void Awake()
    {
      base.Awake();
      // don't do anything when we aren't initialized.
      if (!this.IsNetViewValid()) return;
      PowerNetworkController.RegisterPowerComponent(this);
    }
    protected void OnDestroy()
    {
      PowerNetworkController.UnregisterPowerComponent(this);
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
      if (!this.IsNetViewValid(out var netView)) return;
      if (netView.IsOwner() || !netView.HasOwner())
      {
        if (!netView.IsOwner() && ZNet.instance.IsServer())
        {
          netView.ClaimOwnership();
        }
        Logic.Charge(amount);
        UpdateNetworkedData();
      }
    }

    private void RPC_Discharge(long sender, float amount)
    {
      if (!this.IsNetViewValid(out var netView)) return;
      if (netView.IsOwner() || !netView.HasOwner())
      {
        if (!netView.IsOwner() && ZNet.instance.IsServer())
        {
          netView.ClaimOwnership();
        }
        Logic.Discharge(amount);
        UpdateNetworkedData();
      }
    }

    public string NetworkId => Logic.NetworkId;
    public Vector3 Position => Logic.Position;
    public bool IsActive => Logic.IsActive;
    public Transform ConnectorPoint => Logic.ConnectorPoint;

    public float CapacityRemaining => Logic.CapacityRemaining;
    public bool IsCharging => Logic.IsCharging;
    public void SetCapacity(float val)
    {
      Logic.SetCapacity(val);
    }
    public void SetActive(bool val)
    {
      Logic.SetActive(val);
    }

    public float PeekDischarge(float amount)
    {
      return Logic.PeekDischarge(amount);
    }

    public void CommitDischarge(float amount)
    {
      Logic.CommitDischarge(amount);
    }

    public float ChargeLevel => Logic.ChargeLevel;
    public float Capacity => Logic.Capacity;

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
      Logic.SetNetworkId(id);
    }
  }
}