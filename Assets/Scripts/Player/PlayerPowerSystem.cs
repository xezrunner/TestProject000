using UnityEngine;
using UnityEngine.InputSystem;

using static DebugStats;

class PlayerPowerSystem: MonoBehaviour {
    public PlayerPower equippedPower = new TransversalPower();

    void castEquippedPower() {
        if (equippedPower == null) {
            STATS_PrintQuickLine("no equipped power");
            return;
        }

        STATS_PrintQuickLine($"casting equipped power: {equippedPower}...");
        equippedPower.RequestCast();
    }

    void UPDATE_Input() {
        if (Keyboard.current?.jKey.wasPressedThisFrame ?? false) {
            castEquippedPower();
        }
    }

    void Update() {
        UPDATE_Input();
    }

    void UPDATE_PrintStats() {
        STATS_SectionStart("Power system");

        STATS_PrintLine($"equipped: {equippedPower?.ToString() ?? "none"}");

        STATS_SectionEnd();
    }

    void LateUpdate() {
        UPDATE_PrintStats();
    }
}