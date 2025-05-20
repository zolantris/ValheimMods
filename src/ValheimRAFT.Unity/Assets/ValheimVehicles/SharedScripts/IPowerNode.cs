#region

  using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts.PowerSystem
  {
    /// <summary>
    /// All properties must be thread safe and settable by a ZDO/or other controller property.
    /// </summary>
    public interface IPowerNode
    {
      string NetworkId { get; }
      Vector3 Position { get; }
      bool IsActive { get; }
      Vector3 ConnectorPoint { get; }
      void SetNetworkId(string id);
    }
  }