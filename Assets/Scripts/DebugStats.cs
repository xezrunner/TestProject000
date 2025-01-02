using System.Text;
using TMPro;
using UnityEngine;

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

    int perFrameLineCount = 0;
    int quickLineCount = 0;

    string[] perFrameLines = new string[MAX_LINES];
    string[] quickLineLines = new string[MAX_LINES];

    void addPerFrameLine(string line) {
        if (!enabled) return;
        if (perFrameLineCount >= MAX_LINES) {
            Debug.LogWarning($"DebugStats: too many lines for per-frame line channel! ({perFrameLineCount}) - ignoring new lines.");
            return;
        }

        perFrameLines[perFrameLineCount++] = line;
    }

    void addQuickLine(string line) {
        if (!enabled) return;
        if (quickLineCount >= MAX_LINES) {
            Debug.LogWarning($"DebugStats: too many lines for quickline channel! ({quickLineCount}) - ignoring new lines.");
            return;
        }

        quickLineLines[quickLineCount++] = line;
    }

    public static void STATS_PrintLine     (string line)   => Instance?.addPerFrameLine(line);
    public static void STATS_PrintQuickLine(string line)   => Instance?.addQuickLine   (line);

    public static void STATS_SectionStart    (string name) => Instance?.addPerFrameLine(name.bold());
    public static void STATS_SectionPrintLine(string line) => Instance?.addPerFrameLine($"   - {line}");
    public static void STATS_SectionEnd()                  => Instance?.addPerFrameLine("");

    [SerializeField] float permanentLineTimeoutSec = 4f;
    float quickLineRemovalTimerSec = 0f;

    StringBuilder sb = new(capacity: MAX_LINES);
    void LateUpdate() {
        // Temporary, per-frame lines:
        for (int i = 0; i < perFrameLineCount; i++) sb.AppendLine(perFrameLines[i]);

        sb.AppendLine();

        // "More permanent" lines / quicklines:
        // "Quickline" is a name from Rhythmic - it should be lines that are shown for a short time.
        for (int i = 0; i < quickLineCount; i++) sb.AppendLine(quickLineLines[i].color("#ffffff80"));

        textCom.SetText(sb);
        
        perFrameLineCount = 0;
        sb.Clear();

        quickLineRemovalTimerSec += Time.deltaTime;
        // When the timeout is up, remove a permanent line:
        if (quickLineCount > 0 && quickLineRemovalTimerSec >= permanentLineTimeoutSec) {
            --quickLineCount;
            quickLineRemovalTimerSec = 0f;
        }
    }
}
