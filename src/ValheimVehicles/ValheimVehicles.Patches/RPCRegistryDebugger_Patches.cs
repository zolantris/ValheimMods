using System.Collections.Generic;
using HarmonyLib;
using ValheimVehicles.RPC;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Patches;

public class RPCRegistryDebugger_Patches
{
#if DEBUG
  private static string lastRoutedRPCName = "";
  private static readonly Dictionary<int, string> allRPCHashIdsToNames = new();

  [HarmonyPatch(typeof(ZRoutedRpc), "InvokeRoutedRPC",
    typeof(long), typeof(ZDOID), typeof(string), typeof(object[]))]
  [HarmonyPostfix]
  public static void ZRoutedRpc_ZNet_InvokeRoutedRPC(long targetPeerID, ZDOID targetZDO, string methodName, object[] parameters)
  {
    var methodHash = methodName.GetStableHashCode();
    if (!allRPCHashIdsToNames.TryGetValue(methodHash, out _))
    {
      allRPCHashIdsToNames[methodHash] = methodName;
    }
    lastRoutedRPCName = methodName;
  }

  [HarmonyPatch(typeof(ZNetView), "HandleRoutedRPC")]
  [HarmonyPostfix]
  private static void ZNetView_HandleRoutedRPC_InjectRPC(ZNetView __instance, ZRoutedRpc.RoutedRPCData rpcData)
  {
    if (rpcData != null && !__instance.m_functions.TryGetValue(rpcData.m_methodHash, out _))
    {
      if (RPCManager.RPCHashIdsToHashNames.TryGetValue(rpcData.m_methodHash, out var hashName))
      {
        LoggerProvider.LogDebug($"Detected missing registered RPC method for {hashName}");
      }
      else if (allRPCHashIdsToNames.TryGetValue(rpcData.m_methodHash, out var methodName))
      {
        LoggerProvider.LogDebug($"Detected missing registered RPC method for {methodName}");
      }
      else if (lastRoutedRPCName != "")
      {
        LoggerProvider.LogDebug($"Detected missing registered RPC method for lastRoutedRPC: {lastRoutedRPCName}");
      }
    }
  }
#endif
}