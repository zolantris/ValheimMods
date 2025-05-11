// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public abstract class PowerNodeComponentBase : MonoBehaviour, IPowerNode
  {
    public static readonly List<PowerNodeComponentBase> Instances = new();
    [SerializeField] public Transform connectorPoint;
    protected string networkId = string.Empty;

    protected virtual void Awake()
    {
      Instances.Add(this);

      AssignConnectorPoint();
      if (!PowerNetworkController.Instance)
      {
        gameObject.AddComponent<PowerNetworkController>();
      }

      if (PowerNetworkController.Instance)
      {
        PowerNetworkController.Instance.RegisterNode(this);
      }
    }

    protected virtual void OnDestroy()
    {
      Instances.Remove(this);
    }

    public Transform ConnectorPoint => connectorPoint;

    public string NetworkId => networkId;
    public virtual Vector3 Position => connectorPoint ? connectorPoint.position : transform.position;
    public abstract bool IsActive { get; }

    public virtual void AssignConnectorPoint()
    {
      if (!connectorPoint)
      {
        connectorPoint = transform.Find("power_connector");
      }
      if (!connectorPoint)
        connectorPoint = transform;
    }

    public virtual void SetNetworkId(string id) => networkId = id;
  }
}