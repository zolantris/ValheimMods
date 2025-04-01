// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
namespace ValheimVehicles.SharedScripts
{

  public static class LoggerProvider
  {
    public static Action<string> LogError = Debug.Log;
    public static Action<string> LogMessage = Debug.Log;
    public static Action<string> LogDebug = Debug.Log;
  }
}