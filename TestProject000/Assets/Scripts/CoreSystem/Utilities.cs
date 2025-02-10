using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CoreSystem {

    class RequiredComponentAttribute : System.Attribute { }

    static class CoreSystemUtils {
        public static T GetOrFindAndSetObject<T>(ref T toSet) where T : Object {
            if (toSet) return toSet;

            var it = Object.FindFirstObjectByType<T>();
            toSet = it;
            return it;
        }

        public static System.Type getTypeFromFilePath(string path) {
            if (!File.Exists(path)) return null;

            string unityRelativePath = "Assets" + path.Substring(Application.dataPath.Length); // This can crash!
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(unityRelativePath);

            // NOTE: this will only give us the first MonoBehaviour that it finds. Don't have multiple scripts in a file!
            var type = script.GetClass();
            return type;
        }

        public static void processRequiredComponents(object instance, [CallerFilePath] string callerFilePath = null) {
            var fields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields) {
                var attribute = field.GetCustomAttribute<RequiredComponentAttribute>();
                if (attribute == null) continue;
                if (field.GetValue(instance) == null) {
                    var type = getTypeFromFilePath(callerFilePath);
                    Debug.LogError($"{type.Name ?? Path.GetFileName(callerFilePath) ?? "<???>"}: Required Unity component '{field.Name}' (of type {field.FieldType}) has no value after initialization. Assign it in the inspector, provide a fallback in initialization, or disable root component to continue.");
#if UNITY_EDITOR
                    EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            }
        }
    }

    public static class QuickInput {
        // TODO: TODO: TODO: and/or?

        public static Keyboard keyboard = Keyboard.current;
        public static Mouse    mouse    = Mouse.current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool wasPressed(params ButtonControl[] keys) {
            foreach (var key in keys) if (key.wasPressedThisFrame) return true;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isHeld(params ButtonControl[] keys) {
            foreach (var key in keys) if (key.isPressed) return true;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool wasReleased(params ButtonControl[] keys) {
            foreach (var key in keys) if (key.wasReleasedThisFrame) return true;
            return false;
        }
    }

    public static partial class TextExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveFileExtension(this string text) => text[.. text.LastIndexOf('.')];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this string text) {
        return text == null || text == "";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string text, params string[] what) {
        foreach (var it in what) if (text.Contains(it)) return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWith(this string text, params string[] what) {
        foreach (var it in what) if (text.StartsWith(it)) return true;
        return false;
    }
    

    // TODO: Make these safe (?)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int   AsInt  (this string text) => int.Parse(text);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float AsFloat(this string text) => float.Parse(text);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (bool success, bool result) AsBool (this string text) {
        switch (text.ToLower()) {
            case "0":
            case "false":
            case "n":
            case "no":    
                return (true, false);
            case "1":
            case "true":
            case "y":
            case "yes":
                return (true, true);
            default:
                return (false, false);
        }
    }
    #if false
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AsBool (this string text) {
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
    #endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string bold     (this string text) => $"<b>{text}</b>";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string underline(this string text) => $"<u>{text}</u>";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string italic   (this string text) => $"<i>{text}</i>";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string monospace(this string text) => $"<mspace=0.55em>{text}</mspace>";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string color    (this string text, string color_hex) {
        if (color_hex[0] != '#') color_hex.Insert(0, "#");
        return $"<color={color_hex}>{text}</color>";
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string color    (this string text, UnityEngine.Color unity_color, float alpha = -1f) {
        if (alpha >= 0f) unity_color.a = alpha;
        string hex = UnityColorToHex(unity_color);
        return color(text, hex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string UnityColorToHex(this UnityEngine.Color color) {
        string result = UnityEngine.ColorUtility.ToHtmlStringRGBA(color);
        return $"#{result}";
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AddCallerDebugInfo(this string text, CallerDebugInfoFlags format_flags, CallerDebugInfo info) {
            return AddCallerDebugInfo(text, format_flags, info.callerFilePath, info.callerProcName, info.callerLineNumber);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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