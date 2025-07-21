using System;
using ModSync.Config;

internal static class Logger
{
  public const string ModSyncName = "ModSync";

  private static void Log(string level, string message, ConsoleColor color)
  {
    lock (typeof(Logger)) // thread-safe
    {
      var prevColor = Console.ForegroundColor;
      Console.ForegroundColor = color;
      Console.WriteLine($"{ModSyncName}:{level} -> {message}");
      Console.ForegroundColor = prevColor;
    }
  }

  public static void Error(string message)
  {
    Log("Error", message, ConsoleColor.Red);
  }

  public static void Warn(string message)
  {
    Log("Warn", message, ConsoleColor.Yellow);
  }

  public static void Debug(string message)
  {
    if (!ModSyncConfig.IsVerbose) return;
    Log("Debug", message, ConsoleColor.Green);
  }

  public static void Dev(string message)
  {
#if DEBUG
    Log("Dev", message, ConsoleColor.Green);
#endif
  }
}