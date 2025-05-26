// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Reflection;
using UnityEngine;

namespace ValheimVehicles.Debugging
{
  public static class RpcDebugHelper
  {
    /// <summary>
    /// Logs the method name corresponding to a StableHashCode.
    /// </summary>
    public static void LogMethodFromHash(int hashCode)
    {
      foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
      {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                               BindingFlags.Instance | BindingFlags.Static |
                                               BindingFlags.DeclaredOnly))
        {
          if (method.Name.GetStableHashCode() == hashCode)
          {
            Debug.LogWarning($"[RPC DEBUG] Hash {hashCode} = {type.FullName}.{method.Name}");
            return;
          }
        }
      }

      Debug.LogWarning($"[RPC DEBUG] No method found for hash {hashCode}");
    }

    /// <summary>
    /// Logs all RPC-like methods with their hash, for auditing.
    /// </summary>
    public static void DumpAllMethodHashes()
    {
      foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
      {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                               BindingFlags.Instance | BindingFlags.Static |
                                               BindingFlags.DeclaredOnly))
        {
          var hash = method.Name.GetStableHashCode();
          Debug.Log($"[RPC HASH] {hash} = {type.FullName}.{method.Name}");
        }
      }
    }

    /// <summary>
    /// Matches Valheim's GetStableHashCode implementation.
    /// </summary>
    public static int GetStableHashCode(this string str)
    {
      unchecked
      {
        var hash = 23;
        foreach (var c in str)
          hash = hash * 31 + c;
        return hash;
      }
    }
  }
}