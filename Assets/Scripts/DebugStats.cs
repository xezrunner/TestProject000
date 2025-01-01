using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class DebugStats : MonoBehaviour {
    public static DebugStats Instance;

    [SerializeField] TMP_Text textCom;

    void Awake() {
        if (DebugStats.Instance != null) {
            Debug.LogWarning("Multiple DebugStats instances found - destroying new one.");
            Destroy(this);
            return;
        }
        DebugStats.Instance = this;

        if (!textCom) textCom = GetComponent<TMP_Text>();
        if (!textCom) {
            Debug.LogWarning("DebugStats: No TextMeshPro component found - disabling DebugStats.");
            enabled = false;
        }
    }
    
    const int MAX_LINES = 100;

    const int LINE_CHANNEL_TEMPORARY = 0;
    const int LINE_CHANNEL_PERMANENT = 1;

    int[]     lineCount = new int[2];
    string[,] lines     = new string[2, MAX_LINES];

    [SerializeField] float permanentLineTimeoutSec = 4f;
    
    void addLine(string line, bool permanent = false) {
        if (!enabled) return;

        int index = !permanent ? LINE_CHANNEL_TEMPORARY : LINE_CHANNEL_PERMANENT;
        
        if (index == 0 && lineCount[index] >= MAX_LINES) {
            Debug.LogWarning($"DebugStats: too many lines for line channel {index}! ({lineCount[index]}) - ignoring new lines.");
            return;
        }

        lines[index, lineCount[index]++] = line;
    }

    public static void STATS_PrintLine         (string line) => Instance?.addLine(line);
    public static void STATS_PrintLinePermanent(string line) => Instance?.addLine(line, true);

    public static void STATS_SectionStart    (string name) => Instance?.addLine(name.bold());
    public static void STATS_SectionPrintLine(string line) => Instance?.addLine($"   - {line}");
    public static void STATS_SectionEnd()                  => Instance?.addLine("");

    float permanentLineRemovalTimerSec = 0f;

    StringBuilder sb = new(capacity: MAX_LINES);
    void LateUpdate() {
        // Temporary, per-frame lines:
        for (int i = 0; i < lineCount[LINE_CHANNEL_TEMPORARY]; i++) {
            sb.AppendLine(lines[LINE_CHANNEL_TEMPORARY, i]);
        }

        sb.AppendLine();

        // Permanent lines:
        for (int i = 0; i < lineCount[LINE_CHANNEL_PERMANENT]; i++) {
            sb.AppendLine(lines[LINE_CHANNEL_PERMANENT, i].color("#ffffff80"));
        }

        textCom.SetText(sb);
        
        lineCount[0] = 0;
        sb.Clear();

        permanentLineRemovalTimerSec += Time.deltaTime;
        // When the timeout is up, remove a permanent line:
        if (permanentLineRemovalTimerSec >= permanentLineTimeoutSec) {
            if (lineCount[LINE_CHANNEL_PERMANENT] > 0) --lineCount[LINE_CHANNEL_PERMANENT];
            permanentLineRemovalTimerSec = 0f;
        }
    }
}
