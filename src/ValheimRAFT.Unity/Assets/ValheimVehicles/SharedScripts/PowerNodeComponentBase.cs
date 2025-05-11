// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public abstract class PowerNodeComponentBase : MonoBehaviour, IPowerNode
  {
    [SerializeField] public Transform connectorPoint;
    protected string networkId = string.Empty;

    protected virtual void Awake()
    {
      AssignConnectorPoint();
      PowerNetworkBootstrapper.Register(this);
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