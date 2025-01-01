using UnityEngine;
using UnityEngine.InputSystem;
using static DebugStats;

public enum PlayerManaRefillState { Idle = 0, Waiting = 1, Refilling = 2 }

public class PlayerMagicSystem : MonoBehaviour {
    [Header("Mana properties")]
    public  float manaMax = 100f;
    private float mana    = 100f;

    public float manaRefillValue    = 20f;   // 5 pieces: 100/5 = 20
    public float manaRefillPerSec   = 10.0f;
    public float manaRefillDelaySec = 3.0f;

    [Header("SFX clips")]
    public AudioClip SFXManaRefill;
    public AudioClip SFXManaEmpty; // TODO: randomize many

    // Returns success; if false, not enough mana.
    public bool ConsumeMana(float amount) {
        if (mana < amount) {
            PlayerAudioSFX.PlayMetaSFXClip(SFXManaEmpty, 0.45f);
            return false;
        }

        mana -= amount;

        // The mana should refill to the next "piece" of the mana. e.g.: 100-20=80, refill to 100; 80-20=60, refill to 80; etc...
        // We fudge this by +0.5, so that when you're right at the edge (e.g. mana: 80), it still refills to the next piece.
        manaTarget = manaRefillValue * Mathf.Ceil((mana + 0.5f) / manaRefillValue);

        STATS_PrintLinePermanent($"mana refill pieces: {Mathf.Ceil(mana / manaRefillValue)} * {manaRefillValue} = {manaTarget}");

        manaRefillState = PlayerManaRefillState.Waiting;

        return true;
    }

    PlayerManaRefillState manaRefillState = PlayerManaRefillState.Idle;
    float manaTarget;
    float manaRefillTimerSec = 0f;
    
    void UPDATE_Mana() {
        switch (manaRefillState) {
            case PlayerManaRefillState.Idle: break;
            case PlayerManaRefillState.Waiting: {
                if (manaRefillTimerSec < manaRefillDelaySec) {
                    manaRefillTimerSec += Time.deltaTime;
                } else {
                    manaRefillState = PlayerManaRefillState.Refilling;
                    manaRefillTimerSec = 0f;
                    PlayerAudioSFX.PlayMetaSFXClip(SFXManaRefill, 0.45f);
                }
                STATS_PrintLine("BLOCK case PlayerManaRefillState.Waiting: {");
                break;
            }
            case PlayerManaRefillState.Refilling: {
                if (mana < manaTarget) {
                    mana = Mathf.Min(mana + (manaRefillPerSec * Time.deltaTime), Mathf.Min(manaTarget, manaMax));
                } else {
                    manaRefillState = PlayerManaRefillState.Idle;
                }
                STATS_PrintLine("BLOCK case PlayerManaRefillState.Refilling: {");
                break;
            }
        }
    }

    void UPDATE_Debug() {
        if (Keyboard.current.hKey.wasPressedThisFrame) {
            ConsumeMana(20);
        }
    }

    void Update() {
        UPDATE_Mana();
        UPDATE_Debug();
    }

    void UPDATE_PrintStats() {
        STATS_SectionStart("Magic system");

        // Mana:
        string manaText = "mana: ";
        for (int i = 0; i < 10; i++) {
            manaText += i < (mana / 10) ? '█' : ' ';
        }
        manaText += $"  {mana, 0:##0.000}/{manaMax}  target: {manaTarget}";
        if (mana <= 0f && (int)(Time.time * 2) % 2 == 0) manaText += $"  OUT OF MANA!".color(Color.red).bold();
        STATS_SectionPrintLine(manaText.monospace());

        STATS_SectionPrintLine($"refill state: {manaRefillState}");
        STATS_SectionPrintLine($"refill timer: {manaRefillTimerSec}");

        // ...

        STATS_SectionEnd();
    }
    
    void LateUpdate() {
        UPDATE_PrintStats();
    }
}
