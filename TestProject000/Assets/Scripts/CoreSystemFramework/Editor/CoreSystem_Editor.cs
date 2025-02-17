using UnityEditor;
using CoreSystemFramework;

namespace CoreSystemFramework {

    [CustomEditor(typeof(CoreSystem))]
    public class EditorCoreSystem : Editor {
        new CoreSystem target;

        void Awake() => target = (CoreSystem)base.target;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying) return;

            if (CoreSystem.eventSystemList != null) EditorGUILayout.LabelField($"Event system count: {CoreSystem.eventSystemList.Count}");
        }
    }

}