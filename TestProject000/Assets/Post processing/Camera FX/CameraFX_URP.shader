// TODO: for now, this is just the radial zoom shader renamed.
// This shader should host some of the needed camera FX, such as radial zoom and lens distortion.
// TODO: look up UPK Explorer again with DH1

Shader "TestProject000/URP/CameraFX"
{
    Properties {
        // RadialZoom:
        _CameraFX_RadialZoom_Samples       ("_CameraFX_RadialZoom_Samples"      , Integer) = 0
        _CameraFX_RadialZoom_Center        ("_CameraFX_RadialZoom_Center"       , Vector)  = (0,0,0)
        _CameraFX_RadialZoom_CenterFalloff ("_CameraFX_RadialZoom_CenterFalloff", Float)   = 0
        _CameraFX_RadialZoom_Radius        ("_CameraFX_RadialZoom_Radius"       , Float)   = 0

        // LensDistortion:
        _CameraFX_LensDistortion_Intensity       ("_CameraFX_LensDistortion_Intensity",       Float) = 0 // divided by about 60, check Frag_...()
        _CameraFX_LensDistortion_EnableSquishing ("_CameraFX_LensDistortion_EnableSquishing", Int  ) = 1 // squish that cat!
        _CameraFX_LensDistortion_SquishIntensity ("_CameraFX_LensDistortion_SquishIntensity", Float) = 1 // [0-1]

        // AdditiveColor:
        _CameraFX_AdditiveColor_Color     ("_CameraFX_AdditiveColor_Color"    , Vector) = (1,0,0)
        _CameraFX_AdditiveColor_Intensity ("_CameraFX_AdditiveColor_Intensity", Float)  = 0

        // Test:
        _CameraFX_Test_Image ("_CameraFX_Test_Image", 2D) = "" {}
    }

    HLSLINCLUDE
    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // The Blit.hlsl file provides the vertex shader (Vert),
        // the input structure (Attributes), and the output structure (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // TODO: separate these FX out into individual shader include files!

        Texture2D _CameraFX_Test_Image;

        // RadialZoom:
        // based on https://www.shadertoy.com/view/lsSXR3

        // TODO: this has to be a float for some reason...
        // Unity docs mention that this was the case for the legacy Int property type,
        // though it still wants to be a float, even when I use the new Integer property type.
        // https://docs.unity3d.com/Manual/SL-Properties.html (find: Int (legacy))
        float  _CameraFX_RadialZoom_Samples;

        float2 _CameraFX_RadialZoom_Center;
        float  _CameraFX_RadialZoom_CenterFalloff;
        float  _CameraFX_RadialZoom_Radius;
        
        float4 Frag_RadialZoom (Varyings input) : SV_Target
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

        // LensDistortion:
        // based on https://www.shadertoy.com/view/XlXfRs

        float _CameraFX_LensDistortion_Intensity;
        bool  _CameraFX_LensDistortion_EnableSquishing;
        float _CameraFX_LensDistortion_SquishIntensity;
        
        float4 Frag_LensDistortion2 (Varyings input) : SV_Target {
            float2 uv = input.texcoord;

            float2 result = uv;

            // Sample camera texture with distorted UVs
            half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearRepeat, result);
            return color;
        }

        float4 Frag_LensDistortion (Varyings input) : SV_Target {
            // TODO: Verify this with KoD intro and DisSlomo!
            //       Record video and compare!
            //       Is it squishing horizontally, the same way?
            
            // max(0, ...): Do not support negative values for intensity, as that is an undesired effect.
            float strength = -max(0, _CameraFX_LensDistortion_Intensity) / 60;
            
            float zoom = 1; // for squishing
            if (_CameraFX_LensDistortion_EnableSquishing) zoom += (-strength * _CameraFX_LensDistortion_SquishIntensity);
            
            // map [0, 1] to [-1, 1], to make sure we only distort up until the output edges:
            float2 uv = (input.texcoord - _CameraFX_RadialZoom_Center) * 2.0;

            float theta     = atan2(uv.y, uv.x);
            float startDist = length(uv); // "radial distance"
            // We intentionally square the starting distance here, as a realistic lens distortion effect propagates quadratically.
            // Multiplying it just once would result in a linear distribution from the center.
            float dist      = startDist * (1.0 + strength * startDist * startDist);
            
            float2 resultUV = float2(
                cos(theta) * dist * zoom, // X axis - squish!
                sin(theta) * dist)        // Y axis
                / 2.0 + 0.5;              // remain in output bounds

            float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearRepeat, resultUV);
            return float4(color, 1);
        }

        float3 _CameraFX_AdditiveColor_Color;
        float  _CameraFX_AdditiveColor_Intensity;

        // AdditiveColor:

        float4 Frag_AdditiveColor (Varyings input) : SV_Target {
            float3 base   = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearRepeat, input.texcoord);
            float3 result = base + (_CameraFX_AdditiveColor_Color * _CameraFX_AdditiveColor_Intensity);
            return float4(result, 1);
            //return SAMPLE_TEXTURE2D(_CameraFX_Test_Image, sampler_LinearRepeat, input.texcoord);
        }

    ENDHLSL
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        // LOD 100
        ZWrite Off Cull Off

        Pass
        {
            Name "CameraFX_RadialZoom"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag_RadialZoom
            
            ENDHLSL
        }

        Pass
        {
            Name "CameraFX_LensDistortion"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag_LensDistortion
            
            ENDHLSL
        }

        Pass
        {
            Name "CameraFX_AdditiveColor"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag_AdditiveColor
            
            ENDHLSL
        }
    }
}