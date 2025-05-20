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
    [SerializeField] public Transform connectorPoint;
    protected string networkId = string.Empty;
    [SerializeField] public bool canSelfRegisterToNetwork = false;

    protected virtual void Awake()
    {
      LoggerProvider.LogInfo($"[PowerNodeComponentBase] Awake on {name} ({gameObject.GetInstanceID()})");
      AssignConnectorPoint();
    }

    public Transform ConnectorPoint => connectorPoint;

    public string NetworkId
    {
      get => networkId;
      set => networkId = value;
    }

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

    public virtual void SetNetworkId(string id)
    {
      networkId = id;
    }
  }
}