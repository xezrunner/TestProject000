using System.Text;
using Fragsurf.Movement;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// TODO: 
// - How should we tackle sprites on-screen, for arrival? Would post-process FX apply for UI canvas in world mode?

public enum TransversalPowerState { None = 0, Aiming = 1, Casting = 2, Cooldown = 3 }

public class TransversalPower : MonoBehaviour {
    [SerializeField] Transform playerTransform;
    [SerializeField] Rigidbody playerRigidbody;
    [SerializeField] SurfCharacter playerSurfCharacter;
    [SerializeField] Transform playerCameraTransform;
    [SerializeField] PlayerAiming playerAiming;

    [SerializeField] GameObject aimingGameObject;
    [SerializeField] Transform aimingTransform;
    [SerializeField] Rigidbody aimingRigidbody;

    int SFXCurrentSet = 0;
    [SerializeField] AudioSource SFXSource;
    [SerializeField] AudioClip SFXOutOfMagic;
    [SerializeField] AudioClip SFXMagicRegen;
    [SerializeField] AudioClip[] SFXAimClips;
    [SerializeField] AudioClip SFXAimSpellClip;
    [SerializeField] AudioClip[] SFXCastClips;
    [SerializeField] AudioClip SFXCastSpellClip;

    [SerializeField] TransversalPowerFXController effectsController;

    [SerializeField] TMP_Text playerDebugText;

    // TODO: refactor mana out of TransversalPower
    float manaMax = 100.0f;
    float manaRefillPerSec = 10.0f;
    float manaRefillDelay = 3f;
    float mana = 100.0f;

    public TransversalPowerState state = TransversalPowerState.None;

    public float manaCost = 20.0f;
    public float aimRadius = 10.0f;
    public float castCooldownSec = 1.0f;

    void Start() {
        aimingGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

        var collider = aimingGameObject.GetComponent<BoxCollider>();
        collider.size = playerSurfCharacter.colliderSize;

        aimingTransform = aimingGameObject.GetComponent<Transform>();
        aimingTransform.localScale = collider.size;
        aimingTransform.localPosition = new Vector3(900,900,900);

        aimingRigidbody = aimingGameObject.AddComponent<Rigidbody>();
        aimingRigidbody.useGravity = false;
        aimingRigidbody.isKinematic = true;
    }

    void OnDrawGizmos() {
        if (state == TransversalPowerState.Aiming) {
            Gizmos.DrawWireSphere(lastAimingStartPos, aimRadius);
            Gizmos.DrawLine(lastAimingStartPos, lastAimingTargetPos);
            Gizmos.DrawWireCube(lastAimingTargetPos, playerSurfCharacter.colliderSize);
        }
    }

    Vector3 lastAimingStartPos;
    Vector3 lastAimingTargetPos;
    public void INPUT_SecondaryAttack(InputAction.CallbackContext context) {
        if (!context.action.WasPerformedThisFrame()) return;

        AimOrCast();
    }

    void AimOrCast() {
        Debug.Log("TransversalPower/AimOrCast()");
        
        if (state == TransversalPowerState.Cooldown) {
            // NOTE: Don't do anim here!
            Debug.Log("TransversalPower aim failed: on cooldown!");
            return;
        }

        if (mana < manaCost) {
            // TODO: play anim for out of mana
            Debug.Log("TransversalPower aim failed: out of mana!");
            SFXSource.PlayOneShot(SFXOutOfMagic, 0.43f);
            return;
        }

        if (state == TransversalPowerState.Casting) {
            Debug.Log("TransversalPower aim failed: already casting!");
            return;
        }

        // TEMP: Store previous state for determining cast
        var previousAimingState = state;

        state = previousAimingState != TransversalPowerState.Aiming ? TransversalPowerState.Aiming : TransversalPowerState.None;

        // TEMP:
        // TODO: Cancellation
        if (previousAimingState == TransversalPowerState.Aiming && state == TransversalPowerState.None) Cast();

        if (state == TransversalPowerState.Aiming) {
            SFXSource.PlayOneShot(SFXAimSpellClip, 0.3f);
            SFXSource.PlayOneShot(SFXAimClips[SFXCurrentSet], 0.6f);
        } else if (state == TransversalPowerState.Casting) {
            SFXSource.PlayOneShot(SFXCastSpellClip, 0.3f);
            SFXSource.PlayOneShot(SFXCastClips[SFXCurrentSet], 0.6f);
            SFXCurrentSet = (SFXCurrentSet + 1) % 2;
        } else {
            SFXCurrentSet = (SFXCurrentSet + 1) % 2;
        }

    }

    void UPDATE_Aiming() {
        if (state != TransversalPowerState.Aiming) return;

        lastAimingStartPos = transform.position;
        //Debug.Log($"Aiming state: {state} at {lastAimingStartPos}");

        // Raycast to destination:
        if (true) {
            RaycastHit hit;
            bool didHit = Physics.Raycast(lastAimingStartPos, playerCameraTransform.forward, out hit, aimRadius);
            var hits = Physics.BoxCastAll(lastAimingStartPos, playerSurfCharacter.colliderSize / 2, playerCameraTransform.forward, Quaternion.identity, aimRadius, LayerMask.GetMask(new string[] { "Default" }));

            if (didHit) {
                //Vector3 hitPoint = hit.point + Vector3.Scale(hit.normal, playerSurfCharacter.colliderSize / 2);
                Vector3 hitPoint = hit.point;

                hitPoint += Vector3.Scale(hit.normal, playerSurfCharacter.colliderSize / 2);

                string debugText = $"hits: ";
                foreach (var rayhit in hits) {
                    if (rayhit.collider == hit.collider) continue;

                    var distance = (rayhit.point - hit.point) + (playerSurfCharacter.colliderSize / 2);
                    //hitPoint += Vector3.Scale(distance, rayhit.normal);
                    hitPoint += Vector3.Scale(distance, rayhit.normal);
                    debugText += $"{rayhit.collider.name}(normal: {rayhit.normal}, dist: {distance}), ";
                }
                Debug.Log(debugText);
                lastAimingTargetPos = hitPoint;
            } else {
                // TODO: spherical!
                lastAimingTargetPos = lastAimingStartPos + (playerCameraTransform.forward * aimRadius);
            }
        } 
        // Aiming rigidbody:
        else {
            //aimingRigidbody.MovePosition(playerTransform.position + (playerCameraTransform.forward * aimRadius));
            aimingRigidbody.MovePosition(playerTransform.position);
            aimingRigidbody.AddForce(playerCameraTransform.forward * aimRadius, ForceMode.Impulse);
            lastAimingTargetPos = aimingTransform.position;
        }
    }

    // TODO: parameters
    public void Cast() {
        Debug.Log("TransversalPower/Cast()");

        // TODO: Aiming already has out of mana handled. Do we need to handle it here as well?
        if (mana < manaCost) {
            // TODO: play anim for out of mana
            Debug.Log("TransversalPower cast failed: out of mana!");
            return;
        }

        casting_t = 0;
        state = TransversalPowerState.Casting;

        // TODO: factor out mana!
        mana -= manaCost;
        manaRefillTimer = 0f;

        // TODO: refactor all this into something like TransversalStateControl?
        // The list of stuff we want to disable/enable could change, don't duplicate!
        {
            playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            playerSurfCharacter.moveConfig.enableMovement = false;
            playerAiming.enableBodyRotations = false;
        }

        effectsController?.SetState();
    }

    void CAST_Reset() {
        state = TransversalPowerState.Cooldown;
        casting_t = 0;

        // TODO: see Cast() for refactor
        {
            // TODO: Should we roll our own interpolation? Could look at how the Source movement does it.
            // This would be ideal, as then we wouldn't have to rely on Rigidbody interpolation. Would also make it portable.
            playerRigidbody.interpolation = RigidbodyInterpolation.None;

            playerSurfCharacter.moveConfig.enableMovement = true;
            playerAiming.enableBodyRotations = true;
        }
    }

    Vector3 newPlayerPos;
    float casting_t;
    void UPDATE_Casting() {
        // Handle cooldown:
        if (state == TransversalPowerState.Cooldown) {
            casting_t += Time.deltaTime;
            if (casting_t > castCooldownSec) state = TransversalPowerState.None;
        }

        if (state != TransversalPowerState.Casting) return;

        newPlayerPos = Vector3.Lerp(lastAimingStartPos, lastAimingTargetPos, casting_t);
        
        casting_t += Time.deltaTime * effectsController.animSpeed;
        if (casting_t > 1f) CAST_Reset();
    }

    // TODO: should experiment with moving the Rigidbody movement code here.
    // Since Rigidbody interpolation is on during casting, it could be more correct to have
    // the movement requests here.
    void FIXEDUPDATE_Casting() {
        if (state != TransversalPowerState.Casting) return;
        playerRigidbody.MovePosition(newPlayerPos);
    }
    
    float manaPrev = 100.0f;
    float manaRefillTimer = 0f;
    void UPDATE_Mana() {
        if (mana >= manaMax) return;
        if (state == TransversalPowerState.Casting) return;
        
        if (manaRefillTimer >= manaRefillDelay) {
            mana = Mathf.Min(mana + (manaRefillPerSec * Time.deltaTime), manaMax);

            if (mana >= manaMax) {
                manaRefillTimer = 0f;
                return;
            }

            // TEMP: HACK: Play SFX for mana regen
            if (manaPrev != manaRefillTimer) {
                SFXSource.PlayOneShot(SFXMagicRegen, 0.43f);
                manaPrev = manaRefillTimer;
            }
        } else {
            manaRefillTimer += Time.deltaTime;
        }
    }

    // TODO: no longer needed (?)
    void OnCollisionEnter(Collision other) {
        Debug.Log($"Collision with: {other.gameObject.name}");
    }

    void OnCollisionExit(Collision other) {
        Debug.Log($"No longer colliding with: {other.gameObject.name}");
    }

    void FixedUpdate() {
        FIXEDUPDATE_Casting();
        //UPDATE_Casting();
    }

    void UPDATE_DebugText() {
        if (!playerDebugText) return;

        StringBuilder builder = new StringBuilder();

        builder.AppendLine($"Timescale: {Time.timeScale}");

        builder.AppendLine($"\nPlayer debug".bold());
        builder.AppendLine($"  - position: {playerSurfCharacter.moveData.playerTransform.position}");
        //builder.AppendLine($"  - rotation: {playerSurfCharacter.playerRotationTransform.rotation}");
        builder.AppendLine($"  - velocity: {playerSurfCharacter.moveData.velocity}");
        
        builder.AppendLine($"\nMagic debug".bold());
        builder.AppendLine($"  - refill timer: {manaRefillTimer}/{manaRefillDelay}");
        //builder.AppendLine($"  - mana: {mana}/{manaMax}");

        string manaVisual = "";
        for (int i = 0; i < 10; i++) {
            manaVisual += i < (mana / 10) ? '█' : ' ';
        }

        builder.Append($"  - mana: {manaVisual}  {mana, 0:##0.000}/{manaMax}".monospace());
        if (mana <= 0f && (int)(Time.time * 2) % 2 == 0) builder.AppendLine($"  OUT OF MANA!".color(Color.red).bold());
        else builder.AppendLine();

        //builder.AppendLine($"    0 {manaVisual.color(mana < manaCost ? Color.red : Color.white)} 100".monospace());

        //if (mana <= 0f) builder.AppendLine($"    OUT OF MANA!".color(Color.red).bold());

        builder.AppendLine($"\nTransversalPower debug".bold());
        builder.AppendLine($"  - SFX set: {SFXCurrentSet}");
        builder.AppendLine($"  - state: {state}");
        builder.AppendLine($"  - t: {casting_t}");
        if (mana < manaCost && (int)(Time.time * 2) % 2 == 0) builder.AppendLine($"    OUT OF MANA for TransversalPower!".color(Color.red).bold());

        playerDebugText.SetText(builder);
    }

    void Update() {
        UPDATE_Aiming();
        UPDATE_Casting();
        UPDATE_Mana();

        UPDATE_DebugText();

        if (Keyboard.current?.eKey.wasPressedThisFrame ?? false) {
            if (!effectsController.IsTest) {
                Time.timeScale = 0.1f;
                effectsController.SetState(TransversalPowerEffectsState.In);
                effectsController.t = 1f;
            } else {
                effectsController.SetState(TransversalPowerEffectsState.In);
                effectsController.t = 1;
                effectsController.SetState(TransversalPowerEffectsState.Out);
            }
        }

        if (Keyboard.current?.fKey.wasPressedThisFrame ?? false) {
            Time.timeScale = (Time.timeScale == 1f ? 0.1f : 1f); 
            Debug.Log($"Timescale changed: {Time.timeScale}");
        }

        if (Keyboard.current?.gKey.wasPressedThisFrame ?? false) {
            playerSurfCharacter.moveConfig.enableGravity = !playerSurfCharacter.moveConfig.enableGravity;
            Debug.Log($"enableGravity: {playerSurfCharacter.moveConfig.enableGravity}");
        }

        if (Keyboard.current?.hKey.wasPressedThisFrame ?? false) {
            mana -= 15;
            Debug.Log($"Mana reduced by 15 -> {mana}");
        }
    }
}
