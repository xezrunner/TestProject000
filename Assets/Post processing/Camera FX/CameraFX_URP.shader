// TODO: for now, this is just the radial zoom shader renamed.
// This shader should host some of the needed camera FX, such as radial zoom and lens distortion.
// TODO: look up UPK Explorer again with DH1

Shader "TestProject000/URP/CameraFX"
{
    Properties {
        _CameraFX_RadialZoom_Samples ("_CameraFX_RadialZoom_Samples", Integer) = 0
        _CameraFX_RadialZoom_Center ("_CameraFX_RadialZoom_Center", Vector) = (0,0,0,0)
        _CameraFX_RadialZoom_CenterFalloff ("_CameraFX_RadialZoom_CenterFalloff", Float) = 0
        _CameraFX_RadialZoom_Radius ("_CameraFX_RadialZoom_Radius", Float) = 0
    }

    HLSLINCLUDE
    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // The Blit.hlsl file provides the vertex shader (Vert),
        // the input structure (Attributes), and the output structure (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // Props

        // TODO: this has to be a float for some reason...
        // Unity docs mention that this was the case for the legacy Int property type,
        // though it still wants to be a float, even when I use the new Integer property type.
        // https://docs.unity3d.com/Manual/SL-Properties.html (find: Int (legacy))
        float  _CameraFX_RadialZoom_Samples;

        float2 _CameraFX_RadialZoom_Center;
        float  _CameraFX_RadialZoom_CenterFalloff;
        float  _CameraFX_RadialZoom_Radius;
        
        float4 Frag (Varyings input) : SV_Target
        {
            float3 color = float3(0,0,0);

            const float radius        = _CameraFX_RadialZoom_Radius        / 100;
            const float centerFalloff = _CameraFX_RadialZoom_CenterFalloff / 100;

            // Just render the output as-is when 0, potentially tiny optimization.
            // May not be necessary if we can toggle the renderer feature entirely with whatever I come up with
            // to control them.
            if (radius == 0)
            {
                color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearRepeat, input.texcoord);
                return float4(color, 1);
            }
            
            float2 direction = (input.texcoord - _CameraFX_RadialZoom_Center);

            for (int i = 0; i < _CameraFX_RadialZoom_Samples; ++i)
            {
                float scale = 1 - (radius * (i / _CameraFX_RadialZoom_Samples)) * (saturate(length(direction) / centerFalloff));
                color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearRepeat, direction * scale + _CameraFX_RadialZoom_Center);
            }

            color /= _CameraFX_RadialZoom_Samples;
            return float4(color, 1);
        }

    ENDHLSL
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        // LOD 100
        ZWrite Off Cull Off

        Pass
        {
            Name "CameraFX"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            ENDHLSL
        }
    }
}