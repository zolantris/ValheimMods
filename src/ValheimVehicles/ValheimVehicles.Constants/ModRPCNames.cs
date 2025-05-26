using System;
namespace ValheimVehicles.Constants;

public static class ModRPCNames
{
  public static class RPCUtils
  {
    public static string GetRPCPrefix(string name)
    {
      return $"{ValheimVehiclesPlugin.ModName}_{name}";
    }
    public static string GetRPCPrefix(Action method)
    {
      return $"{ValheimVehiclesPlugin.ModName}_{nameof(method)}";
    }
  }

  public static string SyncConfigKeys = RPCUtils.GetRPCPrefix("RPC_SyncConfigKeys");
}