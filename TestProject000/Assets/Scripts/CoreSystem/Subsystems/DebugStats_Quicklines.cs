using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Runtime.CompilerServices;

namespace CoreSystem {

    partial class DebugStats {
        class QuicklineInfo {
            public QuicklineInfo() { }
            public QuicklineInfo(TMP_Text textCom) => this.textCom = textCom;
            
            public bool     isRetired = true;
            public float    timestamp;
            public TMP_Text textCom;

            public void retire() {
                isRetired = true;
                textCom?.SetText((string)null);
            }
        }

        static int QUICKLINES_COUNT = 8;
        QuicklineInfo[] quicklines = new QuicklineInfo[QUICKLINES_COUNT];

        void createQuicklines() {
            for (int i = 0; i < QUICKLINES_COUNT; ++i) {
                var newLine = _quicklineCreateNewLine();
                quicklines[i] = new(newLine.com);
            }
        }

        void resizeQuicklines(int newCount) {
            var newQuicklines = new QuicklineInfo[newCount];
            for (int i = 0; i < newCount; ++i) {
                newQuicklines[i] = new();

                if (i < quicklines.Length) {
                    newQuicklines[i].isRetired = quicklines[i].isRetired;
                    newQuicklines[i].textCom   = quicklines[i].textCom;
                    newQuicklines[i].timestamp = quicklines[i].timestamp;
                } else {
                    newQuicklines[i].textCom = _quicklineCreateNewLine().com;
                }
            }

            if (newCount < QUICKLINES_COUNT) {
                for (int i = QUICKLINES_COUNT - 1; i >= newCount; --i) {
                    Destroy(quicklines[i].textCom.gameObject);
                }
            }

            QUICKLINES_COUNT = newCount;
            quicklines       = newQuicklines; // TODO: leak?            
        }

        (GameObject obj, TMP_Text com) _quicklineCreateNewLine() {
            GameObject obj;
            TMP_Text   com;
            
            if (quicklineTextPreset) {
                obj = Instantiate(quicklineTextPreset, quicklinesContainer);
                com = obj.GetComponent<TextMeshProUGUI>();
                if (!com) Debug.LogError("QL Prefab exists, but no TMP_Text was found on it!");
            } else {
                obj = new("Quickline");
                com = obj.AddComponent<TextMeshProUGUI>();

                //var rectTrans = obj.GetComponent<RectTransform>();
                obj.transform.SetParent(quicklinesContainer);
            }

            return (obj, com);
        }

        void quicklinePush(string text, CallerDebugInfo callerInfo) {
            quicklinePush(text.AddCallerDebugInfo(CallerDebugInfoFlags.FP, callerInfo));
        }
        
        void quicklinePush(string text) {
            // Find available QL line:
            QuicklineInfo target = null;
            for (int i = 0; i < QUICKLINES_COUNT; ++i) {
                var line = quicklines[ i];

                var isFree = true;
                if (line.textCom == null) {
                    line.textCom = _quicklineCreateNewLine().com;
                    target = line; break;
                }
                if (line.isRetired) {
                    target = line; break;
                }
                // if (!line.textCom.text.IsEmpty()) isFree = false;
                if (Time.time < line.timestamp + quicklineTimeoutSec) isFree = false;

                if (isFree) {
                    target = line; break;
                }
            }

            // If no free target was found, shift everything up by 1 and use last line:
            if (target == null) {
                for (int i = 0; i < QUICKLINES_COUNT - 1; ++i) {
                    QuicklineInfo a, b;
                    a = quicklines[i];
                    b = quicklines[i + 1];

                    a.textCom.SetText(b.textCom.text);
                    a.timestamp = b.timestamp;
                }
                target = quicklines[QUICKLINES_COUNT - 1];
            }

            target.timestamp = Time.time;
            target.isRetired = false;
            target.textCom.SetText(text);
        }

        // public static void STATS_PrintQuickLine(string text) => GrabInstance()?.quicklinePush(text);

        public static void STATS_PrintQuickLine(string text, CallerDebugInfo callerInfo) {
            GrabInstance()?.quicklinePush(text, callerInfo);
        }

        public static void STATS_PrintQuickLine(string text, [CallerFilePath]   string callerFilePath = null,
                                                             [CallerMemberName] string callerProcName = null,
                                                             [CallerLineNumber] int    callerLineNum  = -1) {
            GrabInstance()?.quicklinePush(text, new(callerFilePath, callerProcName, callerLineNum));
        }

        void LATEUPDATE_Quicklines() {
            for (int i = 0; i < QUICKLINES_COUNT; ++i) {
                var line = quicklines[i];
                if (!line.isRetired) {
                    if (Time.time < line.timestamp + quicklineTimeoutSec) continue;
                    line.retire();
                }
            }

            if (Keyboard.current.pKey.wasPressedThisFrame) quicklinePush(   $"Test       {Time.time}");
            if (Keyboard.current.oKey.wasPressedThisFrame) Debug.LogWarning($"Warn Test  {Time.time}");
            if (Keyboard.current.iKey.wasPressedThisFrame) Debug.LogError(  $"Err  Test  {Time.time}");

            if (Keyboard.current.nKey.wasPressedThisFrame) resizeQuicklines(6);
            if (Keyboard.current.mKey.wasPressedThisFrame) resizeQuicklines(30);
        }
    }

}