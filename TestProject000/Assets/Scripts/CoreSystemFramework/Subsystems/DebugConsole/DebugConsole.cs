using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using static CoreSystemFramework.CoreSystemUtils;
using static CoreSystemFramework.QuickInput;

namespace CoreSystemFramework {
    public partial class DebugConsole : MonoBehaviour {
        // TODO: would be neat if we didn't have to cache (rect)transes manually:
        [Header("Components")]
        [RequiredComponent] [SerializeField] RectTransform canvasRectTrans;
        [RequiredComponent] [SerializeField] RectTransform selfRectTrans;

        [SerializeField] CanvasGroup backgroundCanvasGroup;

        [RequiredComponent] [SerializeField] GameObject    contentObj;
        [RequiredComponent] [SerializeField] RectTransform contentRectTrans;

        [RequiredComponent] [SerializeField] ScrollRect    scrollRect;
        [RequiredComponent] [SerializeField] RectTransform scrollRectTrans;
        [RequiredComponent] [SerializeField] RectTransform scrollContentRectTrans;

        [SerializeField]                     GameObject     consoleOutputTextPreset;
        [RequiredComponent] [SerializeField] TMP_InputField consoleInputField;
        [SerializeField]                     TMP_Text       consoleInputFieldText;

        [SerializeField] RectTransform filterButtonsContainer;
        [SerializeField] GameObject    filterButtonPreset;

        [SerializeField] TMP_Text      inputPredictionTextCom;
        [SerializeField] RectTransform inputPredictionTextRectTrans;

        [SerializeField] TMP_Text      argsPredictionText;
        [SerializeField] RectTransform argsPredictionTextRectTrans;

        [SerializeField] TMP_Text debugTextCom;

        [Header("Settings")]
        [SerializeField] float animationSpeed = 3f;
        [SerializeField] float defaultHeight = 450f;
        [SerializeField] Vector2 textPadding = new(24, 16);
        [SerializeField] int inputFieldNormalCaretWidth     = 9;
        [SerializeField] int inputFieldPredictingCaretWidth = 1;

        List<(GameObject obj, TMP_Text com)> uiLines = new();

        float uiLineHeight;
        int   uiLineCount;

        void Awake() {
            consoleOutput = Logging.logMessages;
            if (consoleOutput == null) {
                Debug.LogError("No console output!");
#if UNITY_EDITOR
                EditorApplication.isPaused = true;
#endif
            }

            registerEventCallbacks();

            if (keyboard == null) pushText("no keyboard!");

            if (!selfRectTrans)    selfRectTrans    = GetComponent<RectTransform>();
            if (!canvasRectTrans)  canvasRectTrans  = selfRectTrans?.parent.GetComponent<RectTransform>();
            if (!contentRectTrans) contentRectTrans = selfRectTrans?.GetChild(1)?.GetComponent<RectTransform>(); // @Hardcoded
            if (!contentObj)       contentObj       = contentRectTrans.gameObject;

            if (!consoleInputFieldText)        consoleInputFieldText        = consoleInputField?.textComponent;
            if (!inputPredictionTextRectTrans) inputPredictionTextRectTrans = inputPredictionTextCom?.rectTransform;
            if (!argsPredictionTextRectTrans)  argsPredictionTextRectTrans  = argsPredictionText?.rectTransform;

            processRequiredComponents(this);
            registerCommandsFromAssemblies();

            setupUI();

            setState(state, anim: false);
            resizeConsole(defaultHeight, anim: false); // NOTE: also creates console lines!
        }

        void registerEventCallbacks() {
            Logging.onLogMessageReceived += logMessageReceived;
            if (keyboard != null) keyboard.onTextInput += OnKeyboardTextInput;
        }
        void OnDisable() {
            Logging.onLogMessageReceived -= logMessageReceived;
            if (keyboard != null) keyboard.onTextInput -= OnKeyboardTextInput;
        }

        List<LogLineInfo> consoleOutput;

        void logMessageReceived(LogLineInfo info) {
            if (state) {
                updateConsoleFiltering();
                updateConsoleOutputUI();
                scroll(SCROLL_BOTTOM);
            }
        }

        // TODO: register as console variable!
        static bool  EXPERIMENT_PauseTimeWhileConsoleIsOpen = true;
        static float EXPERIMENT_PauseTimeWhileConsoleIsOpen_LastTimescale = 1f;

        bool state = false;
        public bool getState() => state;

        float openness_t;
        (float from, float to) opennessTargets;

        // TODO: abstract this away!
        CursorLockMode previousCursorLockState;
        bool           previousCursorVisibility;
        void setState(bool newState, bool anim = true) {
            if (newState) {
                contentRectTrans.gameObject.SetActive(true);

                updateConsoleFiltering();
                updateConsoleOutputUI();

                consoleInputField.ActivateInputField();
                scroll(SCROLL_BOTTOM); // TODO: this is flaky!

                previousCursorLockState  = Cursor.lockState;
                previousCursorVisibility = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;

                if (EXPERIMENT_PauseTimeWhileConsoleIsOpen) {
                    EXPERIMENT_PauseTimeWhileConsoleIsOpen_LastTimescale = Time.timeScale;
                    Time.timeScale = 0f;
                }

                opennessTargets.to = 0;
            } else {
                consoleInputField.DeactivateInputField();
                if (EXPERIMENT_PauseTimeWhileConsoleIsOpen) Time.timeScale = EXPERIMENT_PauseTimeWhileConsoleIsOpen_LastTimescale;

                Cursor.lockState = previousCursorLockState;
                Cursor.visible   = previousCursorVisibility;

                opennessTargets.to = contentRectTrans.rect.height;
            }

            state = newState;
            opennessTargets.from = contentRectTrans.anchoredPosition.y;
            openness_t = anim ? 0f : 1.1f;
        }

        void clearConsoleOutput() {
            consoleOutput.Clear();

            updateConsoleFiltering();
            updateConsoleOutputUI();
        }

        public const LogCategory CONSOLEFILTERFLAGS_ALL = (LogCategory)uint.MaxValue;
        LogCategory consoleFilterFlags = CONSOLEFILTERFLAGS_ALL;

        List<LogLineInfo> consoleOutputFiltered = new(capacity: 500);

        // TODO: colors for levels
        public void pushText(LogLevel level, string text) => pushText(text, level: level);

        public void pushText(string text, LogCategory category = LogCategory.CoreSystem, LogLevel level = LogLevel.Info, CallerDebugInfo callerInfo = null) {
            var info = new LogLineInfo() {
                category = category,
                text     = text
                // TODO: caller info
            };
            consoleOutput.Add(info);

            logMessageReceived(info);
        }

        void updateConsoleFiltering() {
            if ((consoleFilterFlags & CONSOLEFILTERFLAGS_ALL) == CONSOLEFILTERFLAGS_ALL) {
                consoleOutputFiltered = consoleOutput;
                return;
            }

            // TODO: @Performance
            consoleOutputFiltered = consoleOutput.Where(x => consoleFilterFlags.HasFlag(x.category)).ToList();
        }

        void setConsoleFilterFlags(LogCategory flags) {
            if (flags == consoleFilterFlags) return;
            if (flags == LogCategory.Unknown) flags = CONSOLEFILTERFLAGS_ALL;
            // if (consoleFilterFlags == CONSOLEFILTERFLAGS_ALL) {
            //     if (flags != LogCategory.Unknown || flags != CONSOLEFILTERFLAGS_ALL) flags ^= CONSOLEFILTERFLAGS_ALL;
            // }

            consoleFilterFlags = flags;
            updateConsoleFiltering();
            refreshFilterButtonStates();

            updateConsoleOutputUI();
        }

        struct ArgCompletion {
            public string name;
            public string argTypeName;
            public string defaultValueAsText;
        }

        struct PredictionInfo {
            public string prediction;
            public string remaining;
            
            public List<ArgCompletion> argCompletions;

            public static bool operator !(PredictionInfo info)     => info.prediction == null;
            public static bool operator false(PredictionInfo info) => info.prediction == null;
            public static bool operator true(PredictionInfo info)  => info.prediction != null;
        }

        PredictionInfo currentPredictionInfo = new() { argCompletions = new(capacity: 5) };

        // Suggestions/predictions:
        void updatePrediction(string input) {
            if (input == null) input = consoleInputField.text;
            if (!inputPredictionTextCom) return;
            
            string[] tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string prediction = null;
            string remaining = null;
            if (tokens.Length == 1) {
                // Find the best match for a command/var:
                int shortest = int.MaxValue;
                if (!input.IsEmpty()) {
                    foreach (var key in commands.Keys) {
                        if (!key.StartsWith(input)) continue;
                        if (input.Length >= key.Length) continue; // If we match a shorter alias, we should provide the next prediction

                        if (key.Length < shortest) {
                            prediction = key;
                            shortest = key.Length;
                        }
                    }
                }

                currentPredictionInfo.prediction = prediction;
                
                remaining = prediction?.Substring(input.Length);
                currentPredictionInfo.remaining = remaining;
                
                updateInlinePredictionUI(remaining);
            }

            // Command argument completions:
            if (remaining != null) return;

            ConsoleCommand command = null;
            if (tokens.Length != 0) {
                var commandName = tokens[0] ?? null;
                if (commands.ContainsKey(commandName)) command = commands[commandName];
            }

            updateInlineArgsPredictionUI(command, tokens);

            updateCaretWidth();
        }

        void completePrediction() {
            if (currentPredictionInfo) {
                // Complete prediction for first token (command/var):
                consoleInputField.text = currentPredictionInfo.prediction;
            } else if (currentPredictionInfo.argCompletions.Count > 0) {
                // Complete prediction for arguments:
                var argCompletion = currentPredictionInfo.argCompletions[0];
                
                var toComplete = argCompletion.defaultValueAsText;
                if (currentPredictionInfo.argCompletions.Count != 1) toComplete += ' '; // Add space to end of input if not last arg, to "move to next arg"
                
                if (argCompletion.defaultValueAsText != null) consoleInputField.text += toComplete; // Append completion
            } else {
                return;
            }

            consoleInputField.caretPosition = consoleInputField.text.Length;
        }

        public void OnConsoleInputFieldTextChanged(string text) {
            updatePrediction(text);
        }

        // TODO: on predictions/suggestions, change caret width temporarily to a thin line

        public void submit(string input) {
            // Use current console input if none provided:
            if (input == null) input = consoleInputField.text;

            input = input.Trim();

            pushText($"> {input}");

            if (input.Length == 0) return;

            var tokens = input.Split(' ');
            var commandName = tokens[0];

            if (!commands.ContainsKey(commandName)) pushText($"  - command not found");
            else {
                var command    = commands[commandName];
                var invocation = invokeFunction(command, tokens); // handles args inside
                if (!invocation.success)       pushText( "command execution failed"); // TEMP:
                if (invocation.result != null) pushText($"command result: {invocation.result}");
            }

            // NOTE: order here is important:
            consoleInputField.ActivateInputField(); // re-focus input field
            consoleInputField.text = null;          // clear input field after submission
        }

        static char[] CONSOLE_TOGGLE_KEYS = {
            '§', '`'
        };

        void OnKeyboardTextInput(char c) {
            // We do this because on some keyboards, § is the key that's on the intended console key, and the Input System can't detect that.
            // 
            // foreach (var it in CONSOLE_TOGGLE_KEYS) {
            //     if (c != it) continue;
            //     setState(!state); break;
            // }

            // TEMP:
            if (c != CONSOLE_TOGGLE_KEYS[0] && c != CONSOLE_TOGGLE_KEYS[1]) return;
            setState(!state);
        }

        void toggleSizing() {
            float targetHeight = sizingTargets.to == defaultHeight ? canvasRectTrans.sizeDelta.y : defaultHeight;
            resizeConsole(targetHeight);
        }

        void Update() {
            if (isHeld_internal(keyboard?.shiftKey) && wasPressed_internal(keyboard?.f1Key)) setState(!state);

            UPDATE_Openness();

            if (!state) return;

            UPDATE_Sizing();
            UPDATE_Scrolling();

            if (!isHeld_internal(keyboard?.altKey) && wasReleased_internal(keyboard?.enterKey)) submit(null);
            
            // TODO: might want to use tab for toggleSizing, then skip args when pressing Tab with inline args prediction? @SkipArgs
            if (isHeld_internal(keyboard.shiftKey) && wasPressed_internal(keyboard.tabKey)) toggleSizing();
            else if (wasPressed_internal(keyboard.tabKey)) completePrediction();

            // TODO: convenience input stuff, like word navigation/select/delete
            if (isHeld_internal(keyboard.ctrlKey) && wasPressed_internal(keyboard.cKey)) consoleInputField.text = null;
        }

        void LateUpdate() {
            // TODO: ignore when Alt/Cmd is being pressed, if Tab remains the key for toggling the console
            if (!state) return;

            // TEMP:
            {
                if (keyboard.shiftKey.isPressed && keyboard.enterKey.wasPressedThisFrame) {
                    for (int i = 1; i <= 30; ++i) {
                        pushText($"This is a log entry that belongs to the CoreSystem log category. {i}");
                        ++i;
                        Debug.LogWarning($"This is a log entry that belongs to the Unity log category. {i}");
                    }
                }

                if (keyboard.ctrlKey.isPressed && keyboard.spaceKey.wasReleasedThisFrame) {
                    float targetHeight = sizingTargets.to == 300f ? 500f : sizingTargets.to == 500f ? canvasRectTrans.sizeDelta.y : 300f;
                    resizeConsole(targetHeight);
                }

                if (keyboard.altKey.isPressed && keyboard.qKey.wasPressedThisFrame) {
                    setConsoleFilterFlags(consoleFilterFlags ^ LogCategory.Unity);
                    updateConsoleOutputUI();
                }
                if (keyboard.altKey.isPressed && keyboard.wKey.wasPressedThisFrame) {
                    setConsoleFilterFlags(consoleFilterFlags ^ LogCategory.CoreSystem);
                    updateConsoleOutputUI();
                }
                if (keyboard.altKey.isPressed && keyboard.eKey.wasPressedThisFrame) {
                    setConsoleFilterFlags(consoleFilterFlags == CONSOLEFILTERFLAGS_ALL ? LogCategory.Unknown : CONSOLEFILTERFLAGS_ALL);
                    updateConsoleOutputUI();
                }
            }
        }
    }


}
