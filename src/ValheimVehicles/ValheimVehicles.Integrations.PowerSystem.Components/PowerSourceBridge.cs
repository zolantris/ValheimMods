using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;

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