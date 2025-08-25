// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{

  /// <summary>
  /// For integration within valheim-raft. This component is extended.
  /// </summary>
  public abstract class WearNTearProviderBase : MonoBehaviour
  {
    // The main resolver for WearNTearStub this will be updated by the integration layer. 
    public static Func<GameObject?, IWearNTearStub?> GetWearNTearComponent = gameObject =>
    {
      if (gameObject == null) return null;
#if VALHEIM
      Debug.Log("WearNTearProviderBase.Resolver default method called. This should never happen outside of unity Environment");
#endif

      // this will be replaced with integration which would do a component check for WearNTear then return this interface.
      return gameObject.GetComponent<IWearNTearStub>();
    };
  }
}