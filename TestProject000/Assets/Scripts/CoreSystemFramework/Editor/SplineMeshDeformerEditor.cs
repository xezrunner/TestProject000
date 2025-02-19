using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineMeshDeformer))]
class SplineMeshDeformerEditor: Editor {
    SplineMeshDeformer instance;

    void OnEnable() => instance = (SplineMeshDeformer)target;
    
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        if (!instance) return;

        if (!Application.isPlaying) return;

        #if !OLD_IMPL
        
        #else
        if (!instance.ready) return;


        if (GUILayout.Button("Run compute shader")) {
            instance.runComputeShader();
        }

        if (!instance.allowAutoZOffsetUpdate && instance.allowDynamicZOffset) {
            GUILayout.Label("[temp] Z offset:");
            instance.zOffset = EditorGUILayout.Slider(instance.zOffset, 0, 200);
        }
        #endif
    }
}