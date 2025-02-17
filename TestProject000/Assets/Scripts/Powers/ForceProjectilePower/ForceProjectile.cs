using UnityEngine;

using static CoreSystemFramework.Logging;

public class ForceProjectile : MonoBehaviour {
    [SerializeField] AudioClip SFX_Hit;

    [SerializeField] ParticleSystem PFX_InFlight;
    [SerializeField] ParticleSystem PFX_Hit;

    new Transform  transform;
    new GameObject gameObject;
    [SerializeField] Rigidbody rigidBody;

    [SerializeField] float speed = 10f;
    [SerializeField] float force = 15f;
    bool isHit = false;

    public Vector3 direction;

    void Awake() {
        this.transform  = base.transform;
        this.gameObject = base.gameObject;
    }

    void Start() => SetState(isHit: false);

    public void SetState(bool isHit) {
        if (!isHit) {
            PFX_InFlight.Play();
            PFX_Hit.Stop();
        } else {
            PFX_InFlight.Stop();
            PFX_Hit.Play();

            PlayerAudioSFX.PlayMetaSFXClip(SFX_Hit);
        }
        this.isHit = isHit;
    }

    void OnCollisionEnter(Collision collision) {
        if (isHit) return;

        var collider = collision.collider;
        if (!collider) return;
        
        log($"collision in-flight: {collision.collider.name} at {collision.transform.position}  position: {transform.position}");

        position -= direction * 0.5f;
        transform.SetPositionAndRotation(position, rotation);

        SetState(isHit: true);

        var colliderRb = collider.attachedRigidbody;
        if (colliderRb) colliderRb.AddForce(direction * force, ForceMode.Impulse);
    }

    Vector3 position; Quaternion rotation;
    float timer;
    void FixedUpdate() {
        if (isHit) {
            if (!PFX_Hit.isPlaying) Destroy(gameObject);
            return;
        }

        transform.GetPositionAndRotation(out position, out rotation);
        position += direction * (speed * Time.deltaTime);
        // transform.SetPositionAndRotation(position, rotation);
        // Rather use RigidBody movement for more accurate physics
        // TODO: in theory, this should be in FixedUpdate()!
        rigidBody.MovePosition(position);

        timer += Time.deltaTime;
        if (timer > 5f) isHit = true; // TEMP: Destroy if going on for too long
    }
}
