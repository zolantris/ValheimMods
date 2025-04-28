// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
namespace ValheimVehicles.SharedScripts
{

  public static class LoggerProvider
  {
    public static void Setup(Action<string> errorAction, Action<string> warningAction, Action<string> debugAction, Action<string> infoAction, Action<string> messageAction)
    {
      LogErrorAction = errorAction;
      LogMessageAction = messageAction;
      LogDebugAction = debugAction;
      LogWarningAction = warningAction;
      LogInfoAction = infoAction;
    }

    private static Action<string> LogErrorAction = Debug.Log;
    private static Action<string> LogWarningAction = Debug.Log;
    private static Action<string> LogMessageAction = Debug.Log;
    private static Action<string> LogInfoAction = Debug.Log;
    private static Action<string> LogDebugAction = Debug.Log;

    public static void LogError(string val)
    {
      LogErrorAction(val);
    }
    public static void LogWarning(string val)
    {
      LogWarningAction(val);
    }

    [Conditional("DEBUG")]
    public static void LogDev(string val)
    {
      LogDebugAction(val);
    }
    
    public static void LogDebug(string val)
    {
      LogDebugAction(val);
    }
    public static void LogMessage(string val)
    {
      LogMessageAction(val);
    }

    public static void LogInfo(string val)
    {
      LogInfoAction(val);
    }
  }
}