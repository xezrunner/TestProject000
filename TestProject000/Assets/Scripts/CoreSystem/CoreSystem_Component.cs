using UnityEngine;
using UnityEngine.SceneManagement;

using CoreSystem;

namespace CoreSystem {

    public partial class CoreSystem : MonoBehaviour {
        public static CoreSystem Instance;

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
                cam.backgroundColor = new(0.05f,0.05f,0.05f,1f);
            }
        }

        [Header("Subsystems")]
        public StartupShell StartupShell;
        public DebugStats   DebugStats;

        void OnApplicationQuit() {
            SceneManager.sceneLoaded -= SCENEMANAGER_SceneLoaded;
        }

        void Update() {
            DeduplicateEventSystems();
        }
    }

}