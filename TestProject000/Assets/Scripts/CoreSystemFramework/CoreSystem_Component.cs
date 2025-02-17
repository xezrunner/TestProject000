using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

using static CoreSystemFramework.Logging;

namespace CoreSystemFramework {

    public partial class CoreSystem : MonoBehaviour {
        public static CoreSystem Instance;

        [Header("Subsystems")]
        public StartupShell StartupShell;
        public DebugStats   DebugStats;
        public DebugConsole DebugConsole;

        [Header("Settings")]
        // TODO: TEMP: not sure how we'll handle full startup -> scenes yet
        [SerializeField] string TEMP_fullStartupTargetScene = "test1a";

        void Awake() {
            if (Instance) {
                Debug.LogError("Multiple CoreSystem instances");
                Application.Quit();
            }
            Instance = this;
            
            Logging.grabInstances();
        }

        void Start() {
            StartupShell?.STSHELL_SetActive(CORESYSTEM_STARTUP_OPTS.isFullStartup);

            if (SceneManager.GetActiveScene().name == CORESYSTEM_SCENE_NAME && !FindAnyObjectByType<Camera>()) { // TEMP:
                var obj = new GameObject("Temporary camera (dev)");
                var cam = obj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = new(0.15f,0.20f,0.15f,1f);
            }

            // TEMP:
            if (CORESYSTEM_STARTUP_OPTS.isFullStartup) {
                var op = SceneManager.LoadSceneAsync($"Scenes/{TEMP_fullStartupTargetScene}/{TEMP_fullStartupTargetScene}", LoadSceneMode.Additive);
                StartCoroutine(SetSceneAsActiveAfterItLoads(op));
            }
        }

        // TEMP:
        IEnumerator SetSceneAsActiveAfterItLoads(AsyncOperation op) {
            while (!op.isDone) yield return null;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(TEMP_fullStartupTargetScene));
            log($"'{TEMP_fullStartupTargetScene}' has been set as the active scene, after loading it from a full startup.");
        }

        void OnDisable() {
            SceneManager.sceneLoaded -= SCENEMANAGER_SceneLoaded;
        }

        void Update() {
            UPDATE_DeduplicateEventSystems();
        }
    }

}