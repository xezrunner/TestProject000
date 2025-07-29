using System.Linq;
using UnityEditor;
using UnityEngine;
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

        // TODO: what do we want here, actually?
        // This used to be .SubsystemRegistration, but in builds (at least on Windows), there are no scenes available
        // during initialization at this point.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CORESYSTEM_Init() {
            Logging.LOGGING_Init();
            
            SceneManager.sceneLoaded += SCENEMANAGER_SceneLoaded;

            grabReferenceToEventSystemList();

            var currentSceneName = SceneManager.GetActiveScene().name;

            bool isFullStartup = OVERRIDE_FullStartup  ||
                                 !Application.isEditor ||
                                 currentSceneName == CORESYSTEM_SCENE_NAME;

            Debug.Log($"--- CoreSystem startup ({(isFullStartup ? "full" : "partial")}) ---");

#if UNITY_EDITOR
            // TODO: This should probably be done in an [ExecuteInEditMode] class during editing. We don't need this at runtime.
            if (!EditorBuildSettings.globalScenes[0].path.EndsWith(CORESYSTEM_SCENE_NAME)) {
                verifyAndOrAddSceneToBuildSettings($"{CORESYSTEM_SCENES_PATH}/{CORESYSTEM_SCENE_NAME}", true);
            }
#endif

            // Load CoreSystem if we don't have it yet:
            // FIXME: check whether CoreSystem is loaded more robustly
            if (currentSceneName == null || currentSceneName != CORESYSTEM_SCENE_NAME) {
                // TODO: how do we want to handle scene loading on initialization?
                var loadMode = isFullStartup ? LoadSceneMode.Single : LoadSceneMode.Additive;
                var fullPath = $"Scenes/{CORESYSTEM_SCENES_PATH}/{CORESYSTEM_SCENE_NAME}";

                Debug.Log($"loading coresystem scene: {fullPath}");

                SceneManager.LoadScene(fullPath, loadMode);
                coreSystemScene = SceneManager.GetSceneByName(CORESYSTEM_SCENE_NAME);
            } else {
                Debug.Log("not loading CoreSystem scene during init, as it's already loaded");
            }

            // Prepare startup options:
            CORESYSTEM_STARTUP_OPTS = new() {
                isFullStartup = isFullStartup
            };
        }

        static bool verifyAndOrAddSceneToBuildSettings(string scenePath, bool asFirst = false) {
#if !UNITY_EDITOR
            return false;
#else
            var buildScenes = EditorBuildSettings.globalScenes.ToList();

            var absolutePath = $"Assets/Scenes/{scenePath}.unity";
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(absolutePath);
            if (!sceneAsset) {
                Debug.LogWarning($"no such scene: {scenePath}, or failed to get a reference to it");
                return false;
            }

            EditorBuildSettingsScene found = null;
            foreach (var scene in buildScenes) {
                if (scene.path != absolutePath) continue;
                found = scene; break;
            }

            if (found != null && asFirst && buildScenes[0].path != absolutePath) {
                buildScenes.Remove(found);
                found = null;
            }

            if (found == null) {
                var sceneConfig = new EditorBuildSettingsScene(absolutePath, enabled: true);

                var newBuildScenes = new EditorBuildSettingsScene[buildScenes.Count + 1];
                newBuildScenes[asFirst ? 0 : newBuildScenes.Length - 1] = sceneConfig;
                buildScenes.CopyTo(newBuildScenes, asFirst ? 1 : 0);
                EditorBuildSettings.globalScenes = newBuildScenes;

                // A restart is required for the build scenes to take effect:
                Debug.LogWarning("A CoreSystem scene was not in the global scene list of the build profiles. It has been added, but a restart is required.");
                EditorApplication.ExitPlaymode();
            }

            return true;
            #endif
        }
    }

}