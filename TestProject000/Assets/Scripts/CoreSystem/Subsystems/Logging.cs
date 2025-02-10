using System;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Text;

using static CoreSystem.CoreSystemUtils;

namespace CoreSystem {
    
    [Flags] public enum LogCategory: uint {
        Unknown    = 0,
        Unity      = 1 << 0,
        CoreSystem = 1 << 1,
        Project    = 1 << 2,
        External   = 1 << 3,
    }

    public enum LogLevel {
        Info = 0, Warning = 1, Error = 2
    }

    public static partial class Logging {
        public static void grabInstances() {
            coreSystemInstance   = CoreSystem.Instance;
            if (!coreSystemInstance) {
                Debug.LogWarning("[logging] No CoreSystem instance!");
                return;
            }
            debugConsoleInstance = coreSystemInstance.DebugConsole;
            debugStatsInstance   = coreSystemInstance.DebugStats;
        }
        
        static CoreSystem   coreSystemInstance;
        static DebugConsole debugConsoleInstance;
        static DebugStats   debugStatsInstance;

        static StringBuilder stringBuilder = new(capacity: 100);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string buildString(params string[] args) {
            stringBuilder.Clear();
            foreach (var arg in args) stringBuilder.Append(arg);
            return stringBuilder.ToString();
        }

        public static LogCategory figureOutCategoryBasedOnCallingInfo(string callerFilePath) {
            // TODO: really?
            if (callerFilePath.Contains("CoreSystem")) return LogCategory.CoreSystem;
            
            var type = getTypeFromFilePath(callerFilePath);
            if (type != null) {
                var assemblyName = type.Assembly.ManifestModule.Name;
                var assemblyPath = type.Assembly.ManifestModule.FullyQualifiedName;

                // CoreSystem:
                // if (assemblyName.StartsWith("CoreSystem")) return LogCategory.CoreSystem;
                
                // Unity/system:
                if (assemblyName.StartsWith(DebugConsole.assemblyPathIgnoreListFileNameStartsWith)) return LogCategory.Unity;
                if (assemblyPath.Contains(DebugConsole.assemblyPathIgnoreListPathContains))         return LogCategory.Unity;
                
                // TODO: TEMP: Assume project otherwise
                return LogCategory.Project;
            }

            return LogCategory.Unknown;
        }

        static bool preCoreSystemInitLoggingWarning = false;
        static void log_main(LogCategory category, LogLevel level, string text, CallerDebugInfo callerInfo) {
            // If you attempt logging before CoreSystem is initialized, force stuff to go to Unity instead:
            if (!coreSystemInstance && !preCoreSystemInitLoggingWarning) {
                Debug.LogWarning("Attempted to log before CoreSystem was initialized. This and further log attempts will be forced to LogCategory.Unity, until CoreSystem is initialized.");
                preCoreSystemInitLoggingWarning = true;
                category = LogCategory.Unity;
            }
            
            // Pre-process:
            // Figure out the logging category:
            category = figureOutCategoryBasedOnCallingInfo(callerInfo.callerFilePath);
            
            // Send:
            switch (category) {
                case LogCategory.Unity: {
                    sendToUnity(text, category, level);
                    break;
                }
                default: {
                    sendToCoreSystem(text, category, level, callerInfo);
                    
                    // Log to Unity console as well (temporarily disable receiving messages from Unity)
                    // @Performance
#if UNITY_EDITOR
                    var previousValue = CoreSystem.UNITY_receiveLogMessages;
                    CoreSystem.UNITY_receiveLogMessages = false;
                    sendToUnity(text, category, level); // TODO: friendlier category name?
                    CoreSystem.UNITY_receiveLogMessages = previousValue;
#endif

                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void sendToCoreSystem(string text, LogCategory category, LogLevel level, CallerDebugInfo callerInfo) {
            // TEMP:
            text = buildString("[", Enum.GetName(typeof(LogCategory), category), "] ", text);
            
            // Debug console:
            if (debugConsoleInstance) debugConsoleInstance.pushText(text, category, level);

            // Debug stats:
            // TODO: log level support in quickstats! Maybe we just want to read console lines, to not duplicate effort (coloring based on level would be already done for us).
            if (debugStatsInstance) debugStatsInstance.quicklinePush(text, callerInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void sendToUnity(string text, LogCategory category, LogLevel level) {
            text = buildString("[", Enum.GetName(typeof(LogCategory), category), "] ", text);
            
            if      (level == LogLevel.Info)    Debug.Log(text);
            else if (level == LogLevel.Warning) Debug.LogWarning(text);
            else if (level == LogLevel.Error)   Debug.LogError(text);
        }
    }

    partial class Logging {
        // Log:

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void log(
            string text,
            [CallerFilePath]   string callerFilePath = null,
            [CallerMemberName] string callerProcName = null,
            [CallerLineNumber] int    callerLineNum  = -1) {
            log_main(LogCategory.Unknown, LogLevel.Info, text, new CallerDebugInfo(callerFilePath, callerProcName, callerLineNum));
        }

        // Stats:

        // Sections:
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void STATS_PrintLine(string component, string text) {
            debugStatsInstance?.pushToStatsDB(component, text);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void STATS_PrintLine(string text, [CallerFilePath]   string callerFilePath = null,
                                                        [CallerMemberName] string callerProcName = null,
                                                        [CallerLineNumber] int    callerLineNum  = -1) {
            debugStatsInstance?.pushToStatsDB(component: callerFilePath, text, append: false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void STATS_Append(string text, [CallerFilePath]   string callerFilePath = null,
                                                     [CallerMemberName] string callerProcName = null,
                                                     [CallerLineNumber] int    callerLineNum  = -1) {
            debugStatsInstance?.pushToStatsDB(component: callerFilePath, text, append: true);
        }

        // Quicklines:
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void STATS_PrintQuickLine(string text, CallerDebugInfo callerInfo) {
            debugStatsInstance?.quicklinePush(text, callerInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void STATS_PrintQuickLine(string text, [CallerFilePath]   string callerFilePath = null,
                                                             [CallerMemberName] string callerProcName = null,
                                                             [CallerLineNumber] int    callerLineNum  = -1) {
            debugStatsInstance?.quicklinePush(text, new(callerFilePath, callerProcName, callerLineNum));
        }
    }

}