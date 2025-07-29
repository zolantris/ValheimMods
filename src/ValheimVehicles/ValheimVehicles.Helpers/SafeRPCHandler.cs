using System;
using System.Collections.Generic;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;
namespace ValheimVehicles.Helpers;

public class SafeRPCHandler : INetView
{
  private readonly HashSet<string> _registeredRpcs = new();
  public ZNetView? m_nview { get; set; }
  public SafeRPCHandler(ZNetView netView)
  {
    if (netView == null)
    {
      LoggerProvider.LogError("InitRPCHandler attempted to init with null netView");
      return;
    }
    m_nview = netView;
  }

  public void Register<T>(string name, Action<long, T> method)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (netView.m_functions.ContainsKey(name.GetStableHashCode())) return;
    netView.Register(name, method);
    _registeredRpcs.Add(name);
  }

  public void Register(string name, Action<long> method)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (netView.m_functions.ContainsKey(name.GetStableHashCode())) return;

    netView.Register(name, method);
    _registeredRpcs.Add(name);
  }

  public void UnregisterAll()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    foreach (var rpc in _registeredRpcs)
    {
      if (!netView.m_functions.ContainsKey(rpc.GetStableHashCode())) return;
      netView.Unregister(rpc);
    }
    _registeredRpcs.Clear();
  }

  public void Unregister(string name)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (!netView.m_functions.ContainsKey(name.GetStableHashCode())) return;
    netView.Unregister(name);
    _registeredRpcs.Remove(name);
  }

  public bool IsRegistered(string name)
  {
    return _registeredRpcs.Contains(name);
  }

  /// <summary>
  /// Overload
  /// </summary>
  public void InvokeRPC(Action callback, params object[] args)
  {
    InvokeRPC(ZRoutedRpc.Everybody, callback, args);
  }

  /// <summary>
  /// Overload
  /// </summary>
  public void InvokeRPC(string rpcName, params object[] args)
  {
    InvokeRPC(ZRoutedRpc.Everybody, rpcName, args);
  }

  /// <summary>
  /// Overload allowing sending in a callback instead of always having to cast to a string.
  /// </summary>
  public void InvokeRPC(long senderId, Action callback, params object[] args)
  {
    var callbackName = callback.ToString();
    InvokeRPC(senderId, callbackName, args);
  }

  /// <summary>
  /// Original method.
  /// </summary>
  public void InvokeRPC(long senderId, string rpcName, params object[] args)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (!IsRegistered(rpcName)) return;
    netView.InvokeRPC(senderId, rpcName, args);
  }
}