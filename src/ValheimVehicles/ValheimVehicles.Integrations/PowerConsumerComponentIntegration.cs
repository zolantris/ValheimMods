// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
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
    public Vector3 ConnectorPoint => transform.position;
    public bool IsActive => Logic.IsActive;
    public bool IsDemanding => Logic.IsDemanding;
    public bool IsPowerDenied => Logic.IsPowerDenied;

    public PowerConsumerData Data = new();
    public static ZDOID? Zdoid = null;

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

    public static List<PowerConsumerComponentIntegration> Instances = new();

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

    protected override void Awake()
    {
      base.Awake();
    }

    public List<Character> charactersWithinActivatorZone = new();

    public virtual void OnCollisionEnter(Collision other) {}
    public virtual void OnCollisionExit(Collision other) {}

    protected override void Start()
    {
      this.WaitForZNetView((nv) =>
      {
        base.Start();
        var zdo = nv.GetZDO();
        if (!PowerZDONetworkManager.TryGetData(zdo, out PowerConsumerData data, true))
        {
          LoggerProvider.LogWarning("[PowerConsumerComponentIntegration] Failed to get PowerConsumerData from PowerZDONetworkManager.");
          return;
        }
        Zdoid = zdo.m_uid;
        Data = data;
        Data.Load();

        PowerNetworkController.RegisterPowerComponent(this);
        PowerZDONetworkManager.RegisterPowerComponentUpdater(zdo.m_uid, data);
      });
    }

    protected override void OnDestroy()
    {
      if (Zdoid.HasValue)
      {
        PowerZDONetworkManager.RemovePowerComponentUpdater(Zdoid.Value);
      }
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