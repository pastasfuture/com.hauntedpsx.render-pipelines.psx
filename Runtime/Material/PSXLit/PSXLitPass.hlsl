#ifndef PSX_LIT_PASS
#define PSX_LIT_PASS

#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderFunctions.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/BakedLighting.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/DynamicLighting.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/MaterialFunctions.hlsl"


// Simply rely on dead code removal to strip unused attributes and varyings from shaders.
// Manually stripping with ifdefing made the shader harder to read.
struct Attributes
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float4 color : COLOR;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float3 uvw : TEXCOORD0;
    float3 positionVS : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    float3 normalWS : TEXCOORD3;
    float4 color : TEXCOORD4;
    float4 fog : TEXCOORD5;
    float3 lighting : TEXCOORD6;
    float2 lightmapUV : TEXCOORD7;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    Varyings o;
    ZERO_INITIALIZE(Varyings, o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);

    float3 objectPositionWS = TransformObjectToWorld(float3(0.0f, 0.0f, 0.0f));
    float3 objectPositionVS = TransformWorldToView(objectPositionWS);
    float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
    float3 positionVS = TransformWorldToView(positionWS);
    ApplyGeometryPushbackToPosition(positionWS, positionVS, _GeometryPushbackParameters);

    float4 positionCS = TransformWorldToHClip(positionWS);
    o.vertex = positionCS;

    float2 uv = ApplyUVAnimationVertex(v.uv, _UVAnimationMode, _UVAnimationParametersFrameLimit, _UVAnimationParameters);

    float3 precisionColor;
    float3 precisionColorInverse;
    float precisionColorIndexNormalized = _PrecisionColor.w;
    float precisionChromaBit = _PrecisionColorInverse.w;
    ApplyPrecisionColorOverride(precisionColor, precisionColorInverse, _PrecisionColor.rgb, _PrecisionColorInverse.rgb, precisionColorIndexNormalized, precisionChromaBit, _PrecisionColorOverrideMode, _PrecisionColorOverrideParameters);

    o.vertex = ApplyPrecisionGeometryToPositionCS(positionWS, positionVS, o.vertex, _PrecisionGeometryOverrideMode, _PrecisionGeometryOverrideParameters, _DrawDistanceOverrideMode, _DrawDistanceOverride);
    o.uvw = ApplyAffineTextureWarpingToUVW(uv, positionCS.w, _AffineTextureWarpingWeight);
    o.color = EvaluateVertexColorPerVertex(v.color, o.uvw.z);
    o.positionVS = positionVS; // TODO: Apply affine warping?
    o.positionWS = positionWS; // TODO: Apply affine warping?
    o.lightmapUV = v.lightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw; // TODO: Apply affine warping?
    o.normalWS = TransformObjectToWorldNormal(v.normal);
    o.normalWS = EvaluateNormalDoubleSidedPerVertex(o.normalWS, o.positionWS, _WorldSpaceCameraPos);
    o.lighting = EvaluateLightingPerVertex(objectPositionWS, positionWS, o.normalWS, v.color, o.lightmapUV, o.uvw.z);
    o.fog = EvaluateFogPerVertex(objectPositionWS, objectPositionVS, positionWS, positionVS, o.uvw.z, _FogWeight, precisionColor, precisionColorInverse);

    return o;
}

half4 LitPassFragment(Varyings i, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);

    float2 positionSS = i.vertex.xy;

    // Remember, we multipled all interpolated vertex values by positionCS.w in order to counter contemporary hardware's perspective correct interpolation.
    // We need to post multiply all interpolated vertex values by the interpolated positionCS.w (stored in uvw.z) component to "normalize" the interpolated values.
    float interpolatorNormalization = 1.0f / i.uvw.z;

    float3 normalWS = normalize(i.normalWS);
    normalWS = EvaluateNormalDoubleSidedPerPixel(normalWS, cullFace);

    float2 uv = i.uvw.xy * interpolatorNormalization;
    float2 uvColor = TRANSFORM_TEX(uv, _MainTex);

    float4 texelSizeLod;
    float lod;
    ComputeLODAndTexelSizeMaybeCallDDX(texelSizeLod, lod, uvColor, _MainTex_TexelSize);
    uvColor = ApplyUVAnimationPixel(texelSizeLod, lod, uvColor, _UVAnimationMode, _UVAnimationParametersFrameLimit, _UVAnimationParameters);

    float4 color = _MainColor * SampleTextureWithFilterMode(TEXTURE2D_ARGS(_MainTex, sampler_MainTex), uvColor, texelSizeLod, lod);

    color = ApplyVertexColorPerPixelColor(color, i.color, interpolatorNormalization, _VertexColorBlendMode);

#if _ALPHATEST_ON
    // Perform alpha cutoff transparency (i.e: discard pixels in the holes of a chain link fence texture, or in the negative space of a leaf texture).
    // Any alpha value < alphaClippingDither will trigger the pixel to be discarded, any alpha value greater than or equal to alphaClippingDither will trigger the pixel to be preserved.
    float alphaClippingDither;
    float alphaForClipping;
    ComputeAndFetchAlphaClippingParameters(alphaClippingDither, alphaForClipping, color.a, positionSS, _AlphaClippingDitherIsEnabled, _AlphaClippingScaleBiasMinMax);
    clip((alphaForClipping > alphaClippingDither) ? 1.0f : -1.0f);
#endif

#if defined(SCENESELECTIONPASS)
    // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
    return float4(_ObjectId, _PassValue, 1.0, 1.0);
#endif

    if (!_IsPSXQualityEnabled)
    {
        // TODO: Handle premultiply alpha case here?
        return color;
    }

    // Rather than paying the cost of interpolating our 6 floats for precision color per vertex, we simply recompute them per pixel here.
    float3 precisionColor;
    float3 precisionColorInverse;
    float precisionColorIndexNormalized = _PrecisionColor.w;
    float precisionChromaBit = _PrecisionColorInverse.w;
    ApplyPrecisionColorOverride(precisionColor, precisionColorInverse, _PrecisionColor.rgb, _PrecisionColorInverse.rgb, precisionColorIndexNormalized, precisionChromaBit, _PrecisionColorOverrideMode, _PrecisionColorOverrideParameters);

    color = ApplyPrecisionColorToColorSRGB(color, precisionColor, precisionColorInverse);
    color.rgb = SRGBToLinear(color.rgb);
    color = ApplyAlphaBlendTransformToColor(color);

    float3 lighting = EvaluateLightingPerPixel(i.positionWS, normalWS, i.lighting, i.lightmapUV, interpolatorNormalization);
    color = ApplyLightingToColor(lighting, color);

#if defined(_EMISSION)
    // Convert to sRGB 5:6:5 color space, then from sRGB to Linear.
    float3 emission = _EmissionColor.rgb * SampleTextureWithFilterMode(TEXTURE2D_ARGS(_EmissionTexture, sampler_EmissionTexture), uvColor, texelSizeLod, lod).rgb;
    emission = ApplyPrecisionColorToColorSRGB(float4(emission, 0.0f), precisionColor, precisionColorInverse).rgb;
    emission = SRGBToLinear(emission);
    emission = ApplyVertexColorPerPixelEmission(emission, i.color, interpolatorNormalization);
    emission = ApplyAlphaBlendTransformToEmission(emission, color.a);

    color.rgb += emission;
#endif

#if defined(_REFLECTION_ON)
    float3 reflection = _ReflectionColor.rgb * SAMPLE_TEXTURE2D(_ReflectionTexture, sampler_ReflectionTexture, uvColor).rgb;
    reflection = ApplyPrecisionColorToColorSRGB(float4(reflection, 0.0f), precisionColor, precisionColorInverse).rgb;
    reflection = SRGBToLinear(reflection);
    reflection = ApplyAlphaBlendTransformToEmission(reflection, color.a);

    float3 V = normalize(i.positionWS - _WorldSpaceCameraPos);
    float3 R = reflect(V, normalWS);
    float3 reflectionDirection = EvaluateReflectionDirectionMode(_ReflectionDirectionMode, R, normalWS, V);
    float4 reflectionCubemap = SAMPLE_TEXTURECUBE(_ReflectionCubemap, sampler_ReflectionCubemap, reflectionDirection);
    reflectionCubemap.rgb *= reflectionCubemap.a;
    reflectionCubemap.rgb = ApplyPrecisionColorToColorSRGB(float4(reflectionCubemap.rgb, 0.0f), precisionColor, precisionColorInverse).rgb;
    // TODO: Convert reflectionCubemap from SRGB to linear space, but only if an LDR texture was supplied...
    // reflectionCubemap = SRGBToLinear(reflectionCubemap);
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

    float4 fog = EvaluateFogPerPixel(i.positionWS, i.positionVS, positionSS, i.fog, interpolatorNormalization, _FogWeight, precisionColor, precisionColorInverse);
    fog.a = ApplyAlphaBlendTransformToFog(fog.a, color.a);
    color = ApplyFogToColor(fog, color);

#if defined(_OUTPUT_LDR)

#if !defined(_BLENDMODE_TONEMAPPER_OFF)
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
    color.rgb = ComputeFramebufferDiscretization(color.rgb, positionSS, precisionColor, precisionColorInverse);
#endif

    return (half4)color;
}

#endif
