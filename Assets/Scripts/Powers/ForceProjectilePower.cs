using System;
using UnityEngine;

using static DebugStats;

public enum ForceProjectilePowerState { None = 0, Aiming = 1, Shooting = 2 }

public class ForceProjectilePower : PlayerPower {
    public ForceProjectilePower() {
        base.autoConsumeMana = false;
        base.manaCost        = 20f;
        base.cooldownSec     = 0.5f;
    }

    [NonSerialized] ForceProjectilePowerState state = ForceProjectilePowerState.None;

    void setState(ForceProjectilePowerState newState) {
        state = newState;
    }
    
    public override (bool success, string reason) POWER_Cast() {
        // TODO: constants for failure messages?
        var failNoMana = (false, "not enough mana");

        switch (state) {
            case ForceProjectilePowerState.None: {
                if (!TestMana()) return failNoMana;

                setState(ForceProjectilePowerState.Aiming);
                break;
            }
            case ForceProjectilePowerState.Aiming: {
                if (!ConsumeMana()) { RequestCancel(); return failNoMana; }

                setState(ForceProjectilePowerState.Shooting);
                break;
            }
            default: {
                PlayEmptyManaSFX();
                return (false, "in cooldown");
            }
        }

        return (true, null);
    }

    public override bool POWER_Cancel() {
        return true;
    }

    void UPDATE_ProcessState() {
        
    }

    void Update() {
        UPDATE_ProcessState();
    }

    void UPDATE_PrintStats() {
        STATS_SectionStart("Force Projectile Power");
        
        STATS_SectionPrintLine($"state: {state}");

        STATS_SectionEnd();
    }

    void LateUpdate() => UPDATE_PrintStats();
}
