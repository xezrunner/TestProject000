using System;
using System.Text;
using Fragsurf.Movement;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public enum TraversalPowerState { None = 0, Aiming = 1, Casting = 2 }

public class TraversalPower : MonoBehaviour {
    [SerializeField] Transform playerTransform;
    [SerializeField] Rigidbody playerRigidbody;
    [SerializeField] SurfCharacter playerSurfCharacter;
    [SerializeField] Transform playerCameraTransform;
    [SerializeField] PlayerAiming playerAiming;

    [SerializeField] GameObject aimingGameObject;
    [SerializeField] Transform aimingTransform;
    [SerializeField] Rigidbody aimingRigidbody;

    [SerializeField] TraversalPowerEffectsController effectsController;

    [SerializeField] TMP_Text playerDebugText;

    public TraversalPowerState state = TraversalPowerState.None;

    public float aimRadius = 10.0f;

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
        if (state == TraversalPowerState.Aiming) {
            Gizmos.DrawWireSphere(lastAimingStartPos, aimRadius);
            Gizmos.DrawLine(lastAimingStartPos, lastAimingTargetPos);
            Gizmos.DrawWireCube(lastAimingTargetPos, playerSurfCharacter.colliderSize);
        }
    }

    Vector3 lastAimingStartPos;
    Vector3 lastAimingTargetPos;
    public void INPUT_SecondaryAttack(InputAction.CallbackContext context) {
        if (!context.action.WasPerformedThisFrame()) return;

        var previousAimingState = state;

        state = previousAimingState != TraversalPowerState.Aiming ? TraversalPowerState.Aiming : TraversalPowerState.None;

        // TEMP:
        // TODO: Cancellation
        if (previousAimingState == TraversalPowerState.Aiming && state == TraversalPowerState.None) Cast();
    }

    void UPDATE_Aiming() {
        if (state != TraversalPowerState.Aiming) return;

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

                String debugText = $"hits: ";
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
        Debug.Log("Casting!");

        casting_t = 0;

        // TODO: refactor into something like TransversalStateControl?
        // The list of stuff we want to disable/enable could change, don't duplicate!
        playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        playerSurfCharacter.moveConfig.enableMovement = false;
        playerAiming.enableBodyRotations = false;

        effectsController?.StartEffect();

        state = TraversalPowerState.Casting;
    }

    void CAST_Reset() {
        state = TraversalPowerState.None;

        // TODO: changing our Rigidbody interpolation to None right when we arrive at the destination
        // causes a visual 'snap', as our position gets set to the final destination.
        // Possible solutions:
        //   - Since the Source movement we use requires the interpolation to be None (as they roll their own),
        //     we could hand over the responsibility of resetting it to None to that (our fork).
        //     That is, set to None when we request movement.
        //
        //   - Wait for the position to stop changing before setting it back to None.
        //     The 'snap' is caused by the position abruptly changing to its intended target, as we request None
        //     while the interpolation is trying to smoothen out the change.
        //     To be fully correct, we should let the interpolation do its job before we disable it.
        //     This also poses the question of whether we should consider that waiting part of the power casting.
        //
        //    - Roll our own interpolation?
        //      Should probably look at how the Source movement does it.
        //      This would be ideal, as then we wouldn't have to rely on Rigidbody interpolation. Would also
        //      make it portable.
        playerRigidbody.interpolation = RigidbodyInterpolation.None;

        playerSurfCharacter.moveConfig.enableMovement = true;
        playerAiming.enableBodyRotations = true;
    }

    Vector3 newPlayerPos;
    float casting_t;
    void UPDATE_Casting() {
        if (state != TraversalPowerState.Casting) return;

        newPlayerPos = Vector3.Lerp(lastAimingStartPos, lastAimingTargetPos, casting_t);
        playerRigidbody.MovePosition(newPlayerPos);

        casting_t += Time.deltaTime;
        if (casting_t > 1f) CAST_Reset();
    }

    void FIXEDUPDATE_Casting() {
        if (state != TraversalPowerState.Casting) return;
    }

    // TODO: no longer needed (?)
    void OnCollisionEnter(Collision other) {
        Debug.Log($"Collision with: {other.gameObject.name}");
    }

    void OnCollisionExit(Collision other) {
        Debug.Log($"No longer colliding with: {other.gameObject.name}");
    }

    void FixedUpdate() {
        //FIXEDUPDATE_Casting();
        UPDATE_Casting();
    }

    void UPDATE_DebugText() {
        if (!playerDebugText) return;

        StringBuilder text = new StringBuilder();
        text.AppendLine($"Player debug");
        text.AppendLine($"  - velocity (x): {playerSurfCharacter.moveData.velocity.x}m");
        text.AppendLine($"  - velocity (y): {playerSurfCharacter.moveData.velocity.y}m");
        text.AppendLine($"  - velocity (z): {playerSurfCharacter.moveData.velocity.z}m");

        playerDebugText.SetText(text);
    }

    void Update() {
        UPDATE_Aiming();
        UPDATE_DebugText();

        if (Keyboard.current?.fKey.wasPressedThisFrame ?? false) {
            Time.timeScale = (Time.timeScale == 1f ? 0.05f : 1f); 
            Debug.Log($"Timescale changed: {Time.timeScale}");
        }

        if (Keyboard.current?.gKey.wasPressedThisFrame ?? false) {
            playerSurfCharacter.moveConfig.enableGravity = !playerSurfCharacter.moveConfig.enableGravity;
            Debug.Log($"enableGravity: {playerSurfCharacter.moveConfig.enableGravity}");
        }
    }
}
