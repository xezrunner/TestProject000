// For reference:
// struct Varyings
// {
//     float4 positionCS : SV_POSITION;
//     float2 texcoord   : TEXCOORD0;
//     UNITY_VERTEX_OUTPUT_STEREO
// };

float _Radius;

float4 kawaseBlur(Texture2D tex, float2 fragCoord, float2 res, float d)
{
    // TODO: clean up!
    // fragCoord *= res;
    float2 s0 = (fragCoord + float2( d,  d)) / res;
    float2 s1 = (fragCoord + float2( d, -d)) / res;
    float2 s2 = (fragCoord + float2(-d,  d)) / res;
    float2 s3 = (fragCoord + float2(-d, -d)) / res;

    float lod = 1.5;
    
    float4 result = (SAMPLE_TEXTURE2D_LOD(_BlitTexture,sampler_LinearRepeat,s0, lod) +
                     SAMPLE_TEXTURE2D_LOD(_BlitTexture,sampler_LinearRepeat,s1, lod) +
                     SAMPLE_TEXTURE2D_LOD(_BlitTexture,sampler_LinearRepeat,s2, lod) +
                     SAMPLE_TEXTURE2D_LOD(_BlitTexture,sampler_LinearRepeat,s3, lod)) / 4.0;

    // float4 result = (SAMPLE_TEXTURE2D(_BlitTexture,sampler_LinearRepeat,s0) +
    //                  SAMPLE_TEXTURE2D(_BlitTexture,sampler_LinearRepeat,s1) +
    //                  SAMPLE_TEXTURE2D(_BlitTexture,sampler_LinearRepeat,s2) +
    //                  SAMPLE_TEXTURE2D(_BlitTexture,sampler_LinearRepeat,s3)) / 4.0;

    // float4 result = SAMPLE_TEXTURE2D(_BlitTexture,sampler_LinearRepeat,fragCoord);

    return result;
}

float4 GaussianBlur_Frag0(Varyings input) : SV_Target
{
    float2 res = _BlitTexture_TexelSize.zw;
    float2 uv = input.texcoord * res;
    return kawaseBlur(_BlitTexture, uv, res, 0.5 * _Radius);
}

float4 GaussianBlur_Frag1(Varyings input) : SV_Target
{
    float2 res = _BlitTexture_TexelSize.zw;
    float2 uv = input.texcoord * res;
    return kawaseBlur(_BlitTexture, uv, res, 1.5 * _Radius);
}

float4 GaussianBlur_Frag2(Varyings input) : SV_Target
{
    float2 res = _BlitTexture_TexelSize.zw;
    float2 uv = input.texcoord * res;
    return kawaseBlur(_BlitTexture, uv, res, 2.5 * _Radius);
}

float4 GaussianBlur_Frag3(Varyings input) : SV_Target
{
    float2 res = _BlitTexture_TexelSize.zw;
    float2 uv = input.texcoord * res;
    return kawaseBlur(_BlitTexture, uv, res, 2.5 * _Radius);
}

float4 GaussianBlur_Frag4(Varyings input) : SV_Target
{
    float2 res = _BlitTexture_TexelSize.zw;
    float2 uv = input.texcoord * res;
    return kawaseBlur(_BlitTexture, uv, res, 3.5 * _Radius);
}