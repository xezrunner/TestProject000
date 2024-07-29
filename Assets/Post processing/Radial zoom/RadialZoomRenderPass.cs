using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

class RadialZoomRenderPass : ScriptableRenderPass {
    class PassData {
        public TextureHandle source;
        public Material      material;
    }

    RadialZoomSettings settings;
    Material material;

    RenderTextureDescriptor textureDescriptor;

    // Shader properties:
    // TODO: more props, also to be added into the volume component!
    static readonly int samplesID       = Shader.PropertyToID("_Samples");
    static readonly int centerID        = Shader.PropertyToID("_Center");
    // TODO: naming - This is intended to be a falloff towards the center, not from it.
    //                The name doens't really clearly reflect that clearly.
    static readonly int centerFalloffID = Shader.PropertyToID("_CenterFalloff");
    static readonly int radiusID        = Shader.PropertyToID("_Radius");

    public RadialZoomRenderPass(Material material, RadialZoomSettings settings)
    {
        this.material = material;
        this.settings = settings;
        textureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
    }

    void UpdateSettings() {
        if (!material) return;

        // TODO: volumes!
        // var volume = VolumeManager.instance?.stack.GetComponent<RadialZoomVolumeComponent>();
        // if (!volume) return;
        
        //material.SetFloat(radiusID, volume.radius.overrideState ? volume.radius.value : settings.radius);

        material.SetInt   (samplesID,       settings.samples);
        material.SetVector(centerID,        settings.center);
        material.SetFloat (centerFalloffID, settings.centerFalloff);
        material.SetFloat (radiusID,        settings.radius);
        
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
        var resourceData = frameData.Get<UniversalResourceData>();
        if (resourceData.isActiveTargetBackBuffer) return; // TODO: why?

        var sourceTexture      = resourceData.activeColorTexture;
        var destinationTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, textureDescriptor, name: "_RadialZoomTexture", clear: false);

        if (!sourceTexture.IsValid() || !destinationTexture.IsValid()) return;

        var cameraData = frameData.Get<UniversalCameraData>();
        textureDescriptor.width  = cameraData.cameraTargetDescriptor.width;
        textureDescriptor.height = cameraData.cameraTargetDescriptor.height;
        textureDescriptor.depthBufferBits = 0; // TODO: is this necessary?

        UpdateSettings();

        // Build the passes:
        // TODO: pass naming!
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "RadialZoom_PrePass", out var passData)) {
            passData.source   = sourceTexture;
            passData.material = material;

            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(destinationTexture, 0);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
        }

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "RadialZoom_PostPass", out var passData)) {
            passData.source   = destinationTexture;
            passData.material = material;

            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(sourceTexture, 0);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
        }
    }

    static void ExecutePass(PassData data, RasterGraphContext context) {
        Blitter.BlitTexture(
            context.cmd, data.source, new Vector4(1,1,1,0), data.material, 0);
    }
}