using UnityEngine;

static class CoreSystemUtils {
    public static T GetOrFindAndSetObject<T>(ref T toSet) where T : Object {
        if (toSet) return toSet;
        
        var it = Object.FindFirstObjectByType<T>();
        toSet = it;
        return it;
    }

    public static bool IsEmpty(this string text) {
        return text == null || text.Length == 0;
    }
}