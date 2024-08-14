using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraFXRendererFeature : ScriptableRendererFeature {
    public Shader shader;

    public Material    material; // TEMP: public for inspector debugging purposes
    CameraFXRenderPass renderPass;
    
    public override void Create() {
        if (!shader) return;

        material   = new(shader);
        renderPass = new(material);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (renderingData.cameraData.cameraType != CameraType.Game) return; // TODO: override?

        renderer.EnqueuePass(renderPass);
    }


    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);

#if UNITY_EDITOR
        if (EditorApplication.isPlaying) Destroy(material);
        else                             DestroyImmediate(material);
#else
        Destroy(material);
#endif
    }
}


