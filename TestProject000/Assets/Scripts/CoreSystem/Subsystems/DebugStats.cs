using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem;
using System.IO;

namespace CoreSystem {

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
                var obj = Instantiate(quicklineTextPreset);
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
        
        class ComponentStatsInfo {
            public bool          isEnabled = true; // TEMP:
            public StringBuilder stringBuilder = new(capacity: STATS_STRINGBUILDER_CAPACITY);
        }

        Dictionary<string, ComponentStatsInfo> statsDatabase = new();

        static string extractComponentFromCallerDebugInfo(string callerFilePath) {
            return Path.GetFileNameWithoutExtension(callerFilePath);
        }

        const int STATS_STRINGBUILDER_CAPACITY = 200;

        ComponentStatsInfo getAndOrAddPerFrameStatsSB(string key) {
            if (!statsDatabase.ContainsKey(key)) statsDatabase.Add(key, new());
            return statsDatabase[key];
        }

        bool canUpdateStats;
        void pushPerFrameLine(string component, string text, bool append = false) {
            if (!canUpdateStats) return;
            
            var info = getAndOrAddPerFrameStatsSB(component);
            if (append) info.stringBuilder.Append(text);
            else        info.stringBuilder.AppendLine(text);
        }

        void STATS_PrintAllStatsAndFlush() {
            var sb = new StringBuilder();
            foreach (var kv in statsDatabase) {
                var info = kv.Value;
                if (!info.isEnabled) continue;

                sb.AppendLine($"{kv.Key}:".bold());
                sb.AppendLine(info.stringBuilder.ToString());
                sb.AppendLine();

                info.stringBuilder.Clear();
            }

            statsTextCom.SetText(sb.ToString());
        }

        // Public methods:
        // TODO: figure out the public API for this!
        public static void STATS_PrintLine(string component, string text) => GrabInstance()?.pushPerFrameLine(component, text);
        public static void STATS_PrintLine(string text, bool printCallerDebugInfo = true, 
                                                        [CallerFilePath]   string callerFilePath = null, [CallerMemberName] string callerProcName = null,
                                                        [CallerLineNumber] int callerLineNum = -1) {
            GrabInstance()?.pushPerFrameLine(extractComponentFromCallerDebugInfo(callerFilePath), !printCallerDebugInfo ? text : text.AddCallerDebugInfo(CallerDebugInfoFlags.ProcName));
        }

        // public static void STATS_PrintQuickLine(string text) => GrabInstance()?.quicklinePush(text);
        public static void STATS_PrintQuickLine(string text,
                                                [CallerFilePath]   string callerFilePath = null,
                                                [CallerMemberName] string callerProcName = null,
                                                [CallerLineNumber] int callerLineNum     = -1) =>
            GrabInstance()?.quicklinePush(text, callerFilePath, callerProcName, callerLineNum);

        // TODO: we may want to read/receive messages from DebugConsole instead:
        static bool UNITY_RedirectLogMessages = true;
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

        float timer;
        void LateUpdate() {
            UPDATE_Quicklines();

            bool comma = Keyboard.current.commaKey.isPressed;

            if (timer > statsUpdateMs || comma) {
                if (!canUpdateStats) canUpdateStats = true;
                else {
                    STATS_PrintAllStatsAndFlush();
                    if (!comma) canUpdateStats = false;
                    timer = 0f;
                }
            }

            timer += Time.unscaledDeltaTime * 1000f;

            STATS_PrintLine($"Test!  {Time.time}  {Time.deltaTime}");
        }

    }
}

/*
    private IEnumerator FPS()
    {
        for (; ; )
        {
            int lastFrameCount = Time.frameCount;
            float lastTime = Time.realtimeSinceStartup;
            yield return new WaitForSeconds(frequency);

            float timeSpan = Time.realtimeSinceStartup - lastTime;
            int frameCount = Time.frameCount - lastFrameCount;

            FramesPerSec = Mathf.RoundToInt(frameCount / timeSpan);
            counter.text = "FPS: " + FramesPerSec.ToString();
        }
    }
*/