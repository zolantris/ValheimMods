using System.Collections.Generic;
using BepInEx;
using Jotunn;

namespace ZdoWatcher;

public static class ZdoVarController
{
  private static readonly Dictionary<string, string> ZdoVariables = new();

  // This is the same UUID hash from ValheimRAFT FYI
  public static readonly int PersistentUidHash =
    "PersistentID".GetStableHashCode();

  public static string NameToVar(BaseUnityPlugin unityPlugin, string key)
  {
    return $"{unityPlugin.name}_{key}";
  }

  public static void RegisterPublicVar(BaseUnityPlugin unityPlugin,
    string key,
    int value)
  {
    var keyId = NameToVar(unityPlugin, key);
    ZdoVariables.Add(keyId, value.ToString());
  }

  public static void RegisterPublicVar(BaseUnityPlugin unityPlugin, string key,
    string value)
  {
    var keyId = NameToVar(unityPlugin, key);
    ZdoVariables.Add(key, value);
  }

  public static void ListPublicVar(bool shouldLog = true)
  {
    if (shouldLog)
    {
      foreach (var keyValuePair in ZdoVariables)
      {
        Logger.LogInfo($"Key: {keyValuePair.Key}, Value: {keyValuePair.Value}");
      }
    }
  }
}