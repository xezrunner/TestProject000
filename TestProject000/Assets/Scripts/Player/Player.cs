using System;
using Fragsurf.Movement;
using UnityEngine;

using static DebugStats;
using static CoreSystem.QuickInput;

public class Player : MonoBehaviour
{
    public static Player Instance;
    void Awake() {
        if (Instance == null) Instance = this;
    }

    void Start() {
        transform = base.transform;
        rigidBody = surfCharacter.rb;
    }

    public new Transform transform;

    // TODO: player transforms, etc.

    public Transform cameraContainerTransform;
    public SurfCharacter surfCharacter;
    public PlayerAiming playerAiming;
    [NonSerialized] public Rigidbody rigidBody;

    [Header("Systems")]
    public PlayerHealthSystem healthSystem;
    public PlayerMagicSystem  magicSystem;

    [Header("SFX")]
    public PlayerAudioSFX audioSFX;

    void UPDATE_PrintStats() {
        if (Time.timeScale != 1f) STATS_PrintLine($"Timescale: {Time.timeScale}");

        bool isFallingOutOfBounds = transform.position.y < -150 && surfCharacter.moveData.velocity.y < -100;
        
        STATS_SectionStart("Player info");
        STATS_PrintLine($"position: {transform.position}  {(isFallingOutOfBounds && ((int)(Time.time * 2) % 2 == 0) ? $"  FALLING OUT OF BOUNDS".color(Color.red).bold() : null)}");
        STATS_PrintLine($"velocity: {surfCharacter.moveData.velocity}  forward: {Vector3.Dot(surfCharacter.moveData.velocity, cameraContainerTransform.forward)}");
        STATS_SectionEnd();
    }

    void LateUpdate() {
        UPDATE_PrintStats();

        if (isHeld(keyboard.rKey)) {
            if (isHeld(keyboard.shiftKey)) Time.timeScale = 0.1f; else Time.timeScale = 0.5f;
        }
        else if (isHeld(keyboard.tKey)) Time.timeScale = 5f;
        else if (Time.timeScale != 1f) Time.timeScale = 1f;
    }
}
