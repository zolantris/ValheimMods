// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;
using ValheimVehicles.SharedScripts.ZDOConfigs;

namespace ValheimVehicles.Integrations
{
  public class PowerConsumerComponentIntegration :
    NetworkedComponentIntegration<PowerConsumerComponentIntegration, PowerConsumerComponent, PowerConsumerZDOConfig>,
    IPowerConsumer
  {
    public string NetworkId { get; private set; }
    public Vector3 Position => transform.position;
    public Transform ConnectorPoint => transform;
    public bool IsActive => Logic.IsActive;
    public bool IsDemanding => Logic.IsDemanding;

    public float RequestedPower(float deltaTime)
    {
      return Logic.RequestedPower(deltaTime);
    }
    public void ApplyPower(float joules, float deltaTime)
    {
      Logic.ApplyPower(joules, deltaTime);
    }

    protected override void Awake()
    {
      base.Awake();
    }

    protected override void RegisterDefaultRPCs()
    {
    }

    private void FixedUpdate()
    {
      // Let logic handle demand evaluation and power state
    }

    public PowerConsumerComponent GetLogic()
    {
      return Logic;
    }

    public void SetNetworkId(string id)
    {
      NetworkId = id;
    }
  }
}