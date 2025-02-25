using System;
using UnityEngine;
using UnityEditor;

using static CoreSystemFramework.Logging;

[ExecuteInEditMode]
class Orbingv2ParticlesController : MonoBehaviour {
    public Collider[] colliders;
    public ParticleSystem pSystem;
    
    [Header("Settings")]
    public float attractionStrength = 1.0f; // Strength of attraction inside bounds
    public float turbulenceStrength = 0.5f; // Base strength of turbulence outside bounds

    [NonSerialized] public int iterations = 0;
    
    public event  Action EDITOR_RepaintEvent;
    public static bool   EDITOR_RealtimeRepaint = true;

    void OnEnable() {
        if (!pSystem) {
            logError("No particle system assinged!");
            EditorApplication.isPlaying = false;
        }
    }
    
    public Vector3 turbulenceAmplitude = new Vector3(0.1f, 0.1f, 0.1f); // Max offset per axis
    public float turbulenceFrequency = 6f; // Spatial scale of turbulence
    public float turbulenceTimeScale = 1f; // Speed of turbulence change over time

    public Vector3 GetTurbulenceVelocity(Vector3 position) {
        // Compute time with time scale for animation
        float t = Time.time * turbulenceTimeScale;

        // Calculate Perlin noise for each component
        // Using different position coordinates for each axis to create varied turbulence
        float xNoise = Mathf.PerlinNoise(position.y * turbulenceFrequency + t, position.z * turbulenceFrequency + t);
        float yNoise = Mathf.PerlinNoise(position.x * turbulenceFrequency + t, position.z * turbulenceFrequency + t);
        float zNoise = Mathf.PerlinNoise(position.x * turbulenceFrequency + t, position.y * turbulenceFrequency + t);

        // Compute the turbulence offset
        // Center the noise (0 to 1) around 0 by subtracting 0.5, then scale by amplitude
        Vector3 offset = new Vector3(
            (xNoise - 0.5f) * turbulenceAmplitude.x,
            (yNoise - 0.5f) * turbulenceAmplitude.y,
            (zNoise - 0.5f) * turbulenceAmplitude.z
        );

        // Convert offset to velocity by dividing by deltaTime
        // This ensures the displacement over one frame matches the original offset
        return offset;
    }

    static Vector3 V3Zero = new(0, 0, 0);

    public static bool IsOutsideBounds(Vector3 position, Vector3 scale)
    {
        Vector3 halfScale = scale * 0.5f;
        return position.x < -halfScale.x || position.x > halfScale.x ||
               position.y < -halfScale.y || position.y > halfScale.y ||
               position.z < -halfScale.z || position.z > halfScale.z;
    }

    public bool boxEnabled = false;
    public float boxSpeed = 1f;
    public Vector3 boxTarget = new(0.636f, 1.832f, 0);
    float boxT = 0f;
    void Update() {
        if (!boxEnabled) return;
        var trans = colliders[0].transform;
        var lerpT = (Mathf.Sin(boxT) + 1f) * 0.5f;
        trans.localPosition = Vector3.Lerp(V3Zero, boxTarget, lerpT);

        boxT += Time.deltaTime * boxSpeed;
    }

    void LateUpdate() {
        int count = pSystem.particleCount;
        var particles = new ParticleSystem.Particle[count];
        pSystem.GetParticles(particles);

        for (int i = 0; i < count; ++i) {
            var collider = colliders[0]; // TEMP:
            bool isInside = collider.bounds.Contains(transform.position + particles[i].position);

            if (!isInside) {
                // if (IsOutsideBounds(particles[i].position, pSystem.shape.scale)) {
                //     particles[i].remainingLifetime = 0;
                //     continue;
                // }

                if (particles[i].startLifetime - particles[i].remainingLifetime < 0.05f) particles[i].remainingLifetime = 0;

                // Find nearest point into the collision:
                // TODO: this will be wrong if the particles / root are not positioned at origin!
                // var closestPointToBounds = collider.ClosestPointOnBounds(particles[i].position);
                // particles[i].position = closestPointToBounds;
            }
        } // for

        pSystem.SetParticles(particles);

        EDITOR_RepaintEvent?.Invoke();
    }
}

[CustomEditor(typeof(Orbingv2ParticlesController))]
class Orbingv2ParticlesControllerEditor : Editor {
    Orbingv2ParticlesController instance;

    void Awake() {
        instance = (Orbingv2ParticlesController)target;
        instance.EDITOR_RepaintEvent += Repaint;
    }
    
    void OnDisable() => instance.EDITOR_RepaintEvent -= Repaint;

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        
        if (!target) return;

        EditorGUILayout.Separator();
    }
}