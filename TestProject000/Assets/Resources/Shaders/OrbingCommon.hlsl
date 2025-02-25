float _OrbingT = 1.0;
float _BlurRadius = 0.0;

half4 blur(Varyings input, half4 color, float blur_radius) {
    // Get the UV coordinates of the current fragment
    float2 uv = input.uv;
    
    // Retrieve the texture's texel size for correct offset scaling
    float2 texelSize = _BaseMap_TexelSize.xy * blur_radius;
    
    // Define a 5x5 Gaussian kernel offsets and weights
    const int kernelSize = 5;
    const float2 offsets[25] = {
        float2(-2, -2), float2(-1, -2), float2(0, -2), float2(1, -2), float2(2, -2),
        float2(-2, -1), float2(-1, -1), float2(0, -1), float2(1, -1), float2(2, -1),
        float2(-2, 0),  float2(-1, 0),  float2(0, 0),  float2(1, 0),  float2(2, 0),
        float2(-2, 1),  float2(-1, 1),  float2(0, 1),  float2(1, 1),  float2(2, 1),
        float2(-2, 2),  float2(-1, 2),  float2(0, 2),  float2(1, 2),  float2(2, 2)
    };
    
    // Precomputed Gaussian weights for a 5x5 kernel (sigma = 1.0)
    static const half weights[25] = {
        0.003765, 0.015019, 0.023792, 0.015019, 0.003765,
        0.015019, 0.059912, 0.094907, 0.059912, 0.015019,
        0.023792, 0.094907, 0.150342, 0.094907, 0.023792,
        0.015019, 0.059912, 0.094907, 0.059912, 0.015019,
        0.003765, 0.015019, 0.023792, 0.015019, 0.003765
    };
    
    half4 blurredColor = half4(0.0, 0.0, 0.0, 0.0);
    
    // Sample the texture and accumulate weighted colors
    for (int i = 0; i < 25; i++) {
        float2 offset = offsets[i] * texelSize;
        half4 sampleColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + offset);
        blurredColor += sampleColor * weights[i];
    }
    
    return blurredColor;
}

half4 Orbing_Process(Varyings input, half4 color) {
    // half4 blue = half4(0,0.333,1,1) * _OrbingT;
    // color += blue;

    color = blur(input, color, _BlurRadius);
    
    return color;
}

// Used in Standard (Physically Based) shader
void Orbing_LitPassFragment(Varyings input, out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(_PARALLAXMAP)
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
#else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
#endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
#endif

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    InitializeBakedGIData(input, inputData);

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

    color = Orbing_Process(input, color);   

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}