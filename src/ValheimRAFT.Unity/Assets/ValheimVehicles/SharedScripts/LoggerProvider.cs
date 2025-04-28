using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace ValheimVehicles.SharedScripts
{
    public static class LoggerProvider
    {
        public static LogLevel ActiveLogLevel = LogLevel.All;

        private static ManualLogSource Logger;
        private static LogLevel GlobalLogLevel = LogLevel.All;

        private static readonly Dictionary<string, string> _callerCache = new();

        public static void Setup(ManualLogSource? logger)
        {
            logger ??= new ManualLogSource("ValheimVehicles");
            Logger = logger;
            GlobalLogLevel = ReadBepInExConsoleLogLevel();

            LogInfo($"Logger initialized. Plugin Level: {ActiveLogLevel}, Global Level: {GlobalLogLevel}");
        }

        public static void LogError(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Error)) return;
            Logger.LogError(Format("Error", val, file, line));
        }

        public static void LogWarning(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Warning)) return;
            Logger.LogWarning(Format("Warning", val, file, line));
        }

        [Conditional("DEBUG")]
        public static void LogDev(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Debug)) return;
            Logger.LogDebug(Format("Dev", val, file, line));
        }

        public static void LogDebug(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Debug)) return;
            Logger.LogDebug(Format("Debug", val, file, line));
        }

        public static void LogMessage(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Message)) return;
            Logger.LogMessage(Format("Message", val, file, line));
        }

        public static void LogInfo(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Info)) return;
            Logger.LogInfo(Format("Info", val, file, line));
        }

        private static bool IsLevelEnabled(LogLevel level)
        {
            if (ActiveLogLevel == LogLevel.None || GlobalLogLevel == LogLevel.None) return false;
            if (ActiveLogLevel == LogLevel.All && GlobalLogLevel == LogLevel.All) return true;

            return level <= ActiveLogLevel && level <= GlobalLogLevel;
        }

        private static string Format(string logType, string message, string file, int line)
        {
            var callerKey = $"{file}:{line}";

            if (!_callerCache.TryGetValue(callerKey, out var callerInfo))
            {
                var stackTrace = new StackTrace(2, true);
                var frame = stackTrace.GetFrame(0);
                var method = frame?.GetMethod();
                var declaringType = method?.DeclaringType;
                var fullTypeName = declaringType?.FullName ?? "UnknownType";
                var methodName = method?.Name ?? "UnknownMethod";
                var fileName = System.IO.Path.GetFileName(file);

                callerInfo = $"[{fullTypeName}.{fileName}:{line} ({methodName})]";
                _callerCache[callerKey] = callerInfo;
            }

            return $"{logType}:{callerInfo} {message}";
        }

        private static LogLevel ReadBepInExConsoleLogLevel()
        {
            try
            {
                var configPath = Path.Combine(Paths.ConfigPath, "BepInEx.cfg");
                if (!File.Exists(configPath))
                {
                    return LogLevel.All;
                }

                var configFile = new ConfigFile(configPath, true);
                if (configFile.TryGetEntry("Logging", "ConsoleDisplayedLevel", out ConfigEntry<string> consoleLevelEntry))
                {
                    if (Enum.TryParse(consoleLevelEntry.Value, true, out LogLevel parsedLevel))
                    {
                        return parsedLevel;
                    }
                }
            }
            catch (Exception e)
            {
                Logger?.LogWarning($"LoggerProvider: Failed to read BepInEx.cfg console level. Defaulting to All. Exception: {e.Message}");
            }

            return LogLevel.All;
        }
    }
}
