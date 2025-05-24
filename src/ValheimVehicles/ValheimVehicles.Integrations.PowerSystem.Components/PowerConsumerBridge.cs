// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

namespace ValheimVehicles.Integrations
{
  public class PowerConsumerBridge :
    PowerNetworkDataEntity<PowerConsumerBridge, PowerConsumerComponent, PowerConsumerData>,
    IPowerConsumer
  {
    public string NetworkId => Logic.NetworkId;
    public Vector3 Position => transform.position;
    public Vector3 ConnectorPoint => transform.position;
    public bool IsActive => Logic.IsActive;
    public bool IsDemanding => Logic.IsDemanding;
    public bool IsPowerDenied => Logic.IsPowerDenied;

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

    public static List<PowerConsumerBridge> Instances = new();

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

#if DEBUG
    public virtual void OnCollisionEnter(Collision other) {}
    public virtual void OnCollisionExit(Collision other) {}

    protected override void Start()
    {
      base.Start();
    }

    protected override void OnDestroy()
    {
      base.OnDestroy();
    }
#endif

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