using TMPro;
using UnityEngine;

namespace CoreSystem {

    public partial class DebugConsole {
        void UPDATE_Openness() {
            if (open_t == 1f) return;
            if (open_t  > 1f) open_t = 1f;

            float panelY = contentRectTrans.rect.height;
            if (state) panelY *= EasingFunctions.InQuad (1 - open_t);
            else       panelY *= EasingFunctions.OutQuad(open_t);
            
            contentRectTrans.anchoredPosition = new(contentRectTrans.anchoredPosition.x, panelY);

            open_t += Time.deltaTime * animationSpeed;
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
            if (sizing_t  > 1f) sizing_t = 1f;

            float panelHeight = Mathf.Lerp(sizing_from, sizing_to, EasingFunctions.OutQuad(sizing_t));
            contentRectTrans.sizeDelta = new(contentRectTrans.sizeDelta.x, panelHeight);

            if (sizing_t == 1f) {
                if (sizing_from >= sizing_to) createConsoleLines(sizing_to);
                updateConsoleOutputUI();
            }

            sizing_t += Time.deltaTime * animationSpeed;
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

            debugTextCom?.SetText($"total lines: {consoleOutputCount}  filtered lines: {consoleOutputFilteredCount}  ui lines: {uiLineCount} | " + 
                                  $"scroll: {scroll:N2}  height: {contentHeight:N3}  indexIntoOutput: {indexIntoOutput} | " +
                                  $"filter: [{(consoleFilterFlags == CONSOLEFILTERFLAGS_ALL ? "None" : $"{consoleFilterFlags}")}]");

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

    }
    
}