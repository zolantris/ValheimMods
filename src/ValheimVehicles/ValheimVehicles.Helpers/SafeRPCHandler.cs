using System;
using System.Collections.Generic;
namespace ValheimVehicles.Helpers;

public class SafeRPCHandler(
  ZNetView netView)
{
  private readonly HashSet<string> _registeredRpcs = new();

  public void Register<T>(string name, Action<long, T> method)
  {
    if (_registeredRpcs.Contains(name)) return;
    netView.Register(name, method);
    _registeredRpcs.Add(name);
  }

  public void Register(string name, Action<long> method)
  {
    if (netView == null) return;
    if (_registeredRpcs.Contains(name)) return;
    netView.Register(name, method);
    _registeredRpcs.Add(name);
  }

  public void UnregisterAll()
  {
    if (netView == null) return;
    foreach (var rpc in _registeredRpcs)
    {
      netView.Unregister(rpc);
    }

    _registeredRpcs.Clear();
  }

  public void Unregister(string name)
  {
    if (!_registeredRpcs.Contains(name)) return;
    netView.Unregister(name);
    _registeredRpcs.Remove(name);
  }
}