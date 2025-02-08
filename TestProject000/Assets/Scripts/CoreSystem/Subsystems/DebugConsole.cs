using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoreSystem {

    [Flags] public enum LogCategory: uint {
        Unknown    = 0,
        Unity      = 1 << 0,
        CoreSystem = 1 << 1,
        Project    = 1 << 2,
        External   = 1 << 3,
    }

    // TODO: completely control console with cmd arg

    public partial class DebugConsole : MonoBehaviour {
        [Header("Components")]
        [SerializeField] RectTransform canvasRectTrans;
        [SerializeField] RectTransform selfRectTrans;

        [SerializeField] RectTransform contentRectTrans;
        
        [SerializeField] ScrollRect    scrollRect;
        [SerializeField] RectTransform scrollRectTrans;
        [SerializeField] RectTransform scrollContentRectTrans;
        
        [SerializeField] GameObject consoleOutputTextPreset;

        [SerializeField] TMP_Text debugTextCom;

        [Header("Settings")]
        [SerializeField] float animationSpeed = 3f;
        [SerializeField] float defaultHeight  = 450f;
        [SerializeField] Vector2 textPadding = new(24, 16);

        List<(GameObject obj, TMP_Text com)> uiLines = new();

        float uiLineHeight;
        int   uiLineCount;

        void Awake() {
            if (!selfRectTrans)   selfRectTrans   = GetComponent<RectTransform>();
            if (!canvasRectTrans) canvasRectTrans = selfRectTrans.parent.GetComponent<RectTransform>();
            if (!canvasRectTrans) Debug.LogError("No canvas?");

            if (!contentRectTrans) contentRectTrans = selfRectTrans.GetChild(1).GetComponent<RectTransform>(); // @Hardcoded
            if (!contentRectTrans) {
                Debug.LogError("Content rect transform not found. This is fatal.");
                Application.Quit();
            }

            registerEventCallbacks();
            registerCommandsFromAssemblies();
            
            setState(state, anim: false);
            resizeConsole(defaultHeight, anim: false); // NOTE: also creates console lines!
        }

        void registerEventCallbacks() {
            Application.logMessageReceived += UNITY_logMessageReceived;
        }
        void OnApplicationQuit() {
            Application.logMessageReceived -= UNITY_logMessageReceived;
        }

        public static bool UNITY_ReceiveLogMessages = true;
        void UNITY_logMessageReceived(string text, string stackTrace, LogType level) {
            if (!UNITY_ReceiveLogMessages) return;
            
            // TODO: this stuff is also in DebugStats_Quicklines
            if      (level == LogType.Warning) text = $"<color=#FB8C00>{text}</color>";
            else if (level == LogType.Error)   text = $"<color=#EF5350>{text}</color>";

            pushText(LogCategory.Unity, text);
        }

        float open_t;
        bool  state = false;

        void setState(bool newState, bool anim = true) {
            if (newState) {
                updateConsoleFiltering();
            }
            
            state = newState;
            open_t = anim ? 0f : 1.1f;
        }

        public class ConsoleLineInfo {
            public LogCategory category;
            // TODO: 
            // log level?
            // caller member info?
            // stack trace?
            public string text;
        }

        int consoleOutputCount;
        List<ConsoleLineInfo> consoleOutput = new(capacity: 500);

        LogCategory consoleFilterFlags = LogCategory.Unity | LogCategory.CoreSystem;
        public const LogCategory CONSOLEFILTERFLAGS_ALL = (LogCategory)uint.MaxValue;

        int consoleOutputFilteredCount = 0;
        List<ConsoleLineInfo> consoleOutputFiltered = new(capacity: 500);

        void pushText(LogCategory category, string text) {
            var info = new ConsoleLineInfo() {
                category = category,
                text     = text
            };
            consoleOutputCount += 1;
            consoleOutput.Add(info);

            // TODO: If the console is visible:
            if (state) {
                updateConsoleFiltering();
                updateConsoleOutputUI();
                scroll(SCROLL_BOTTOM);
            }
        }

        void updateConsoleFiltering() {
            if ((consoleFilterFlags & CONSOLEFILTERFLAGS_ALL) == CONSOLEFILTERFLAGS_ALL) {
                consoleOutputFiltered = consoleOutput;
                consoleOutputFilteredCount = consoleOutputCount;
                return;
            }

            // TODO: @Performance
            consoleOutputFiltered = consoleOutput.Where(x => consoleFilterFlags.HasFlag(x.category)).ToList();
            consoleOutputFilteredCount = consoleOutputFiltered.Count;
        }

        void setConsoleFilterFlags(LogCategory flags) {
            if (flags == consoleFilterFlags) return;
            consoleFilterFlags = flags;

            updateConsoleFiltering();
        }

        public void OnValueChanged(Vector2 v) {
            // scrollDebugTextCom.SetText($"scroll: {v}");
            updateConsoleOutputUI();
        }

        // TODO: we might want to snap scrolling to the next line
        float scrollTarget = -1f;
        void scroll(float t) {
            scrollTarget = t;
        }

        void Update() {
            UPDATE_Openness();
            if (!state) return;

            UPDATE_Sizing();
        }
        
        void LateUpdate() {
            // TODO: ignore when Alt/Cmd is being pressed, if Tab remains the key for toggling the console
            if (Keyboard.current.tabKey.wasPressedThisFrame) {
                setState(!state);
            }

            if (!state) return;

            if (scrollTarget != -1f) {
                scrollRect.verticalNormalizedPosition = scrollTarget;
                scrollTarget = -1f;
            }

            // -----

            if (Keyboard.current.enterKey.wasPressedThisFrame) {
                var command = commands["test_command"];
                if (command != null) {
                    // TODO:
                    Debug.Log("invoking test_command...");
                    var invocation = command.invokeFunction(); // command.function(0, 1);
                    if (invocation.success && invocation.result != null) Debug.Log("  result: " + invocation.result);
                }
            }

            if (Keyboard.current.shiftKey.isPressed && Keyboard.current.enterKey.wasPressedThisFrame) {
                for (int i = 1; i <= 30; ++i) {
                    pushText(LogCategory.CoreSystem, $"This is a log entry that belongs to the CoreSystem log category. {i}");
                    ++i;
                    Debug.LogWarning($"This is a log entry that belongs to the Unity log category. {i}");
                }
            }

            if (Keyboard.current.spaceKey.wasReleasedThisFrame) {
                float targetHeight = sizing_to == 300f ? 500f : sizing_to == 500f ? canvasRectTrans.sizeDelta.y : 300f;
                resizeConsole(targetHeight);
            }

            if (Keyboard.current.qKey.wasPressedThisFrame) {
                setConsoleFilterFlags(consoleFilterFlags ^ LogCategory.Unity);
                updateConsoleOutputUI();
            }
            if (Keyboard.current.wKey.wasPressedThisFrame) {
                setConsoleFilterFlags(consoleFilterFlags ^ LogCategory.CoreSystem);
                updateConsoleOutputUI();
            }
            if (Keyboard.current.eKey.wasPressedThisFrame) {
                setConsoleFilterFlags(consoleFilterFlags == CONSOLEFILTERFLAGS_ALL ? LogCategory.Unknown : CONSOLEFILTERFLAGS_ALL);
                updateConsoleOutputUI();
            }
        }
    }

}