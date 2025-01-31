using UnityEngine;
using UnityEngine.InputSystem;

using static DebugStats;

class PlayerPowerSystem: MonoBehaviour {
    public PlayerPower equippedPower;

    void castEquippedPower() {
        if (equippedPower == null) {
            STATS_PrintQuickLine("no equipped power");
            return;
        }

        var result = equippedPower.RequestCast(); 
        STATS_PrintQuickLine($"{equippedPower.name} -- success: {result.success} {(result.reason != null ? $"({result.reason})" : null)}");
    }

    void cancelEquippedPower() {
        if (equippedPower == null) {
            STATS_PrintQuickLine("no equipped power");
            return;
        }

        bool success = equippedPower.RequestCancel();
        STATS_PrintQuickLine($"{equippedPower.name} -- success: {success}");
    }

    void UPDATE_Input() {
        if (Keyboard.current.jKey.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame) castEquippedPower();
        if (Keyboard.current.kKey.wasPressedThisFrame) cancelEquippedPower();

        // TEMP: switch powers
        // TODO: will need to have a switchPower() that will automatically cancel any on-going powers
        var prevPower = equippedPower;
        if (Keyboard.current.qKey.wasPressedThisFrame) equippedPower = FindAnyObjectByType<TransversalPower>();
        if (Keyboard.current.eKey.wasPressedThisFrame) equippedPower = FindAnyObjectByType<ForceProjectilePower>();

        if (equippedPower != prevPower) prevPower.POWER_Cancel();
    }

    void Update() {
        UPDATE_Input();
    }

    void UPDATE_PrintStats() {
        STATS_SectionStart("Power system");

        STATS_PrintLine($"equipped: {equippedPower?.ToString() ?? "(none)"}");

        STATS_SectionEnd();
    }

    void LateUpdate() {
        UPDATE_PrintStats();
    }
}