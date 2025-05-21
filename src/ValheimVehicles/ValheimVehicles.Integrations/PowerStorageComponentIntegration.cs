// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.Integrations.ZDOConfigs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations
{
  public class PowerStorageComponentIntegration :
    NetworkedComponentIntegration<PowerStorageComponentIntegration, PowerStorageComponent, PowerStorageZDOConfig>,
    IPowerStorage
  {
    public Coroutine? registerCoroutine = null;
    public PowerStorageData Data = new();

    public static List<PowerStorageComponentIntegration> Instances = new();
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
      Instances.Remove(this);
      Instances.RemoveAll(x => !x);


      if (Zdoid.HasValue)
      {
        PowerZDONetworkManager.RemovePowerComponentUpdater(Zdoid.Value);
      }
      PowerNetworkController.UnregisterPowerComponent(this);
    }

    protected override void Awake()
    {
      // don't do anything when we aren't initialized.
      registerCoroutine = this.WaitForZNetView((nv) =>
      {
        var zdo = nv.GetZDO();
        if (!PowerZDONetworkManager.TryGetData(zdo, out PowerStorageData data, true))
        {
          LoggerProvider.LogWarning("[PowerStorageComponentIntegration] Failed to get PowerStorageData from PowerZDONetworkManager.");
          return;
        }
        Zdoid = zdo.m_uid;
        Data = data;
        Data.Load();

        base.Awake();
        PowerNetworkController.RegisterPowerComponent(this);
        PowerZDONetworkManager.RegisterPowerComponentUpdater(zdo.m_uid, data);
      });
    }

    protected override void OnDestroy()
    {
      base.OnDestroy();

      if (registerCoroutine != null)
      {
        StopCoroutine(registerCoroutine);
        registerCoroutine = null;
      }
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

    public float CapacityRemaining => Data.MaxCapacity - Data.StoredEnergy;

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

    public float ChargeLevel => Data.StoredEnergy;
    public float Capacity => Data.MaxCapacity;

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
      var idFromZdo = netView.GetZDO().GetString(VehicleZdoVars.Power_NetworkId);
      Logic.SetNetworkId(idFromZdo);
    }
  }
}