using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CoreSystem {

    static class CoreSystemUtils {
        public static T GetOrFindAndSetObject<T>(ref T toSet) where T : Object {
            if (toSet) return toSet;

            var it = Object.FindFirstObjectByType<T>();
            toSet = it;
            return it;
        }
    }

    public static partial class TextExtensions {
    public static string RemoveFileExtension(this string text) => text[.. text.LastIndexOf('.')];

    public static bool IsEmpty(this string text) {
        return text == null || text == "";
    }

    public static bool Contains(this string text, params string[] what) {
        foreach (var it in what) {
            if (text.Contains(it)) return true;
        }
        return false;
    }

    // TODO: Make these safe (?)
    public static int   AsInt  (this string text) => int.Parse(text);
    public static float AsFloat(this string text) => float.Parse(text);
    public static bool  AsBool (this string text) {
        switch (text.ToLower()) {
            case "0":
            case "false":
            case "n":
            case "no":    
                return false;
            case "1":
            case "true":
            case "y":
            case "yes":
                return true;
            default: {
                Debug.LogWarning("expected boolean - returning false.");
                return false;
            }
        }
    }

    public static string bold     (this string text) => $"<b>{text}</b>";
    public static string underline(this string text) => $"<u>{text}</u>";
    public static string italic   (this string text) => $"<i>{text}</i>";
    public static string monospace(this string text) => $"<mspace=0.55em>{text}</mspace>";
    public static string color    (this string text, string color_hex) {
        if (color_hex[0] != '#') color_hex.Insert(0, "#");
        return $"<color={color_hex}>{text}</color>";
    }
    public static string color    (this string text, UnityEngine.Color unity_color, float alpha = -1f) {
        if (alpha >= 0f) unity_color.a = alpha;
        string hex = UnityColorToHex(unity_color);
        return color(text, hex);
    }

    public static string UnityColorToHex(this UnityEngine.Color color) {
        string result = UnityEngine.ColorUtility.ToHtmlStringRGBA(color);
        return $"#{result}";
    }
    public static UnityEngine.Color HexToUnityColor(this string hex) {
        UnityEngine.Color color = new(1f, 0f, 0f, 1f);
        UnityEngine.ColorUtility.TryParseHtmlString(hex, out color);
        return color;
    }
}

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
        public CallerDebugInfo(string callerFilePath, string callerProcName, int callerLineNumber) {
            this.callerFilePath   = callerFilePath;
            this.callerProcName   = callerProcName;
            this.callerLineNumber = callerLineNumber;
        }
        public string callerFilePath;
        public string callerProcName;
        public int callerLineNumber;
    }

    public static class CallerDebugInfoUtils {
        public static bool CALLER_RemoveExtFromFilenames = true;

        public static string AddCallerDebugInfo(this string text, CallerDebugInfoFlags format_flags, CallerDebugInfo info) {
            return AddCallerDebugInfo(text, format_flags, info.callerFilePath, info.callerProcName, info.callerLineNumber);
        }

        public static string AddCallerDebugInfo(this string text, CallerDebugInfoFlags formatFlags,
                                                  [CallerFilePath]   string callerFilePath = null,
                                                  [CallerMemberName] string callerProcName = null,
                                                  [CallerLineNumber] int    callerLineNum = -1) {
            if (formatFlags == CallerDebugInfoFlags.None) return text;

            string s = null;
            if (!callerFilePath.IsEmpty() && formatFlags.HasFlag(CallerDebugInfoFlags.FileName)) {
                string fileName = Path.GetFileName(callerFilePath);
                if (CALLER_RemoveExtFromFilenames) fileName = fileName.RemoveFileExtension();
                s += fileName;
            }
            if (!callerProcName.IsEmpty() && formatFlags.HasFlag(CallerDebugInfoFlags.ProcName)) {
                if (formatFlags.HasFlag(CallerDebugInfoFlags.FileName)) s += "::";
                s += $"{callerProcName}()";
            }
            if (formatFlags.HasFlag(CallerDebugInfoFlags.LineNumber)) {
                s += $"@{callerLineNum}";
            }

            s += $": {text}";
            return s;
        }
    }

}