// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Integrations
{
  public static class ZDOClaimUtility
  {
    private const string RPCName = "ValheimVehicles_ClaimZDO";
    private static bool _isRegistered;

    // public static void RegisterClaimZdoRpc()
    // {
    //   if (_isRegistered || ZRoutedRpc.instance == null)
    //     return;
    //
    //   if (ZNet.instance.IsServer())
    //   {
    //     ZRoutedRpc.instance.Register<ZDOID>(RPCName, Server_HandleClaim);
    //     LoggerProvider.LogDebug($"[ZDOClaim] Registered RPC handler on server.");
    //   }
    //   else
    //   {
    //     // Dummy client handler to allow sending
    //     ZRoutedRpc.instance.Register<ZDOID>(RPCName, (_, __) => {});
    //     LoggerProvider.LogDebug($"[ZDOClaim] Registered RPC stub on client.");
    //   }
    //
    //   _isRegistered = true;
    // }
    //
    // public static void RequestClaim(ZNetView view)
    // {
    //   if (!view || view.GetZDO() == null)
    //   {
    //     LoggerProvider.LogWarning($"[ZDOClaim] Invalid ZNetView or ZDO.");
    //     return;
    //   }
    //
    //   RequestClaim(view.GetZDO().m_uid);
    // }
    //
    // public static void RequestClaim(ZDOID id)
    // {
    //   if (ZNet.instance == null || ZRoutedRpc.instance == null)
    //     return;
    //
    //   if (ZNet.instance.IsServer())
    //   {
    //     LoggerProvider.LogDebug($"[ZDOClaim] Already server, no RPC needed.");
    //     return; // optional: call Server_HandleClaim directly if local server context
    //   }
    //
    //   ZRoutedRpc.instance.InvokeRoutedRPC(
    //     ZRoutedRpc.instance.GetServerPeerID(),
    //     RPCName,
    //     id
    //   );
    //
    //   LoggerProvider.LogDebug($"[ZDOClaim] Requested server to claim ZDO {id}");
    // }
    //
    // private static void Server_HandleClaim(long sender, ZDOID id)
    // {
    //   if (!ZNet.instance || !ZNet.instance.IsServer())
    //   {
    //     ZLog.LogWarning("[ZDOClaim] Server_HandleClaim called on non-server.");
    //     return;
    //   }
    //
    //   var zdo = ZDOMan.instance.GetZDO(id);
    //   if (zdo == null || !zdo.Persistent)
    //   {
    //     ZLog.LogWarning($"[ZDOClaim] Invalid or non-persistent ZDO {id} from {sender}");
    //     return;
    //   }
    //
    //   if (zdo.GetOwner() != ZDOMan.GetSessionID())
    //   {
    //     zdo.SetOwner(ZDOMan.GetSessionID());
    //   }
    //
    //   if (ZNetScene.instance.FindInstance(id) == null)
    //   {
    //     var prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
    //     if (prefab == null)
    //     {
    //       ZLog.LogError($"[ZDOClaim] Missing prefab for hash {zdo.GetPrefab()}");
    //       return;
    //     }
    //
    //     Object.Instantiate(prefab, zdo.GetPosition(), Quaternion.identity);
    //     ZLog.Log($"[ZDOClaim] Server claimed and instantiated {prefab.name} at {zdo.GetPosition()}");
    //   }
    // }
  }
}