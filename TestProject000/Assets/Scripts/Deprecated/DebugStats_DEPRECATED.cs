using System.Runtime.CompilerServices;
using System.Text;
using TMPro;
using UnityEngine;

#if false
public class DebugStats : MonoBehaviour {
    public static DebugStats Instance;

    [SerializeField] TMP_Text textCom;

    void Awake() {
        if (Instance != null) {
            Debug.LogWarning("Multiple DebugStats instances found - destroying new one.");
            Destroy(this);
            return;
        }
        Instance = this;

        if (!textCom) textCom = GetComponent<TMP_Text>();
        if (!textCom) {
            Debug.LogWarning("DebugStats: No TextMeshPro component found - disabling DebugStats.");
            enabled = false;
        }
    }

    const int MAX_LINES = 100;
    const int MAX_QUICKLINES_VIEW = 7;

    string[] perFrameLines  = new string[MAX_LINES];
    string[] quickLineLines = new string[MAX_LINES];

    int perFrameLineCount = 0;
    int quickLineCount = 0;

    void addPerFrameLine(string line) {
        if (!enabled) return;
        if (perFrameLineCount >= MAX_LINES) {
            Debug.LogWarning($"DebugStats: too many lines for per-frame line channel! ({perFrameLineCount}) - ignoring new lines.");
            return;
        }

        perFrameLines[perFrameLineCount++] = line;
    }

    void addQuickLine(string line, [CallerFilePath]   string caller_file_path = null,
                                   [CallerMemberName] string caller_proc_name = null,
                                   [CallerLineNumber] int    caller_line_num = -1) {
        if (!enabled) return;
        if (quickLineCount >= MAX_LINES) {
            Debug.LogWarning($"DebugStats: too many lines for quickline channel! ({quickLineCount}) - ignoring new lines.");
            return;
        }

        if (quickLineCount > MAX_QUICKLINES_VIEW) quickLineViewIndex = quickLineCount - MAX_QUICKLINES_VIEW; // visual overflow

        line = line.add_caller_debug_info(CallerDebugInfoFlags.FP, caller_file_path, caller_proc_name, caller_line_num);

        quickLineLines[quickLineCount++] = line;
        // Debug.Log($"[ql] {line}"); // TEMP: eventually, a logging system should take care of logging out to different targets.
    }

    // TODO: We want a system here where we could define sections as structures (created with STATS_SectionStart) and could assign
    //       priorities to each section.
    //       Currently, script execution order appears to be random, despite setting it in Project Settings.

    public static void STATS_PrintLine     (string line)   => Instance?.addPerFrameLine(line);
    public static void STATS_PrintQuickLine(string line,
                         [CallerFilePath]   string caller_file_path = null,
                         [CallerMemberName] string caller_proc_name = null,
                         [CallerLineNumber] int    caller_line_num = -1) => 
                         Instance?.addQuickLine(line, caller_file_path, caller_proc_name, caller_line_num);

    public static void STATS_SectionStart    (string name) => Instance?.addPerFrameLine(name.bold());
    public static void STATS_PrintLine(string line) => Instance?.addPerFrameLine($"   {line}");
    public static void STATS_SectionEnd()                  => Instance?.addPerFrameLine("");

    int quickLineViewIndex = 0;
    [SerializeField] float quickLineTimeoutSec = 4f;
    float quickLineRemovalTimerSec = 0f;

    StringBuilder sb = new(capacity: MAX_LINES);

    static bool STATS_EnableDebugInfo = false;
    void LateUpdate() {
        if (STATS_EnableDebugInfo) {
            STATS_SectionStart("Debug stats");
            STATS_PrintLine($"ql count: {quickLineCount} ql view index: {quickLineViewIndex}  ql timer: {quickLineRemovalTimerSec:0.00}/{quickLineTimeoutSec:0.00}");
            STATS_SectionEnd();
        }

        // Temporary, per-frame lines:
        for (int i = 0; i < perFrameLineCount; ++i) sb.AppendLine(perFrameLines[i]);

        sb.AppendLine();

        // "More permanent" lines / quicklines:
        // "Quickline" is a name from Rhythmic/XZShared - it should be lines that are shown for a short time.
        for (int i = quickLineViewIndex; i < quickLineCount; ++i) sb.AppendLine(quickLineLines[i].color("#ffffff80"));

        textCom.SetText(sb);

        perFrameLineCount = 0;
        sb.Clear();

        // When the timeout is up, remove a permanent line:
        if (quickLineCount > 0) {
            if (quickLineRemovalTimerSec >= quickLineTimeoutSec) {
                quickLineViewIndex += 1; // View from the next, newer line
                // Once all entries are removed, reset the view index and count:
                // TODO: this limits the quickline count to MAX_LINES if it doesn't fully clear out in time
                if (quickLineViewIndex >= quickLineCount) {
                    quickLineCount = 0;
                    quickLineViewIndex = 0;
                }
                quickLineRemovalTimerSec = 0f;
            }
            else quickLineRemovalTimerSec += Time.deltaTime;
        }
    }
}
#endif