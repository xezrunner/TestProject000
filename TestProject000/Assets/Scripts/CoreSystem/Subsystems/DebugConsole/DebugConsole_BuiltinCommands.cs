using static CoreSystem.Logging;

namespace CoreSystem {

    partial class DebugConsole {
        static DebugConsole console => CoreSystem.Instance?.DebugConsole;

        [ConsoleCommand] static void core_logtest() => log("Logging from CoreSystem!");
        
        [ConsoleCommand] static void resize_console(float height, bool anim = true) => console?.resizeConsole(height, anim);
        
        [ConsoleCommand] static void clear() => console?.clearConsoleOutput();
    }

}