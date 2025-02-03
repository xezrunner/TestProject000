using UnityEngine;
using UnityEngine.InputSystem;

using static CoreSystem.DebugStats;

class Test: MonoBehaviour {
    void Awake() {
        Debug.Log("Hello from Test!");
    }

    float awesomeness = 2f;

    void LateUpdate() {
        STATS_PrintStats();
        if (Keyboard.current.cKey.wasPressedThisFrame) awesomeness += 1;
    }

    void STATS_PrintStats() {
        STATS_PrintLine($"STATS from {gameObject.name}  {Time.time}");
        STATS_PrintLine($"  - awesomeness: {awesomeness}");
    }
}