using System.Collections.Generic;
using Jotunn;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;
using Logger = HarmonyLib.Tools.Logger;

namespace ValheimVehicles.Integrations;

public class PowerSourceBridge :
  PowerNetworkDataEntity<PowerSourceBridge, PowerSourceComponent, PowerSourceData>
{
  public static List<PowerSourceBridge> Instances = new();

  public void OnEnable()
  {
    if (!Instances.Contains(this))
    {
      Instances.Add(this);
    }

    this.WaitForPowerSystemNodeData<PowerSourceData>(SetData);
  }

  public void OnDisable()
  {
    Instances.Remove(this);
    Instances.RemoveAll(x => !x);
  }

  protected override void RegisterDefaultRPCs()
  {
  }

  // public void SetFuelOrRPC(float amount)
  // {
  //   if (!this.IsNetViewValid(out var netView)) return;
  //
  //   if (ZNet.instance.IsServer())
  //   {
  //     Data.SetFuel(amount);
  //     UpdateNetworkedData();
  //   }
  //   else
  //   {
  //     RpcHandler.InvokeRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(RPC_SetFuel), amount);
  //   }
  // }

  // public void AddFuelOrRPC(float amount)
  // {
  //   if (!this.IsNetViewValid(out var netView)) return;
  //
  //   var isClientInstance = ZNet.m_instance.IsClientInstance();
  //   var isServerInstance = ZNet.m_instance.IsServerInstance();
  //   var isLocalInstance = ZNet.m_instance.IsLocalInstance();
  //
  //   LoggerProvider.LogInfoDebounced($"isClientInstance {isClientInstance} , isServerInstance {isServerInstance}, isLocalInstance {isLocalInstance}");
  //
  //   if (ZNet.instance.IsServer())
  //   {
  //     Data.AddFuel(amount);
  //     UpdateNetworkedData();
  //   }
  //   else
  //   {
  //     RpcHandler.InvokeRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(RPC_AddFuel), amount);
  //   }
  // }

  // keep for data model.
  public void AddFuel(float amount)
  {
    // AddFuelOrRPC(amount);
  }

  public void SetData(PowerSourceData data)
  {
    Logic.SetData(data);
  }

  public bool IsRunning => Logic.isRunning;
  public bool IsActive => Logic.IsActive;
  public string NetworkId => Data.NetworkId;
  public Vector3 Position => Logic.Position;
  public Vector3 ConnectorPoint => Logic.ConnectorPoint;

  public void SetNetworkId(string id)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    var idFromZdo = netView.GetZDO().GetString(VehicleZdoVars.PowerSystem_NetworkId);
    Logic.SetNetworkId(idFromZdo);
  }
}