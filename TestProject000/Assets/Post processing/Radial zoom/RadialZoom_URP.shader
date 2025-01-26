Shader "TestProject000/URP/RadialZoom"
{
    HLSLINCLUDE
    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // The Blit.hlsl file provides the vertex shader (Vert),
        // the input structure (Attributes), and the output structure (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // Props
        float  _Samples;
        float2 _Center;
        float  _CenterFalloff;
        float  _Radius;
        
        float4 Frag (Varyings input) : SV_Target
        {
            float3 color = float3(0,0,0);

            const float radius        = _Radius        / 100;
            const float centerFalloff = _CenterFalloff / 100;

            // Just render the output as-is when 0, potentially tiny optimization.
            // May not be necessary if we can toggle the renderer feature entirely with whatever I come up with
            // to control them.
            if (radius == 0)
            {
                color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearRepeat, input.texcoord);
                return float4(color, 1);
            }
            
            float2 direction = (input.texcoord - _Center);

            for (int i = 0; i < _Samples; ++i)
            {
                float scale = 1 - (radius * (i / _Samples)) * (saturate(length(direction) / centerFalloff));
                color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearRepeat, direction * scale + _Center);
            }

            color /= _Samples;
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
            Name "RadialZoom"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            ENDHLSL
        }
    }
}