using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

using static CoreSystem.CoreSystemUtils;
using static CoreSystem.QuickInput;

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
        // TODO: would be neat if we didn't have to cache (rect)transes manually:
        [Header("Components")]
        [RequiredComponent] [SerializeField] RectTransform canvasRectTrans;
        [RequiredComponent] [SerializeField] RectTransform selfRectTrans;

        [RequiredComponent] [SerializeField] RectTransform contentRectTrans;
        
        [RequiredComponent] [SerializeField] ScrollRect    scrollRect;
        [RequiredComponent] [SerializeField] RectTransform scrollRectTrans;
        [RequiredComponent] [SerializeField] RectTransform scrollContentRectTrans;
        
        [SerializeField]                     GameObject     consoleOutputTextPreset;
        [RequiredComponent] [SerializeField] TMP_InputField consoleInputField;
        [SerializeField]                     TMP_Text       consoleInputFieldText;

        [SerializeField] RectTransform filterButtonsContainer;
        [SerializeField] GameObject    filterButtonPreset;

        [SerializeField] TMP_Text      inputPredictionText;
        [SerializeField] RectTransform inputPredictionTextRectTrans;

        [SerializeField] TMP_Text      argsPredictionText;
        [SerializeField] RectTransform argsPredictionTextRectTrans;

        [SerializeField] TMP_Text debugTextCom;

        [Header("Settings")]
        [SerializeField] float   animationSpeed = 3f;
        [SerializeField] float   defaultHeight  = 450f;
        [SerializeField] Vector2 textPadding    = new(24, 16);
        [SerializeField] int inputFieldNormalCaretWidth     = 9;
        [SerializeField] int inputFieldPredictingCaretWidth = 1;

        List<(GameObject obj, TMP_Text com)> uiLines = new();

        float uiLineHeight;
        int   uiLineCount;

        Keyboard keyboard = Keyboard.current;

        void Awake() {
            registerEventCallbacks();

            if (keyboard == null) pushText("no keyboard!");

            if (!selfRectTrans)    selfRectTrans    = GetComponent<RectTransform>();
            if (!canvasRectTrans)  canvasRectTrans  = selfRectTrans?.parent.GetComponent<RectTransform>();
            if (!contentRectTrans) contentRectTrans = selfRectTrans?.GetChild(1)?.GetComponent<RectTransform>(); // @Hardcoded

            if (!consoleInputFieldText)        consoleInputFieldText        = consoleInputField?.textComponent;
            if (!inputPredictionTextRectTrans) inputPredictionTextRectTrans = inputPredictionText?.rectTransform;
            if (!argsPredictionTextRectTrans)  argsPredictionTextRectTrans  = argsPredictionText?.rectTransform;

            processRequiredComponents(this);
            registerCommandsFromAssemblies();

            setupUI();
            
            setState(state, anim: false);
            resizeConsole(defaultHeight, anim: false); // NOTE: also creates console lines!
        }

        void registerEventCallbacks() {
            Application.logMessageReceived += UNITY_logMessageReceived;
            if (keyboard != null) keyboard.onTextInput += OnKeyboardTextInput;
        }
        void OnApplicationQuit() {
            Application.logMessageReceived -= UNITY_logMessageReceived;
            if (keyboard != null) keyboard.onTextInput -= OnKeyboardTextInput;
        }

        public static bool UNITY_ReceiveLogMessages = true;
        void UNITY_logMessageReceived(string text, string stackTrace, LogType level) {
            if (!UNITY_ReceiveLogMessages) return;
            
            // TODO: this stuff is also in DebugStats_Quicklines
            if      (level == LogType.Warning) text = $"<color=#FB8C00>{text}</color>";
            else if (level == LogType.Error)   text = $"<color=#EF5350>{text}</color>";

            pushText(text, LogCategory.Unity);
        }

        float open_t;
        bool  state = false;

        void setState(bool newState, bool anim = true) {
            if (newState) {
                updateConsoleFiltering();
                consoleInputField.ActivateInputField();
            } else {
                consoleInputField.DeactivateInputField();
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

        public const LogCategory CONSOLEFILTERFLAGS_ALL = (LogCategory)uint.MaxValue;
        LogCategory consoleFilterFlags = CONSOLEFILTERFLAGS_ALL;

        int consoleOutputFilteredCount = 0;
        List<ConsoleLineInfo> consoleOutputFiltered = new(capacity: 500);

        void pushText(string text, LogCategory category = LogCategory.CoreSystem) {
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
            if (flags == LogCategory.Unknown) flags = CONSOLEFILTERFLAGS_ALL;
            if (consoleFilterFlags == CONSOLEFILTERFLAGS_ALL) {
                if (flags != LogCategory.Unknown || flags != CONSOLEFILTERFLAGS_ALL) flags ^= CONSOLEFILTERFLAGS_ALL;
            }

            consoleFilterFlags = flags;
            updateConsoleFiltering();
        }

        // Suggestions/predictions:
        string currentInputPrediction;
        void updatePrediction(string input) {
            if (input == null) input = consoleInputField.text;
            if (!inputPredictionText) return;

            currentInputPrediction = null;

            int shortest = int.MaxValue;
            if (!input.IsEmpty()) {
                foreach (var key in commands.Keys) {
                    if (!key.StartsWith(input))     continue;
                    if (input.Length >= key.Length) continue;

                    if (key.Length < shortest) {
                        currentInputPrediction = key;
                        shortest = key.Length;
                    }
                }
                if (currentInputPrediction == input) currentInputPrediction = null;
            }
            updateInlinePredictionUI(currentInputPrediction?.Substring(input.Length));

            ConsoleCommand command = null;
            string[] tokens = null;
            if (currentInputPrediction == null && !input.IsEmpty()) {
                // Command argument prediction:
                tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var commandName = tokens[0];
                if (commands.ContainsKey(commandName)) command = commands[commandName];
            }
            updateInlineArgsPredictionUI(command, tokens);

            updateCaretWidth();
        }

        void completePrediction() {
            if (currentInputPrediction == null) return;

            consoleInputField.text = currentInputPrediction;
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
                var invocation = command.invokeFunction(tokens[1..]); // handles args inside
                if (!invocation.success)       pushText( "  - command execution failed"); // TEMP:
                if (invocation.result != null) pushText($"  - command result: {invocation.result}");
            }

            // NOTE: order here is important:
            consoleInputField.ActivateInputField(); // re-focus input field
            consoleInputField.text = null;          // clear input field after submission
        }

        static char[] CONSOLE_TOGGLE_KEYS = {
            '§', '`'
        };

        void OnKeyboardTextInput(char c) {
            // We do this because in my setup, § is the key that's on the intended console key.
            foreach (var it in CONSOLE_TOGGLE_KEYS) {
                if (c != it) continue;
                setState(!state); break;
            }
        }

        void Update() {
            if (isHeld(keyboard?.shiftKey) && wasPressed(keyboard?.f1Key)) setState(!state);
            
            UPDATE_Openness();

            if (!state) return;

            UPDATE_Sizing();
            UPDATE_Scrolling();

            if (!isHeld(keyboard?.altKey) && wasReleased(keyboard?.enterKey)) submit(null);

            if (wasReleased(keyboard.tabKey)) completePrediction();
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
                    float targetHeight = sizing_to == 300f ? 500f : sizing_to == 500f ? canvasRectTrans.sizeDelta.y : 300f;
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

        [ConsoleCommand]
        static void resize_console(float height, bool anim = true) {
            CoreSystem.Instance?.DebugConsole?.resizeConsole(height, anim);
        }

    }


}