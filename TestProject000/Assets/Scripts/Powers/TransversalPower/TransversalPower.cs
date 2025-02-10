using System.Collections.Generic;
using UnityEngine;
using static CoreSystem.Logging;

public enum TransversalPowerState { None = 0, Aiming = 1, Casting = 2, Cooldown = 3 }

public class TransversalPower: PlayerPower {
    public TransversalPower() {
        // TODO: attribute for props?
        base.autoConsumeMana = false;
        base.manaCost = 20f;   // TODO: should we define mana portion constants?
        base.cooldownSec = 1f;
    }

    Player    playerInstance;
    Transform playerTransform;
    Transform playerCameraTransform;
    Rigidbody playerRigidbody;

    [SerializeField] TransversalPowerFXController vfxController;

    void Awake() {
        playerInstance = Player.Instance;
        if (!playerInstance) {
            Debug.LogError("Player instance not found!");
            Application.Quit();
        }

        // Detach aiming indicator from player, as it requires global positioning:
        aimingIndicatorTransform.SetParent(null);
    }

    void Start() {
        playerTransform       = playerInstance.transform;
        playerCameraTransform = playerInstance.cameraContainerTransform;
        playerRigidbody       = playerInstance.rigidBody;
    }

    [Header("Settings")]
    [SerializeField] float aimingMaxDistance   = 15f; // TODO: aiming will have to be a squashed ellipse at some point
    [SerializeField] float castingBaselineMult = 5f;  // The baseline speed multiplier for casting. This is how fast a max-range cast goes, then scales up with smaller distances.

    [Header("Components")]
    [SerializeField] GameObject aimingIndicatorObject;
    [SerializeField] Transform  aimingIndicatorTransform;

    [SerializeField] float          aimingIndicatorTopParticleSize = 2.5f; // TODO: @Hardcoded
    [SerializeField] Transform      aimingIndicatorTopParticlesTransform;
    [SerializeField] ParticleSystem aimingIndicatorMiddleParticles;
    [SerializeField] Transform      aimingIndicatorBottomParticlesTransform;

    [Header("SFX clips")]
    [SerializeField] AudioClip[] SFX_AimingClips  = new AudioClip[2];
    [SerializeField] AudioClip[] SFX_CastingClips = new AudioClip[2];
    [SerializeField] AudioClip[] SFX_SpellClips   = new AudioClip[2];

    TransversalPowerState state = TransversalPowerState.None;

    void setState(TransversalPowerState newState) {

        if (newState == TransversalPowerState.None) base.isBeingCast = false;
        else base.isBeingCast = true;

        // TODO: handle visibility more precisely
        aimingIndicatorObject.SetActive(newState == TransversalPowerState.Aiming || newState == TransversalPowerState.Casting);

        if (newState == TransversalPowerState.Casting) {
            castingStartPoint   = playerTransform.position;
            float distance      = Vector3.Distance(castingStartPoint, castingTargetPoint);
            castingDistanceFrac = 1f - (distance / aimingMaxDistance);      // Scale down with distance (0 is max distance, 1 is no distance)
            castingDistanceFrac = Mathf.Clamp(castingDistanceFrac, 0f, 1f); // Avoid tiny negative values (is a range between 0-1 anyway)

            // Set player movement properties:

            // BUG: there is an initial jerk as we start the cast
            // BUG: landing is inconsistent as well - at slower timescales, the destination is not guaranteed to be reached properly
            // e.g. on top of ledges and such
            {
                // Initially, we had this work using FixedUpdate, but that results in laggy, jerky motion at slower timescales.
                // This is also apparent in Dishonored 1, though it isn't visible at normal timescales.
                // Dishonored 2 is smooth during Blink travel, but it also somehow* employs collision throughout movement.
                // * When there's a wall opening up from the bottom, and you can aim Blink through it, but not yet fit through as the player,
                // you will get stuck on the wall during travel, but then snap into place as the cast finishes.
                // At the same time, you don't slide on the floor in DH2, which we do here in some instances.
                // Perhaps we shouldn't actually use RigidBody for collision detection here, and rather roll our own basic detection (that perhaps ignores floors with some tolerance),
                // along with our own interpolation to be smooth.
            }

            // TODO: Should we roll our own interpolation? Could look at how the Source movement does it.
            // That would be ideal, as then we wouldn't have to rely on Rigidbody interpolation. Would also make it portable.
            playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            playerInstance.surfCharacter.moveConfig.enableMovement = false;
            playerInstance.playerAiming.enableBodyRotations        = false;
        } else if (state == TransversalPowerState.Casting && newState != TransversalPowerState.Casting) {
            playerRigidbody.interpolation = RigidbodyInterpolation.None;
            playerInstance.surfCharacter.moveConfig.enableMovement = true;
            playerInstance.playerAiming.enableBodyRotations        = true;
            casting_t = 0f;

            // TEMP: TODO: @StuckCollision
            // set final position forcefully
            // 
            // In Dishonored 2 on the curator mission, there's a smol tunnel you can get into with Blink.
            // When you try to Blink inside it, the player's collision doesn't fit, but the Blink does finish and you get teleported
            // to the destination forcefully.
            // The console says this at that point "Blink cancelled: player's velocity is too low!"
            playerTransform.position = castingTargetPoint;
        }

        state = newState;
        
        playStateSFX();
        vfxController?.SetState((TransversalPowerEffectsState)state);
        
        timer = 0f;
    }

    public override (bool success, string reason) POWER_Cast() {
        if (state == TransversalPowerState.None) {
            if (!TestMana()) return (false, "not enough mana");

            setState(TransversalPowerState.Aiming);
        } else if (state == TransversalPowerState.Aiming) {
            // TODO: additional checks!

            // TODO: checking for mana in casting is redundant when aiming tests mana:
            bool success = ConsumeMana();
            if (!success) {
                base.RequestCancel();
                return (false, "not enough mana");
            }

            setState(TransversalPowerState.Casting);
        } else {
            PlayEmptyManaSFX();
            return (false, "in cooldown");
        }

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

    const float SFX_MagicVolume = 0.3f;
    const float SFX_SpellVolume = 0.25f;
    int sfxPairIndex = 0;
    
    void playStateSFX() {
        if (state == TransversalPowerState.Aiming) {
            PlayerAudioSFX.PlayMetaSFXClip(SFX_AimingClips[sfxPairIndex], SFX_MagicVolume);
            PlayerAudioSFX.PlayMetaSFXClip(SFX_SpellClips[0], SFX_SpellVolume);
        } else if (state == TransversalPowerState.Casting) {
            PlayerAudioSFX.PlayMetaSFXClip(SFX_CastingClips[sfxPairIndex], SFX_MagicVolume);
            PlayerAudioSFX.PlayMetaSFXClip(SFX_SpellClips[1], SFX_SpellVolume);
        }
        if (state == TransversalPowerState.Cooldown) sfxPairIndex = (sfxPairIndex + 1) % SFX_AimingClips.Length;
    }

    float timer;

    void OnDrawGizmos() {
        if (state == TransversalPowerState.Aiming) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(playerTransform.position, playerTransform.position + (playerCameraTransform.forward * aimingMaxDistance));

            RaycastHit hitInfo;
            bool hit = Physics.Raycast(origin: playerTransform.position, direction: playerCameraTransform.forward, out hitInfo, aimingMaxDistance);
            if (hit) Gizmos.DrawCube(hitInfo.point, new Vector3(0.1f, 0.1f, 0.1f));

            Gizmos.color = new Color(0f, 0f, 1f, 0.25f);
            Gizmos.DrawCube(DEBUGVIS_AimTargetPosition_BeforePullback, new Vector3(1f, 1f, 1f));
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawCube(DEBUGVIS_AimTargetPosition, new Vector3(1f, 1f, 1f));

            Gizmos.color = Color.cyan;
            foreach (var it in DEBUGVIS_OffsetHits) {
                Gizmos.DrawLine(DEBUGVIS_AimTargetPosition, DEBUGVIS_AimTargetPosition + (-it.normal * it.distance));
                Gizmos.DrawCube(DEBUGVIS_AimTargetPosition + (-it.normal * it.distance), new Vector3(0.1f, 0.1f, 0.1f));
            }

            Gizmos.color = new(1,1,1,0.3f);
            Gizmos.DrawCube(DEBUGVIS_CastTargetPlayerHeightAdjustment, new Vector3(1f, 2f, 1f));
        }
    }

    static readonly Vector3[] RAYCAST_DIRECTIONS = new Vector3[] {
        new(-1,0,0), new(1,0,0), // left, right
        new(0,-1,0), new(0,1,0), // up, down
        new(0,0,1),  new(0,0,-1) // forwards, backwards
    };

    Vector3 castingTargetPoint, castingStartPoint, castingCurrentPoint;
    float castingDistanceFrac;
    float casting_t;

    Vector3 DEBUGVIS_AimTargetPosition_BeforePullback; // @DebugVisualization
    Vector3 DEBUGVIS_AimTargetPosition;                // @DebugVisualization
    Vector3 DEBUGVIS_CastTargetPlayerHeightAdjustment; // @DebugVisualization
    List<RaycastHit> DEBUGVIS_OffsetHits = new();      // @DebugVisualization
    void UPDATE_ProcessState() {
        if (!base.isBeingCast) return;

        timer += Time.deltaTime;

        // Aiming indicator and target position:
        if (state == TransversalPowerState.Aiming) {
            // Furthest aiming point (without collision):
            var targetPoint = playerTransform.position + (playerCameraTransform.forward * aimingMaxDistance);

            // Raycast to find any nearest collision:
            RaycastHit hitInfo;
            bool didHit = Physics.Raycast(origin: playerTransform.position, direction: playerCameraTransform.forward,
                                          hitInfo: out hitInfo, maxDistance: aimingMaxDistance, layerMask: ~LayerMask.GetMask("Player"));

            if (didHit) STATS_PrintLine($"collision: {hitInfo.collider.name}  point: {hitInfo.point}  distance: {hitInfo.distance}");

            if (didHit) targetPoint = hitInfo.point; // Set target point to the collision point (inside geometry!)

            // Currently, the target point is inside some collision. If we imagine the collision is a wall and the indicator is a cube,
            // at this point, the cube would be "inside" the wall, with one half on one side, the other half on the other side.

            // 1. Pull back the target point:
            // This is necessary, as we will be raycasting from inside the indicator, to neatly align with nearby geometry.

            // Some space is required between the indicator center and the collision(s), to figure out the distance between them.
            // If we tried raycasting from the collision point as-is, there would be no hits, as it would start "from the other side".
            // The amount of space should be more than 0, but less than [step 2 max raycast distance].
            // Since it's an approximation, the more we pull back, the more we risk not touching oddly-shaped geometry later on.
            DEBUGVIS_AimTargetPosition_BeforePullback = targetPoint; // @DebugVisualization
            targetPoint -= playerCameraTransform.forward * 0.25f;    // seems safe

            DEBUGVIS_OffsetHits.Clear(); // @DebugVisualization
                                         // 2. Check for collisions around the aiming point, in all directions (cube):
            foreach (var dir in RAYCAST_DIRECTIONS) {
                didHit = Physics.Raycast(targetPoint, dir, out hitInfo, 0.5f);
                if (didHit) {
                    DEBUGVIS_OffsetHits.Add(hitInfo); // @DebugVisualization
                    STATS_PrintLine($"hit: {hitInfo.collider.name}  normal: {-dir}  distance: {hitInfo.distance}");

                    // 3. Offset the target point, so that it is [just outside] the collision:
                    // hitInfo.normal (-dir) is the normal of (direction outwards from) the surface hit by the raycast
                    // 0.5 is the half extent of the indicator. Since we are halfway inside geometry, we want to pull the other half out:
                    targetPoint += -dir * (0.5f - hitInfo.distance);
                }
            }

            // If there was no collision, targetPosition is the furthest point set at the start.
            // Otherwise, it is just outside the nearest collision.

            float playerHeightHalf = playerInstance.surfCharacter.colliderSize.y / 2f;

            castingTargetPoint = targetPoint;
            // For casting, we don't want the player to get stuck in the ground:
            didHit = Physics.Raycast(targetPoint, Vector3.down, out hitInfo, playerHeightHalf);
            if (didHit) {
                castingTargetPoint += Vector3.up * (playerHeightHalf - hitInfo.distance);
                DEBUGVIS_CastTargetPlayerHeightAdjustment = castingTargetPoint; // @DebugVisualization
            }

            // Set aiming indicator position (root):
            // Using SetPositionAndRotation() for global (worldspace) positioning:
            // Local positioning would require detaching the indicator from the player hierarchy - worthless inconvenience.
            aimingIndicatorTransform.SetPositionAndRotation(targetPoint, Quaternion.identity);
            DEBUGVIS_AimTargetPosition = targetPoint; // @DebugVisualization

            // TEMP: @AimingIndicatorParticles
            {
                Vector3 topParticlesPoint = targetPoint;
                if (aimingIndicatorTopParticlesTransform) {
                    topParticlesPoint = targetPoint + (Vector3.up * playerHeightHalf); // TODO: the offset should probably actually be related to camera!

                    didHit = Physics.Raycast(targetPoint, Vector3.up, out hitInfo, maxDistance: aimingIndicatorTopParticleSize);
                    if (didHit) topParticlesPoint += Vector3.down * (aimingIndicatorTopParticleSize - hitInfo.distance);

                    aimingIndicatorTopParticlesTransform.SetPositionAndRotation(topParticlesPoint, aimingIndicatorTopParticlesTransform.rotation);
                }

                if (aimingIndicatorBottomParticlesTransform) {
                    didHit = Physics.Raycast(targetPoint, Vector3.down, out hitInfo);
                    // TODO: 0.3 seems like a magical value...
                    var point = didHit ? hitInfo.point + (Vector3.up * 0.3f) : targetPoint + (Vector3.down * 200f);
                    aimingIndicatorBottomParticlesTransform.SetPositionAndRotation(point, aimingIndicatorBottomParticlesTransform.rotation);

                    if (Vector3.Distance(topParticlesPoint, point) < 0.3f) {
                        point += Vector3.up * playerHeightHalf;
                        aimingIndicatorTopParticlesTransform.SetPositionAndRotation(point, aimingIndicatorTopParticlesTransform.rotation);
                    }
                }
            }
        }

        if (state == TransversalPowerState.Casting) {
            castingCurrentPoint = Vector3.Lerp(castingStartPoint, castingTargetPoint, casting_t);
            // NOTE: Player is moved in FixedUpdate()!

            casting_t += Time.deltaTime * ((1f + castingDistanceFrac) * castingBaselineMult);
            if (casting_t > 1f) setState(TransversalPowerState.Cooldown);
        }

        // TEMP:
        // if (state == TransversalPowerState.Casting  && timer >= 1f) setState(TransversalPowerState.Cooldown);
        if (state == TransversalPowerState.Cooldown && timer >= 1f) setState(TransversalPowerState.None);
    }

    void FixedUpdate() {
        if (state == TransversalPowerState.Casting) playerRigidbody.MovePosition(castingCurrentPoint);
    }

    void Update() {
        UPDATE_ProcessState();
    }

    void UPDATE_PrintStats() {
        STATS_PrintLine($"state: {state}");
        STATS_PrintLine($"timer (dummy): {timer}");
        STATS_PrintLine($"aiming point: absolute: {DEBUGVIS_AimTargetPosition_BeforePullback}  indicator: {aimingIndicatorTransform.position}");
        if (true || state == TransversalPowerState.Casting) {
            STATS_PrintLine(" - Casting state properties:");
            STATS_PrintLine($"    casting start point: {castingStartPoint}  target point: {castingTargetPoint}");
            STATS_PrintLine($"    distance: {Vector3.Distance(playerTransform.position, castingTargetPoint)}m   mult: {castingDistanceFrac} (*baseline: {(1f + castingDistanceFrac) * castingBaselineMult})");
            STATS_PrintLine($"    casting t: {casting_t}");
        }
    }

    void LateUpdate() => UPDATE_PrintStats();

}