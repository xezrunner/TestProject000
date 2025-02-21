using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityToolbarExtender;

using static CoreSystemFramework.QuickInput;

// https://github.com/marijnz/unity-toolbar-extender

[InitializeOnLoad]
public class ExtendEditorPlayButtons {
    static ExtendEditorPlayButtons() {
        ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
    }

    const string CORESYSTEM_SCENE_NAME = "coresystem";

    const string SHIFT_HELD_TEXT = "(play)";

    static bool DisallowRestoringToCoreScene = true;

    static void OnToolbarGUI() {
        bool isShiftHeld = isHeld(keyboard.shiftKey);
        string coreSystemButtonText = CORESYSTEM_SCENE_NAME + (isShiftHeld ? " (play)" : null);

        if (GUILayout.Button(new GUIContent(coreSystemButtonText, ""), EditorStyles.toolbarButton)) {
            start_scene(CORESYSTEM_SCENE_NAME, isShiftHeld);
        }
        
        if (prevSceneName != null) {
            if (DisallowRestoringToCoreScene && prevSceneName == CORESYSTEM_SCENE_NAME) goto cont;
            
            if (GUILayout.Button(new GUIContent($"back to: {prevSceneName}", ""), EditorStyles.toolbarButton)) {
                start_scene(prevSceneName, false);
            }
        }

    cont:
        GUILayout.FlexibleSpace();
    }

    static string targetSceneName = null;
    static void start_scene(string scene_name, bool play = true) {
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;

        targetSceneName = scene_name;
        shouldPlay = play;

        EditorApplication.update += OnUpdate;
    }

    static bool   shouldPlay = false;
    static string prevSceneName = null;

    static void OnUpdate() {
        if (targetSceneName == null ||
            EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isCompiling ||
            EditorApplication.isPlayingOrWillChangePlaymode) {
            return;
        }
        
        EditorApplication.update -= OnUpdate;

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
            string[] guids = AssetDatabase.FindAssets("t:scene " + targetSceneName, null);
            if (guids.Length == 0) {
                Debug.LogWarning("Couldn't find scene file");
            } else {
                prevSceneName = EditorSceneManager.GetActiveScene().name;

                string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                EditorSceneManager.OpenScene(scenePath);
                if (shouldPlay) EditorApplication.isPlaying = true;
            }
        }
        targetSceneName = null;
    }
}
