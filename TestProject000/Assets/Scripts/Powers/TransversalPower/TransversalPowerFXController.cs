using System;
using UnityEngine;

using static DebugStats;

// Dishonored 1 Blink VFX
//
// TODO: Feels a bit too harsh between "eases" - DH1 kind of feels smooth/"relaxed" the entire way

public enum TransversalPowerEffectsState { 
    Idle,   // No effects (pre-/start)
    Warmup, // Aiming effects (subtle)
    In,     // Casting
    Out     // Post-cast rolloff
}

public struct TransversalPowerFXValues {
    public float fovAddition;
    public float radialZoom;
    public float lensDistortion;
}

public class TransversalPowerFXController : MonoBehaviour {
    public float animSpeed = 1f; // TEMP: this shouldn't be public/serialized.
    public float outAnimSpeed = 1.419f; // TEMP: this shouldn't be public/serialized.

    public Camera playerCamera;

    public GameObject     ArrivalSprites;
    // TODO: this is on the player camera in test1a right now!
    // We should localize stuff like this, so that they are not scattered across different objects.
    public ParticleSystem ArrivalParticles;

    public static TransversalPowerFXValues ANIMDATA_State_In = new() {
        fovAddition = 0f,
        radialZoom = 20f,
        lensDistortion = 18f
    };
    public static TransversalPowerFXValues ANIMDATA_State_Warmup = new() {
        fovAddition = 0f,
        radialZoom = 0f,
        lensDistortion = 5f // TEMP: tweak!
    };

    public  TransversalPowerFXValues animData;        // Current values (regardless of state, used in UPDATE_Anim())
    private TransversalPowerFXValues animData_prev;   // Previous state values (used after state change)
    public  TransversalPowerFXValues animData_target; // Target values (used for current state, interpolation)

    public bool IsTest   = false; // For testing above values in real-time.
    public bool IsActive = true;  

    [NonSerialized] public TransversalPowerEffectsState state = TransversalPowerEffectsState.Idle;

    float temp_startFov;
    void Start() {
        if (!playerCamera) {
            Debug.LogWarning("TransversalPowerFXController: player camera has not been assigned. Setting IsActive to false!");
            IsActive = false;
            return;
        }

        temp_startFov = playerCamera.fieldOfView;
        //SetState(state);
    }

    float temp_t_timer;

    public void SetState(TransversalPowerEffectsState newState = TransversalPowerEffectsState.In) {
        Debug.Log($"TransversalPowerFXController SetState(): {state} -> {newState}");

        t = 0f;
        animData_prev = animData; // Store current values, before state change

        switch (newState) {
            case TransversalPowerEffectsState.In: animData_target = ANIMDATA_State_In; break;
            default:                              animData_target = default;           break;
        }

        ArrivalSprites.SetActive(newState == TransversalPowerEffectsState.Out);
        if (newState == TransversalPowerEffectsState.Out) ArrivalParticles.Play();
        
        state = newState;

        temp_t_timer = 0;
    }

    [Range(0, 1)]
    public float t;

    [Range(0,360)]
    public float test;

    const float SINE_IDENTITY_VALUE = 1.5707964f; // The value for which Mathf.Sin returns exactly 1, bearing in mind floating point inaccuracies

    // TODO: Magic value - as of writing, 58 works best, but we really should figure out a better reasoning for this value!
    public float FX_Out_WobbleCount = 58f; // @Wobble

    // Also used by editor for debugging:
    public event  Action EDITOR_RepaintEvent;
    public static bool   EDITOR_RealtimeRepaint = true;
    public (float sine, float value) getValueForOutStateWobble() { // @Wobble
        // Placing the easing function on 't' here results in what Dishonored 2's Blink cooldown (out) looks like.
        float inverseT = 1 - t;

        // The intensity of the wobble, correlated with how many wobbles we want.
        float wobbleIntensity = FX_Out_WobbleCount * inverseT;

#if false
        // float sine = (1f + Mathf.Sin(wobbleT - SINE_IDENTITY_VALUE)) / 2f;
        //
        // Sines aren't working out too well for us, mostly because it's already basically eased.
        // Wrap a larger value to be between 0 and 1, linearly:
        float mod = wobbleIntensity % 2f;
        float sine = (mod > 1f) ? 2f - mod : mod;

        float value = sine * inverseT;
#endif

        float sine = (1f + Mathf.Sin(wobbleIntensity)) / 2f;
        float value = sine * EasingFunctions.InCubic(inverseT);

#if UNITY_EDITOR
        if (EDITOR_RealtimeRepaint) EDITOR_RepaintEvent?.Invoke();
#endif

        return (sine, value);
    }

    void updateAnimData() {
        switch (state) {
            default: {
                // Animate to the given target values:
                animData.radialZoom     = animData_target.radialZoom     * t;
                animData.lensDistortion = animData_target.lensDistortion * t;
                animData.fovAddition    = animData_target.fovAddition    * t;
                break;
            }
            case TransversalPowerEffectsState.Out: { // @Wobble
                // Bring down the effects:
                //animData.radialZoom  = animData_prev.radialZoom         * (1 - t);
                //animData.fovAddition = animData_prev.fovAddition        * (1 - t);
                animData.radialZoom  = animData_prev.radialZoom         * EasingFunctions.InCubic(1 - t);
                animData.fovAddition = animData_prev.fovAddition        * EasingFunctions.InCubic(1 - t);

                // Ping-pong the lens distortion (wobble):
                (float _, float value)  = getValueForOutStateWobble();
                animData.lensDistortion = animData_prev.lensDistortion * value;
                break;
            }
        }

        if (!IsTest && t >= 1f) {
            if      (state == TransversalPowerEffectsState.In)  SetState(TransversalPowerEffectsState.Out);
            else if (state == TransversalPowerEffectsState.Out) SetState(TransversalPowerEffectsState.Idle);
        }
    }

    void updateT() {
        if (IsTest) return;

        // TODO: For warmup, we'll likely be using something like Mathf.Sin or similar, so we need just regular time:
        //       Perhaps we could also just simply use Time.time?
        if (state == TransversalPowerEffectsState.Warmup) {
            t += Time.deltaTime * animSpeed;
            return;
        }
        //else if (state == TransversalPowerEffectsState.Out)  t += Time.deltaTime * 1.419f; // TODO: To reach 1 in 0.7s (ref: DH1-DLC06_Twk_Effects)
        else if (state == TransversalPowerEffectsState.Out)  t += Time.deltaTime * outAnimSpeed; // TODO: To reach 1 in 0.7s (ref: DH1-DLC06_Twk_Effects)
        else if (state != TransversalPowerEffectsState.Idle) t += Time.deltaTime * animSpeed;

        temp_t_timer += Time.deltaTime;

        if (t > 1f) {
            t = 1f;

            // TEMP: measure how long it takes to reach 1 for state:Out - it should take 0.7s * 10.
            if (temp_t_timer > 0f) Debug.Log($"Took {temp_t_timer}s to reach t:1 for state: {state} -- out:0.7*10={0.7f*10f}");
            temp_t_timer = 0f;
        }
    }

    void UPDATE_Anim() {
        if (!IsActive) return;
        
        CameraFX_Settings.radialZoom.radius        = animData.radialZoom;
        CameraFX_Settings.lensDistortion.intensity = animData.lensDistortion;
        playerCamera.fieldOfView                   = temp_startFov + animData.fovAddition;

        //if (IsTest) return;

        updateAnimData();
        updateT();

        //Debug.Log($"UPDATE_Anim(): state: {state}  t: {t}  lensDistortion: {animData.lensDistortion}");
    }

    void Update() {
        UPDATE_Anim();
    }

    void UPDATE_PrintStats() {
        STATS_SectionStart("Transversal VFX Controller");
        
        STATS_SectionPrintLine($"animSpeed: {animSpeed}");
        STATS_SectionPrintLine($"state: {state}");
        STATS_SectionPrintLine($"t: {t}");

        // var wobbleVars = getValueForOutStateWobble();
        // STATS_SectionPrintLine($"wobble: sine: {wobbleVars.sine}  value: {wobbleVars.value}");

        STATS_SectionEnd();
    }

    void LateUpdate() => UPDATE_PrintStats();
}