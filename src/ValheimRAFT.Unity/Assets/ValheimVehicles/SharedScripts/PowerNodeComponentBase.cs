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
    [SerializeField] public Transform connectorPointTransform;
    protected string networkId = string.Empty;
    [SerializeField] public bool canSelfRegisterToNetwork = false;

    protected virtual void Awake()
    {
      LoggerProvider.LogInfo($"[PowerNodeComponentBase] Awake on {name} ({gameObject.GetInstanceID()})");
      AssignConnectorPoint();
    }

    public Vector3 ConnectorPoint => connectorPointTransform.position;

    public string NetworkId
    {
      get => networkId;
      set => networkId = value;
    }

    public virtual Vector3 Position => connectorPointTransform ? ConnectorPoint : transform.position;
    public abstract bool IsActive { get; }

    public virtual void AssignConnectorPoint()
    {
      if (!connectorPointTransform)
      {
        connectorPointTransform = transform.Find("power_connector");
      }
      if (!connectorPointTransform)
        connectorPointTransform = transform;
    }

    public virtual void SetNetworkId(string id)
    {
      networkId = id;
    }
  }
}