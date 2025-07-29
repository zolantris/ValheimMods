// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;

namespace ValheimVehicles.Integrations
{
  public static class ZPackageUtil
  {
    /// <summary>
    /// Serialize an ISerializableConfig instance to a ZPackage.
    /// </summary>
    public static ZPackage Serialize<T, TInterface>(ISerializableConfig<T, TInterface> config)
      where T : ISerializableConfig<T, TInterface>, new()
    {
      var pkg = new ZPackage();
      try
      {
        config.Serialize(pkg);
      }
      catch (Exception ex)
      {
        LoggerProvider.LogError($"[ZPackageUtil] Failed to serialize {typeof(T).Name}: {ex.Message}");
      }

      return pkg;
    }

    public static byte[] SerializeToBytes<T, TInterface>(ISerializableConfig<T, TInterface> config)
      where T : ISerializableConfig<T, TInterface>, new()
    {
      var pkg = new ZPackage();
      try
      {
        config.Serialize(pkg);
      }
      catch (Exception ex)
      {
        LoggerProvider.LogError($"[ZPackageUtil] Failed to serialize {typeof(T).Name}: {ex.Message}");
      }

      return pkg.GetArray(); // ✅ actual byte[] array
    }
    /// <summary>
    /// Safely attempts to deserialize a type from a ZPackage using its Deserialize method.
    /// </summary>
    public static T Deserialize<T>(ZPackage pkg) where T : new()
    {
      try
      {
        var instance = new T();
        if (instance is ISerializableConfig<T, object> serializable)
        {
          return serializable.Deserialize(pkg);
        }

        LoggerProvider.LogError($"[ZPackageUtil] Type {typeof(T).Name} does not implement ISerializableConfig");
        return new T();
      }
      catch (Exception ex)
      {
        LoggerProvider.LogError($"[ZPackageUtil] Failed to deserialize {typeof(T).Name}: {ex.Message}");
        return new T();
      }
    }
  }

  public static class ConfigSyncUtility
  {
    /// <summary>
    /// Sends the local config snapshot to the server and asks it to validate it before applying mutation.
    /// If config is out-of-sync, the server will re-send the true config to the client.
    /// </summary>
    public static void RequestValidatedMutation<TConfig, TInterface>(
      IPrefabCustomConfigRPCSync<TConfig> configSync,
      TInterface controller,
      string rpcName)
      where TConfig : ISerializableConfig<TConfig, TInterface>, new()
    {
      if (!configSync.IsNetViewValid(out var netView)) return;

      var proposed = new TConfig();
      proposed.ApplyFrom(controller);

      var pkg = new ZPackage();
      proposed.Serialize(pkg);

      netView.InvokeRPC(netView.GetZDO().GetOwner(), rpcName, pkg);
    }

    /// <summary>
    /// Sends config + payload to the server and lets the server validate then apply mutation.
    /// </summary>
    public static void RequestValidatedMutationWithPayload<TConfig, TInterface>(
      IPrefabCustomConfigRPCSync<TConfig> configSync,
      TInterface controller,
      string rpcName,
      Action<ZPackage> writePayload)
      where TConfig : ISerializableConfig<TConfig, TInterface>, new()
    {
      if (!configSync.IsNetViewValid(out var netView)) return;

      var proposed = new TConfig();
      proposed.ApplyFrom(controller);

      var pkg = new ZPackage();
      proposed.Serialize(pkg);
      writePayload(pkg);

      netView.InvokeRPC(netView.GetZDO().GetOwner(), rpcName, pkg);
    }

    /// <summary>
    /// On the server: compares proposed config with current config. If mismatched, re-syncs client.
    /// If matched, calls the mutation action and commits the config.
    /// </summary>
    public static void HandleValidatedMutation<TConfig, TInterface>(
      IPrefabCustomConfigRPCSync<TConfig> configSync,
      ZNetView netView,
      long sender,
      ZPackage pkg,
      Func<TConfig, TConfig, bool> comparer,
      Action<TConfig> mutation)
      where TConfig : ISerializableConfig<TConfig, TInterface>, new()
    {
      if (!netView.IsOwner()) return;

      var proposed = new TConfig();
      proposed.Deserialize(pkg);
      var current = configSync.Config;

      if (!comparer(proposed, current))
      {
        LoggerProvider.LogWarning("[ConfigSync] Mismatch in proposed config; re-syncing client");
        netView.InvokeRPC(sender, nameof(configSync.RPC_Load));
        return;
      }

      mutation(current);
      configSync.CommitConfigChange(current);
    }

    /// <summary>
    /// On the server: same as HandleValidatedMutation, but reads extra payload to perform the mutation.
    /// </summary>
    public static void HandleValidatedMutationWithPayload<TConfig, TInterface>(
      IPrefabCustomConfigRPCSync<TConfig> configSync,
      ZNetView netView,
      long sender,
      ZPackage pkg,
      Func<TConfig, TConfig, bool> comparer,
      Action<TConfig, ZPackage> mutation)
      where TConfig : ISerializableConfig<TConfig, TInterface>, new()
    {
      if (!netView.IsOwner()) return;

      var proposed = new TConfig();
      proposed.Deserialize(pkg);
      var current = configSync.Config;

      if (!comparer(proposed, current))
      {
        LoggerProvider.LogWarning("[ConfigSync] Payload mutation failed validation, resyncing...");
        netView.InvokeRPC(sender, nameof(configSync.RPC_Load));
        return;
      }

      mutation(current, pkg);
      configSync.CommitConfigChange(current);
    }

    /// <summary>
    /// Default comparer using serialized binary comparison.
    /// </summary>
    public static bool DefaultComparer<T, TInterface>(T a, T b)
      where T : ISerializableConfig<T, TInterface>, new()
    {
      if (a == null || b == null) return ReferenceEquals(a, b);

      // Prefer hash comparison if implemented
      var hashMethod = typeof(T).GetMethod("GetStableHashCode", Type.EmptyTypes);
      if (hashMethod != null && hashMethod.ReturnType == typeof(int))
      {
        var hashA = (int)hashMethod.Invoke(a, null);
        var hashB = (int)hashMethod.Invoke(b, null);
        return hashA == hashB;
      }

      // Fallback to byte serialization comparison
      LoggerProvider.LogInfo("Using fallback byte serialization comparison for config");
      var bytesA = ZPackageUtil.SerializeToBytes<T, TInterface>(a);
      var bytesB = ZPackageUtil.SerializeToBytes<T, TInterface>(b);
      return StructuralComparisons.StructuralEqualityComparer.Equals(bytesA, bytesB);
    }
  }
}