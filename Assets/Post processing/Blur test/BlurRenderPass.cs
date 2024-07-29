using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class BlurRenderPass : ScriptableRenderPass {
    class BlurPassData {
        internal TextureHandle src;
        internal Material mat;
    }

    BlurSettings defaultSettings;
    Material material;

    private RenderTextureDescriptor blurTextureDescriptor;

    static readonly int horizontalBlurID = Shader.PropertyToID("_HorizontalBlur");
    static readonly int verticalBlurID = Shader.PropertyToID("_VerticalBlur");

    const string blurTextureName = "_BlurTexture";

    const string blurPassName = "BlurRenderPass";
    const string verticalPassName = "VerticalBlurRenderPass";
    const string horizontalPassName = "HorizontalBlurRenderPass";

    public BlurRenderPass(Material material, BlurSettings defaultSettings) {
        this.material = material;
        this.defaultSettings = defaultSettings;

        blurTextureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        // Don't blit from the backbuffer:
        if (resourceData.isActiveTargetBackBuffer) return;

        UniversalCameraData cameraData     = frameData.Get<UniversalCameraData>();

        TextureHandle sourceTexture      = resourceData.activeColorTexture;
        TextureHandle destinationTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, blurTextureDescriptor, blurTextureName, false);
        //TextureHandle destinationTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, cameraData.cameraTargetDescriptor, blurTextureName, false);

        if (!sourceTexture.IsValid() || !destinationTexture.IsValid()) return;
        
        blurTextureDescriptor.width = cameraData.cameraTargetDescriptor.width;
        blurTextureDescriptor.height = cameraData.cameraTargetDescriptor.height;
        blurTextureDescriptor.depthBufferBits = 0;

        // NOTE: update blur settings!
        UpdateBlurSettings();

        // NOTE:
        // Looks like in order to get an effect drawing to the screen, we have to use at least 2 passes:
        // - Copy the camera to a temporary texture and do your shader stuff to it
        // - Set this temporary texture as the destination

        using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>(blurPassName, out var passData)) {
            passData.src = sourceTexture;
            passData.mat = material;

            builder.AllowPassCulling(false);
            builder.UseTexture(passData.src);                   // input  -- act on the source texture (game output) first.
            builder.SetRenderAttachment(destinationTexture, 0); // output -- send this pass's output to a temporary texture

            // Shader pass:
            builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) => ExecutePass(data, context, 0));
        }

        using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>(blurPassName, out var passData)) {
            passData.src = destinationTexture;
            passData.mat = material;

            builder.AllowPassCulling(false);
            builder.UseTexture(passData.src);              // input  -- act on the now processed texture (destinationTexture above)
            builder.SetRenderAttachment(sourceTexture, 0); // output -- send it to the game output

            // Shader pass:
            builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) => ExecutePass(data, context, 0));
        }

/*
        using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>(verticalPassName, out var passData)) {
            passData.src = srcCamColor;
            passData.mat = material;

            builder.UseTexture(passData.src);    // input
            builder.SetRenderAttachment(dst, 0); // output

            builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) => ExecutePass(data, context, 0));
        }

        using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>(horizontalPassName, out var passData)) {
            passData.src = dst;
            passData.mat = material;

            builder.UseTexture(passData.src);    // input
            builder.SetRenderAttachment(srcCamColor, 0); // output

            builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) => ExecutePass(data, context, 1));
        }
*/
    }

    private static void ExecutePass(BlurPassData data, RasterGraphContext context, int pass)
    {
        Blitter.BlitTexture(context.cmd, data.src, new Vector4(1f, 1f, 0f, 0f), data.mat, pass);
    }

    void UpdateBlurSettings() {
        if (!material) return;

        var volumeComponent = VolumeManager.instance.stack.GetComponent<BlurVolumeComponent>();
        float horizontalBlur = volumeComponent.horizontalBlur.overrideState ? volumeComponent.horizontalBlur.value : defaultSettings.horizontalBlur;
        float verticalBlur = volumeComponent.verticalBlur.overrideState ? volumeComponent.verticalBlur.value : defaultSettings.verticalBlur;

        material.SetFloat(horizontalBlurID, horizontalBlur);
        material.SetFloat(verticalBlurID, verticalBlur);
    }
}
