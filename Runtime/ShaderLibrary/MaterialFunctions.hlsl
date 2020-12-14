#ifndef PSX_MATERIAL_FUNCTIONS
#define PSX_MATERIAL_FUNCTIONS

// This file contains functions invokable within the forward rendered "material pass" context.
// They rely on some psx standard globals uniforms and UnityPerMaterial materials to be setup in order to function.
// Note, a more functional approach could have been taken here,
// all of these uniforms could have been passed in to decouple these functions from the material pass context.
// In practice, these functions will only ever be invoked within that context, and not having to thread so many uniforms through
// improves code readability inside the *Pass.hlsl files.

float4 ApplyPrecisionGeometryToPositionCS(float3 positionWS, float3 positionVS, float4 positionCS, float precisionGeometryWeight, int drawDistanceOverrideMode, float2 drawDistanceOverride)
{
    if (_IsPSXQualityEnabled)
    {
        // Force triangle to degenerate (by writing zero to all components including W) if vertex is greater than our user specified draw distance.
        float2 drawDistance = _DrawDistance;
        if (drawDistanceOverrideMode == PSX_DRAW_DISTANCE_OVERRIDE_MODE_DISABLED)
        {
            drawDistance = FLT_MAX;
        }
        else if (drawDistanceOverrideMode == PSX_DRAW_DISTANCE_OVERRIDE_MODE_OVERRIDE)
        {
            drawDistance = drawDistanceOverride; 
        }
        else if (drawDistanceOverrideMode == PSX_DRAW_DISTANCE_OVERRIDE_MODE_ADD)
        {
            drawDistance.x = max(0.0f, _DrawDistance.x + drawDistanceOverride.x);
            drawDistance.y = drawDistance.x * drawDistance.x;
        }
        else if (drawDistanceOverrideMode == PSX_DRAW_DISTANCE_OVERRIDE_MODE_MULTIPLY)
        {
            drawDistance.x = max(0.0f, _DrawDistance.x * drawDistanceOverride.x);
            drawDistance.y = drawDistance.x * drawDistance.x;
        }

        positionCS = EvaluateDrawDistanceIsVisible(positionWS, _WorldSpaceCameraPos, positionVS, _DrawDistanceFalloffMode, drawDistance.x, drawDistance.y) ? positionCS : 0.0f;

        // Snap vertices to pixel centers. PSX does not support sub-pixel vertex accuracy.
        float w = positionCS.w;
        positionCS.xy *= rcp(w); // Apply divide by W to temporarily homogenize coordinates.

        float4 screenSizePrecisionGeometry = _ScreenSizeRasterization * _PrecisionGeometry.xxyy;
        float2 positionSS = floor((positionCS.xy * 0.5f + 0.5f) * screenSizePrecisionGeometry.xy + 0.5f);

        // Material can locally decrease vertex snapping contribution with _PrecisionGeometryWeight.
        positionCS.xy = lerp(positionCS.xy, (positionSS * screenSizePrecisionGeometry.zw) * 2.0f - 1.0f, precisionGeometryWeight);
        positionCS.xy *= w; // Unapply divide by W, as the hardware will automatically perform this transform between the vertex and fragment shaders.
    }
    return positionCS;
}

float4 ApplyPrecisionGeometryToPositionCS(float3 positionWS, float3 positionVS, float4 positionCS, float precisionGeometryWeight)
{
    int drawDistanceOverrideMode = PSX_DRAW_DISTANCE_OVERRIDE_MODE_NONE;
    float2 drawDistanceOverrideUnused = float2(0.0f, 0.0f);
    return ApplyPrecisionGeometryToPositionCS(positionWS, positionVS, positionCS, precisionGeometryWeight, drawDistanceOverrideMode, drawDistanceOverrideUnused);
}

float3 ApplyAffineTextureWarpingToUVW(float2 uv, float positionCSW, float affineTextureWarpingWeight)
{
    float3 uvw = float3(uv, 1.0f);

    if (_IsPSXQualityEnabled)
    {
        // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
        // This emulates the affine texture transform of the PSX era that did not take perspective correction into account as this division was prohibitively expensive.
        uvw.z = lerp(1.0f, positionCSW, _AffineTextureWarping * affineTextureWarpingWeight);
        uvw.xy *= uvw.z;
    }

    return uvw;
}

float4 EvaluateColorPerVertex(float4 vertexColor, float affineWarpingScale)
{
    float4 color = 0.0f;

#if defined(_VERTEX_COLOR_MODE_COLOR) || defined(_VERTEX_COLOR_MODE_COLOR_BACKGROUND)
    // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
    color = vertexColor * affineWarpingScale; 
#endif

    return color;
}

float3 EvaluateNormalDoubleSidedPerVertex(float3 normalFrontFaceWS, float3 positionWS, float3 worldSpaceCameraPos)
{
    float3 normalWS = normalFrontFaceWS;

#if defined(_SHADING_EVALUATION_MODE_PER_VERTEX) && defined(_DOUBLE_SIDED_ON)
    // Double-sided normal is only needs to be resolved at the vertex stage if we are performing per-vertex lighting.
    // When performing per-pixel lighting we can defer to the pixel shader, which has a higher cost, but allows us to use VFACE semantic
    // for accurate detection of whether we are rendering the front face or back face.
    // For per-vertex lighting, we need to rely on extrapolating an approximate detection of back faces via the sign of dot(normal, view)
    // which is accurate with hard surface normals, but results in some normal flipping around edges with soft normals and glancing angles.
    // Note: No need to normalize the V vector, or the normal vector, as we only care about the sign of NdotV.
    float3 V = worldSpaceCameraPos - positionWS;
    float NdotV = dot(normalWS, V);
    normalWS *= (NdotV >= 0.0f) ? 1.0f : _DoubleSidedConstants.z;
#endif

    return normalWS;
}

float3 EvaluateNormalDoubleSidedPerPixel(float3 normalFrontFaceWS, FRONT_FACE_TYPE cullFace)
{
    float3 normalWS = normalFrontFaceWS;

#if defined(_SHADING_EVALUATION_MODE_PER_PIXEL) && defined(_DOUBLE_SIDED_ON)
    bool isFrontFace = IS_FRONT_VFACE(cullFace, true, false);
    normalWS *= isFrontFace ? 1.0f : _DoubleSidedConstants.z;
#endif

    return normalWS;
}

float3 EvaluateLightingPerVertex(float3 positionWS, float3 normalWS, float4 vertexColor, float2 lightmapUV, float affineWarpingScale)
{
    float3 lighting = 0.0f;

#if defined(_SHADING_EVALUATION_MODE_PER_VERTEX)

#if defined(_LIGHTING_BAKED_ON) || defined(_VERTEX_COLOR_MODE_LIGHTING) || defined(_LIGHTING_DYNAMIC_ON)
    if (_IsPSXQualityEnabled)
    {
        lighting = 0.0f;

#if defined(_LIGHTING_BAKED_ON)
    #ifdef LIGHTMAP_ON
        lighting = SRGBToLinear(SampleLightmap(lightmapUV, normalWS));
    #else
        // Interestingly, it seems that unity_SHXx terms are always provided in linear space, regardless of color space setting of render pipeline.
        lighting = SampleSH(normalWS);
    #endif

        lighting *= _BakedLightingMultiplier;
#endif
        
#if defined(_VERTEX_COLOR_MODE_LIGHTING)
        lighting += SRGBToLinear(vertexColor.rgb) * _VertexColorLightingMultiplier;
#endif

#if defined(_LIGHTING_DYNAMIC_ON)
        lighting += EvaluateDynamicLighting(positionWS, normalWS) * _DynamicLightingMultiplier;
#endif

        // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
        lighting *= affineWarpingScale;
    }
    else
    {
        lighting = 1.0f;
    }
#endif

#else // _SHADING_EVALUATION_MODE_PER_PIXEL

#if defined(_VERTEX_COLOR_MODE_LIGHTING)
    // Still need to evaluate vertex color per vertex, even in per pixel overall evaluation mode.
    lighting = SRGBToLinear(vertexColor.rgb) * _VertexColorLightingMultiplier;
    // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
    lighting *= affineWarpingScale;
#endif

#endif

    return lighting;
}

float3 EvaluateLightingPerPixel(float3 positionWS, float3 normalWS, float3 vertexLighting, float2 lightmapUV, float affineWarpingScaleInverse)
{
    float3 lighting = 0.0f;

#if defined(_SHADING_EVALUATION_MODE_PER_VERTEX)
#if defined(_LIGHTING_BAKED_ON) || defined(_VERTEX_COLOR_MODE_LIGHTING) || defined(_LIGHTING_DYNAMIC_ON)
    if (_LightingIsEnabled > 0.5f)
    {
        lighting = vertexLighting * affineWarpingScaleInverse;
    }
#endif
#elif defined(_SHADING_EVALUATION_MODE_PER_PIXEL)
#if defined(_LIGHTING_BAKED_ON) || defined(_VERTEX_COLOR_MODE_LIGHTING) || defined(_LIGHTING_DYNAMIC_ON)
    if (_LightingIsEnabled > 0.5f)
    {

    #if defined(_LIGHTING_BAKED_ON)
    #ifdef LIGHTMAP_ON
        lighting = SRGBToLinear(SampleLightmap(lightmapUV, normalWS));
    #else
        // Interestingly, it seems that unity_SHXx terms are always provided in linear space, regardless of color space setting of render pipeline.
        lighting = SampleSH(normalWS);
    #endif

        lighting *= _BakedLightingMultiplier;
#endif
        
#if defined(_VERTEX_COLOR_MODE_LIGHTING)
        lighting += vertexLighting * affineWarpingScaleInverse;
#endif

#if defined(_LIGHTING_DYNAMIC_ON)
        lighting += EvaluateDynamicLighting(positionWS, normalWS) * _DynamicLightingMultiplier;
#endif
    }
#endif
#endif

    return lighting;
}

float4 ApplyLightingToColor(float3 lighting, float4 color)
{
#if defined(_LIGHTING_BAKED_ON) || defined(_VERTEX_COLOR_MODE_LIGHTING) || defined(_LIGHTING_DYNAMIC_ON)
    if (_LightingIsEnabled > 0.5f)
    {
        color.rgb *= lighting;
    }
#endif

    return color;
}

float4 EvaluateFogPerVertex(float3 positionWS, float3 positionVS, float affineWarpingScale, float fogWeight)
{
    float4 fog = 0.0f;

#if defined(_FOG_ON) && defined(_SHADING_EVALUATION_MODE_PER_VERTEX)
    if (_IsPSXQualityEnabled)
    {
        float fogAlpha = EvaluateFogFalloff(positionWS, _WorldSpaceCameraPos, positionVS, _FogFalloffMode, _FogDistanceScaleBias, _FogFalloffCurvePower);
        fogAlpha *= _FogColor.a;

        // TODO: We could perform this discretization and transform to linear space on the CPU side and pass in.
        // For now just do it here to make this code easier to refactor as we figure out the architecture.
        float3 fogColor = floor(_FogColor.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
        fogColor = SRGBToLinear(fogColor);
        fogColor *= fogAlpha;

        if (_FogIsAdditionalLayerEnabled)
        {
            float fogAlphaLayer1 = EvaluateFogFalloff(positionWS, _WorldSpaceCameraPos, positionVS, _FogFalloffModeLayer1, _FogDistanceScaleBiasLayer1, _FogFalloffCurvePowerLayer1);
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

        fogAlpha *= fogWeight;
        
        fogAlpha = saturate(floor(fogAlpha * _FogPrecisionAlphaAndInverse.x + 0.5f) * _FogPrecisionAlphaAndInverse.y);

        fog = float4(fogColor, fogAlpha);

        // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
        fog *= affineWarpingScale;
    }
#endif

    return fog;
}

float ApplyPrecisionAlphaToAlpha(float alpha)
{
    return floor(alpha * _PrecisionAlphaAndInverse.x + 0.5f) * _PrecisionAlphaAndInverse.y;
}

float4 ApplyPrecisionColorToColorSRGB(float4 color)
{
    // Convert to RGB 5:6:5 color space (default) - this will actually be whatever color space the user specified in the Precision Volume Override.
    color.rgb = floor(color.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
    color.a = ApplyPrecisionAlphaToAlpha(color.a);

    return color;
}

float4 ApplyAlphaBlendTransformToColor(float4 color)
{
#if defined(_ALPHAPREMULTIPLY_ON)
    color.rgb *= color.a;
#elif defined(_ALPHAMODULATE_ON)
    color.rgb = lerp(float3(1.0f, 1.0f, 1.0f), color.rgb, color.a);
#endif

    return color;
}

float3 ApplyAlphaBlendTransformToEmission(float3 emission, float colorAlpha)
{
#if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAMODULATE_ON)
    emission *= colorAlpha;
#endif

    return emission;
}

float ApplyAlphaBlendTransformToFog(float fogAlpha, float colorAlpha)
{
#if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAMODULATE_ON)
    fogAlpha *= colorAlpha;
#endif

    return fogAlpha;
}

float4 EvaluateFogPerPixel(float3 positionWS, float3 positionVS, float2 positionSS, float4 vertexFog, float affineWarpingScaleInverse, float fogWeight)
{
    float3 fogColor = 0.0f;
    float fogAlpha = 0.0f;

#if defined(_FOG_ON)
#if defined(_SHADING_EVALUATION_MODE_PER_VERTEX)
    fogColor = vertexFog.rgb * affineWarpingScaleInverse;
    fogAlpha = vertexFog.a * affineWarpingScaleInverse;
#elif defined(_SHADING_EVALUATION_MODE_PER_PIXEL)
    fogAlpha = EvaluateFogFalloff(positionWS, _WorldSpaceCameraPos, positionVS, _FogFalloffMode, _FogDistanceScaleBias, _FogFalloffCurvePower);
    fogAlpha *= _FogColor.a;
    
    // TODO: We could perform this discretization and transform to linear space on the CPU side and pass in.
    // For now just do it here to make this code easier to refactor as we figure out the architecture.
    fogColor = floor(_FogColor.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
    fogColor = SRGBToLinear(fogColor);

    if (_FogIsAdditionalLayerEnabled)
    {
        float fogAlphaLayer1 = EvaluateFogFalloff(positionWS, _WorldSpaceCameraPos, positionVS, _FogFalloffModeLayer1, _FogDistanceScaleBiasLayer1, _FogFalloffCurvePowerLayer1);
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

    fogAlpha *= fogWeight;

    fogAlpha = ComputeFogAlphaDiscretization(fogAlpha, positionSS);
#endif

#endif

    return float4(fogColor, fogAlpha);
}

float4 ApplyFogToColor(float4 fog, float4 color)
{
#if defined(_FOG_ON)
    // fogColor has premultiplied alpha.
    color.rgb = lerp(color.rgb, fog.rgb, fog.a);
#endif

    return color;
}

void ComputeLODAndTexelSizeMaybeCallDDX(out float4 texelSizeLod, out float lod, float2 uv, float4 texelSize)
{
#if defined(_TEXTURE_FILTER_MODE_N64_MIPMAPS) || defined(_TEXTURE_FILTER_MODE_POINT_MIPMAPS)
    ComputeLODAndTexelSizeFromEvaluateDDXDDY(texelSizeLod, lod, uv, texelSize);
#else // defined(_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS)
    // No modifications.
    texelSizeLod = texelSize;
    lod = 0.0f;
#endif
}

float4 SampleTextureWithFilterMode(TEXTURE2D_PARAM(tex, samp), float2 uv, float4 texelSizeLod, float lod)
{
#if defined(_TEXTURE_FILTER_MODE_POINT) || defined(_TEXTURE_FILTER_MODE_POINT_MIPMAPS) 
    return SampleTextureWithFilterModePoint(TEXTURE2D_ARGS(tex, samp), uv, texelSizeLod, lod);
#elif defined(_TEXTURE_FILTER_MODE_N64) || defined(_TEXTURE_FILTER_MODE_N64_MIPMAPS)
    return SampleTextureWithFilterModeN64(TEXTURE2D_ARGS(tex, samp), uv, texelSizeLod, lod);
#else // defined(_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS)
    return SAMPLE_TEXTURE2D(tex, samp, uv);
#endif
}

#endif