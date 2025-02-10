using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using static CoreSystem.QuickInput;

namespace CoreSystem {

    public partial class DebugConsole {
        void setupUI() {
            consoleInputField.SetTextWithoutNotify(null);
            inputPredictionText.SetText((string)null);
            argsPredictionText.SetText((string)null);

            setupFilterButtons();
        }
        
        void UPDATE_Openness() {
            if (open_t == 1f) return;
            if (open_t  > 1f) open_t = 1f; // this is done twice intentionally

            float panelY = contentRectTrans.rect.height;
            if (state) panelY *= EasingFunctions.InQuad (1 - open_t);
            else       panelY *= EasingFunctions.OutQuad(open_t);
            
            contentRectTrans.anchoredPosition = new(contentRectTrans.anchoredPosition.x, panelY);

            open_t += Time.deltaTime * animationSpeed;
            if (open_t  > 1f) open_t = 1f;
        }

        void resizeConsole(float newHeight, bool anim = true) {
            sizing_from = contentRectTrans.sizeDelta.y;
            sizing_to   = newHeight;
            sizing_t    = anim ? 0f : 1.1f;

            if (sizing_from < sizing_to) createConsoleLines(sizing_to);
        }

        float sizing_t, sizing_from, sizing_to;
        void UPDATE_Sizing() {
            if (sizing_t == 1f) return;
            if (sizing_t  > 1f) sizing_t = 1f; // this is done twice intentionally

            float panelHeight = Mathf.Lerp(sizing_from, sizing_to, EasingFunctions.OutQuad(sizing_t));
            contentRectTrans.sizeDelta = new(contentRectTrans.sizeDelta.x, panelHeight);

            if (sizing_t == 1f) {
                if (sizing_from >= sizing_to) createConsoleLines(sizing_to);
                updateConsoleOutputUI();
            }

            sizing_t += Time.deltaTime * animationSpeed;
            if (sizing_t  > 1f) sizing_t = 1f;
        }

        // We use this to add an extra UI line for virtualized, yet smooth scrolling:
        const int extraLineCountForSmoothScrolling = 2; // NOTE: For larger Y padding, we need more extra lines.
        void createConsoleLines(float height = -1f) {
            var consoleHeight = height == -1f ? scrollRectTrans.rect.height : height; // TODO: size var

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

        void updateConsoleOutputUI() {
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

            debugTextCom?.SetText($"total lines: {consoleOutputCount}  filtered lines: {consoleOutputFilteredCount}  ui lines: {uiLineCount}" + 
                                  $" | scroll: {scroll:N2}  height: {contentHeight:N3}  indexIntoOutput: {indexIntoOutput}" +
                                  $" | filter: [{(consoleFilterFlags == CONSOLEFILTERFLAGS_ALL ? "None" : $"{consoleFilterFlags}")}]" +
                                  $" | current prediction: {currentInputPrediction}");

            for (int i = 0 ; i < uiLineCount; ++i) {
                var uiLine = uiLines[i];

                if (indexIntoOutput + i >= consoleOutputFilteredCount) {
                    // Deactivate invisible lines
                    uiLine.obj.SetActive(false);
                    continue;
                }

                // Write the appropriate log index into the line:
                var line = consoleOutputFiltered[indexIntoOutput + i];
                uiLine.obj.SetActive(true);
                uiLine.com.SetText(line.text);

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

        // UGUI has the scrolling T flipped:
        public const float SCROLL_TOP    = 1f;
        public const float SCROLL_BOTTOM = 0f;

        public void OnScrollingChanged(Vector2 v) {
            // scrollDebugTextCom.SetText($"scroll: {v}");
            updateConsoleOutputUI();
        }

        // TODO: we might want to snap scrolling to the next line
        float scrollTarget = -1f;
        void scroll(float t) {
            scrollTarget = t;
        }

        void UPDATE_Scrolling() {
            if (scrollTarget != -1f) {
                scrollRect.verticalNormalizedPosition = scrollTarget;
                scrollTarget = -1f;
            }
        }

        static (Color normal, Color highlight) filterButtonActiveColors   = (new(1f, 1f, 1f),       new(0.9f, 0.9f, 0.9f));
        static (Color normal, Color highlight) filterButtonInactiveColors = (new(0.2f, 0.2f, 0.2f), new(0.25f, 0.25f, 0.25f));
        public void OnFilterButtonClick(Button button) {
            var flag = Enum.Parse<LogCategory>(button.name);
            LogCategory filterFlags = consoleFilterFlags;

            if (isHeld(keyboard.leftCtrlKey) || isHeld(keyboard.leftCommandKey))
                 filterFlags = flag;
            else filterFlags ^= flag;
            
            setConsoleFilterFlags(filterFlags);

            refreshFilterButtonStates();
        }

        void refreshFilterButtonStates() {
            for (int i = 1; i < filterButtonsContainer.childCount; ++i) {
                var button = filterButtonsContainer.GetChild(i)?.GetComponent<Button>();
                if (!button) continue;

                var flag = Enum.Parse<LogCategory>(button.name);
                
                // Set colors:
                var hasFlag = consoleFilterFlags.HasFlag(flag);
                var colorToSet = hasFlag ? filterButtonActiveColors : filterButtonInactiveColors;
                var colors = button.colors;
                colors.normalColor      = colorToSet.normal;
                colors.selectedColor    = colorToSet.normal;
                colors.highlightedColor = colorToSet.highlight;
                button.colors = colors;

                var text = button.GetComponentInChildren<TMP_Text>();
                if (text) text.color = hasFlag ? Color.black : Color.white;
            }
        }

        void setupFilterButtons() {
            if (!filterButtonsContainer || !filterButtonPreset) {
                pushText($"Filter button [container / preset] not assigned and will not be available. (container: {(bool)filterButtonsContainer}, preset: {(bool)filterButtonPreset})");
            }
            if (filterButtonPreset) filterButtonPreset.SetActive(true);

            var enumNames = Enum.GetNames(typeof(LogCategory));
            for (int i = 1; i < enumNames.Length; ++i) {
                var obj = Instantiate(filterButtonPreset, filterButtonsContainer);
                var com = obj.GetComponentInChildren<TMP_Text>(); // TODO: should we have a button prefab?
                obj.name = enumNames[i];
                com.SetText(enumNames[i][0].ToString());
            }

            if (filterButtonPreset) filterButtonPreset.SetActive(false);

            refreshFilterButtonStates();
        }

        static Dictionary<string, string> friendlyTypeNameAlternatives = new() {
            { typeof(bool)  .Name, "bool" },
            { typeof(float) .Name, "float" },
            { typeof(double).Name, "double" },
        };
        string getFriendlyTypeNameAlternative(string typeName) {
            if (friendlyTypeNameAlternatives.ContainsKey(typeName)) return friendlyTypeNameAlternatives[typeName];
            else return typeName;
        }

        bool showingInlinePrediction    = false;
        bool showingInlineArgPrediction = false;

        void updateCaretWidth() {
            var showing = showingInlinePrediction || showingInlineArgPrediction;
            consoleInputField.caretWidth = showing ? inputFieldPredictingCaretWidth : inputFieldNormalCaretWidth;
        }
        
        void updateInlinePredictionUI(string visualText) {
            inputPredictionText.SetText(visualText);

            if (currentInputPrediction != null) {
                consoleInputField.ForceLabelUpdate();    // Force the label to update on this frame
                consoleInputFieldText.ForceMeshUpdate(); // Update bounds @Performance

                var x = consoleInputFieldText.GetRenderedValues().x; // @Performance
                inputPredictionTextRectTrans.offsetMin = new(x, inputPredictionTextRectTrans.offsetMin.y);

                showingInlinePrediction = true;
            } else {
                showingInlinePrediction = false;
            }
        }

        void updateInlineArgsPredictionUI(ConsoleCommand command = null, string[] tokens = null) {
            if (command == null || tokens == null || tokens.Length - 1 >= command.functionArgsInfo.Length) {
                showingInlineArgPrediction = false;
                argsPredictionText.SetText((string)null);
                return;
            }
            
            // Command argument prediction:
            StringBuilder builder = new(capacity: 30 * command.functionArgsInfo.Length);
            for (int i = tokens.Length - 1; i < command.functionArgsInfo.Length; ++i) {
                var info = command.functionArgsInfo[i];
                string typeName = getFriendlyTypeNameAlternative(info.ParameterType.Name);
                builder.Append($"[{info.Name.bold()}: {typeName}{(info.HasDefaultValue ? $" -- {info.DefaultValue.ToString().ToLower()}" : null)}] ");
            }
            if (builder.Length > 0) builder.Length -= 1; // Remove trailing space
            var returnTypeName = getFriendlyTypeNameAlternative(command.functionReturnType.Name);
            if (command.functionReturnType != typeof(void)) builder.Append($" -> returns: {returnTypeName}");

            argsPredictionText.SetText(builder.ToString());

            // Update bounds @Performance
            // In case we don't have completions above, we have to update the same text com here regardless:
            consoleInputFieldText.ForceMeshUpdate();
            if (!inputPredictionText.text.IsEmpty()) inputPredictionText.ForceMeshUpdate();

            var textInfo = consoleInputFieldText.textInfo;
            var charInfo = textInfo.characterInfo[consoleInputField.text.Length - 1];
            var x = consoleInputFieldText.rectTransform.TransformPoint(charInfo.bottomRight).x;
            x += 20f;
            argsPredictionTextRectTrans.position = new(x, argsPredictionTextRectTrans.position.y);

            showingInlineArgPrediction = true;
        }

    }
    
}