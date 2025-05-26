// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations
{
  public class PowerStorageBridge :
    PowerNetworkDataEntity<PowerStorageBridge, PowerStorageComponent, PowerStorageData>
  {
    public static List<PowerStorageBridge> Instances = new();

    public void OnEnable()
    {
      if (!Instances.Contains(this))
      {
        Instances.Add(this);
      }

      this.WaitForPowerSystemNodeData<PowerStorageData>(SetData);
    }

    public void SetData(PowerStorageData data)
    {
      Logic.SetData(data);
    }

    public void OnDisable()
    {
      Instances.Remove(this);
      Instances.RemoveAll(x => !x);
    }

    protected override void RegisterDefaultRPCs()
    {
    }

    public string NetworkId => Data.NetworkId;
    public Vector3 Position => Logic.Position;
    public bool IsActive => Logic.IsActive;
    public Vector3 ConnectorPoint => Logic.ConnectorPoint;
    public bool IsCharging => Logic.IsCharging;

    public void SetNetworkId(string id)
    {
      if (!this.IsNetViewValid(out var netView)) return;
      var idFromZdo = netView.GetZDO().GetString(VehicleZdoVars.PowerSystem_NetworkId);
      Logic.SetNetworkId(idFromZdo);
    }
  }
}