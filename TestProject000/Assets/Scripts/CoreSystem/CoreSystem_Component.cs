using UnityEngine;
using UnityEngine.SceneManagement;

using static CoreSystemUtils;

public partial class CoreSystem : MonoBehaviour {
    public static CoreSystem Instance;

    void Start() {
        StartupShell?.STSHELL_SetActive(CORESYSTEM_STARTUP_OPTS.isFullStartup);
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
