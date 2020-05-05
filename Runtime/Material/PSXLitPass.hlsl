#ifndef PSX_LIT_PASS
#define PSX_LIT_PASS

#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderFunctions.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/BakedLighting.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/DynamicLighting.hlsl"

struct Attributes
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
#if defined(_LIGHTING_VERTEX_COLOR_ON)
    float4 color : COLOR;
#endif
#if defined(_LIGHTING_BAKED_ON)
    float2 lightmapUV : TEXCOORD1;
#endif
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float3 uvw : TEXCOORD0;
    float3 positionVS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
#if defined(_SHADING_EVALUATION_MODE_PER_VERTEX)
    float4 fog : TEXCOORD3;
#if defined(_LIGHTING_VERTEX_COLOR_ON) || defined(_LIGHTING_BAKED_ON) || defined(_LIGHTING_DYNAMIC_ON)
    float3 lighting : TEXCOORD4;
#endif
#elif defined(_SHADING_EVALUATION_MODE_PER_PIXEL)
    float3 positionWS : TEXCOORD3;
#if defined(_LIGHTING_BAKED_ON) && defined(LIGHTMAP_ON)
    float2 lightmapUV : TEXCOORD4;
#if defined(_LIGHTING_VERTEX_COLOR_ON)
    float3 lighting : TEXCOORD5;
#endif
#elif defined(_LIGHTING_VERTEX_COLOR_ON)
    float3 lighting : TEXCOORD4;
#endif
#endif
};

Varyings LitPassVertex(Attributes v)
{
    Varyings o;

    float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
    float3 positionVS = TransformWorldToView(positionWS);
    float4 positionCS = TransformWorldToHClip(positionWS);
    o.vertex = positionCS;

    if (_IsPSXQualityEnabled)
    {
        // Force triangle to degenerate (by writing zero to all components including W) if vertex is greater than our user specified draw distance.
        o.vertex = (abs(positionVS.z) > _DrawDistance) ? 0.0f : o.vertex;

        // Snap vertices to pixel centers. PSX does not support sub-pixel vertex accuracy.
        float w = o.vertex.w;
        o.vertex.xy *= rcp(w); // Apply divide by W to temporarily homogenize coordinates.

        float4 screenSizePrecisionGeometry = _ScreenSize * _PrecisionGeometry.xxyy;
        float2 positionSS = floor((o.vertex.xy * 0.5f + 0.5f) * screenSizePrecisionGeometry.xy + 0.5f);
        o.vertex.xy = (positionSS * screenSizePrecisionGeometry.zw) * 2.0f - 1.0f;
        o.vertex.xy *= w; // Unapply divide by W, as the hardware will automatically perform this transform between the vertex and fragment shaders.
    }

    float2 uv = v.uv;

    if (_IsPSXQualityEnabled)
    {
        // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
        // This emulates the affine texture transform of the PSX era that did not take perspective correction into account as this division was prohibitively expensive.
        o.uvw.z = lerp(1.0f, positionCS.w, _AffineTextureWarping * _AffineTextureWarpingWeight);
        o.uvw.xy = uv * o.uvw.z;
    }
    else
    {
        o.uvw = float3(uv.x, uv.y, 1.0f);
    }

    o.positionVS = positionVS;

    float3 normalWS = TransformObjectToWorldNormal(v.normal);
    o.normalWS = normalWS;

#if defined(_SHADING_EVALUATION_MODE_PER_VERTEX)

#if defined(_LIGHTING_BAKED_ON) || defined(_LIGHTING_VERTEX_COLOR_ON) || defined(_LIGHTING_DYNAMIC_ON)
    if (_IsPSXQualityEnabled)
    {
        o.lighting = 0.0f;

#if defined(_LIGHTING_BAKED_ON)
    #ifdef LIGHTMAP_ON
        float2 lightmapUV = v.lightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw;
        o.lighting = SRGBToLinear(SampleLightmap(lightmapUV, normalWS));
    #else
        // TODO: Track down why we need this scale factor for probes. I have a suspision it is because we are working in
        // gamma space, and that the supporting code is meant to be executed in linear, but we will see.
        const float PROBE_SCALE_FACTOR = 12.0f;
        o.lighting = SRGBToLinear(SampleSH(normalWS)) * PROBE_SCALE_FACTOR;
    #endif

        o.lighting *= _BakedLightingMultiplier;
#endif
        
#if defined(_LIGHTING_VERTEX_COLOR_ON)
        o.lighting += SRGBToLinear(v.color.rgb) * _VertexColorLightingMultiplier;
#endif

#if defined(_LIGHTING_DYNAMIC_ON)
        o.lighting += EvaluateDynamicLighting(positionWS, normalWS) * _DynamicLightingMultiplier;
#endif

        // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
        o.lighting *= o.uvw.z;
    }
    else
    {
        o.lighting = 1.0f;
    }
#endif

#if defined(_SHADING_EVALUATION_MODE_PER_VERTEX)
    if (_IsPSXQualityEnabled)
    {
        float fogAlpha = EvaluateFogFalloff(positionWS, _WorldSpaceCameraPos, positionVS, _FogFalloffMode, _FogDistanceScaleBias);
        fogAlpha *= _FogColor.a;

        // TODO: We could perform this discretization and transform to linear space on the CPU side and pass in.
        // For now just do it here to make this code easier to refactor as we figure out the architecture.
        float3 fogColor = floor(_FogColor.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
        fogColor = SRGBToLinear(fogColor);
        fogColor *= fogAlpha;

        if (_FogIsAdditionalLayerEnabled)
        {
            float fogAlphaLayer1 = EvaluateFogFalloff(positionWS, _WorldSpaceCameraPos, positionVS, _FogFalloffModeLayer1, _FogDistanceScaleBiasLayer1);
            fogAlphaLayer1 *= _FogColorLayer1.a;

            float3 fogColorLayer1 = floor(_FogColorLayer1.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
            fogColorLayer1 = SRGBToLinear(fogColorLayer1);
            fogColorLayer1 *= fogAlphaLayer1;

            // Blend between fog layer0 and fog layer1 with "over" blend mode:
            // We store final fogColor as most-multiplied, rather than premultiplied.
            // This is necessary because we want to perform alpha discretization once, post alpha blending.
            // In order to do this, we need to compute the blend ratio between the two colors.
            // Pre-multiplied blend example: 
            // fogColor = fogColor * (1.0f - fogAlphaLayer1) + fogColorLayer1;
            float fogColorWeight0 = fogAlpha * (1.0f - fogAlphaLayer1);
            float fogColorWeight1 = fogAlphaLayer1;
            float fogColorWeightTotal = fogColorWeight0 + fogColorWeight1;
            float fogColorNormalization = (fogColorWeightTotal > 1e-5f) ? (1.0f / fogColorWeightTotal) : 0.0f;
            fogColor = (fogColor * fogColorWeight0 + fogColorLayer1 * fogColorWeight1) * fogColorNormalization;

            // Apply over blend mode to alpha channel.
            fogAlpha = fogAlpha * (1.0f - fogAlphaLayer1) + fogAlphaLayer1;
        }
        
        fogAlpha = saturate(floor(fogAlpha * _FogPrecisionAlphaAndInverse.x + 0.5f) * _FogPrecisionAlphaAndInverse.y);

        o.fog = float4(fogColor, fogAlpha);

        // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
        o.fog *= o.uvw.z;
    }
#endif

#elif defined(_SHADING_EVALUATION_MODE_PER_PIXEL)
    o.normalWS = TransformObjectToWorldNormal(v.normal);
    o.positionWS = positionWS;

#if defined(_LIGHTING_BAKED_ON) && defined(LIGHTMAP_ON)
    o.lightmapUV = v.lightmapUV.xy;
#endif

#if defined(_LIGHTING_VERTEX_COLOR_ON)
    // Still need to evaluate vertex color per vertex, even in per pixel overall evaluation mode.
    o.lighting = SRGBToLinear(v.color.rgb) * _VertexColorLightingMultiplier;
    // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
    o.lighting *= o.uvw.z;
#endif

#endif

    return o;
}

half4 LitPassFragment(Varyings i) : SV_Target
{
    float2 positionSS = i.vertex.xy;

    // Remember, we multipled all interpolated vertex values by positionCS.w in order to counter contemporary hardware's perspective correct interpolation.
    // We need to post multiply all interpolated vertex values by the interpolated positionCS.w (stored in uvw.z) component to "normalize" the interpolated values.
    float interpolatorNormalization = 1.0f / i.uvw.z;

    float3 normalWS = normalize(i.normalWS);

    float2 uv = i.uvw.xy * interpolatorNormalization;
    float2 uvColor = TRANSFORM_TEX(uv, _MainTex);
    float4 color = _MainColor * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvColor);

#if _ALPHATEST_ON
    // Perform alpha cutoff transparency (i.e: discard pixels in the holes of a chain link fence texture, or in the negative space of a leaf texture).
    // Any alpha value < alphaClippingDither will trigger the pixel to be discarded, any alpha value greater than or equal to alphaClippingDither will trigger the pixel to be preserved.
    float alphaClippingDither = FetchAlphaClippingDither(positionSS);
    clip((color.a > alphaClippingDither) ? 1.0f : -1.0f);
#endif

    if (!_IsPSXQualityEnabled)
    {
        // TODO: Handle premultiply alpha case here?
        return color;
    }

    // Convert to RGB 5:6:5 color space (default) - this will actually be whatever color space the user specified in the Precision Volume Override.
    color.rgb = floor(color.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
    color.a = floor(color.a * _PrecisionAlphaAndInverse.x + 0.5f) * _PrecisionAlphaAndInverse.y;
    color.rgb = SRGBToLinear(color.rgb);

#if defined(_ALPHAPREMULTIPLY_ON)
    color.rgb *= color.a;
#elif defined(_ALPHAMODULATE_ON)
    color.rgb = lerp(float3(1.0f, 1.0f, 1.0f), color.rgb, color.a);
#endif

#if defined(_SHADING_EVALUATION_MODE_PER_VERTEX)
#if defined(_LIGHTING_BAKED_ON) || defined(_LIGHTING_VERTEX_COLOR_ON) || defined(_LIGHTING_DYNAMIC_ON)
    if (_LightingIsEnabled > 0.5f)
    {
        color.rgb *= i.lighting * interpolatorNormalization;
    }
#endif
#elif defined(_SHADING_EVALUATION_MODE_PER_PIXEL)
#if defined(_LIGHTING_BAKED_ON) || defined(_LIGHTING_VERTEX_COLOR_ON) || defined(_LIGHTING_DYNAMIC_ON)
    if (_LightingIsEnabled > 0.5f)
    {
        float3 lighting = 0.0f;

    #if defined(_LIGHTING_BAKED_ON)
    #ifdef LIGHTMAP_ON
        float2 lightmapUV = i.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
        lighting = SRGBToLinear(SampleLightmap(lightmapUV, normalWS));
    #else
        // TODO: Track down why we need this scale factor for probes. I have a suspision it is because we are working in
        // gamma space, and that the supporting code is meant to be executed in linear, but we will see.
        const float PROBE_SCALE_FACTOR = 12.0f;
        lighting = SRGBToLinear(SampleSH(normalWS)) * PROBE_SCALE_FACTOR;
    #endif

        lighting *= _BakedLightingMultiplier;
#endif
        
#if defined(_LIGHTING_VERTEX_COLOR_ON)
        lighting += i.lighting * interpolatorNormalization;
#endif

#if defined(_LIGHTING_DYNAMIC_ON)
        lighting += EvaluateDynamicLighting(i.positionWS, normalWS) * _DynamicLightingMultiplier;
#endif

        color.rgb *= lighting;
    }
#endif
#endif

#ifdef _EMISSION
    // Convert to sRGB 5:6:5 color space, then from sRGB to Linear.
    float2 uvEmission = TRANSFORM_TEX(uv, _EmissionTexture);
    float3 emission = _EmissionColor.rgb * SAMPLE_TEXTURE2D(_EmissionTexture, sampler_EmissionTexture, uvEmission).rgb;
    emission = floor(emission * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
    emission = SRGBToLinear(emission);

#if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAMODULATE_ON)
    emission *= color.a;
#endif

    color.rgb += emission;
#endif

#if defined(_REFLECTION_ON)
    float2 uvReflection = TRANSFORM_TEX(uv, _ReflectionTexture);
    float3 reflection = _ReflectionColor.rgb * SAMPLE_TEXTURE2D(_ReflectionTexture, sampler_ReflectionTexture, uvReflection).rgb;
    reflection = floor(reflection * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
    reflection = SRGBToLinear(reflection);

#if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAMODULATE_ON)
    reflection *= color.a;
#endif

    float3 V = normalize(-i.positionVS);
    float3 R = reflect(V, i.normalWS);
    float4 reflectionCubemap = SAMPLE_TEXTURECUBE(_ReflectionCubemap, sampler_ReflectionCubemap, R);
    reflectionCubemap.rgb *= reflectionCubemap.a;
    reflectionCubemap.rgb = floor(reflectionCubemap.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
    reflection *= reflectionCubemap.rgb;

    if (_ReflectionBlendMode == 0)
    {
        // Additive
        color.rgb += reflection;
    }
    else if (_ReflectionBlendMode == 1)
    {
        // Subtractive
        color.rgb = max(0.0f, color.rgb - reflection);
    }
    else if (_ReflectionBlendMode == 2)
    {
        // Multiply
        color.rgb *= reflection;
    }

#endif

#if defined(_SHADING_EVALUATION_MODE_PER_VERTEX)
    float3 fogColor = i.fog.rgb * interpolatorNormalization;
    float fogAlpha = i.fog.a * interpolatorNormalization;
#elif defined(_SHADING_EVALUATION_MODE_PER_PIXEL)
    float fogAlpha = EvaluateFogFalloff(i.positionWS, _WorldSpaceCameraPos, i.positionVS, _FogFalloffMode, _FogDistanceScaleBias);
    fogAlpha *= _FogColor.a;
    
    // TODO: We could perform this discretization and transform to linear space on the CPU side and pass in.
    // For now just do it here to make this code easier to refactor as we figure out the architecture.
    float3 fogColor = floor(_FogColor.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
    fogColor = SRGBToLinear(fogColor);

    if (_FogIsAdditionalLayerEnabled)
    {
        float fogAlphaLayer1 = EvaluateFogFalloff(i.positionWS, _WorldSpaceCameraPos, i.positionVS, _FogFalloffModeLayer1, _FogDistanceScaleBiasLayer1);
        fogAlphaLayer1 *= _FogColorLayer1.a;

        float3 fogColorLayer1 = floor(_FogColorLayer1.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
        fogColorLayer1 = SRGBToLinear(fogColorLayer1);
        fogColorLayer1 *= fogAlphaLayer1;

        // Blend between fog layer0 and fog layer1 with "over" blend mode:
        // We store final fogColor as most-multiplied, rather than premultiplied.
        // This is necessary because we want to perform alpha discretization once, post alpha blending.
        // In order to do this, we need to compute the blend ratio between the two colors.
        // Pre-multiplied blend example: 
        // fogColor = fogColor * (1.0f - fogAlphaLayer1) + fogColorLayer1;
        float fogColorWeight0 = fogAlpha * (1.0f - fogAlphaLayer1);
        float fogColorWeight1 = fogAlphaLayer1;
        float fogColorWeightTotal = fogColorWeight0 + fogColorWeight1;
        float fogColorNormalization = (fogColorWeightTotal > 1e-5f) ? (1.0f / fogColorWeightTotal) : 0.0f;
        fogColor = (fogColor * fogColorWeight0 + fogColorLayer1 * fogColorWeight1) * fogColorNormalization;

        // Apply over blend mode to alpha channel.
        fogAlpha = fogAlpha * (1.0f - fogAlphaLayer1) + fogAlphaLayer1;
    }

    fogAlpha = ComputeFogAlphaDiscretization(fogAlpha, positionSS);
#endif

#if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAMODULATE_ON)
    fogAlpha *= color.a;
#endif

    // fogColor has premultiplied alpha.
    color.rgb = lerp(color.rgb, fogColor, fogAlpha);
    
#if !defined(_ALPHAMODULATE_ON)
    // Apply tonemapping and gamma correction.
    // This is a departure from classic PS1 games, but it allows for greater flexibility, giving artists more controls for creating the final look and feel of their game.
    // Otherwise, they would need to spend a lot more time in the texturing phase, getting the textures alone to produce the mood they are aiming for.
    if (_TonemapperIsEnabled)
    {
        color.rgb = TonemapperGeneric(color.rgb);
    }
#endif
    
    color.rgb = LinearToSRGB(color.rgb);

    // Convert the final color value to 5:6:5 color space (default) - this will actually be whatever color space the user specified in the Precision Volume Override.
    // This emulates a the limited bit-depth frame buffer.
    color.rgb = ComputeFramebufferDiscretization(color.rgb, positionSS);

    return (half4)color;
}

#endif
