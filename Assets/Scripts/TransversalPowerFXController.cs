using System;
using UnityEditor;
using UnityEngine;

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

    public Camera playerCamera;

    public static TransversalPowerFXValues TARGETS_State_In = new() {
        fovAddition = 20f,
        radialZoom = 15f,
        lensDistortion = 16f
    };
    public static TransversalPowerFXValues TARGETS_State_Warmup = new() {
        fovAddition = 0f,
        radialZoom = 0f,
        lensDistortion = 5f // TEMP: tweak!
    };

    TransversalPowerFXValues         animData_prev; // Previous values (used after state change)
    public TransversalPowerFXValues  animData;      // Current values (regardless of state, used in UPDATE_Anim())

    public TransversalPowerFXValues animTargetData;

    public bool IsTest   = false; // For testing above values in real-time.
    public bool IsActive = true;  

    float temp_startFov;
    void Start() {
        temp_startFov = playerCamera.fieldOfView;
    }

    float temp_t_timer;

    TransversalPowerEffectsState _state = TransversalPowerEffectsState.Idle;
    public TransversalPowerEffectsState state { get { return _state; } }
    public void SetState(TransversalPowerEffectsState newState = TransversalPowerEffectsState.In) {
        Debug.Log($"SetState(): {_state} -> {newState}");

        t = 0f;
        animData_prev = animData; // Store current values, before state change

        // TEMP: really?
        animTargetData = _state == TransversalPowerEffectsState.Warmup ? TARGETS_State_Warmup : TARGETS_State_In;
        
        _state = newState;

        temp_t_timer = 0;
    }

    [Range(0, 1)]
    public float t;

    [Range(0,360)]
    public float test;

    const float SINE_IDENTITY_VALUE = 1.5707964f; // The value for which Mathf.Sin returns exactly 1, bearing in mind floating point inaccuracies

    public event Action EDITOR_RepaintEvent;

    public float FX_Out_WobbleCount = 10;

    // Also used by editor for debugging:
    public static bool EDITOR_RealtimeRepaint = true;
    public (float sine, float value) getValueForOutStateWobble() {
        // Placing the easing function on 't' here results in what Dishonored 2's Blink cooldown (out) looks like.
        float inverseT = 1 - t;

        // The intensity of the wobble, correlated with how many wobbles we want.
        // 1 wobble means 1 "quish/expand".
        float wobbleIntensity = inverseT * (FX_Out_WobbleCount * 2);

        // float sine = (1f + Mathf.Sin(wobbleT - SINE_IDENTITY_VALUE)) / 2f;
        //
        // Sines aren't working out too well for us, mostly because it's already basically eased.
        // Wrap a larger value to be between 0 and 1, linearly:
        // TODO: TODO: verify whether this works as we expect:
        float mod = wobbleIntensity % 2f;
        float sine = (mod > 1f) ? 2f - mod : mod;

        float value = sine * inverseT;

#if UNITY_EDITOR
        if (EDITOR_RealtimeRepaint) EDITOR_RepaintEvent?.Invoke();
#endif

        return (sine, value);
    }

    void updateAnimData() {
        switch (_state) {
            default: {
                    // Animate to the given target values:
                    animData.radialZoom = animTargetData.radialZoom         * t;
                    animData.lensDistortion = animTargetData.lensDistortion * t;
                    animData.fovAddition = animTargetData.fovAddition       * t;
                    break;
                }
            case TransversalPowerEffectsState.Out: {
                    // Bring down the effects:
                    animData.radialZoom = animData_prev.radialZoom          * (1 - t);
                    animData.fovAddition = animData_prev.fovAddition        * (1 - t);

                    // Ping-pong the lens distortion (wobble):
                    (float _, float value) = getValueForOutStateWobble();
                    animData.lensDistortion = animData_prev.lensDistortion * value;
                    break;
                }
        }

        if (!IsTest && t >= 1f) {
            if      (_state == TransversalPowerEffectsState.In)  SetState(TransversalPowerEffectsState.Out);
            else if (_state == TransversalPowerEffectsState.Out) SetState(TransversalPowerEffectsState.Idle);
        }
    }

    void updateT() {
        if (IsTest) return;

        // TODO: For warmup, we'll likely be using something like Mathf.Sin or similar, so we need just regular time:
        //       Perhaps we could also just simply use Time.time?
        if (_state == TransversalPowerEffectsState.Warmup) {
            t += Time.deltaTime * animSpeed;
            return;
        }
        else if (_state == TransversalPowerEffectsState.Out)  t += Time.deltaTime * 1.419f; // To reach 1 in 0.7s (ref: DH1-DLC06_Twk_Effects)
        else if (_state != TransversalPowerEffectsState.Idle) t += Time.deltaTime * animSpeed;

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
}