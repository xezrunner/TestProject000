using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TestSMD))]
class TestSMDEditor: Editor {
    TestSMD instance;

    void OnEnable() => instance = (TestSMD)target;
    
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if (!instance.SMD) return;

        if (!instance.preset) {
            GUILayout.Label("No preset assigned!");
            return;
        }

        if (!Application.isPlaying) return;

        GUILayout.Label($"This will create {instance.x * instance.z} objects.");

        if (GUILayout.Button("Precache / recreate object pool")) instance.preCache();
        if (GUILayout.Button("Spawn array")) instance.spawnArrayOfObjs();
    }
}