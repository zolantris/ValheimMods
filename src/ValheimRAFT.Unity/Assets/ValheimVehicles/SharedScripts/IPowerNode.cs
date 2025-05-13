#region

  using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts.PowerSystem
  {
    public interface IPowerNode
    {
      string NetworkId { get; }
      Vector3 Position { get; }
      bool IsActive { get; }
      Transform ConnectorPoint { get; }
      void SetNetworkId(string id);
    }
  }