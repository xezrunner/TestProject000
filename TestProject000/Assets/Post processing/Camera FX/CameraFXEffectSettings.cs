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
        setInteger("CameraFX_RadialZoom_Settings.samples",       CameraFX_Settings.radialZoom.samples);
        setVector ("CameraFX_RadialZoom_Settings.center",        CameraFX_Settings.radialZoom.center);
        setFloat  ("CameraFX_RadialZoom_Settings.centerFalloff", CameraFX_Settings.radialZoom.centerFalloff);
        setFloat  ("CameraFX_RadialZoom_Settings.radius",        CameraFX_Settings.radialZoom.radius);

        setFloat("CameraFX_LensDistortion_Settings.intensity", CameraFX_Settings.lensDistortion.intensity);
        setFloat("CameraFX_LensDistortion_Settings.enableSquishing", CameraFX_Settings.lensDistortion.enableSquishing);
        setFloat("CameraFX_LensDistortion_Settings.squishIntensity", CameraFX_Settings.lensDistortion.squishIntensity);

        setVector("CameraFX_AdditiveColor_Settings.color",     CameraFX_Settings.additiveColor.color);
        setFloat ("CameraFX_AdditiveColor_Settings.intensity", CameraFX_Settings.additiveColor.intensity);

        // TEMP: TEMP: TEMP:
        var tex = Resources.Load<Texture>("Temp/test1");
        material.SetTexture(ShaderPropertyCache.PROPERTY_CACHE["CameraFX_Test_Settings.image"], tex);
    } 
}

// Main settings:

[Serializable]
public class CameraFX_Settings {
    public static bool printShaderPropertyCacheOnInit = false;

    // TODO: CameraFXRenderPass has an instance of us, but we are using this class as static storage.
    // This is bad! Powers should probably each have an instance of settings (?)
    // Or something, idk
    static CameraFX_Settings() {
        CameraFX_Settings.ResetAll();
    }

    public static void ResetAll()
    {
        radialZoom     = new();
        lensDistortion = new();
        additiveColor  = new();
        test = new();
    }
    
    public static CameraFX_RadialZoom_Settings     radialZoom;
    public static CameraFX_LensDistortion_Settings lensDistortion;
    public static CameraFX_AdditiveColor_Settings  additiveColor;
    public static CameraFX_Test_Settings  test;
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
    [ShaderProperty] public int samples = 32; // TODO: 64?

    [ShaderProperty] public Vector2 center        = new(1.5f, 0.5f);
    [ShaderProperty] public float   centerFalloff = 35;

    [ShaderProperty] public float radius = 0;
}

[Serializable]
[ShaderPropertySettings]
public class CameraFX_LensDistortion_Settings {
    [ShaderProperty] public float test = 0;

    [ShaderProperty] public float intensity = 0;
    
    [ShaderProperty] public int   enableSquishing = 1;
    [ShaderProperty] public float squishIntensity = 1f;
}

[Serializable]
[ShaderPropertySettings]
public class CameraFX_AdditiveColor_Settings {
    [ShaderProperty] public Vector3 color     = new(1,0,0);
    [ShaderProperty] public float   intensity = 0;
}

[Serializable]
[ShaderPropertySettings]
public class CameraFX_Test_Settings {
    [ShaderProperty] public Texture2D image;
}