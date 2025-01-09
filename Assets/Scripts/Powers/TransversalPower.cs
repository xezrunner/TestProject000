using UnityEngine;
using static DebugStats;

public enum TransversalPowerState { None = 0, Aiming = 1, Casting = 2, Cooldown = 3 }

class TransversalPower: PlayerPower {
    public TransversalPower() {
        // TODO: attribute for props?
        this.autoConsumeMana = false;
        this.manaCost = 20f;   // TODO: should we define mana portion constants?
        this.cooldownSec = 1f;
    }

    // TODO: port TransversalPower!

    TransversalPowerState state = TransversalPowerState.None;

    void setState(TransversalPowerState newState) {
        state = newState;
        if (state == TransversalPowerState.None) base.isBeingCast = false;
        else base.isBeingCast = true;

        timer = 0f; // TEMP:
    }

    public override bool POWER_Cast() {
        if      (state == TransversalPowerState.None)   setState(TransversalPowerState.Aiming);
        else if (state == TransversalPowerState.Aiming) setState(TransversalPowerState.Casting);
        else return false;

        return true;
    }

    public override bool POWER_Cancel() {
        if (state == TransversalPowerState.Aiming) setState(TransversalPowerState.Cooldown);
        else return false;

        return true;
    }

    float timer;

    void UPDATE_ProcessState() {
        if (!base.isBeingCast) return;

        timer += Time.deltaTime;

        if (state == TransversalPowerState.Casting && timer >= 1f) {
            setState(TransversalPowerState.Cooldown);
        }
        if (state == TransversalPowerState.Cooldown && timer >= 1f) {
            setState(TransversalPowerState.None);
        }
    }

    void Update() {
        UPDATE_ProcessState();
    }

    void UPDATE_PrintStats() {
        STATS_SectionStart("Transversal power");

        STATS_SectionPrintLine($"state: {state}");
        STATS_SectionPrintLine($"timer: {timer}");
        
        STATS_SectionEnd();
    }

    void LateUpdate() => UPDATE_PrintStats();

}