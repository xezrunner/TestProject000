using CoreSystem;
using UnityEngine;
using UnityEngine.InputSystem;

using static CoreSystem.DebugStats;

[DebugStatsSettings(priority: 1, displayName: "A test class")]
class Test: MonoBehaviour {
    void Awake() {
        Debug.Log("Hello from Test!");
    }

    // [ConsoleVariable]
    static float awesomeness = 2f;

    // aliases: [0] will always be function name ('test_command' here), the rest is any aliases you provide
    [ConsoleCommand(aliases: "test", helpText = "help", isCheatCommand = false)] // all of these optional
    static bool test_command(int a = 6, int b = 9) {
        Debug.Log($"Hello from a console command!  {a}, {b}");
        return true; // Return values supported!
    }

    void LateUpdate() {
        STATS_PrintStats();
        if (Keyboard.current.cKey.wasPressedThisFrame) awesomeness += 1;
    }

    void STATS_PrintStats() {
        STATS_PrintLine($"STATS from {gameObject.name}  {Time.time}");
        STATS_PrintLine($"  - awesomeness: {awesomeness}");
    }
}