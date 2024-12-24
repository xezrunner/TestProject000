using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

partial class CameraFXRenderPass : ScriptableRenderPass {
    class PassData {
        public TextureHandle source;
        public Material      material;
    }

    // TODO: initialize? how should we manage CameraFX settings? should its sub-settings remain static/global?
    // Tie into some future settings system?
    CameraFX_Settings settings;

    Material                material;
    RenderTextureDescriptor textureDescriptor;

    public CameraFXRenderPass(Material material) {
        if (!material) {
            Debug.LogError("CameraFXRenderPass ctor(): no material!");
            return;
        }
        
        this.material = material;
        this.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing; // TODO: tweak!
        this.textureDescriptor = new(Screen.width, Screen.height, RenderTextureFormat.Default, 0);

        if (CameraFX_Settings.printShaderPropertyCacheOnInit) {
            Debug.Log("CameraFXRenderPass: Shader property cache: ");
            foreach (var entry in ShaderPropertyCache.PROPERTY_CACHE) {
                Debug.Log($"  - value: {entry.Value,4}  key: {entry.Key}");
            }
        }
    }

    partial void updateSettings(); // implemented in CameraFXEffectSettings.cs

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

        updateSettings();

        // Build passes:
        // Basically, process into a texture, then push that back into the output:
        // TODO: this might change if we want multiple passes
        //       in that case, we would want each pass to trickle down and the final pass to be output.

        var radialZoomDestinationTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, textureDescriptor, name: "_CameraFXRadialZoomTex", clear: false
        );
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("CameraFX_RadialZoom", out var passData)) {
            passData.source   = activeTexture; // source texture
            passData.material = material;

            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(radialZoomDestinationTexture, 0); // temp target texture

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => executePass(data, context, 0));
        }

        var lensDistortionDestinationTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, textureDescriptor, name: "_CameraFXLensDistortionTex", clear: false
        );
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "CameraFX_LensDistortion", out var passData)) {
            passData.source   = radialZoomDestinationTexture; // last temp target texture
            passData.material = material;

            // NOTE: this is referring to 'passData' as context - it's actually set to the destination texture just above:
            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(lensDistortionDestinationTexture, 0); // render to active texture

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => executePass(data, context, 1));
        }

        var additiveColorDestinationTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, textureDescriptor, name: "_CameraFXAdditiveColorTex", clear: false
        );
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "CameraFX_AdditiveColor", out var passData)) {
            passData.source   = lensDistortionDestinationTexture; // last temp target texture
            passData.material = material;

            // NOTE: this is referring to 'passData' as context - it's actually set to the destination texture just above:
            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(activeTexture, 0); // render to active texture

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => executePass(data, context, 2));
        }
    }

    static void executePass(PassData data, RasterGraphContext context, int pass) {
        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1,1,1,0), data.material, pass);
    }
}