using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem;
using System.IO;
using System;
using UnityEditor;

namespace CoreSystem {

    // TODO: this (along with the fpsInfo local variable below) should probably be part of CoreSystem?
    public struct FPSInfo {
        public int   lastFrameCount;
        public float lastTime;

        public float fpsAccurate;
        public int   fpsRounded; // This should mostly be in parity with the Unity Editor stats window
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DebugStatsSettingsAttribute : Attribute {
        public DebugStatsSettingsAttribute(int priority = 0, string displayName = null, bool startAsEnabled = false) {
            this.priority        = priority;
            this.displayName     = displayName;
            this.startEnabled  = startAsEnabled;
        }
        public int    priority;
        public string displayName;
        public bool   startEnabled;
    }

    [DebugStatsSettings(priority: 9999)]
    public partial class DebugStats : MonoBehaviour {
        static DebugStats GrabInstance() => CoreSystem.Instance?.DebugStats;

        [Header("Components")]
        [SerializeField] TMP_Text      statsTextCom;

        [SerializeField] RectTransform quicklinesContainer;
        [SerializeField] GameObject    quicklineTextPreset; // Whatever is set as the preset will be removed at runtime!

        [Header("Configuration")]
        [SerializeField] float statsUpdateMs       = 1000f;
        [SerializeField] float quicklineTimeoutSec = 5f;

        void Awake() {
            if (!statsTextCom) {
                Debug.LogWarning("[debugstats] No TextMeshPro component assigned!");
                statsTextCom = GetComponent<TMP_Text>();
                if (!statsTextCom) Debug.LogWarning("[debugstats] No TextMeshPro component found!");
            }
            if (!quicklinesContainer) Debug.LogWarning("[debugstats] No TextMeshPro component assigned!");

            // Setup quickline text preset:
            if (quicklineTextPreset) {
                var obj = Instantiate(quicklineTextPreset, quicklinesContainer);
                var com = obj.GetComponent<TextMeshProUGUI>();
                if (!com) Debug.LogError("QL Prefab exists, but no TMP_Text was found on it!");
                com.SetText((string)null);

                Destroy(quicklineTextPreset);
                quicklineTextPreset = obj; // To preserve the above changes, since we deviate in the editor for previewing purposes
            }

            createQuicklines();
        }

        void OnEnable() {
            Application.logMessageReceived += UNITY_logMessageReceived;
        }
        void OnDisable() {
            Application.logMessageReceived -= UNITY_logMessageReceived;
        }
        
        const int STATS_STRINGBUILDER_CAPACITY = 200;

        class ComponentStatsInfo {
            public ComponentStatsInfo() { }
            public ComponentStatsInfo(string key, DebugStatsSettingsAttribute attribute = null) {
                if (attribute != null) {
                    isEnabled    = attribute.startEnabled;
                    priority     = attribute.priority;
                    displayName  = attribute.displayName.IsEmpty() ? Path.GetFileNameWithoutExtension(key) : attribute.displayName;
                }
            }

            public bool          isEnabled     = false; // TODO: @EnableComponents
            public int           priority;
            public string        displayName;
            public StringBuilder stringBuilder = new(capacity: STATS_STRINGBUILDER_CAPACITY);
        }

        // key:   string -- When printing with STATS_PrintLine(text, ...), the caller file path will be used to determine this (filename without ext), as the "component name".
        // value: class of info for a given component
        // pushToStatsDB() is used to add text for a given component into a Dictionary (hashtable), which is then later collected and printed at once in LateUpdate().
        Dictionary<string, ComponentStatsInfo> statsDatabase = new();

        ComponentStatsInfo getAndOrAddStatsDBEntry(string key) {
            // @Performance
            // I imagine this is probably slow, could profile. Not sure what else could be done here, aside from pre-caching
            // the keys (Reflection?).
            if (!statsDatabase.ContainsKey(key)) {
                DebugStatsSettingsAttribute attribute = null;

                // Attempt to get priority from the same-named class:
                // Using some Unity Editor Asset Database magic/hack here:
                if (File.Exists(key)) {
                    string unityRelativePath = "Assets" + key.Substring(Application.dataPath.Length); // This can crash!
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(unityRelativePath);

                    // NOTE: this will only give us the first MonoBehaviour that it finds. Don't have multiple scripts in a file!
                    var type = script.GetClass();
                    if (type != null) attribute = (DebugStatsSettingsAttribute)Attribute.GetCustomAttribute(type, typeof(DebugStatsSettingsAttribute));
                }

                var info = new ComponentStatsInfo(key, attribute);
                info.isEnabled = true; // TEMP:
                statsDatabase.Add(key, info);
                statsDBPrioritiesListIsDirty = true;
            }
            return statsDatabase[key];
        }

        // TODO: I'm not really sure about this approach where we deny writing stuff.
        // In some quick testing, performance does seem to be good, but we would really need to have this be scattered
        // around multiple components to truly test.
        bool canUpdateStats = true;
        void pushToStatsDB(string component, string text, bool append = false) {
            if (!canUpdateStats) return;
            
            var info = getAndOrAddStatsDBEntry(component);
            if (append) info.stringBuilder.Append(text);
            else        info.stringBuilder.AppendLine(text);
        }

        void flushStatsDB() {
            foreach (var kv in statsDatabase) {
                var info = kv.Value;
                if (!info.isEnabled) continue; // TODO: this might be bad?
                info.stringBuilder.Clear();
            }
        }

        bool statsDBPrioritiesListIsDirty = true;
        List<(string key, ComponentStatsInfo info)> statsDBPriorities = new();

        void printStatsDB() {
            var sb = new StringBuilder();

            if (statsDBPrioritiesListIsDirty) {
                statsDBPriorities.Clear();
                foreach (var kv in statsDatabase) {
                    statsDBPriorities.Add((kv.Key, kv.Value));
                }
                statsDBPriorities.Sort((x1, x2) => x2.info.priority.CompareTo(x1.info.priority));
                statsDBPrioritiesListIsDirty = false;
            }

            foreach (var kv in statsDBPriorities) {
                var key  = kv.key;
                var info = kv.info;
                if (!info.isEnabled) continue;

                sb.AppendLine($"{info.displayName}:  (priority: {info.priority})".bold());
                sb.AppendLine(info.stringBuilder.ToString());
                sb.AppendLine();
            }

            statsTextCom.SetText(sb.ToString());
        }

        // Public methods:
        public static void STATS_PrintLine(string component, string text) => GrabInstance()?.pushToStatsDB(component, text);
        public static void STATS_PrintLine(string text, bool printCallerDebugInfo = true, 
                                                        [CallerFilePath]   string callerFilePath = null, [CallerMemberName] string callerProcName = null,
                                                        [CallerLineNumber] int callerLineNum = -1) {
            var component   = callerFilePath;
            var textToPrint = !printCallerDebugInfo ? text : text.AddCallerDebugInfo(CallerDebugInfoFlags.ProcName);
            GrabInstance()?.pushToStatsDB(component, textToPrint, append: false);
        }
        public static void STATS_Append(string text, bool printCallerDebugInfo = true, 
                                                        [CallerFilePath]   string callerFilePath = null, [CallerMemberName] string callerProcName = null,
                                                        [CallerLineNumber] int callerLineNum = -1) {
            // @CopyPasta:
            var component = callerFilePath;
            var textToPrint = !printCallerDebugInfo ? text : text.AddCallerDebugInfo(CallerDebugInfoFlags.ProcName);
            GrabInstance()?.pushToStatsDB(component, textToPrint, append: true);
        }

        // public static void STATS_PrintQuickLine(string text) => GrabInstance()?.quicklinePush(text);
        public static void STATS_PrintQuickLine(string text,
                                                [CallerFilePath]   string callerFilePath = null,
                                                [CallerMemberName] string callerProcName = null,
                                                [CallerLineNumber] int callerLineNum     = -1) =>
            GrabInstance()?.quicklinePush(text, callerFilePath, callerProcName, callerLineNum);

        // TODO: we may want to read/receive messages from DebugConsole instead:
        public static bool UNITY_RedirectLogMessages = true;
        void UNITY_logMessageReceived(string text, string stackTrace, LogType level) {
            if (!UNITY_RedirectLogMessages) return;
            
            if      (level == LogType.Warning) text = $"<color=#FB8C00>{text}</color>";
            else if (level == LogType.Error)   text = $"<color=#EF5350>{text}</color>";

            quicklinePush(text);
        }

        public static void debugLog_noHandle(string text) {
            var before = UNITY_RedirectLogMessages;
            UNITY_RedirectLogMessages = false;
            Debug.Log(text);
            UNITY_RedirectLogMessages = before;
        }

        FPSInfo fpsInfo;

        const float fpsStatsPollingFrequency = 0.5f;
        float       fpsStatsTimer;
        void UPDATE_FPSStats() {
            if (fpsStatsTimer < fpsStatsPollingFrequency) {
                fpsStatsTimer += Time.deltaTime; return;
            }

            float timeSpan   = Time.realtimeSinceStartup - fpsInfo.lastTime;
            int   frameCount = Time.frameCount           - fpsInfo.lastFrameCount;

            fpsInfo.lastFrameCount = Time.frameCount;
            fpsInfo.lastTime       = Time.realtimeSinceStartup;
            fpsInfo.fpsAccurate    = frameCount / timeSpan;
            fpsInfo.fpsRounded     = Mathf.RoundToInt(fpsInfo.fpsAccurate);

        }

        void Update() {
            // NOTE: From this component, we should not print to stats DB in Update(), as we flush it in LateUpdate() / UPDATE_PrintStats().
            UPDATE_FPSStats();
        }

        float statsPrintTimer = float.MaxValue; // Print on first frame -- canUpdateStats is also set to true because of this
        bool  updateOverride;
        void LATEUPDATE_PrintStats() {
            if (Keyboard.current.commaKey.wasPressedThisFrame) updateOverride = !updateOverride;
            
            if (statsPrintTimer > statsUpdateMs || updateOverride) { // If the timer has elapsed, or overriden:
                if (!canUpdateStats) canUpdateStats = true;
                else {
                    printStatsDB();
                    canUpdateStats = false;
                    statsPrintTimer = 0f;
                }
            }
            else {
                statsPrintTimer += Time.unscaledDeltaTime * 1000f;
            }

            flushStatsDB(); // @Performance
        }

        void LateUpdate() {
            STATS_PrintLine($"FPS: {fpsInfo.fpsAccurate:N2} ({fpsInfo.fpsRounded})"); // TODO: control

            LATEUPDATE_Quicklines();
            LATEUPDATE_PrintStats();
        }
    }
}