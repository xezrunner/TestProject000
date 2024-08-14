using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

// updateSettings() for the render pass:
// Keep updated based on the settings here.
// An attempt was made to automatize this, but performance concerns made it worthless.
partial class CameraFXRenderPass {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void setFloat  (string name, float   value) => material.SetFloat  (ShaderPropertyCache.PROPERTY_CACHE[name], value);
    void setVector (string name, Vector4 value) => material.SetVector (ShaderPropertyCache.PROPERTY_CACHE[name], value);
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
    public static CameraFX_RadialZoom_Settings radialZoom = new();
}

// Individual FX: 

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