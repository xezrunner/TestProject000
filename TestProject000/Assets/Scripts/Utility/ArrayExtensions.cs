using System;

public static class ArrayExtensions {
    public static T[] FillAll<T>(this T[] array, T value) {
        Array.Fill(array, value);
        return array;
    }
}