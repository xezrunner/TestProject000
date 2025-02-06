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

    public class DebugConsole : MonoBehaviour {
        [Header("Components")]
        [SerializeField] RectTransform canvasRectTrans;
        [SerializeField] RectTransform selfRectTrans;
        
        [SerializeField] ScrollRect    scrollRect;
        [SerializeField] RectTransform scrollRectTrans;
        [SerializeField] RectTransform scrollContentRectTrans;
        
        [SerializeField] GameObject consoleOutputTextPreset;

        [SerializeField] TMP_Text debugTextCom;

        [Header("Settings")]
        [SerializeField] Vector2 textPadding = new(24, 16);

        List<(GameObject obj, TMP_Text com)> uiLines = new();

        float uiLineHeight;
        int   uiLineCount;

        void Awake() {
            if (!selfRectTrans)   selfRectTrans   = GetComponent<RectTransform>();
            if (!canvasRectTrans) canvasRectTrans = selfRectTrans.parent.GetComponent<RectTransform>();

            createConsoleLines();
        }

        void OnEnable() {
            Application.logMessageReceived += UNITY_logMessageReceived;
        }
        void OnDisable() {
            Application.logMessageReceived -= UNITY_logMessageReceived;
        }

        void setState() {
            // ...
            
            updateConsoleFiltering();
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
            if (true) {
                updateConsoleFiltering();
                updateLines();
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

        public static bool UNITY_RedirectLogMessages = true;
        void UNITY_logMessageReceived(string text, string stackTrace, LogType level) {
            if (!UNITY_RedirectLogMessages) return;
            
            if      (level == LogType.Warning) text = $"<color=#FB8C00>{text}</color>";
            else if (level == LogType.Error)   text = $"<color=#EF5350>{text}</color>";

            pushText(LogCategory.Unity, text);
        }

        // UGUI has the scrolling T flipped:
        public const float SCROLL_TOP    = 1f;
        public const float SCROLL_BOTTOM = 0f;

        void resizeConsole(float newHeight = 500f) {
            selfRectTrans.sizeDelta = new(selfRectTrans.sizeDelta.x, newHeight);
            createConsoleLines();
        }

        // We use this to add an extra UI line for virtualized, yet smooth scrolling:
        const int extraLineCountForSmoothScrolling = 2; // NOTE: For larger Y padding, we need more extra lines.
        void createConsoleLines() {
            var consoleHeight = scrollRectTrans.rect.height; // TODO: size var

            uiLineHeight = consoleOutputTextPreset.GetComponent<RectTransform>().rect.height;
            uiLineCount = Mathf.RoundToInt(consoleHeight / uiLineHeight) + extraLineCountForSmoothScrolling;

            for (int i = uiLines.Count; i < uiLineCount; ++i) {
                var obj = Instantiate(consoleOutputTextPreset, scrollContentRectTrans);
                var com = obj.GetComponent<TMP_Text>();

                obj.SetActive(true);
                com.SetText((string)null);
                
                uiLines.Add(new(obj, com));
            }
            for (int i = uiLines.Count - 1; i >= uiLineCount; --i) {
                var line = uiLines[i];
                // Leave these lines intact, since the console might get resized later again:
                line.obj.SetActive(false);
            }

            consoleOutputTextPreset.SetActive(false);
        }

        void updateLines() {
            // This function does "virtualized scrolling", where only the visible UI lines are updated with the console log output.
            // This results in much better performance, compared to keeping the whole log output within the console.

            // Set scroll content height based on how many log lines there are:
            var contentHeight = (consoleOutputFilteredCount * uiLineHeight) + (textPadding.y * 2f);

            // NOTE: despite UGUI's flipped nature, this is [0-1, top-bottom] here:
            var scroll = (consoleOutputFilteredCount <= uiLineCount) ? 
                        // If we scroll when scrolling shouldn't be possible (content fits on screen), Unity will claim 
                        // that we scrolled to the bottom (1) for some reason.
                        // We'll limit this here such that we always remain scrolled to the top when this is the case.
                        // TODO: BUG: should we apply this universally?
                        SCROLL_BOTTOM :
                        // BUG: UGUI sometimes gives us a scrolling value of like 1.0000014, which if we subtract,
                        // will obviously give us a negative number. So let's clamp it:
                        Mathf.Clamp01(SCROLL_TOP - scrollRect.verticalNormalizedPosition);
            // Alternatively (potentially more accurate?)
            // scroll = (consoleOutputFilteredCount <= uiLineCount) ? SCROLL_BOTTOM : Mathf.Clamp01(scrollContentRectTrans.anchoredPosition.y / scrollRectTrans.rect.height);

            // This is the index into the console log output lines (as opposed to the constant few visible UI lines).
            // We want this index to be offset by the visible UI line count when scrolling is possible, since we want the
            // log end with the last UI line.
            // NOTE: take into consideration the extra UI line that exists (+1), versus what we want to see in the UI.
            // The extra is used for smooth scrolling.
            var indexIntoOutput = Mathf.FloorToInt(scroll * Mathf.Min(a: consoleOutputFilteredCount,
                                                                      b: consoleOutputFilteredCount - (uiLineCount - extraLineCountForSmoothScrolling)));

            debugTextCom?.SetText($"total lines: {consoleOutputCount}  filtered lines: {consoleOutputFilteredCount} | "     + 
                                  $"scroll: {scroll:N2}  height: {contentHeight:N3}  indexIntoOutput: {indexIntoOutput} | " +
                                  $"filter: [{consoleFilterFlags}]");

            for (int i = 0 ; i < uiLineCount; ++i) {
                var uiLine = uiLines[i];

                if (indexIntoOutput + i >= consoleOutputFilteredCount) {
                    // Deactivate invisible lines
                    uiLine.obj.SetActive(false);
                    continue;
                }

                // Write the appropriate log index into the line:
                var outputLine = consoleOutputFiltered[indexIntoOutput + i];
                uiLine.obj.SetActive(true);
                //line.com.SetText($"visual line index {i:D2}  text output index: {(indexIntoOutput + i):D3} | {outputLine}");
                uiLine.com.SetText(outputLine.text);

                // Position line within the scroll content area to the position where it should be:
                // It is intentional that this is an "integer" that only updates when we scroll one line's worth of space.
                // With this setup, the lines will move naturally, but also update seamlessly as they "fall into their correct place".
                Vector2 targetPos = new(x: 0, y: (indexIntoOutput + i) * uiLineHeight);
                // Apply padding:
                // @Incomplete: large padding values cause the lines to disappear sooner than expected with longer scroll heights
                // at the top and bottom of the console.
                // Perhaps we'd need to take the padding into consideration when creating the extra lines, to determine the amount
                // of extra lines to create.
                targetPos   += textPadding;
                targetPos.y *= -1; // UI has top at 1, so let's flip our logical calculations from above into that.

                uiLine.com.rectTransform.anchoredPosition = targetPos;
            }

            scrollContentRectTrans.sizeDelta = new(scrollContentRectTrans.sizeDelta.x, contentHeight);
        }

        public void OnValueChanged(Vector2 v) {
            // scrollDebugTextCom.SetText($"scroll: {v}");
            updateLines();
        }

        // TODO: we might want to snap scrolling to the next line
        float scrollTarget = -1f;
        void scroll(float t) {
            scrollTarget = t;
        }

        float t;
        void LateUpdate() {
            if (scrollTarget != -1f) {
                scrollRect.verticalNormalizedPosition = scrollTarget;
                scrollTarget = -1f;
            }

            if (Keyboard.current.shiftKey.isPressed && Keyboard.current.enterKey.wasPressedThisFrame) {
                for (int i = 0; i < 30; ++i) {
                    pushText(LogCategory.CoreSystem, $"This is a log entry that belongs to the CoreSystem log category. {i}");
                    Debug.LogWarning($"This is a log entry that belongs to the Unity log category. {i}");
                }
            }

            if (Keyboard.current.spaceKey.wasReleasedThisFrame) {
                float targetHeight = selfRectTrans.sizeDelta.y == 300f ? 500f : selfRectTrans.sizeDelta.y == 500f ? canvasRectTrans.sizeDelta.y : 300f;
                resizeConsole(targetHeight);
            }

            if (Keyboard.current.tabKey.wasReleasedThisFrame) {
                updateLines();
            }

            if (Keyboard.current.qKey.wasPressedThisFrame) {
                setConsoleFilterFlags(consoleFilterFlags ^ LogCategory.Unity);
                updateLines();
            }
            if (Keyboard.current.wKey.wasPressedThisFrame) {
                setConsoleFilterFlags(consoleFilterFlags ^ LogCategory.CoreSystem);
                updateLines();
            }
            if (Keyboard.current.eKey.wasPressedThisFrame) {
                setConsoleFilterFlags(consoleFilterFlags == CONSOLEFILTERFLAGS_ALL ? LogCategory.Unknown : CONSOLEFILTERFLAGS_ALL);
                updateLines();
            }
        }
    }

}