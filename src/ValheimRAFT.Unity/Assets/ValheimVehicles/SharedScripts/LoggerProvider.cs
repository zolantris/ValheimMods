using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug; // Minimal UnityEngine pull

namespace ValheimVehicles.SharedScripts
{
    public static class LoggerProvider
    {
        public static LogLevel ActiveLogLevel = LogLevel.All;

        private static ManualLogSource? Logger;
        private static LogLevel GlobalLogLevel = LogLevel.All;

        private static bool _hasInitialized = false;
        private static readonly Dictionary<string, string> _callerCache = new();

        public static void Setup(ManualLogSource? logger)
        {
            if (_hasInitialized)
            {
                Debug.LogWarning("LoggerProvider: Setup called more than once. Ignoring duplicate initialization.");
                return;
            }

            _hasInitialized = true;

            Logger = logger ?? new ManualLogSource("ValheimVehiclesLoggerProviderFallback");
            GlobalLogLevel = ReadBepInExConsoleLogLevel();

            LogInfo($"Logger initialized. Plugin Level: {ActiveLogLevel}, Global Level: {GlobalLogLevel}");
        }

        public static void LogError(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Error)) return;
            SafeLog(LogLevel.Error, Format("Error", val, file, line));
        }

        public static void LogWarning(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Warning)) return;
            SafeLog(LogLevel.Warning, Format("Warning", val, file, line));
        }

        [Conditional("DEBUG")]
        public static void LogDev(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Debug)) return;
            SafeLog(LogLevel.Debug, Format("Dev", val, file, line));
        }

        public static void LogDebug(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Debug)) return;
            SafeLog(LogLevel.Debug, Format("Debug", val, file, line));
        }

        public static void LogMessage(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Message)) return;
            SafeLog(LogLevel.Message, Format("Message", val, file, line));
        }

        public static void LogInfo(string val,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!IsLevelEnabled(LogLevel.Info)) return;
            SafeLog(LogLevel.Info, Format("Info", val, file, line));
        }

        private static bool IsLevelEnabled(LogLevel level)
        {
            if (ActiveLogLevel == LogLevel.None || GlobalLogLevel == LogLevel.None) return false;
            if (ActiveLogLevel == LogLevel.All && GlobalLogLevel == LogLevel.All) return true;

            return level <= ActiveLogLevel && level <= GlobalLogLevel;
        }

        private static void SafeLog(LogLevel level, string message)
        {
            try
            {
                if (Logger != null)
                {
                    switch (level)
                    {
                        case LogLevel.Fatal:
                            Logger.LogFatal(message);
                            break;
                        case LogLevel.Error:
                            Logger.LogError(message);
                            break;
                        case LogLevel.Warning:
                            Logger.LogWarning(message);
                            break;
                        case LogLevel.Message:
                            Logger.LogMessage(message);
                            break;
                        case LogLevel.Info:
                            Logger.LogInfo(message);
                            break;
                        case LogLevel.Debug:
                            Logger.LogDebug(message);
                            break;
                        default:
                            Logger.LogMessage(message);
                            break;
                    }
                }
                else
                {
                    Debug.LogWarning(message); // Unity fallback if no logger
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LoggerProvider: Failed to log message. Exception: {e.Message}\nOriginalMessage: {message}");
            }
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
                var fileName = Path.GetFileName(file);

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
                Debug.LogWarning($"LoggerProvider: Failed to read BepInEx.cfg console level. Defaulting to All. Exception: {e.Message}");
            }

            return LogLevel.All;
        }
    }
}
