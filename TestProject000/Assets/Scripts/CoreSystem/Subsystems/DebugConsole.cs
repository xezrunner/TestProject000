using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoreSystem {
    
    public class DebugConsole : MonoBehaviour {
        [Header("Components")]
        [SerializeField] RectTransform canvasRectTrans;
        [SerializeField] RectTransform selfRectTrans;
        
        [SerializeField] ScrollRect    scrollRect;
        [SerializeField] RectTransform scrollRectTrans;
        [SerializeField] RectTransform scrollContentRectTrans;
        
        [SerializeField] GameObject consoleOutputTextPreset;

        [SerializeField] TMP_Text scrollDebugTextCom;

        [Header("Settings")]
        [SerializeField] Vector2 textPadding = new(24, 16);

        List<(GameObject obj, TMP_Text com)> lines = new();

        float uiLineHeight;
        int   uiLineCount;

        void Awake() {
            if (!selfRectTrans)   selfRectTrans   = GetComponent<RectTransform>();
            if (!canvasRectTrans) canvasRectTrans = selfRectTrans.parent.GetComponent<RectTransform>();
            
            consoleOutput = new(capacity: 300);

            createConsoleLines();
        }

        void OnEnable() {
            Application.logMessageReceived += UNITY_logMessageReceived;
        }
        void OnDisable() {
            Application.logMessageReceived -= UNITY_logMessageReceived;
        }

        public List<string> consoleOutput; // All messages

        public static bool UNITY_RedirectLogMessages = true;
        void UNITY_logMessageReceived(string text, string stackTrace, LogType level) {
            if (!UNITY_RedirectLogMessages) return;
            
            if      (level == LogType.Warning) text = $"<color=#FB8C00>{text}</color>";
            else if (level == LogType.Error)   text = $"<color=#EF5350>{text}</color>";

            consoleOutput.Add(text);
            updateLines();

            scroll(SCROLL_BOTTOM);
        }
        
        void pushText(string text) {

        }

        void resizeConsole(float newHeight = 500f) {
            selfRectTrans.sizeDelta = new(selfRectTrans.sizeDelta.x, newHeight);
            createConsoleLines();
        }

        void createConsoleLines() {
            var consoleHeight = scrollRectTrans.rect.height; // TODO: size var

            uiLineHeight = consoleOutputTextPreset.GetComponent<RectTransform>().rect.height;
            uiLineCount = Mathf.RoundToInt(consoleHeight / uiLineHeight) + 1;

            for (int i = lines.Count; i < uiLineCount; ++i) {
                var obj = Instantiate(consoleOutputTextPreset, scrollContentRectTrans);
                var com = obj.GetComponent<TMP_Text>();

                obj.SetActive(true);
                com.SetText((string)null);
                
                lines.Add(new(obj, com));
            }
            for (int i = lines.Count - 1; i >= uiLineCount; --i) {
                var line = lines[i];
                // Leave these lines intact, since the console might get resized later again:
                line.obj.SetActive(false);
            }

            consoleOutputTextPreset.SetActive(false);
        }

        void updateLines() {
            // Set scroll content height:
            var fullHeight = (consoleOutput.Count * uiLineHeight) + textPadding.y;
            scrollContentRectTrans.sizeDelta = new(scrollContentRectTrans.sizeDelta.x, fullHeight);
            
            var scroll = consoleOutput.Count <= uiLineCount ? 0 : 1f - scrollRect.verticalNormalizedPosition; // 0-1, top-bottom

            // NOTE: take into consideration the extra UI line that exists, versus what we want to see in the UI.
            // The extra is only used for smooth scrolling.
            var indexIntoOutput = Mathf.FloorToInt(scroll * Mathf.Min(consoleOutput.Count, consoleOutput.Count - (uiLineCount - 1)));
            if (indexIntoOutput < 0) indexIntoOutput = 0;

            for (int i = 0 ; i < uiLineCount; ++i) {
                var line = lines[i];

                Vector2 targetPos = new(x: 0, y: (indexIntoOutput + i) * uiLineHeight);
                targetPos += textPadding / 2;
                targetPos.y *= -1; // UI has top at 1

                scrollDebugTextCom.SetText($"output lines: {consoleOutput.Count} | scroll: {scroll:N2}  targetHeight: {fullHeight:N3}  *: {(fullHeight * scroll):N3}  ");

                line.com.rectTransform.anchoredPosition = targetPos;
                
                if (indexIntoOutput + i < consoleOutput.Count) {
                    var outputLine = consoleOutput[indexIntoOutput + i];
                    line.obj.SetActive(true);
                    //line.com.SetText($"visual line index {i:D2}  text output index: {(indexIntoOutput + i):D3} | {outputLine}");
                    line.com.SetText(outputLine);
                } else {
                    line.obj.SetActive(false);
                }
            }
        }

        public const float SCROLL_TOP    = 1f;
        public const float SCROLL_BOTTOM = 0f;

        public void OnValueChanged(Vector2 v) {
            // scrollDebugTextCom.SetText($"scroll: {v}");
            updateLines();
        }

        // TODO: we might want to snap scrolling to the next line
        float scrollTarget = -1f;
        void scroll(float t) {
            scrollTarget = t;
        }

        float t = 0;
        void LateUpdate() {
            if (scrollTarget != -1f) {
                scrollRect.verticalNormalizedPosition = scrollTarget;
                scrollTarget = -1f;
            }

            if (Keyboard.current.shiftKey.isPressed && Keyboard.current.enterKey.wasReleasedThisFrame) {
                for (int i = 0; i < 30; ++i) Debug.Log(i);
            }

            if (Keyboard.current.spaceKey.wasReleasedThisFrame) {
                float targetHeight = selfRectTrans.sizeDelta.y == 300f ? 500f : selfRectTrans.sizeDelta.y == 500f ? canvasRectTrans.sizeDelta.y : 300f;
                resizeConsole(targetHeight);
            }
        }
    }
    
}