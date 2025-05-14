// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;
using ValheimVehicles.SharedScripts.ZDOConfigs;

namespace ValheimVehicles.Integrations
{
  public class PowerConsumerComponentIntegration :
    NetworkedComponentIntegration<PowerConsumerComponentIntegration, PowerConsumerComponent, PowerConsumerZDOConfig>,
    IPowerConsumer
  {
    public string NetworkId => Logic.NetworkId;
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
    public void SetActive(bool val)
    {
      Logic.SetActive(val);
    }

    protected override void Awake()
    {
      base.Awake();
    }

    public List<Character> charactersWithinActivatorZone = new();

    public virtual void OnCollisionEnter(Collision other) {}
    public virtual void OnCollisionExit(Collision other) {}

    protected override void Start()
    {
      // don't do anything when we aren't initialized.
      if (!this.IsNetViewValid(out var netView)) return;
      base.Start();
      PowerNetworkController.RegisterPowerComponent(this);
    }

    protected override void OnDestroy()
    {
      PowerNetworkController.UnregisterPowerComponent(this);
      base.OnDestroy();
    }

    protected override void RegisterDefaultRPCs()
    {
    }

    private void FixedUpdate()
    {
      // Let logic handle demand evaluation and power state
    }

    public void SetNetworkId(string id)
    {
      Logic.SetNetworkId(id);
    }
  }
}