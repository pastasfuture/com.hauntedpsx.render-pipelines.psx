#ifndef PSX_BAKED_LIGHTING
#define PSX_BAKED_LIGHTING

// These helper functions are all taken from the Universal Render Pipeline.
// See Universal Render Pipeline lighting.hlsl for reference.

// This include is needed to get SampleSH9() helper function.
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

// Samples SH L0, L1 and L2 terms
half3 SampleSH(half3 normalWS)
{
    // LPPV is not supported.
    real4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;

    return max(half3(0, 0, 0), SampleSH9(SHCoefficients, normalWS));
}

// Needed to implement vertex shader compatable versions of SampleSingleLightmap and SampleDirectionalLightmap since we need to call SAMPLE_TEXTURE2D_LOD in the vertex shader.
real3 SampleSingleLightmapVertex(TEXTURE2D_PARAM(lightmapTex, lightmapSampler), float2 uv, float4 transform, bool encodedLightmap, real4 decodeInstructions)
{
    // transform is scale and bias
    uv = uv * transform.xy + transform.zw;
    real3 illuminance = real3(0.0, 0.0, 0.0);
    // Remark: baked lightmap is RGBM for now, dynamic lightmap is RGB9E5
    if (encodedLightmap)
    {
        real4 encodedIlluminance = SAMPLE_TEXTURE2D_LOD(lightmapTex, lightmapSampler, uv, 0).rgba;
        illuminance = DecodeLightmap(encodedIlluminance, decodeInstructions);
    }
    else
    {
        illuminance = SAMPLE_TEXTURE2D_LOD(lightmapTex, lightmapSampler, uv, 0).rgb;
    }
    return illuminance;
}

real3 SampleDirectionalLightmapVertex(TEXTURE2D_PARAM(lightmapTex, lightmapSampler), TEXTURE2D_PARAM(lightmapDirTex, lightmapDirSampler), float2 uv, float4 transform, float3 normalWS, bool encodedLightmap, real4 decodeInstructions)
{
    // In directional mode Enlighten bakes dominant light direction
    // in a way, that using it for half Lambert and then dividing by a "rebalancing coefficient"
    // gives a result close to plain diffuse response lightmaps, but normalmapped.

    // Note that dir is not unit length on purpose. Its length is "directionality", like
    // for the directional specular lightmaps.

    // transform is scale and bias
    uv = uv * transform.xy + transform.zw;

    real4 direction = SAMPLE_TEXTURE2D_LOD(lightmapDirTex, lightmapDirSampler, uv, 0);
    // Remark: baked lightmap is RGBM for now, dynamic lightmap is RGB9E5
    real3 illuminance = real3(0.0, 0.0, 0.0);
    if (encodedLightmap)
    {
        real4 encodedIlluminance = SAMPLE_TEXTURE2D_LOD(lightmapTex, lightmapSampler, uv, 0).rgba;
        illuminance = DecodeLightmap(encodedIlluminance, decodeInstructions);
    }
    else
    {
        illuminance = SAMPLE_TEXTURE2D_LOD(lightmapTex, lightmapSampler, uv, 0).rgb;
    }
    real halfLambert = dot(normalWS, direction.xyz - 0.5) + 0.5;
    return illuminance * halfLambert / max(1e-4, direction.w);
}

// Sample baked lightmap. Non-Direction and Directional if available.
// Realtime GI is not supported.
half3 SampleLightmap(float2 lightmapUV, half3 normalWS)
{
#ifdef UNITY_LIGHTMAP_FULL_HDR
    bool encodedLightmap = false;
#else
    bool encodedLightmap = true;
#endif

    half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);

    // The shader library sample lightmap functions transform the lightmap uv coords to apply bias and scale.
    // However, universal pipeline already transformed those coords in vertex. We pass half4(1, 1, 0, 0) and
    // the compiler will optimize the transform away.
    half4 transformCoords = half4(1, 1, 0, 0);

#ifdef DIRLIGHTMAP_COMBINED
    return SampleDirectionalLightmapVertex(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
        TEXTURE2D_ARGS(unity_LightmapInd, samplerunity_Lightmap),
        lightmapUV, transformCoords, normalWS, encodedLightmap, decodeInstructions);
#elif defined(LIGHTMAP_ON)
    return SampleSingleLightmapVertex(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), lightmapUV, transformCoords, encodedLightmap, decodeInstructions);
#else
    return half3(0.0, 0.0, 0.0);
#endif
}

// #if defined(LIGHTMAP_ON)
// #define SAMPLE_GI(lmName, shName, normalWSName) SampleLightmap(lmName, normalWSName)
// #else
// #define SAMPLE_GI(lmName, shName, normalWSName) SampleSHVertex(shName, normalWSName)
// #endif

#endif