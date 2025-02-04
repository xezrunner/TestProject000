using UnityEngine;
using UnityEngine.SceneManagement;

using CoreSystem;

namespace CoreSystem {

    public partial class CoreSystem : MonoBehaviour {
        public static CoreSystem Instance;

        [Header("Subsystems")]
        public StartupShell StartupShell;
        public DebugStats   DebugStats;
        public DebugConsole DebugConsole;

        void Awake() {
            if (Instance) {
                Debug.LogError("Multiple CoreSystem instances");
                Application.Quit();
            }
            Instance = this;
        }

        void Start() {
            StartupShell?.STSHELL_SetActive(CORESYSTEM_STARTUP_OPTS.isFullStartup);

            if (SceneManager.GetActiveScene().name == CORESYSTEM_SCENE_NAME && !Camera.main) { // TEMP:
                var obj = new GameObject("Temporary camera (dev)");
                var cam = obj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = new(0.15f,0.20f,0.15f,1f);
            }
        }

        void OnApplicationQuit() {
            SceneManager.sceneLoaded -= SCENEMANAGER_SceneLoaded;
        }

        void Update() {
            DeduplicateEventSystems();
        }
    }

}