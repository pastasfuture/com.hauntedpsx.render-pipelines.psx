#ifndef PSX_MATERIAL_FUNCTIONS
#define PSX_MATERIAL_FUNCTIONS

// This file contains functions invokable within the forward rendered "material pass" context.
// They rely on some psx standard globals uniforms and UnityPerMaterial materials to be setup in order to function.
// Note, a more functional approach could have been taken here,
// all of these uniforms could have been passed in to decouple these functions from the material pass context.
// In practice, these functions will only ever be invoked within that context, and not having to thread so many uniforms through
// improves code readability inside the *Pass.hlsl files.

// Warning: Needs to stay in sync with PSXRenderPipeline::ComputePrecisionGeometryParameters().
float2 ComputePrecisionGeometryParameters(float precisionGeometryNormalized)
{
    float precisionGeometryExponent = lerp(6.0f, 0.0f, precisionGeometryNormalized);
    float precisionGeometryScaleInverse = pow(2.0f, max(0.0f, precisionGeometryExponent)); // max() is to silence warnings / precision issues.
    float precisionGeometryScale = 1.0f / precisionGeometryScaleInverse;

    return float2(precisionGeometryScale, precisionGeometryScaleInverse);
}

void ApplyPrecisionColorOverride(out float3 precisionColorOut, out float3 precisionColorInverseOut, float3 precisionColorIn, float3 precisionColorInverseIn, float precisionColorIndexNormalized, float precisionChromaBit, int precisionColorOverrideMode, float3 precisionColorOverrideParameters)
{
    precisionColorOut = precisionColorIn;
    precisionColorInverseOut = precisionColorInverseIn;

    if (!_IsPSXQualityEnabled)
    {
        // Nothing to do.
    }
    else if (precisionColorOverrideMode == PSX_PRECISION_COLOR_OVERRIDE_MODE_NONE)
    {
        // Nothing to do.
    }
    else if (precisionColorOverrideMode == PSX_PRECISION_COLOR_OVERRIDE_MODE_DISABLED)
    {
        precisionColorOut = precisionColorOverrideParameters.x;
        precisionColorInverseOut = precisionColorOverrideParameters.y;
    }
    else if (precisionColorOverrideMode == PSX_PRECISION_COLOR_OVERRIDE_MODE_OVERRIDE)
    {
        precisionColorOut = precisionColorOverrideParameters.x;
        precisionColorInverseOut = precisionColorOverrideParameters.y;
    }
    else if (precisionColorOverrideMode == PSX_PRECISION_COLOR_OVERRIDE_MODE_ADD)
    {
        float precisionColorIndexNormalizedOverride = saturate(precisionColorIndexNormalized + precisionColorOverrideParameters.z);
        float precisionColorIndex = floor(precisionColorIndexNormalizedOverride * 7.0f + 0.5f);
        float precisionColorScale = exp2(precisionColorIndex + 1.0f) - 1.0f;
        float precisionColorScaleInverse = 1.0f / precisionColorScale;

        precisionColorOut = precisionColorScale;
        precisionColorInverseOut = precisionColorScaleInverse;
    }
    else if (precisionColorOverrideMode == PSX_PRECISION_COLOR_OVERRIDE_MODE_MULTIPLY)
    {
        float precisionColorIndexNormalizedOverride = saturate(precisionColorIndexNormalized * precisionColorOverrideParameters.z);
        float precisionColorIndex = floor(precisionColorIndexNormalizedOverride * 7.0f + 0.5f);
        float precisionColorScale = exp2(precisionColorIndex + 1.0f) - 1.0f;
        float precisionColorScaleInverse = 1.0f / precisionColorScale;

        precisionColorOut = precisionColorScale;
        precisionColorInverseOut = precisionColorScaleInverse;
    }
}

void ApplyGeometryPushbackToPosition(inout float3 positionWS, inout float3 positionVS, bool geometryPushbackEnabled, float geometryPushbackDistanceMin, float geometryPushbackDistanceMax)
{
    if (geometryPushbackEnabled)
    {
        positionVS.z = (-positionVS.z < geometryPushbackDistanceMin || -positionVS.z > geometryPushbackDistanceMax) ? positionVS.z : -geometryPushbackDistanceMax;
        positionWS = PSXTransformViewToWorld(positionVS);
    }
}

void ApplyGeometryPushbackToPosition(inout float3 positionWS, inout float3 positionVS, float4 geometryPushbackParameters)
{
    bool geometryPushbackEnabled = geometryPushbackParameters.x > 0.5f;
    float geometryPushbackDistanceMin = geometryPushbackParameters.y;
    float geometryPushbackDistanceMax = geometryPushbackParameters.z;

    ApplyGeometryPushbackToPosition(positionWS, positionVS, geometryPushbackEnabled, geometryPushbackDistanceMin, geometryPushbackDistanceMax);
}

float4 ApplyPrecisionGeometryToPositionCS(float3 positionWS, float3 positionVS, float4 positionCS, int precisionGeometryOverrideMode, float3 precisionGeometryOverrideParameters, int drawDistanceOverrideMode, float2 drawDistanceOverride)
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

        float precisionGeometryScale = _PrecisionGeometry.x;
        float precisionGeometryScaleInverse = _PrecisionGeometry.y;
        float precisionGeometryNormalized = _PrecisionGeometry.z; // Raw slider value, needed for handling override modes.
        bool precisionGeometryEnabled = _PrecisionGeometry.w;
        if (precisionGeometryOverrideMode == PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_NONE)
        {
            // Nothing to do.
        }
        else if (precisionGeometryOverrideMode == PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_DISABLED)
        {
            precisionGeometryEnabled = false;
        }
        else if (precisionGeometryOverrideMode == PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_OVERRIDE)
        {
            precisionGeometryEnabled = true;
            precisionGeometryScale = precisionGeometryOverrideParameters.x;
            precisionGeometryScaleInverse = precisionGeometryOverrideParameters.y;
        }
        else if (precisionGeometryOverrideMode == PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_ADD)
        {
            float precisionGeometryOverrideOffset = precisionGeometryOverrideParameters.z;
            float2 scaleAndScaleInverse = ComputePrecisionGeometryParameters(saturate(precisionGeometryNormalized + precisionGeometryOverrideOffset));
            precisionGeometryScale = scaleAndScaleInverse.x;
            precisionGeometryScaleInverse = scaleAndScaleInverse.y;
        }
        else if (precisionGeometryOverrideMode == PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_MULTIPLY)
        {
            float precisionGeometryOverrideScale = precisionGeometryOverrideParameters.z;
            float2 scaleAndScaleInverse = ComputePrecisionGeometryParameters(saturate(precisionGeometryNormalized * precisionGeometryOverrideScale));
            precisionGeometryScale = scaleAndScaleInverse.x;
            precisionGeometryScaleInverse = scaleAndScaleInverse.y;
        }

        if (precisionGeometryEnabled)
        {
            // Snap vertices to pixel centers. PSX does not support sub-pixel vertex accuracy.
            float w = positionCS.w;
            positionCS.xy *= rcp(w); // Apply divide by W to temporarily homogenize coordinates.

            float4 screenSizePrecisionGeometry = _ScreenSizeRasterization * float4(precisionGeometryScale, precisionGeometryScale, precisionGeometryScaleInverse, precisionGeometryScaleInverse);
            float2 positionSS = floor((positionCS.xy * 0.5f + 0.5f) * screenSizePrecisionGeometry.xy + 0.5f);

            // Material can locally decrease vertex snapping contribution with _PrecisionGeometryWeight.
            positionCS.xy = (positionSS * screenSizePrecisionGeometry.zw) * 2.0f - 1.0f;
            positionCS.xy *= w; // Unapply divide by W, as the hardware will automatically perform this transform between the vertex and fragment shaders.
        }

    }
    return positionCS;
}

float4 ApplyPrecisionGeometryToPositionCS(float3 positionWS, float3 positionVS, float4 positionCS, int precisionGeometryMode, float3 precisionGeometryParameters)
{
    int drawDistanceOverrideMode = PSX_DRAW_DISTANCE_OVERRIDE_MODE_NONE;
    float2 drawDistanceOverrideUnused = float2(0.0f, 0.0f);
    return ApplyPrecisionGeometryToPositionCS(positionWS, positionVS, positionCS, precisionGeometryMode, precisionGeometryParameters, drawDistanceOverrideMode, drawDistanceOverrideUnused);
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

float4 EvaluateVertexColorPerVertex(float4 vertexColor, float affineWarpingScale)
{
    float4 color = 0.0f;

#if defined(_VERTEX_COLOR_MODE_COLOR) || defined(_VERTEX_COLOR_MODE_COLOR_BACKGROUND) || defined(_VERTEX_COLOR_MODE_ALPHA_ONLY) || defined(_VERTEX_COLOR_MODE_EMISSION) || defined(_VERTEX_COLOR_MODE_EMISSION_AND_ALPHA_ONLY) || defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING)
    // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
    color = vertexColor * affineWarpingScale;
#endif

    return color;
}

float4 ApplyVertexColorPerPixelColorVertexColorModeColor(float4 color, float4 vertexColorNormalized, int vertexColorBlendMode)
{
    float4 res = color;

    if (vertexColorBlendMode == PSX_VERTEX_COLOR_BLEND_MODE_MULTIPLY)
    {
        res *= vertexColorNormalized;
    }
    else if (vertexColorBlendMode == PSX_VERTEX_COLOR_BLEND_MODE_ADDITIVE)
    {
        res.rgb = saturate(vertexColorNormalized.rgb * vertexColorNormalized.a + res.rgb);
        res.a *= vertexColorNormalized.a;
    }
    else if (vertexColorBlendMode == PSX_VERTEX_COLOR_BLEND_MODE_SUBTRACTIVE)
    {
        res.rgb = saturate(vertexColorNormalized.rgb * -vertexColorNormalized.a + res.rgb);
        res.a *= vertexColorNormalized.a;
    }
    else
    {
        // Encountered unsupported blend mode. Do not apply vertex color.
        // This case should never happen.
    }

    return res;
}

bool ComputeVertexColorModeSplitColorAndLightingIsLighting(float4 vertexColorNormalized)
{
    return max(vertexColorNormalized.r, max(vertexColorNormalized.g, vertexColorNormalized.b)) >= 0.5f;
}

float4 ApplyVertexColorPerPixelColor(float4 color, float4 vertexColor, float affineWarpingScaleInverse, int vertexColorBlendMode)
{
    float4 res = color;
    float4 vertexColorNormalized = vertexColor * affineWarpingScaleInverse;

#if defined(_VERTEX_COLOR_MODE_COLOR)
    res = ApplyVertexColorPerPixelColorVertexColorModeColor(color, vertexColorNormalized, vertexColorBlendMode);
#elif defined(_VERTEX_COLOR_MODE_COLOR_BACKGROUND)
    res.rgb = lerp(vertexColorNormalized.rgb, res.rgb, res.a);
    res.a = 1.0f;
#elif defined(_VERTEX_COLOR_MODE_ALPHA_ONLY) || defined(_VERTEX_COLOR_MODE_EMISSION_AND_ALPHA_ONLY)
    res.a *= vertexColorNormalized.a;
#elif defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING)
    if (ComputeVertexColorModeSplitColorAndLightingIsLighting(vertexColorNormalized))
    {
        // Lighting mode, still need to apply alpha to support alpha fade.
        res.a *= vertexColorNormalized.a;
    }
    else
    {
        vertexColorNormalized = float4(saturate(lerp(0.5f, vertexColorNormalized.rgb, vertexColorNormalized.a) * 2.0f), vertexColorNormalized.a);
        res *= vertexColorNormalized;
    }
#endif

    return res;
}

float3 ApplyVertexColorPerPixelEmission(float3 emission, float4 vertexColor, float affineWarpingScaleInverse)
{
    float3 res = emission;

#if defined(_VERTEX_COLOR_MODE_EMISSION) || defined(_VERTEX_COLOR_MODE_EMISSION_AND_ALPHA_ONLY)
    res *= vertexColor * affineWarpingScaleInverse;
#endif

    return res;
}

float3 EvaluateNormalDoubleSidedPerVertex(float3 normalFrontFaceWS, float3 positionWS, float3 worldSpaceCameraPos)
{
    float3 normalWS = normalFrontFaceWS;

#if (defined(_SHADING_EVALUATION_MODE_PER_VERTEX) || defined(_SHADING_EVALUATION_MODE_PER_OBJECT)) && defined(_DOUBLE_SIDED_ON)
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

float3 EvaluateLightingPerVertex(float3 objectPositionWS, float3 positionWS, float3 normalWS, float4 vertexColor, float2 lightmapUV, float affineWarpingScale)
{
    float3 lighting = 0.0f;

#if (defined(_SHADING_EVALUATION_MODE_PER_VERTEX) || defined(_SHADING_EVALUATION_MODE_PER_OBJECT))

#if defined(_SHADING_EVALUATION_MODE_PER_OBJECT)
    float3 evaluationPositionWS = objectPositionWS;
#else // defined(_SHADING_EVALUATION_MODE_PER_VERTEX)
    float3 evaluationPositionWS = positionWS;
#endif

#if defined(_LIGHTING_BAKED_ON) || defined(_VERTEX_COLOR_MODE_LIGHTING) || defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING) || defined(_LIGHTING_DYNAMIC_ON)
    if (_IsPSXQualityEnabled)
    {
        lighting = 0.0f;

#if defined(_LIGHTING_BAKED_ON) && !defined(LIGHTMAP_SHADOW_MASK)
    #if (defined(LIGHTMAP_ON) && defined(_SHADING_EVALUATION_MODE_PER_VERTEX))
        // Only sample lightmaps in per vertex evaluation mode. For per-object we force object SH term sampling.
        lighting = SRGBToLinear(SampleLightmap(lightmapUV, normalWS));
    #else
        // Interestingly, it seems that unity_SHXx terms are always provided in linear space, regardless of color space setting of render pipeline.
        lighting = SampleSH(normalWS);
    #endif

        lighting *= _BakedLightingMultiplier;
#endif

#if defined(_VERTEX_COLOR_MODE_LIGHTING)
        lighting += SRGBToLinear(vertexColor.rgb) * _VertexColorLightingMultiplier;
#elif defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING)
        lighting += ComputeVertexColorModeSplitColorAndLightingIsLighting(vertexColor)
            ? SRGBToLinear(saturate(lerp(0.5f, vertexColor.rgb, vertexColor.a) * 2.0f - 1.0f)) * _VertexColorLightingMultiplier
            : 0.0f;
#endif

#if defined(_LIGHTING_DYNAMIC_ON)
    #if defined(LIGHTMAP_SHADOW_MASK)
        half4 shadowMask = SampleShadowMask(lightmapUV);
    #else
        half4 shadowMask = 1.0h;
    #endif
        lighting += EvaluateDynamicLighting(evaluationPositionWS, normalWS, shadowMask) * _DynamicLightingMultiplier;
#endif

        // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
        lighting *= affineWarpingScale;
    }
    else
    {
        lighting = 1.0f;
    }
#endif // endof defined(_SHADING_EVALUATION_MODE_PER_VERTEX)

#else // defined(_SHADING_EVALUATION_MODE_PER_PIXEL)

#if defined(_VERTEX_COLOR_MODE_LIGHTING) || defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING)

#if defined(_VERTEX_COLOR_MODE_LIGHTING)
    // Still need to evaluate vertex color per vertex, even in per pixel overall evaluation mode.
    lighting = SRGBToLinear(vertexColor.rgb) * _VertexColorLightingMultiplier;
#elif defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING)
    lighting = ComputeVertexColorModeSplitColorAndLightingIsLighting(vertexColor)
        ? SRGBToLinear(saturate(vertexColor.rgb * 2.0f - 1.0f)) * _VertexColorLightingMultiplier
        : 0.0f;
#endif

    // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
    lighting *= affineWarpingScale;
#endif

#endif

    return lighting;
}

float3 EvaluateLightingPerPixel(float3 positionWS, float3 normalWS, float3 vertexLighting, float2 lightmapUV, float affineWarpingScaleInverse)
{
    float3 lighting = 0.0f;

#if (defined(_SHADING_EVALUATION_MODE_PER_VERTEX) || defined(_SHADING_EVALUATION_MODE_PER_OBJECT))
#if defined(_LIGHTING_BAKED_ON) || defined(_VERTEX_COLOR_MODE_LIGHTING) || defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING) || defined(_LIGHTING_DYNAMIC_ON)
    if (_LightingIsEnabled > 0.5f)
    {
        lighting = vertexLighting * affineWarpingScaleInverse;
    }
#endif
#elif defined(_SHADING_EVALUATION_MODE_PER_PIXEL)
#if defined(_LIGHTING_BAKED_ON) || defined(_VERTEX_COLOR_MODE_LIGHTING) || defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING) || defined(_LIGHTING_DYNAMIC_ON)
    if (_LightingIsEnabled > 0.5f)
    {

    #if defined(_LIGHTING_BAKED_ON) && !defined(LIGHTMAP_SHADOW_MASK)
    #ifdef LIGHTMAP_ON
        lighting = SRGBToLinear(SampleLightmap(lightmapUV, normalWS));
    #else
        // Interestingly, it seems that unity_SHXx terms are always provided in linear space, regardless of color space setting of render pipeline.
        lighting = SampleSH(normalWS);
    #endif

        lighting *= _BakedLightingMultiplier;
#endif

#if defined(_VERTEX_COLOR_MODE_LIGHTING) || defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING)
        lighting += vertexLighting * affineWarpingScaleInverse;
#endif

#if defined(_LIGHTING_DYNAMIC_ON)
    #if defined(LIGHTMAP_SHADOW_MASK)
        half4 shadowMask = SampleShadowMask(lightmapUV);
    #else
        half4 shadowMask = 1.0h;
    #endif
        lighting += EvaluateDynamicLighting(positionWS, normalWS, shadowMask) * _DynamicLightingMultiplier;
#endif
    }
#endif
#endif

    return lighting;
}

float4 ApplyLightingToColor(float3 lighting, float4 color)
{
#if defined(_LIGHTING_BAKED_ON) || defined(_VERTEX_COLOR_MODE_LIGHTING) || defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING) || defined(_LIGHTING_DYNAMIC_ON)
    if (_LightingIsEnabled > 0.5f)
    {
        color.rgb *= lighting;
    }
#endif

    return color;
}

#if !defined(_FOG_ON)
#define _FOG_EVALUATION_MODE_DISABLED
#elif (defined(_SHADING_EVALUATION_MODE_PER_VERTEX) || defined(_SHADING_EVALUATION_MODE_PER_OBJECT)) && !defined(_FOG_EVALUATION_MODE_FORCE_PER_PIXEL)
#define _FOG_EVALUATION_MODE_PER_VERTEX
#else
#define _FOG_EVALUATION_MODE_PER_PIXEL
#endif

float4 EvaluateFogPerVertex(float3 objectPositionWS, float3 objectPositionVS, float3 positionWS, float3 positionVS, float affineWarpingScale, float fogWeight, float3 precisionColor, float3 precisionColorInverse)
{
    float4 fog = 0.0f;

#if defined(_FOG_EVALUATION_MODE_PER_VERTEX)
    if (_IsPSXQualityEnabled)
    {
    #if defined(_SHADING_EVALUATION_MODE_PER_OBJECT)
        float3 evaluationPositionWS = objectPositionWS;
        float3 evaluationPositionVS = objectPositionVS;
    #else // defined(_SHADING_EVALUATION_MODE_PER_VERTEX)
        float3 evaluationPositionWS = positionWS;
        float3 evaluationPositionVS = positionVS;
    #endif

        FogFalloffData fogFalloffDataLayer0 = EvaluateFogFalloffData(evaluationPositionWS, _WorldSpaceCameraPos, evaluationPositionVS, _FogFalloffMode, _FogHeightFalloffMirrored == 1, _FogDistanceScaleBias, _FogFalloffCurvePower);
        float4 fogFalloffColorLayer0 = EvaluateFogFalloffColorPerVertex(fogFalloffDataLayer0);
        float fogAlpha = fogFalloffDataLayer0.falloff;
        fogAlpha *= _FogColor.a * lerp(1.0f, fogFalloffColorLayer0.a, _FogColorLUTWeight.x);

        // TODO: We could perform this discretization and transform to linear space on the CPU side and pass in.
        // For now just do it here to make this code easier to refactor as we figure out the architecture.
        float3 fogColor = floor(_FogColor.rgb * lerp(float3(1.0f, 1.0f, 1.0f), fogFalloffColorLayer0.rgb, _FogColorLUTWeight.x) * precisionColor + 0.5f) * precisionColorInverse;
        fogColor = SRGBToLinear(fogColor);
        fogColor *= fogAlpha;

        if (_FogIsAdditionalLayerEnabled)
        {
            FogFalloffData fogFalloffDataLayer1 = EvaluateFogFalloffData(evaluationPositionWS, _WorldSpaceCameraPos, evaluationPositionVS, _FogFalloffModeLayer1, _FogHeightFalloffMirroredLayer1 == 1, _FogDistanceScaleBiasLayer1, _FogFalloffCurvePowerLayer1);
            float fogAlphaLayer1 = fogFalloffDataLayer1.falloff;
            fogAlphaLayer1 *= _FogColorLayer1.a * lerp(fogFalloffColorLayer0.a, 1.0f, _FogColorLUTWeight.y);

            float3 fogColorLayer1 = floor(_FogColorLayer1.rgb * lerp(float3(1.0f, 1.0f, 1.0f), fogFalloffColorLayer0.rgb, _FogColorLUTWeight.y) * precisionColor + 0.5f) * precisionColorInverse;
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

float4 ApplyPrecisionColorToColorSRGB(float4 color, float3 precisionColor, float3 precisionColorInverse)
{
    // Convert to RGB 5:6:5 color space (default) - this will actually be whatever color space the user specified in the Precision Volume Override.
    color.rgb = floor(color.rgb * precisionColor + 0.5f) * precisionColorInverse;
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

float4 EvaluateFogPerPixel(float3 positionWS, float3 positionVS, float2 positionSS, float4 vertexFog, float affineWarpingScaleInverse, float fogWeight, float3 precisionColor, float3 precisionColorInverse)
{
    float3 fogColor = 0.0f;
    float fogAlpha = 0.0f;

#if defined(_FOG_EVALUATION_MODE_PER_VERTEX)
    fogColor = vertexFog.rgb * affineWarpingScaleInverse;
    fogAlpha = vertexFog.a * affineWarpingScaleInverse;
#elif defined(_FOG_EVALUATION_MODE_PER_PIXEL)
    FogFalloffData fogFalloffDataLayer0 = EvaluateFogFalloffData(positionWS, _WorldSpaceCameraPos, positionVS, _FogFalloffMode, _FogHeightFalloffMirrored == 1, _FogDistanceScaleBias, _FogFalloffCurvePower);
    float4 fogFalloffColorLayer0 = EvaluateFogFalloffColorPerPixel(fogFalloffDataLayer0);
    fogAlpha = _FogColor.a * fogFalloffDataLayer0.falloff * lerp(1.0f, fogFalloffColorLayer0.a, _FogColorLUTWeight.x);

    // TODO: We could perform this discretization and transform to linear space on the CPU side and pass in.
    // For now just do it here to make this code easier to refactor as we figure out the architecture.
    fogColor = floor(_FogColor.rgb * lerp(float3(1.0f, 1.0f, 1.0f), fogFalloffColorLayer0.rgb, _FogColorLUTWeight.x) * precisionColor + 0.5f) * precisionColorInverse;
    fogColor = SRGBToLinear(fogColor);

    if (_FogIsAdditionalLayerEnabled)
    {
        FogFalloffData fogFalloffDataLayer1 = EvaluateFogFalloffData(positionWS, _WorldSpaceCameraPos, positionVS, _FogFalloffModeLayer1, _FogHeightFalloffMirroredLayer1 == 1, _FogDistanceScaleBiasLayer1, _FogFalloffCurvePowerLayer1);
        float fogAlphaLayer1 = _FogColorLayer1.a * fogFalloffDataLayer1.falloff * lerp(1.0f, fogFalloffColorLayer0.a, _FogColorLUTWeight.y);

        float3 fogColorLayer1 = floor(_FogColorLayer1.rgb * lerp(float3(1.0f, 1.0f, 1.0f), fogFalloffColorLayer0.rgb, _FogColorLUTWeight.y) * precisionColor.rgb + 0.5f) * precisionColorInverse;
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

    return float4(fogColor, fogAlpha);
}

float4 ApplyFogToColor(float4 fog, float4 color)
{
#if defined(_FOG_ON)
    if (_FogBlendMode == PSX_FOG_BLEND_MODE_OVER)
    {
        // fogColor has premultiplied alpha.
        color.rgb = lerp(color.rgb, fog.rgb, fog.a);
    }
    else if (_FogBlendMode == PSX_FOG_BLEND_MODE_ADDITIVE)
    {
        color.rgb += fog.rgb * fog.a;
    }
    else if (_FogBlendMode == PSX_FOG_BLEND_MODE_SUBTRACTIVE)
    {
        color.rgb = max(float3(0.0f, 0.0f, 0.0f), color.rgb - fog.rgb * fog.a);
    }
    else if (_FogBlendMode == PSX_FOG_BLEND_MODE_MULTIPLY)
    {
        // fogColor has premultiplied alpha.
        color.rgb *= lerp(float3(1.0f, 1.0f, 1.0f), fog.rgb, fog.a);
    }
#endif

    return color;
}

float4 ApplyFogToColorWithHardwareAdditiveBlend(float4 fog, float4 color)
{
#if defined(_FOG_ON)
    if (_FogBlendMode == PSX_FOG_BLEND_MODE_OVER)
    {
        // Simply lerp down the final result based on fog, as fog was handled in base pass.
        color.rgb = lerp(color.rgb, 0.0, fog.a);
    }
    else if (_FogBlendMode == PSX_FOG_BLEND_MODE_ADDITIVE)
    {
        // Do nothing, fog energy add was handled in the base pass.
    }
    else if (_FogBlendMode == PSX_FOG_BLEND_MODE_SUBTRACTIVE)
    {
        // Because we are not accumulating to a signed render target, we need to handle the subtraction at every layer.
        // This is not mathematically equivalent, but is correct for the extremes (when the layerWeight is zero and when the layerWeight is one).
        color.rgb = max(float3(0.0f, 0.0f, 0.0f), color.rgb - fog.rgb * fog.a);
    }
    else if (_FogBlendMode == PSX_FOG_BLEND_MODE_MULTIPLY)
    {
        // For multiply, we need to handle the subtraction at every layer (which is mathematically equivalent).
        color.rgb *= lerp(float3(1.0f, 1.0f, 1.0f), fog.rgb, fog.a);
    }
#endif

    return color;
}

void ComputeLODAndTexelSizeMaybeCallDDX(out float4 texelSizeLod, out float lod, float2 uv, float4 texelSize)
{
#if defined(_TEXTURE_FILTER_MODE_N64_MIPMAPS) || defined(_TEXTURE_FILTER_MODE_POINT_MIPMAPS) || (defined(_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS) && defined(_LOD_REQUIRES_ADJUSTMENT))
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
#elif defined(_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS) && defined(_LOD_REQUIRES_ADJUSTMENT)
    return SAMPLE_TEXTURE2D_LOD(tex, samp, uv, lod);
#else // defined(_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS)
    return SAMPLE_TEXTURE2D(tex, samp, uv);
#endif
}


float2 ApplyUVAnimationVertex(float2 uv, int uvAnimationMode, float2 uvAnimationParametersFrameLimit, float4 uvAnimationParameters)
{
    float2 uvAnimated = uv;
    float timeSeconds = _Time.y;

    if (uvAnimationMode == PSX_UV_ANIMATION_MODE_NONE)
    {
        // Do nothing.
    }
    else
    {
        bool frameLimitEnabled = uvAnimationParametersFrameLimit.x > 0.5f;
        float frameLimit = uvAnimationParametersFrameLimit.y;
        timeSeconds = frameLimitEnabled
            ? (floor(timeSeconds * frameLimit) / frameLimit)
            : timeSeconds;

        if (uvAnimationMode == PSX_UV_ANIMATION_MODE_PAN_LINEAR)
        {
            // frac() to limit range of pan to avoid texture sampler precision issues with large values.
            uvAnimated = frac(uvAnimationParameters.xy * timeSeconds) + uv;
        }
        else if (uvAnimationMode == PSX_UV_ANIMATION_MODE_PAN_SIN)
        {
            float2 frequency = uvAnimationParameters.xy;
            float2 scale = uvAnimationParameters.zw;
            float2 uvPannedBase = float2(
                sin(timeSeconds * frequency.x),
                sin(timeSeconds * frequency.y)
            ) * scale;
            float2 uvPannedBaseSign = float2(
                (uvPannedBase.x >= 0.0f) ? 1.0f : -1.0f,
                (uvPannedBase.y >= 0.0f) ? 1.0f : -1.0f
            );
            // frac() to limit range of pan to avoid texture sampler precision issues with large values.
            uvPannedBase = frac(abs(uvPannedBase)) * uvPannedBaseSign;

            uvAnimated = uvPannedBase + uv;
        }
        else if (uvAnimationMode == PSX_UV_ANIMATION_MODE_FLIPBOOK)
        {
            // Do nothing. This will be applied in the fragment shader.
        }
    }


    return uvAnimated;
}

float2 ApplyUVAnimationPixel(inout float4 texelSizeLod, inout float lod, float2 uv, int uvAnimationMode, float2 uvAnimationParametersFrameLimit, float4 uvAnimationParameters)
{
    float2 uvAnimated = uv;
    float timeSeconds = _Time.y;

    if (uvAnimationMode == PSX_UV_ANIMATION_MODE_NONE)
    {
        // Do nothing.
    }
    else
    {
        bool frameLimitEnabled = uvAnimationParametersFrameLimit.x > 0.5f;
        float frameLimit = uvAnimationParametersFrameLimit.y;
        timeSeconds = frameLimitEnabled
            ? (floor(timeSeconds * frameLimit) / frameLimit)
            : timeSeconds;

        if (uvAnimationMode == PSX_UV_ANIMATION_MODE_PAN_LINEAR)
        {
            // Do nothing. This was applied in the vertex shader.
        }
        else if (uvAnimationMode == PSX_UV_ANIMATION_MODE_PAN_SIN)
        {
            // Do nothing. This was applied in the vertex shader.
        }
        else if (uvAnimationMode == PSX_UV_ANIMATION_MODE_FLIPBOOK)
        {
            float width = uvAnimationParameters.x;
            float height = uvAnimationParameters.y;
            float tileCount = width * height;

            // LOD caculation needs to take into account the scale of the flipbook,
            // otherwise we will overblur the results.
            float lodCorrected = max(0.0, lod - log2(min(width, height)));
            texelSizeLod.xy *= exp2(lodCorrected - lod);
            texelSizeLod.zw *= exp2(lod - lodCorrected);
            lod = lodCorrected;

            float tileIndex = floor(timeSeconds * uvAnimationParameters.z);

            // Loop limit tileIndex to range [0, tileCount - 1].
            tileIndex = floor(tileIndex / tileCount) * -tileCount + tileIndex;

            float tileY = floor(tileIndex / width);
            float tileX = tileIndex - tileY * width;

            // If you'd like to maintain this flip feature, it might be a good idea to store flipY in uvAnimationParameters.w
            // and implement a material parameter so users can toggle the flip on and off. I'm guessing you'd only need flipY
            // (I'd be suprised if someone authored their flipbook right to left).
            //
            // Conditionals like this are very cheap, contrary to what is often reccomended.
            // It boils down to a single conditional assignment instruction, not a branch.
            // I prefer to write these types of conditions in explicit conditional assignment form,
            // rather than using scale, bias arithmetic, which is often harder to read + debug.
            bool flipY = true;
            tileY = flipY ? (height - 1.0 - tileY) : tileY;

            float2 tileScale = 1.0 / float2(width, height);
            float2 tileUvBase = float2(tileX, tileY) * tileScale;

            float2 texelSizeLodHalf = 0.5 * texelSizeLod.xy;

            uvAnimated = frac(uv) * tileScale;
            uvAnimated = clamp(uvAnimated, texelSizeLodHalf, tileScale - texelSizeLodHalf);
            uvAnimated += tileUvBase;
        }
    }


    return uvAnimated;
}

#endif