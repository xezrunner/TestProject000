using UnityEngine;
using UnityEngine.InputSystem;

using static CoreSystem.Logging;
using static CoreSystem.QuickInput;

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
        if (wasPressed(keyboard.jKey, mouse.rightButton)) castEquippedPower();
        if (wasPressed(keyboard.kKey))                    cancelEquippedPower();

        // TEMP: switch powers
        // TODO: will need to have a switchPower() that will automatically cancel any on-going powers
        var prevPower = equippedPower;
        if (wasPressed(keyboard.qKey)) equippedPower = FindAnyObjectByType<TransversalPower>();
        if (wasPressed(keyboard.eKey)) equippedPower = FindAnyObjectByType<ForceProjectilePower>();

        if (equippedPower != prevPower) prevPower.POWER_Cancel();
    }

    void Update() {
        UPDATE_Input();
    }

    void UPDATE_PrintStats() {
        STATS_PrintLine($"equipped: {equippedPower?.ToString() ?? "(none)"}");
    }

    void LateUpdate() {
        UPDATE_PrintStats();
    }
}