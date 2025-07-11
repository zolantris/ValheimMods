// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

namespace ValheimVehicles.Integrations
{
  public class PowerConsumerBridge :
    PowerNetworkDataEntity<PowerConsumerBridge, PowerConsumerComponent, PowerConsumerData>
  {
    public string NetworkId => Data.NetworkId;
    public Vector3 Position => transform.position;
    public Vector3 ConnectorPoint => transform.position;
    public bool IsActive => Data.IsActive;
    public bool IsDemanding => Data.IsDemanding;
    public bool IsPowerDenied => Data.CanRunConsumerForDeltaTime(1f);

    public void SetData(PowerConsumerData data)
    {
      Logic.SetData(data);
    }
    public static List<PowerConsumerBridge> Instances = new();

    public void OnEnable()
    {
      if (!Instances.Contains(this))
      {
        Instances.Add(this);
      }
      this.WaitForPowerSystemNodeData<PowerConsumerData>(SetData);
    }

    public void OnDisable()
    {
      Instances.Remove(this);
      Instances.RemoveAll(x => !x);
    }

    protected override void RegisterDefaultRPCs()
    {
    }
  }
}