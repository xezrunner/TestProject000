using UnityEngine;
using static DebugStats;

public enum TransversalPowerState { None = 0, Aiming = 1, Casting = 2, Cooldown = 3 }

class TransversalPower: PlayerPower {
    public TransversalPower() {
        // TODO: attribute for props?
        base.autoConsumeMana = false;
        base.manaCost = 20f;   // TODO: should we define mana portion constants?
        base.cooldownSec = 1f;
    }

    [SerializeField] AudioClip[] SFX_AimingClips  = new AudioClip[2];
    [SerializeField] AudioClip[] SFX_CastingClips = new AudioClip[2];
    [SerializeField] AudioClip[] SFX_SpellClips   = new AudioClip[2];

    TransversalPowerState state = TransversalPowerState.None;

    void setState(TransversalPowerState newState) {
        state = newState;

        if (state == TransversalPowerState.None) base.isBeingCast = false;
        else base.isBeingCast = true;

        playStateSFX();

        if (state == TransversalPowerState.Cooldown) sfxPairIndex = (sfxPairIndex + 1) % SFX_AimingClips.Length;

        timer = 0f; // TEMP:
    }

    public override (bool success, string reason) POWER_Cast() {
        if (state == TransversalPowerState.None) {
            if (!TestMana()) return (false, "not enough mana");

            setState(TransversalPowerState.Aiming);
        }
        else if (state == TransversalPowerState.Aiming) {
            // TODO: additional checks!
            
            // TODO: checking for mana in casting is redundant when aiming tests mana:
            bool success = ConsumeMana();
            if (!success) {
                base.RequestCancel();
                return (false, "not enough mana");
            }

            setState(TransversalPowerState.Casting);
        }
        else return (false, "in cooldown");

        return (true, null);
    }

    public override bool POWER_Cancel() {
        if (state == TransversalPowerState.Aiming) {
            PlayEmptyManaSFX(); // TODO: Play cancel SFX
            // NOTE: In DH1, on cancellation, there is a cooldown. In DH2, this was made more snappy.
            // base.isBeingCast gets set to false on cancellation. In UPDATE_ProcessState(), it is an early return.
            // If we want to add a cooldown on cancellation, we would have to either remove that early return, or
            // handle the cancellation cooldown specifically in some other way.
            setState(TransversalPowerState.None);
        }
        else return false;

        return true;
    }

    int sfxPairIndex = 0;

    const float SFX_MagicVolume = 0.6f;
    const float SFX_SpellVolume = 0.35f;
    
    void playStateSFX() {
        if (state == TransversalPowerState.Aiming) {
            PlayerAudioSFX.PlayMetaSFXClip(SFX_AimingClips[sfxPairIndex], SFX_MagicVolume);
            PlayerAudioSFX.PlayMetaSFXClip(SFX_SpellClips[0], SFX_SpellVolume);
        } else if (state == TransversalPowerState.Casting) {
            PlayerAudioSFX.PlayMetaSFXClip(SFX_CastingClips[sfxPairIndex], SFX_MagicVolume);
            PlayerAudioSFX.PlayMetaSFXClip(SFX_SpellClips[1], SFX_SpellVolume);
        }
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