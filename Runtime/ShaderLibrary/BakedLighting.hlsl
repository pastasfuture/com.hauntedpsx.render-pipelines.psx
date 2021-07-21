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

#if defined(_BRDF_MODE_LAMBERT)
    // Nothing to do here, the above factoring is already setup for a Lambert BRDF.

#elif defined(_BRDF_MODE_WRAPPED_LIGHTING)
    // Need to adjust our SH coefficients to handle convolution with the Wrapped Lighting BRDF rather than Lambert.
    // The derivation of SH wrapped lighting can be found here:
    // https://blog.selfshadow.com/2011/12/31/righting-wrap-part-1/
    // In short, the coefficients for each L2 band for Lambert are: [1.0 / PI, 2.0 / (3.0 * PI), 1.0 / (4.0 * PI)]
    // In comparison, the coefficients for each L2 band for Wrapped Lighting are: [1.0 / PI, 1.0 / (3.0 * PI) * (2.0 - w), 1.0 / (4.0 * PI) * (1.0 - w)^2]
    // with a hardcoded wrap factor of 1.0 this simplifies to: [1.0 / PI, 1.0 / (3.0 * PI), 0.0]
    // So to convert from Lambert to Wrapped Lighting, we simply multiply each L2 band by: [1.0, 1.0 / 3.0, 0.0]
    // One challenge is the coefficients are already swizzled and factored with the y20 constant term pre-factored into the dc term, so we need to carefully remove that.
    // Normalization from: https://www.ppsloan.org/publications/StupidSH36.pdf
    // Appendix A10 Shader/CPU code for Irradiance Environment Maps
    // Need to add back 1/3 of the L2.z term to the dc term, since it was previously subtracted out from the factoring above.
    // The 1/3rd in the L2 term is unreleated to the L1 1/3rd scale factor.
    SHCoefficients[0] = real4(unity_SHAr.xyz * 1.0 / 3.0, unity_SHAr.w + unity_SHBr.z * 1.0 / 3.0);
    SHCoefficients[1] = real4(unity_SHAg.xyz * 1.0 / 3.0, unity_SHAg.w + unity_SHBg.z * 1.0 / 3.0);
    SHCoefficients[2] = real4(unity_SHAb.xyz * 1.0 / 3.0, unity_SHAb.w + unity_SHBb.z * 1.0 / 3.0);
    SHCoefficients[3] = 0.0;
    SHCoefficients[4] = 0.0;
    SHCoefficients[5] = 0.0;
    SHCoefficients[6] = 0.0;
#endif

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

half4 SampleShadowMask(float2 lightmapUV)
{
#if defined(LIGHTMAP_SHADOW_MASK)
    #if defined(LIGHTMAP_ON)
        return SAMPLE_TEXTURE2D_LOD(unity_ShadowMask, samplerunity_ShadowMask, lightmapUV, 0);
    #else
        return unity_ProbesOcclusion;
    #endif
#else
    return 1.0h;
#endif
}

// #if defined(LIGHTMAP_ON)
// #define SAMPLE_GI(lmName, shName, normalWSName) SampleLightmap(lmName, normalWSName)
// #else
// #define SAMPLE_GI(lmName, shName, normalWSName) SampleSHVertex(shName, normalWSName)
// #endif

#endif