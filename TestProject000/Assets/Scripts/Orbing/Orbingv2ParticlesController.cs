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
    
    public Vector3 ApplyTurbulence(Vector3 position, float amplitude)
    {
        // Incorporate time to make turbulence dynamic
        float t = Time.time * turbulenceTimeScale;
    
        // Generate noise for each axis using different coordinate pairs
        float xNoise = Mathf.PerlinNoise(position.y * turbulenceFrequency + t, position.z * turbulenceFrequency + t);
        float yNoise = Mathf.PerlinNoise(position.x * turbulenceFrequency + t, position.z * turbulenceFrequency + t);
        float zNoise = Mathf.PerlinNoise(position.x * turbulenceFrequency + t, position.y * turbulenceFrequency + t);
    
        // Compute offset by centering noise (0-1 to -0.5 to 0.5) and scaling by amplitude
        Vector3 offset = new Vector3(
            (xNoise - 0.5f) * amplitude,
            (yNoise - 0.5f) * amplitude,
            (zNoise - 0.5f) * amplitude
        );
    
        // Return the original position plus the turbulent offset
        return position + offset;
    }

    static Vector3 V3Zero = new(0, 0, 0);

    void LateUpdate() {
        int count = pSystem.particleCount;
        var particles = new ParticleSystem.Particle[count];
        pSystem.GetParticles(particles);

        for (int i = 0; i < count; ++i) {
            Vector3 particlePos = particles[i].position;
            bool isInside = false;
            Collider currentCollider = null;

            foreach (var collider in colliders) {
                if (collider.bounds.Contains(particlePos)) {
                    isInside = true;
                    currentCollider = collider;
                    break;
                }
            }
            
            if (isInside) {
                var dist = Vector3.Distance(particlePos, currentCollider.bounds.center);
                Vector3 turbulencePos = ApplyTurbulence(particles[i].position, Mathf.Lerp(0, 0.1f, dist * 0.5f));
                particles[i].position = turbulencePos * 1;
            } else {
                Vector3 turbulencePos = ApplyTurbulence(particles[i].position, 0.1f);
                particles[i].position = turbulencePos;
            }

#if false
            if (isInside && currentCollider != null) {
                // Attract particle towards the center of the bounds:
                Vector3 center = currentCollider.bounds.center;
                Vector3 direction = (center - particlePos).normalized;

                // particles[i].velocity = direction * attractionStrength * Time.deltaTime;
                // particles[i].velocity = V3Zero;
                
                //particles[i].position = new(particles[i].position.x + (direction.x * attractionStrength * Time.deltaTime), particles[i].position.y, particles[i].position.z);
                
                particles[i].velocity = V3Zero;
                // particles[i].position = V3Zero;
            } else {
                // Apply turbulence based on distance to nearest bound
                float minDistance = float.MaxValue;
                foreach (var collider in colliders) {
                    Vector3 closestPoint = collider.ClosestPoint(particlePos);
                    float distance = Vector3.Distance(particlePos, closestPoint);
                    minDistance = Mathf.Min(minDistance, distance);
                }

                Vector3 randomDirection = UnityEngine.Random.insideUnitSphere.normalized;
                particles[i].velocity += randomDirection * turbulenceStrength * minDistance;
                particles[i].velocity *= Time.deltaTime;
                // particles[i].velocity = V3Zero;
                // particles[i].position = new(-5,0,0);
            }
#endif
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

        GUILayout.Label($"Iterations: {instance.iterations}");
    }
}