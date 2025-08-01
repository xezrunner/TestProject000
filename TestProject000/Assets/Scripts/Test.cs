using CoreSystemFramework;
using UnityEngine;
using UnityEngine.InputSystem;

using static CoreSystemFramework.DebugStats;
using static CoreSystemFramework.Logging;
using static CoreSystemFramework.QuickInput;

[DebugStatsSettings(priority: 1, displayName: "A test class")]
class Test: MonoBehaviour {
    void Awake() {
        Debug.Log("Hello from Test!");
    }

    // [ConsoleVariable]
    static float awesomeness = 2f;

    // TODO: overloads?
    // aliases: [0] will always be function name ('add_command' here), the rest is any aliases you provide
    [ConsoleCommand(aliases: "add", help = "help here", isCheatCommand = false)] // all of the options are optional
    static int add_command(int a = 6, int b = 9) {
        Debug.Log($"Hello from a console command!  {a}, {b}");
        return a + b; // Return values supported!
    }

    [ConsoleCommand()]
    static void proj_logtest() {
        log("Logging from TestProject000()!");
    }

    void LateUpdate() {
        STATS_PrintStats();
        if (wasPressed(keyboard.cKey)) awesomeness += 1;
    }

    void STATS_PrintStats() {
        STATS_PrintLine($"STATS from {gameObject.name}  {Time.time}");
        STATS_PrintLine($"  - awesomeness: {awesomeness}");
    }
}