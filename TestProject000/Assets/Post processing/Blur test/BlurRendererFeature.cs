using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public class BlurSettings {
    public float horizontalBlur;
    public float verticalBlur;
}

public class BlurRendererFeature : ScriptableRendererFeature {
    public BlurSettings settings;
    public Shader shader;

    Material material;
    BlurRenderPass pass;

    public override void Create() {
        if (!shader) return;

        material = new(shader);
        pass = new(material, settings);
        pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);

#if UNITY_EDITOR
        if (EditorApplication.isPlaying) {
            Destroy(material);
        } else {
            DestroyImmediate(material);
        }
#else
            Destroy(material);
#endif
    }
}
