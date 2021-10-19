#ifndef PSX_DYNAMIC_LIGHTING
#define PSX_DYNAMIC_LIGHTING

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// This file is a pared down version of URP's forward lighting setup.
// See URP's implementation as reference.

// Abstraction over Light shading data.
struct Light
{
    half3 direction;
    half3 color;
    half distanceAttenuation;
    half shadowAttenuation;
};

// Matches Unity Vanila attenuation
// Attenuation smoothly decreases to light range.
float DistanceAttenuation(float distanceSqr, half2 distanceAttenuation)
{
    // We use a shared distance attenuation for additional directional and puctual lights
    // for directional lights attenuation will be 1
    float lightAtten = rcp(distanceSqr);

    // Use the smoothing factor also used in the Unity lightmapper.
    half factor = distanceSqr * distanceAttenuation.x;
    half smoothFactor = saturate(1.0h - factor * factor);
    smoothFactor = smoothFactor * smoothFactor;

    return lightAtten * smoothFactor;
}

half AngleAttenuation(half3 spotDirection, half3 lightDirection, half2 spotAttenuation)
{
    // Spot Attenuation with a linear falloff can be defined as
    // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
    // This can be rewritten as
    // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
    // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
    // SdotL * spotAttenuation.x + spotAttenuation.y

    // If we precompute the terms in a MAD instruction
    half SdotL = dot(spotDirection, lightDirection);
    half atten = saturate(SdotL * spotAttenuation.x + spotAttenuation.y);
    return atten * atten;
}

half BakedShadow(half4 shadowMask, half4 occlusionProbeChannels)
{
    int occlusionMaskChannel = (int)occlusionProbeChannels.x;
    float shadowStrength = occlusionProbeChannels.y;
    half bakedShadow = 1.0h;
    if (occlusionMaskChannel == -1)
    {
        bakedShadow = 1.0h;
    }
    else if (occlusionMaskChannel == 0)
    {
        bakedShadow = shadowMask.x;
    }
    else if (occlusionMaskChannel == 1)
    {
        bakedShadow = shadowMask.y;
    }
    else if (occlusionMaskChannel == 2)
    {
        bakedShadow = shadowMask.z;
    }
    else if (occlusionMaskChannel == 3)
    {
        bakedShadow = shadowMask.w;
    }

    bakedShadow = lerp(1.0h, bakedShadow, shadowStrength);
    
    return bakedShadow;
}

half AdditionalLightShadow(half4 shadowMask, half4 occlusionProbeChannels)
{
#ifdef LIGHTMAP_SHADOW_MASK
    half bakedShadow = BakedShadow(shadowMask, occlusionProbeChannels);
#else
    half bakedShadow = 1.0h;
#endif

    return bakedShadow;
}

// Fills a light struct given a perObjectLightIndex
Light GetAdditionalPerObjectLight(int perObjectLightIndex, float3 positionWS, half4 shadowMask)
{
//     // Abstraction over Light input constants
// #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
//     float4 lightPositionWS = _AdditionalLightsBuffer[perObjectLightIndex].position;
//     half3 color = _AdditionalLightsBuffer[perObjectLightIndex].color.rgb;
//     half4 distanceAndSpotAttenuation = _AdditionalLightsBuffer[perObjectLightIndex].attenuation;
//     half4 spotDirection = _AdditionalLightsBuffer[perObjectLightIndex].spotDirection;
//     half4 lightOcclusionProbeInfo = _AdditionalLightsBuffer[perObjectLightIndex].occlusionProbeChannels;
// #else
    float4 lightPositionWS = _AdditionalLightsPosition[perObjectLightIndex];
    half3 color = _AdditionalLightsColor[perObjectLightIndex].rgb;
    half4 distanceAndSpotAttenuation = _AdditionalLightsAttenuation[perObjectLightIndex];
    half4 spotDirection = _AdditionalLightsSpotDir[perObjectLightIndex];
    half4 occlusionProbeChannels = _AdditionalLightOcclusionProbeChannel[perObjectLightIndex];
// #endif

    // Directional lights store direction in lightPosition.xyz and have .w set to 0.0.
    // This way the following code will work for both directional and punctual lights.
    float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
    float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);

    half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
    half attenuation = DistanceAttenuation(distanceSqr, distanceAndSpotAttenuation.xy) * AngleAttenuation(spotDirection.xyz, lightDirection, distanceAndSpotAttenuation.zw);

    if (attenuation > 1e-3h)
    {
        attenuation *= AdditionalLightShadow(shadowMask, occlusionProbeChannels);
    }
    
    Light light;
    light.direction = lightDirection;
    light.distanceAttenuation = attenuation;
    light.shadowAttenuation = 1.0;//AdditionalLightRealtimeShadow(perObjectLightIndex, positionWS, shadowMask, occlusionProbeChannels);
    light.color = color;

//     // In case we're using light probes, we can sample the attenuation from the `unity_ProbesOcclusion`
// #if defined(LIGHTMAP_ON) || defined(_MIXED_LIGHTING_SUBTRACTIVE)
//     // First find the probe channel from the light.
//     // Then sample `unity_ProbesOcclusion` for the baked occlusion.
//     // If the light is not baked, the channel is -1, and we need to apply no occlusion.

//     // probeChannel is the index in 'unity_ProbesOcclusion' that holds the proper occlusion value.
//     int probeChannel = lightOcclusionProbeInfo.x;

//     // lightProbeContribution is set to 0 if we are indeed using a probe, otherwise set to 1.
//     half lightProbeContribution = lightOcclusionProbeInfo.y;

//     half probeOcclusionValue = unity_ProbesOcclusion[probeChannel];
//     light.distanceAttenuation *= max(probeOcclusionValue, lightProbeContribution);
// #endif

    return light;
}

// Returns a per-object index given a loop index.
// This abstract the underlying data implementation for storing lights/light indices
int GetPerObjectLightIndex(uint index)
{
/////////////////////////////////////////////////////////////////////////////////////////////
// Structured Buffer Path                                                                   /
//                                                                                          /
// Lights and light indices are stored in StructuredBuffer. We can just index them.         /
// Currently all non-mobile platforms take this path :(                                     /
// There are limitation in mobile GPUs to use SSBO (performance / no vertex shader support) /
/////////////////////////////////////////////////////////////////////////////////////////////
// #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
//     uint offset = unity_LightData.x;
//     return _AdditionalLightsIndices[offset + index];

// /////////////////////////////////////////////////////////////////////////////////////////////
// // UBO path                                                                                 /
// //                                                                                          /
// // We store 8 light indices in float4 unity_LightIndices[2];                                /
// // Due to memory alignment unity doesn't support int[] or float[]                           /
// // Even trying to reinterpret cast the unity_LightIndices to float[] won't work             /
// // it will cast to float4[] and create extra register pressure. :(                          /
// /////////////////////////////////////////////////////////////////////////////////////////////
// #elif !defined(SHADER_API_GLES)
#if !defined(SHADER_API_GLES)
    // since index is uint shader compiler will implement
    // div & mod as bitfield ops (shift and mask).
    
    // TODO: Can we index a float4? Currently compiler is
    // replacing unity_LightIndicesX[i] with a dp4 with identity matrix.
    // u_xlat16_40 = dot(unity_LightIndices[int(u_xlatu13)], ImmCB_0_0_0[u_xlati1]);
    // This increases both arithmetic and register pressure.
    return unity_LightIndices[index / 4][index % 4];
#else
    // Fallback to GLES2. No bitfield magic here :(.
    // We limit to 4 indices per object and only sample unity_4LightIndices0.
    // Conditional moves are branch free even on mali-400
    // small arithmetic cost but no extra register pressure from ImmCB_0_0_0 matrix.
    half2 lightIndex2 = (index < 2.0h) ? unity_LightIndices[0].xy : unity_LightIndices[0].zw;
    half i_rem = (index < 2.0h) ? index : index - 2.0h;
    return (i_rem < 1.0h) ? lightIndex2.x : lightIndex2.y;
#endif
}

// Fills a light struct given a loop i index. This will convert the i
// index to a perObjectLightIndex
Light GetAdditionalLight(uint i, float3 positionWS, half4 shadowMask)
{
    int perObjectLightIndex = GetPerObjectLightIndex(i);
    return GetAdditionalPerObjectLight(perObjectLightIndex, positionWS, shadowMask);
}

uint GetAdditionalLightsCount()
{
    // TODO: we need to expose in SRP api an ability for the pipeline cap the amount of lights
    // in the culling. This way we could do the loop branch with an uniform
    // This would be helpful to support baking exceeding lights in SH as well
    return (uint)(int)max(0.0, min(_AdditionalLightsCount.x, unity_LightData.y));
}

half3 LightingLambert(half3 lightColor, half3 lightDir, half3 normal)
{
    half NdotL = saturate(dot(normal, lightDir));
    return lightColor * NdotL * INV_PI;
}

// http://blog.stevemcauley.com/2011/12/03/energy-conserving-wrapped-diffuse/
half3 LightingWrappedLighting(half3 lightColor, half3 lightDir, half3 normal)
{
    half NdotL = dot(normal, lightDir);
    const half w = 1.0h;
    const half wrapCorrectionInverse = 1.0h / ((1.0h + w) * (1.0h + w));
    return lightColor * INV_PI * saturate(NdotL * wrapCorrectionInverse + wrapCorrectionInverse);
}

half3 EvaluateDynamicLighting(float3 positionWS, half3 normalWS, half4 shadowMask)
{
	half3 outgoingRadiance = 0.0h;
	uint lightsCount = GetAdditionalLightsCount();

#if (SHADER_TARGET >= 30)
    for (uint lightIndex = (uint)0u; lightIndex < lightsCount; ++lightIndex)
    {
#else
    for (uint lightIndex = (uint)0u; lightIndex < (uint)8u; ++lightIndex)
    {
        if (lightIndex < lightsCount)
#endif
        {
            Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
            half3 lightColor = light.color * light.distanceAttenuation;

        #if defined(_BRDF_MODE_LAMBERT)
            outgoingRadiance += LightingLambert(lightColor, light.direction, normalWS);
        #elif defined(_BRDF_MODE_WRAPPED_LIGHTING)
            outgoingRadiance += LightingWrappedLighting(lightColor, light.direction, normalWS);
        #endif
        }
    }
    return outgoingRadiance;
}

#endif