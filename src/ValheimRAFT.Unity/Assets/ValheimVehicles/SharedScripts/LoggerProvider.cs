using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace ValheimVehicles.SharedScripts
{
    public static class LoggerProvider
    {
        public static LogLevel ActiveLogLevel = LogLevel.All;

        private static ManualLogSource Logger;

        private static readonly Dictionary<string, string> _callerCache = new();

        public static void Setup(ManualLogSource logger)
        {
            Logger = logger;
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
            if (ActiveLogLevel == LogLevel.All) return true;
            if (ActiveLogLevel == LogLevel.None) return false;
            return level <= ActiveLogLevel;
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
    }
}
