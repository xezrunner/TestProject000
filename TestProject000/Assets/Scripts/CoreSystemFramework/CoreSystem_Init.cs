using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace CoreSystemFramework {

    partial class CoreSystem {
        const string CORESYSTEM_SCENES_PATH = "coresystem";
        const string CORESYSTEM_SCENE_NAME = "coresystem";

        static bool OVERRIDE_FullStartup = false;

        struct StartupOptions {
            public bool isFullStartup;
        }
        static StartupOptions CORESYSTEM_STARTUP_OPTS;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // TODO: what do we want here, actually?
        static void CORESYSTEM_Init() {
            SceneManager.sceneLoaded += SCENEMANAGER_SceneLoaded;

            grabReferenceToEventSystemList();

            // TODO: also when launching random scenes
            bool isFullStartup = OVERRIDE_FullStartup || !Application.isEditor;

            Debug.Log($"--- CoreSystem startup ({(isFullStartup ? "full" : "partial")}) ---");

            var currentSceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"current scene name: {currentSceneName}");

            // Load CoreSystem scene:
            if (currentSceneName != CORESYSTEM_SCENE_NAME) {
                // TODO: not sure if we want to handle further scene loading on initialization ourselves:
                var loadMode = isFullStartup ? LoadSceneMode.Single : LoadSceneMode.Additive;
                var fullPath = $"Scenes/{CORESYSTEM_SCENES_PATH}/{CORESYSTEM_SCENE_NAME}";

                Debug.Log($"loading coresystem scene: {fullPath}");
                bool success = verifyAndOrAddSceneToBuildSettings($"{CORESYSTEM_SCENES_PATH}/{CORESYSTEM_SCENE_NAME}");
                Assert.IsTrue(success);

                SceneManager.LoadScene(fullPath, loadMode);
                coreSystemScene = SceneManager.GetSceneByName(CORESYSTEM_SCENE_NAME);
            } else {
                Debug.Log("not loading CoreSystem scene, as the currently loaded scene is already it.");
            }

            // Prepare startup options:
            CORESYSTEM_STARTUP_OPTS = new() {
                isFullStartup = isFullStartup
            };
        }

        static bool verifyAndOrAddSceneToBuildSettings(string scenePath) {
            List<EditorBuildSettingsScene> buildScenes = EditorBuildSettings.globalScenes.ToList();

            var absolutePath = $"Assets/Scenes/{scenePath}.unity";
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(absolutePath);
            if (!sceneAsset) {
                Debug.LogWarning($"no such scene: {scenePath}, or failed to get a reference to it");
                return false;
            }

            var found = false;
            foreach (var scene in buildScenes) {
                if (scene.path != absolutePath) continue;
                found = true; break;
            }

            if (!found) {
                buildScenes.Add(new(absolutePath, enabled: true));
                EditorBuildSettings.globalScenes = buildScenes.ToArray();
                // A restart is required for the build scenes to take effect:
                Debug.LogWarning("A CoreSystem scene was not in the global scene list of the build profiles. It has been added, but a restart is required.");
                EditorApplication.ExitPlaymode();
            }

            return true;
        }
    }

}