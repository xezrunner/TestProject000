Shader "TestProject000/GaussianBlur" {
    Properties {
        _Radius("Radius", Float) = 2.5
    }

    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // The Blit.hlsl file provides the vertex shader (Vert),
    // the input structure (Attributes), and the output structure (Varyings)
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    #include "GaussianBlurCommon.hlsl"
    
    ENDHLSL

    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        // LOD 100
        ZWrite Off Cull Off

        Pass
        {
            Name "GaussianBlur_Pass0"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GaussianBlur_Frag0
            ENDHLSL
        }

        Pass
        {
            Name "GaussianBlur_Pass1"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GaussianBlur_Frag1
            ENDHLSL
        }

        Pass
        {
            Name "GaussianBlur_Pass2"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GaussianBlur_Frag2
            ENDHLSL
        }

        Pass
        {
            Name "GaussianBlur_Pass3"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GaussianBlur_Frag3
            ENDHLSL
        }

        Pass
        {
            Name "GaussianBlur_Pass4"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GaussianBlur_Frag4
            ENDHLSL
        }
    }
}
