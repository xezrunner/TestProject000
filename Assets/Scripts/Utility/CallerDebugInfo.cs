using System.IO;
using System.Runtime.CompilerServices;

public enum CallerDebugInfoFlags {
    None = 0,
    FileName = 1 << 0,
    ProcName = 1 << 1,
    LineNumber = 1 << 2,

    FPL = FileName | ProcName | LineNumber,
    FP = FileName | ProcName,
    FL = FileName | LineNumber,
}

public struct CallerDebugInfo {
    public CallerDebugInfo(string caller_file_path, string caller_proc_name, int caller_line_number) {
        this.caller_file_path = caller_file_path;
        this.caller_proc_name = caller_proc_name;
        this.caller_line_number = caller_line_number;
    }
    public string caller_file_path;
    public string caller_proc_name;
    public int caller_line_number;
}

public static partial class TextExtensions {
    public static bool CALLER_RemoveExtFromFilenames = true;

    public static string add_caller_debug_info(this string text, CallerDebugInfoFlags format_flags, CallerDebugInfo info) {
        return add_caller_debug_info(text, format_flags, info.caller_file_path, info.caller_proc_name, info.caller_line_number);
    }

    public static string add_caller_debug_info(this string text, CallerDebugInfoFlags format_flags,
                                              [CallerFilePath] string caller_file_path = null,
                                              [CallerMemberName] string caller_proc_name = null,
                                              [CallerLineNumber] int caller_line_num = -1) {
        if (format_flags == CallerDebugInfoFlags.None) return text;

        string s = null;
        if (!caller_file_path.is_empty() && format_flags.HasFlag(CallerDebugInfoFlags.FileName)) {
            string file_name = Path.GetFileName(caller_file_path);
            if (CALLER_RemoveExtFromFilenames) file_name = file_name.remove_file_ext();
            s += file_name;
        }
        if (!caller_proc_name.is_empty() && format_flags.HasFlag(CallerDebugInfoFlags.ProcName)) {
            if (format_flags.HasFlag(CallerDebugInfoFlags.FileName)) s += "::";
            s += $"{caller_proc_name}()";
        }
        if (format_flags.HasFlag(CallerDebugInfoFlags.LineNumber)) {
            s += $"@{caller_line_num}";
        }

        s += $": {text}";
        return s;
    }
}