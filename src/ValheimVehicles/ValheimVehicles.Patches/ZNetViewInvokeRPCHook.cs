// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Debugging;

#if DEBUG
namespace ValheimVehicles.Patches
{
  [HarmonyPatch]
  public static class ZNetViewInvokeRPCHook
  {
    private static readonly FieldInfo ZRpcField =
      typeof(ZNetView).GetField("m_zrpc", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo RpcFunctionsField =
      typeof(ZRpc).GetField("m_functions", BindingFlags.NonPublic | BindingFlags.Instance);

    [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.InvokeRPC), typeof(string), typeof(object[]))]
    [HarmonyPrefix]
    public static void InvokeRPC_StringPrefix(ZNetView __instance, string methodName)
    {
      if (ZRpcField?.GetValue(__instance) is not ZRpc zrpc) return;
      if (RpcFunctionsField?.GetValue(zrpc) is not IDictionary<int, object> functions) return;

      var hash = methodName.GetStableHashCode();
      if (!functions.ContainsKey(hash))
      {
        Debug.LogWarning(
          $"[RPC WARNING] Local RPC '{methodName}' (Hash={hash}) not registered on {__instance.gameObject.name}");

        RpcDebugHelper.LogMethodFromHash(hash);
      }
    }

    [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.InvokeRPC), typeof(ZRpc), typeof(string), typeof(object[]))]
    [HarmonyPrefix]
    public static void InvokeRPC_RemotePrefix(ZNetView __instance, ZRpc rpc, string methodName)
    {
      if (RpcFunctionsField?.GetValue(rpc) is not IDictionary<int, object> functions) return;

      var hash = methodName.GetStableHashCode();
      if (!functions.ContainsKey(hash))
      {
        Debug.LogWarning(
          $"[RPC WARNING] Remote RPC '{methodName}' (Hash={hash}) not registered for {__instance.gameObject.name}");

        RpcDebugHelper.LogMethodFromHash(hash);
      }
    }
  }
}
#endif