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

    Player    playerInstance;
    Transform playerTransform;
    Transform playerCameraTransform;

    void Awake() {
        playerInstance = Player.Instance;
        if (!playerInstance) {
            Debug.LogError("Player instance not found!");
            Application.Quit();
        }
    }

    void Start() {
        playerTransform = playerInstance.transform;
        playerCameraTransform = playerInstance.cameraContainerTransform;
    }

    [NonSerialized] ForceProjectilePowerState state = ForceProjectilePowerState.None;

    void setState(ForceProjectilePowerState newState) {
        if (newState == ForceProjectilePowerState.Shooting) {
            shootOrigin = playerTransform.position; // TODO: +add?
            shootDirection = playerCameraTransform.forward;
            timer = 0f;
        }
        
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

    void OnDrawGizmos() {
        Gizmos.color = new(1, 1, 1);

        // Aiming:
        // Gizmos.DrawCube(aimTargetPoint, new Vector3(0.5f, 0.5f, 0.5f));

        // Shooting:
        Gizmos.color = new Color(0, 1, 0);
        Gizmos.DrawWireCube(shootOrigin, new Vector3(0.5f, 0.5f, 0.5f));

        Vector3 dest = shootOrigin + (shootDirection * Vector3.Distance(shootOrigin, aimTargetPoint));

        Gizmos.color = new Color(1, 0, 0);
        Gizmos.DrawLine(shootOrigin, dest);
        
        Gizmos.DrawWireCube(aimTargetPoint, new Vector3(0.5f, 0.5f, 0.5f));
    }

    Vector3 aimTargetPoint;
    public float aimMaxDistance = 15f;

    Vector3 shootOrigin;
    Vector3 shootDirection;

    float timer;

    void UPDATE_ProcessState() {
        switch (state) {
            default: break;
            case ForceProjectilePowerState.Aiming: {
                // All of this is just for the aiming visual:
                aimTargetPoint = playerTransform.position + (playerCameraTransform.forward);
                
                RaycastHit hitInfo;
                bool didHit = Physics.Raycast(origin: playerTransform.position, direction: playerCameraTransform.forward, out hitInfo);

                if (didHit) {
                    STATS_PrintLine($"collision: {hitInfo.collider.name}  point: {hitInfo.point}  distance: {hitInfo.distance}");

                    aimTargetPoint = hitInfo.point;
                }

                break;
            }
            case ForceProjectilePowerState.Shooting: {
                STATS_PrintLine("shooting projectile...");

                timer += Time.deltaTime;
                if (timer > cooldownSec) setState(ForceProjectilePowerState.None);
                break;
            }
        }
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
