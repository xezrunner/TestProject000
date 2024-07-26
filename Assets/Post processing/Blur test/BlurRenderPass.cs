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

    public BlurRenderPass(Material material, BlurSettings defaultSettings) {
        this.material = material;
        this.defaultSettings = defaultSettings;

        blurTextureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
    }

    static readonly int horizontalBlurID = Shader.PropertyToID("_HorizontalBlur");
    static readonly int verticalBlurID = Shader.PropertyToID("_VerticalBlur");

    const string blurTextureName = "_BlurTexture";

    const string blurPassName = "BlurRenderPass";
    const string verticalPassName = "VerticalBlurRenderPass";
    const string horizontalPassName = "HorizontalBlurRenderPass";

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
        base.RecordRenderGraph(renderGraph, frameData);

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        TextureHandle srcCamColor = resourceData.activeColorTexture;
        TextureHandle dst = UniversalRenderer.CreateRenderGraphTexture(renderGraph, blurTextureDescriptor, blurTextureName, false);
        if (!srcCamColor.IsValid() || !dst.IsValid()) return;

        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        // Don't blit from the backbuffer:
        if (resourceData.isActiveTargetBackBuffer) return;

        blurTextureDescriptor.width = cameraData.cameraTargetDescriptor.width;
        blurTextureDescriptor.height = cameraData.cameraTargetDescriptor.height;
        blurTextureDescriptor.depthBufferBits = 0;

        // NOTE: update blur settings!
        UpdateBlurSettings();

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(blurPassName, out var passData)) {
            passData.src = srcCamColor;
            passData.mat = material;

            builder.UseTexture(passData.src);    // input
            builder.SetRenderAttachment(dst, 0); // output

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data,context,0));
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
