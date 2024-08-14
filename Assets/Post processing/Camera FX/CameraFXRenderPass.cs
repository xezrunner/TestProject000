using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

partial class CameraFXRenderPass : ScriptableRenderPass {
    class PassData {
        public TextureHandle source;
        public Material      material;
    }

    CameraFX_Settings settings;

    Material                material;
    RenderTextureDescriptor textureDescriptor;

    //static int[] cachedShaderPropIDs = ShaderPropertyCache.BuildShaderPropertyIDs();

    public CameraFXRenderPass(Material material) {
        this.material = material;
        this.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing; // TODO: tweak!
        this.textureDescriptor = new(Screen.width, Screen.height, RenderTextureFormat.Default, 0);

        Debug.Log("Shader property cache: ");
        foreach (var entry in ShaderPropertyCache.PROPERTY_CACHE) {
            Debug.Log($"  - value: {entry.Value,4}  key: {entry.Key}");
        }
    }

    partial void updateSettings();

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
        var resourceData = frameData.Get<UniversalResourceData>();
        if (resourceData.isActiveTargetBackBuffer) return; // TODO: why?

        var sourceTexture      = resourceData.activeColorTexture;
        var destinationTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, textureDescriptor, name: "_CameraFXTexture", clear: false);

        if (!sourceTexture.IsValid() || !destinationTexture.IsValid()) return;

        var cameraData = frameData.Get<UniversalCameraData>();
        textureDescriptor.width  = cameraData.cameraTargetDescriptor.width;
        textureDescriptor.height = cameraData.cameraTargetDescriptor.height;
        textureDescriptor.depthBufferBits = 0; // TODO: is this necessary?

        updateSettings();

        // Build passes:
        // Basically, process into a texture, then push that back into the output:
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "CameraFX_PrePass", out var passData)) {
            passData.source   = sourceTexture;
            passData.material = material;

            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(destinationTexture, 0);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => executePass(data, context));
        }

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "CameraFX_PostPass", out var passData)) {
            passData.source   = destinationTexture;
            passData.material = material;

            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(sourceTexture, 0);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => executePass(data, context));
        }
    }

    static void executePass(PassData data, RasterGraphContext context) {
        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1,1,1,0), data.material, 0);
    }
}