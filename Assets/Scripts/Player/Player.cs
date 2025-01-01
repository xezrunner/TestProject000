using UnityEngine;

using static DebugStats;

public class Player : MonoBehaviour
{
    // TODO: player transforms, etc.

    [Header("Systems")]
    public PlayerHealthSystem healthSystem;
    public PlayerMagicSystem  manaSystem;

    [Header("SFX")]
    public PlayerAudioSFX audioSFX;

    void UPDATE_PrintStats() {
        //if ((int)(Time.time * 2) % 2 == 0) STATS_PrintLine($"  FALLING OUT OF BOUNDS".color(Color.red).bold());
    }

    void LateUpdate() {
        UPDATE_PrintStats();
    }
}
