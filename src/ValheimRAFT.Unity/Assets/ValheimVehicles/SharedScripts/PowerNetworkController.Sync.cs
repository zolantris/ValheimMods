// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public partial class PowerNetworkController
  {
    /// <summary>
    /// This is for integrations. The INetworkedComponent will be extended from.
    /// </summary>
    /// <param name="nodes"></param>
    public void SyncNetworkState(List<IPowerNode> nodes)
    {
      foreach (var node in nodes)
      {
        if (node is INetworkedComponent netComp)
        {
          netComp.UpdateNetworkedData();
        }
      }
    }

    public void SyncNetworkStateClient(List<IPowerNode> nodes)
    {
      foreach (var node in nodes)
      {
        if (node is INetworkedComponent netComp)
        {
          netComp.SyncNetworkedData();
        }
      }
    }
  }
}