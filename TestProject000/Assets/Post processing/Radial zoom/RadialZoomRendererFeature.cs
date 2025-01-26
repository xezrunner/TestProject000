using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/* Resources: 
- https://drive.google.com/file/d/1mg1I_670SDc5iTkjobgsobPv7KvLRyLR/view
- https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/render-graph-write-render-pass.html
- https://gist.github.com/phi-lira/10159a824e4e522060c47e21762941bb
- https://www.shadertoy.com/view/lsSXR3
*/

[Serializable]
public class RadialZoomSettings {
    [Range(4, 32)] public int samples = 16;
    public Vector2 center        = new(1.5f, 0.5f);
    public float   centerFalloff = 60; // TODO: tweak, naming (see pass!)
    public float   radius        = 0;
}

public class RadialZoomRendererFeature : ScriptableRendererFeature {
    public static RadialZoomSettings settings = new();

    public Shader shader;
    Material material;

    RadialZoomRenderPass pass;

    public override void Create() {
        if (!shader) return;

        material = new Material(shader);
        pass = new RadialZoomRenderPass(material, settings);
        pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing; // TODO: verify/change!
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        renderer.EnqueuePass(pass);
    }

    // TODO: this alone is worth investigating for creating an extensible renderer feature thing.
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