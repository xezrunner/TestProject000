using System;
using System.Runtime.CompilerServices;
using UnityEngine;

// updateSettings() for the render pass:
// Keep updated based on the settings here.
// An attempt was made to automatize this, but performance concerns made it worthless.
partial class CameraFXRenderPass {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void setFloat  (string name, float   value) => material.SetFloat  (ShaderPropertyCache.PROPERTY_CACHE[name], value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void setVector (string name, Vector4 value) => material.SetVector (ShaderPropertyCache.PROPERTY_CACHE[name], value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void setInteger(string name, int     value) => material.SetInteger(ShaderPropertyCache.PROPERTY_CACHE[name], value);

    partial void updateSettings() {
        if (!material) return;

        // TODO: volumes!
        // var volume = VolumeManager.instance?.stack.GetComponent<RadialZoomVolumeComponent>();
        // if (!volume) return;

        // Set shader properties:
        setInteger(nameof(CameraFX_Settings.radialZoom.samples),       CameraFX_Settings.radialZoom.samples);
        setVector (nameof(CameraFX_Settings.radialZoom.center),        CameraFX_Settings.radialZoom.center);
        setFloat  (nameof(CameraFX_Settings.radialZoom.centerFalloff), CameraFX_Settings.radialZoom.centerFalloff);
        setFloat  (nameof(CameraFX_Settings.radialZoom.radius),        CameraFX_Settings.radialZoom.radius);
    }
}

// Main settings:

[Serializable]
public class CameraFX_Settings {
    public static bool printShaderPropertyCacheOnInit = false;
    
    public static CameraFX_RadialZoom_Settings radialZoom = new();
}

// Individual FX: 

// [ShaderPropertySettings] tags a class as containing shader properties.
// [ShaderProperty]         tags a field as a shader property.
// Tagged classes will be scanned for tagged fields, turned into shader cache IDs and cached.
// Using the set<T> wrappers will properly look up the IDs from the cache and it should be, in theory, fast.
// TODO: verify

[Serializable]
[ShaderPropertySettings]
public class CameraFX_RadialZoom_Settings {
    [Range(4, 32)] 
    [ShaderProperty] public int samples = 16;

    [ShaderProperty] public Vector2 center        = new(1.5f, 0.5f);
    [ShaderProperty] public float   centerFalloff = 60;

    [ShaderProperty] public float radius = 0;
}

[Serializable]
[ShaderPropertySettings]
public class CameraFX_LensDistortion_Settings {
    [ShaderProperty] public float test = 0;
    [ShaderProperty] public float intensity = 0;
}