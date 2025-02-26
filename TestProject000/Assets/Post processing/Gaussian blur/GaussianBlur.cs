using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class GaussianBlurFeature : ScriptableRendererFeature {
    public Shader shader;
    public Material material;
    
    GaussianBlurPass renderPass;

    public override void Create() {
        if (!shader) return;

        material   = new(shader);
        renderPass = new(material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        // if (renderingData.cameraData.cameraType != CameraType.Game) return; // TODO: override?

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


class GaussianBlurPass : ScriptableRenderPass {
    class PassData {
        public TextureHandle source;
        public Material      material;
    }

    Material material;
    RenderTextureDescriptor textureDescriptor;
    int tempRT;

    public GaussianBlurPass(Material material) {
        if (!material) {
            Debug.LogError("CameraFXRenderPass ctor(): no material!");
            return;
        }
        
        this.material = material;

        renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing; // TODO:
        textureDescriptor = new(Screen.width, Screen.height, RenderTextureFormat.Default, 0);

        tempRT = Shader.PropertyToID("_TempBlurRT");
    }


    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
        if (!material) return;

        var resourceData = frameData.Get<UniversalResourceData>();
        if (resourceData.isActiveTargetBackBuffer) return; // TODO: why?

        var activeTexture = resourceData.activeColorTexture; // cache
        if (!activeTexture.IsValid()) return;

        var cameraData = frameData.Get<UniversalCameraData>();
        textureDescriptor.width  = cameraData.cameraTargetDescriptor.width;
        textureDescriptor.height = cameraData.cameraTargetDescriptor.height;
        textureDescriptor.depthBufferBits = 0; // TODO: is this necessary?

        // Pass 0:
        var pass0destinationTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, textureDescriptor, name: "GaussianBlurTex_Pass0", clear: false
        );

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("GaussianBlur_Pass0", out var passData)) {
            passData.source = activeTexture; // source texture
            passData.material = material;

            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(pass0destinationTexture, 0); // temp target texture

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => executePass(data, context, 0));
        }

        // Pass 1:
        var pass1DestinationTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, textureDescriptor, name: "GaussianBlurTex_Pass1", clear: false
        );
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "GaussianBlur_Pass1", out var passData)) {
            passData.source = pass0destinationTexture; // last temp target texture
            passData.material = material;

            // NOTE: this is referring to 'passData' as context - it's actually set to the destination texture just above:
            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(pass1DestinationTexture, 0); // render to active texture

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => executePass(data, context, 1));
        }

        // Pass 2:
        var pass2DestinationTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, textureDescriptor, name: "GaussianBlurTex_Pass2", clear: false
        );
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "GaussianBlur_Pass2", out var passData)) {
            passData.source = pass1DestinationTexture; // last temp target texture
            passData.material = material;

            // NOTE: this is referring to 'passData' as context - it's actually set to the destination texture just above:
            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(pass2DestinationTexture, 0); // render to active texture

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => executePass(data, context, 2));
        }

        // Pass 3:
        var pass3DestinationTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, textureDescriptor, name: "GaussianBlurTex_Pass3", clear: false
        );
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "GaussianBlur_Pass3", out var passData)) {
            passData.source = pass2DestinationTexture; // last temp target texture
            passData.material = material;

            // NOTE: this is referring to 'passData' as context - it's actually set to the destination texture just above:
            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(pass3DestinationTexture, 0); // render to active texture

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => executePass(data, context, 3));
        }

        // Pass 4 (final):
        var pass4DestinationTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, textureDescriptor, name: "GaussianBlurTex_Pass4", clear: false
        );
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "GaussianBlur_Pass4", out var passData)) {
            passData.source   = pass3DestinationTexture; // last temp target texture
            passData.material = material;

            // NOTE: this is referring to 'passData' as context - it's actually set to the destination texture just above:
            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(activeTexture, 0); // render to active texture

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => executePass(data, context, 4));
        }
    }

    static void executePass(PassData data, RasterGraphContext context, int pass) {
        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1,1,1,0), data.material, pass);
    }

#if false
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        CommandBuffer cmd = CommandBufferPool.Get("Gaussian Blur Pass");

        // Get camera descriptor and allocate a temporary RenderTexture.
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        cmd.GetTemporaryRT(tempRT, descriptor);

        // First pass: horizontal blur.
        cmd.Blit(source, tempRT, blurMaterial, 0);

        // Second pass: vertical blur back to the source.
        cmd.Blit(tempRT, source, blurMaterial, 1);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
#endif

    public override void FrameCleanup(CommandBuffer cmd) {
        cmd.ReleaseTemporaryRT(tempRT);
    }
}