using UnityEditor;

namespace CoreSystemFramework {

    [CustomEditor(typeof(CoreSystem))]
    public class EditorCoreSystem : Editor {
        new CoreSystem target;

        void Awake() => target = (CoreSystem)base.target;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying) return;

            if (CoreSystem.eventSystemsList != null) EditorGUILayout.LabelField($"Event system count: {CoreSystem.eventSystemsList.Count}");
        }
    }

}