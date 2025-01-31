using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// TODO: when moved to XZShared, re-route logging!

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ShaderPropertySettingsAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ShaderPropertyAttribute : Attribute { }

public static class ShaderPropertyCache {
    // 0: None
    // 1: Processing status
    // 2: Modules, Types, Fields
    public static int DEBUG_Print = 1;

    public static Dictionary<string, int> PROPERTY_CACHE = new(capacity: 50);

    static ShaderPropertyCache() => BuildShaderPropertyIDs();

    public static void BuildShaderPropertyIDs() {
        PROPERTY_CACHE.Clear();

        string projName = getProjectName();
        if (DEBUG_Print > 0) Debug.Log($"ShaderPropertyCache: building cache for proj {projName}..."); 

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            string moduleName = assembly.FullName.Split(',')[0];

            // Ignore system and other Unity-related modules - those don't contain console commands for us:
            if (!moduleName.StartsWith("Assembly-CSharp")) {    // Allow Unity default Assembly-CSharp
                if (!moduleName.StartsWith(projName)) continue; // Ignore anything that isn't related to the current project
                // ...
            }

            if (DEBUG_Print > 1) Debug.Log($"  - module: |{moduleName}|");

            Type[] types = assembly.GetTypes();
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            foreach (Type type in types) {
                // Filter for shader property settings class attributes:
                if (!type.IsDefined(typeof(ShaderPropertySettingsAttribute))) continue;

                if (DEBUG_Print > 1) Debug.Log($"    - type: {type.Name}");
                
                // Fields:
                FieldInfo[] fieldInfos = type.GetFields(bindingFlags);
                foreach (FieldInfo info in fieldInfos) {
                    if (!info.IsDefined(typeof(ShaderPropertyAttribute))) continue;
                    //ShaderPropertyAttribute attrib = (ShaderPropertyAttribute)info.GetCustomAttribute(typeof(ShaderPropertyAttribute));

                    var fieldName = info.Name;
                    var fullName  = $"{type.Name}.{fieldName}";

                    // Transform the field name into the shader prop name convention:
                    // _CameraFX_RadialZoom_Radius
                    //var shaderPropName = $"_{Char.ToUpper(fieldName[0])}{fieldName[1..]}";
                    var shaderPropName = $"_{type.Name.Replace("_Settings", "")}_{Char.ToUpper(fieldName[0])}{fieldName[1..]}";

                    if (DEBUG_Print > 1) Debug.Log($"      - SHADERPROP: {shaderPropName}  for: '{fullName}'");
                    PROPERTY_CACHE.Add(fullName, Shader.PropertyToID(shaderPropName)); 
                }
            }
        }

        if (DEBUG_Print > 0) Debug.Log($"ShaderPropertyCache: cached {PROPERTY_CACHE.Count} shader properties.");
    }

    static string getProjectName() {
        // TODO: Improve this!
        string[] projectPathTokens = Application.dataPath.Split('/');
#if UNITY_EDITOR
        string projectName = projectPathTokens[^2];
#else
        // In non-editor builds, the dataPath is <proj>/Build/<proj>_Data - we have to go up one more:
        // TODO: This is totally busted on macOS:
        //string projectName = projectPathTokens[^3];
        string projectName = "TestProject000"; // TEMP: workaround
#endif
        return projectName;
    }
}