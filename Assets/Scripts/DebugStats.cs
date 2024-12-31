using System.Text;
using TMPro;
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

    int lineCount = 0;
    string[] lines = new string[MAX_LINES];

    public void AddLine(string line) {
        if (!enabled) return;
        if (lineCount + 1 >= MAX_LINES) {
            Debug.LogWarning($"DebugStats: too many lines! ({lineCount}) - ignoring new lines.");
            return;
        }

        lines[lineCount++] = line;
    }

    public static void STATS_PrintLine(string line)        => Instance?.AddLine(line);

    public static void STATS_SectionStart(string name)     => Instance?.AddLine(name.bold());
    public static void STATS_PrintSectionLine(string line) => Instance?.AddLine($"   - {line}");
    public static void STATS_SectionEnd()                  => Instance?.AddLine("");

    StringBuilder sb = new(capacity: MAX_LINES);
    void LateUpdate() {
        for (int i = 0; i < lineCount; i++) {
            sb.AppendLine(lines[i]);
        }

        textCom.SetText(sb);
        
        sb.Clear();
        lineCount = 0;
    }
}
