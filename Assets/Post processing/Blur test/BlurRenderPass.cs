using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class BlurRenderPass : ScriptableRenderPass {
    class PassData {
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

        TextureHandle srcCamColor = resourceData.activeColorTexture;
        TextureHandle dst = UniversalRenderer.CreateRenderGraphTexture(renderGraph, blurTextureDescriptor, blurTextureName, false);
        
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        // Don't blit from the backbuffer:
        if (resourceData.isActiveTargetBackBuffer) return;

        blurTextureDescriptor.width = cameraData.cameraTargetDescriptor.width;
        blurTextureDescriptor.height = cameraData.cameraTargetDescriptor.height;
        blurTextureDescriptor.depthBufferBits = 0;

        // NOTE: update blur settings!
        UpdateBlurSettings();

        if (!srcCamColor.IsValid() || !dst.IsValid()) return;

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(verticalPassName, out var passData)) {
            passData.src = srcCamColor;
            passData.mat = material;

            builder.UseTexture(passData.src);    // input
            builder.SetRenderAttachment(dst, 0); // output

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context, 0));
        }

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(horizontalPassName, out var passData)) {
            passData.src = dst;
            passData.mat = material;

            builder.UseTexture(passData.src);    // input
            builder.SetRenderAttachment(srcCamColor, 0); // output

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context, 1));
        }
    }

    private static void ExecutePass(PassData data, RasterGraphContext context, int pass)
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
