using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CoreSystemFramework.Logging;

namespace CoreSystemFramework {

    partial class DebugConsole {
        static DebugConsole console => CoreSystem.Instance?.DebugConsole;

        [ConsoleCommand] static void core_logtest()   => log      ("Logging from CoreSystem!");
        [ConsoleCommand] static void core_unity_log() => Debug.Log("Logging from CoreSystem through Unity's Debug.Log()!");
        
        [ConsoleCommand] static void resize_console(float height, bool anim = true) => console?.resizeConsole(height, anim);
        
        [ConsoleCommand] static void clear() => console?.clearConsoleOutput();

        [ConsoleCommand(help = "Set the console filter flags. Argument: <flag> or <flag1,flag2,...> (without spaces)")]
        static void filter(string flagString) => console?.setConsoleFilterFlags(Enum.Parse<LogCategory>(flagString));

        [ConsoleCommand] static void array_arg_test(string[] entries, bool test) {
            StringBuilder builder = new();

            builder.Append("Got: [");
            foreach (string arg in entries) { builder.Append(arg); builder.Append(", "); }
            builder.Length -= 2; // remove trailing ', '
            builder.Append($"] and 'test' was {test}.");

            log(builder.ToString());
        }

        [ConsoleCommand] static void help(string commandName) => console?.pushText(console.commands[commandName]?.info.help ?? "command not found");

        [ConsoleCommand] static void set_vsync(int mode) => QualitySettings.vSyncCount = mode;

        [ConsoleCommand] static void level(string name) => CoreSystem.SCENE_Switch(name);

        [ConsoleCommand] static void restart_level() => CoreSystem.SCENE_Switch(SceneManager.GetActiveScene().name);

        [ConsoleCommand] static async void free() {
            await Resources.UnloadUnusedAssets();
            GC.Collect();
        }
    }

}