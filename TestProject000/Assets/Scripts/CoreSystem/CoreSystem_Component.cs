using UnityEngine;
using UnityEngine.SceneManagement;

using static CoreSystemUtils;

namespace CoreSystem {

    public partial class CoreSystem : MonoBehaviour {
        public static CoreSystem Instance;

        void Start() {
            StartupShell?.STSHELL_SetActive(CORESYSTEM_STARTUP_OPTS.isFullStartup);

            if (SceneManager.GetActiveScene().name == CORESYSTEM_SCENE_NAME && !Camera.main) { // TEMP:
                var obj = new GameObject();
                var cam = obj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = new(0.05f,0.05f,0.05f,1f);
            }
        }

        [Header("Subsystems")]
        public CoreSystem_StartupShell StartupShell;

        void OnApplicationQuit() {
            SceneManager.sceneLoaded -= SCENEMANAGER_SceneLoaded;
        }

        void Update() {
            if (eventSystemsList.Count > 1) {
                Debug.Log($"[coresystem] multiple event systems ({eventSystemsList.Count}) - de-duplicating event systems...");
                for (int i = 1; i < eventSystemsList.Count; ++i) {
                    Debug.Log($"destroying ES belonging to {eventSystemsList[i].gameObject.name}");
                    // TODO: are we sure we want to keep the first one (belonging to CoreSystem)?
                    DestroyImmediate(eventSystemsList[1].gameObject);
                }
            }
        }
    }

}