using System;
using UnityEngine;

using static DebugStats;

public enum ForceProjectilePowerState { None = 0, Aiming = 1, Shooting = 2 }

public class ForceProjectilePower : PlayerPower {
    Player    playerInstance;
    Transform playerTransform;
    Transform playerCameraTransform;

    [SerializeField] AudioClip SFX_Aiming;   // Loop info: Start: 2996  End: 22567
    [SerializeField] AudioClip SFX_Shooting;

    [SerializeField] GameObject PREFAB_ForceProjectile;

    void Awake() {
        playerInstance = Player.Instance;
        if (!playerInstance) {
            Debug.LogError("Player instance not found!");
            Application.Quit();
        }

        base.autoConsumeMana = false;
        base.manaCost        = 10f;
        base.cooldownSec     = 0.5f;
    }

    void Start() {
        playerTransform = playerInstance.transform;
        playerCameraTransform = playerInstance.cameraContainerTransform;
    }

    [NonSerialized] ForceProjectilePowerState state = ForceProjectilePowerState.None;

    void Shoot() {
        shootDirection = playerCameraTransform.forward + (playerCameraTransform.up * 0.13f); // TODO: TEMP: offset for force projectile collision size
        shootOrigin = playerTransform.position; // + (shootDirection * 2f); // TODO: how much to offset forwards by?

        STATS_PrintQuickLine($"shootOrigin: {shootOrigin}  shootDirection: {shootDirection}");

        // TODO: pre-cache a few! (?)
        var projectileObject = Instantiate(PREFAB_ForceProjectile, position: shootOrigin, rotation: Quaternion.identity);
        var projectile       = projectileObject.GetComponent<ForceProjectile>();
        projectile.direction = shootDirection;

        PlayerAudioSFX.PlayMetaSFXClip(SFX_Shooting);

        timer = 0f;
    }

    void setState(ForceProjectilePowerState newState) {
        if (newState == ForceProjectilePowerState.Aiming) {
            //PlayerAudioSFX.PlayMetaSFXClip(SFX_Aiming);
        }
        else if (newState == ForceProjectilePowerState.Shooting) Shoot();
        
        state = newState;
    }
    
    public override (bool success, string reason) POWER_Cast() {
        // TODO: constants for failure messages?
        switch (state) {
            case ForceProjectilePowerState.None: {
                if (!TestMana()) return CAST_FAIL_NOMANA;

                //setState(ForceProjectilePowerState.Aiming);
                // TEMP:
                {
                    if (ConsumeMana()) setState(ForceProjectilePowerState.Shooting);
                }
                break;
            }
            case ForceProjectilePowerState.Aiming: {
                if (!ConsumeMana()) {
                    RequestCancel();
                    return CAST_FAIL_NOMANA;
                }

                setState(ForceProjectilePowerState.Shooting);
                break;
            }
            default: {
                PlayEmptyManaSFX();
                return CAST_FAIL_COOLDOWN;
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
