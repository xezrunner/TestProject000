using UnityEngine;

using static DebugStats;

class PlayerPowerSystem: MonoBehaviour {
    public PlayerPower equippedPower;

    void UPDATE_PrintStats() {
        STATS_SectionStart("Power system");

        STATS_PrintLine($"equipped: {equippedPower?.name ?? "none"}");

        STATS_SectionEnd();
    }

    void LateUpdate() {
        UPDATE_PrintStats();
    }
}