using UnityEngine;
using UnityEngine.InputSystem;
using static DebugStats;

public class Player : MonoBehaviour
{
    public static Player Instance;
    void Awake() {
        if (Instance == null) Instance = this;
    }

    // TODO: player transforms, etc.

    public Transform cameraContainerTransform;

    [Header("Systems")]
    public PlayerHealthSystem healthSystem;
    public PlayerMagicSystem  magicSystem;

    [Header("SFX")]
    public PlayerAudioSFX audioSFX;

    void UPDATE_PrintStats() {
        //if ((int)(Time.time * 2) % 2 == 0) STATS_PrintLine($"  FALLING OUT OF BOUNDS".color(Color.red).bold());
        if (Time.timeScale != 1f) STATS_PrintLine($"Timescale: {Time.timeScale}");
    }

    void LateUpdate() {
        UPDATE_PrintStats();

        if (Keyboard.current?.rKey.isPressed ?? false) Time.timeScale = 0.1f;
        else if (Keyboard.current?.tKey.isPressed ?? false) Time.timeScale = 5f;
        else if (Time.timeScale != 1f) Time.timeScale = 1f;
    }
}
