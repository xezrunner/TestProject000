using UnityEngine;

using static CoreSystemFramework.Logging;
using static CoreSystemFramework.QuickInput;

public enum PlayerManaRefillState {
    Idle = 0, Waiting = 1, Refilling = 2 
}

public class PlayerMagicSystem : MonoBehaviour {
    public bool TEST_DisableManaCostsDuringDevelopment = false;
    
    [Header("Mana properties")]
    public float manaMax = 100f;
    
    public float manaRefillValue    = 20f;   // 5 pieces: 100/5 = 20
    public float manaRefillPerSec   = 10.0f;
    public float manaRefillDelaySec = 3.0f;

    [Header("SFX clips")]
    // TODO: Naming! SFX_...
    public AudioClip SFXManaRefill;
    public AudioClip SFXManaEmpty; // TODO: randomize many

    const float SFX_VOLUME = 0.10f;

    public void PlayEmptyManaSFX() {
        PlayerAudioSFX.PlayMetaSFXClip(SFXManaEmpty, SFX_VOLUME);
    }

    float mana = 100f;
    public float getMana() => mana;

    // Returns success; if false, not enough mana.
    public bool ConsumeMana(float amount) {
        if (mana < amount) {
            PlayEmptyManaSFX();
            return false;
        }

        if (TEST_DisableManaCostsDuringDevelopment) return true;

        mana -= amount;

        // The mana should refill to the next "piece" of the mana. e.g.: 100-20=80, refill to 100; 80-20=60, refill to 80; etc...
        // We fudge this by +0.5, so that when you're right at the edge (e.g. mana: 80), it still refills to the next piece.
        //manaTarget = manaRefillValue * Mathf.Ceil((mana + 0.5f) / manaRefillValue);
        manaTarget = mana + manaRefillValue;

        manaRefillTimer = 0f;
        manaRefillState = PlayerManaRefillState.Waiting;

        log($"-{amount.ToString().bold()} -> [mana:{mana}/refill:{manaRefillValue} = {Mathf.Ceil(mana / manaRefillValue)}] * refill:{manaRefillValue} = {manaTarget}");

        return true;
    }
    public bool TestMana(float amount) {
        if (mana >= amount) return true;

        PlayEmptyManaSFX();
        return false;
    }


    PlayerManaRefillState manaRefillState = PlayerManaRefillState.Idle;
    float manaTarget;
    float manaRefillTimer = 0f;
    
    void UPDATE_Mana() {
        switch (manaRefillState) {
            default:
            case PlayerManaRefillState.Idle: break;
            case PlayerManaRefillState.Waiting: {
                if (manaRefillTimer < manaRefillDelaySec) {
                    manaRefillTimer += Time.deltaTime;
                } else {
                    manaRefillState = PlayerManaRefillState.Refilling;
                    PlayerAudioSFX.PlayMetaSFXClip(SFXManaRefill, SFX_VOLUME);
                }
                break;
            }
            case PlayerManaRefillState.Refilling: {
                if (mana < manaTarget) mana = Mathf.Min(mana + (manaRefillPerSec * Time.deltaTime), Mathf.Min(manaTarget, manaMax));
                else                   manaRefillState = PlayerManaRefillState.Idle;
                break;
            }
        }
    }

    void UPDATE_Debug() {
        if (wasPressed(keyboard.hKey)) ConsumeMana(20);
        if (wasPressed(keyboard.gKey)) mana = 100;
    }

    void Update() {
        UPDATE_Mana();
        UPDATE_Debug();
    }

    void UPDATE_PrintStats() {
        // Mana:
        string manaText = "mana: ";
        for (int i = 0; i < 10; i++) manaText += i < (mana / 10) ? '█' : ' ';
        manaText += $"  {mana, 0:##0.000}/{manaMax}  target: {manaTarget}";
        if (mana <= 0f && (int)(Time.time * 2) % 2 == 0) manaText += $"  OUT OF MANA!".color(Color.red).bold();
        STATS_PrintLine(manaText.monospace());

        STATS_PrintLine($"refill state: {manaRefillState}  {manaRefillTimer:0.00}/{manaRefillDelaySec:0.00}");

        // ...
    }
    
    void LateUpdate() {
        UPDATE_PrintStats();
    }
}
