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
};

Varyings LitPassVertex(Attributes v)
{
    Varyings o;
    ZERO_INITIALIZE(Varyings, o);

    float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
    float3 positionVS = TransformWorldToView(positionWS);
    float4 positionCS = TransformWorldToHClip(positionWS);
    o.vertex = positionCS;

    o.vertex = ApplyPrecisionGeometryToPositionCS(positionWS, positionVS, o.vertex, _PrecisionGeometryWeight, _DrawDistanceOverrideMode, _DrawDistanceOverride);
    o.uvw = ApplyAffineTextureWarpingToUVW(v.uv, positionCS.w, _AffineTextureWarpingWeight);
    o.color = EvaluateColorPerVertex(v.color, o.uvw.z);
    o.positionVS = positionVS; // TODO: Apply affine warping?
    o.positionWS = positionWS; // TODO: Apply affine warping?
    o.lightmapUV = v.lightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw; // TODO: Apply affine warping?
    o.normalWS = TransformObjectToWorldNormal(v.normal);
    o.normalWS = EvaluateNormalDoubleSidedPerVertex(o.normalWS, o.positionWS, _WorldSpaceCameraPos);
    o.lighting = EvaluateLightingPerVertex(positionWS, o.normalWS, v.color, o.lightmapUV, o.uvw.z);
    o.fog = EvaluateFogPerVertex(positionWS, positionVS, o.uvw.z, _FogWeight);

    return o;
}

half4 LitPassFragment(Varyings i, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
{
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
    float4 color = _MainColor * SampleTextureWithFilterMode(TEXTURE2D_ARGS(_MainTex, sampler_MainTex), uvColor, texelSizeLod, lod);

#if defined(_VERTEX_COLOR_MODE_COLOR)
    color *= i.color * interpolatorNormalization;
#elif defined(_VERTEX_COLOR_MODE_COLOR_BACKGROUND)
    color.rgb = lerp(i.color.rgb * interpolatorNormalization, color.rgb, color.a);
    color.a = 1.0f;
#endif

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

    color = ApplyPrecisionColorToColorSRGB(color);
    color.rgb = SRGBToLinear(color.rgb);
    color = ApplyAlphaBlendTransformToColor(color);

    float3 lighting = EvaluateLightingPerPixel(i.positionWS, normalWS, i.lighting, i.lightmapUV, interpolatorNormalization);
    color = ApplyLightingToColor(lighting, color);

#if defined(_EMISSION)
    // Convert to sRGB 5:6:5 color space, then from sRGB to Linear.
    float3 emission = _EmissionColor.rgb * SampleTextureWithFilterMode(TEXTURE2D_ARGS(_EmissionTexture, sampler_EmissionTexture), uvColor, texelSizeLod, lod).rgb;
    emission = ApplyPrecisionColorToColorSRGB(float4(emission, 0.0f)).rgb;
    emission = SRGBToLinear(emission);
    emission = ApplyAlphaBlendTransformToEmission(emission, color.a);

    color.rgb += emission;
#endif

#if defined(_REFLECTION_ON)
    float3 reflection = _ReflectionColor.rgb * SAMPLE_TEXTURE2D(_ReflectionTexture, sampler_ReflectionTexture, uvColor).rgb;
    reflection = ApplyPrecisionColorToColorSRGB(float4(reflection, 0.0f)).rgb;
    reflection = SRGBToLinear(reflection);
    reflection = ApplyAlphaBlendTransformToEmission(reflection, color.a);

    float3 V = normalize(i.positionWS - _WorldSpaceCameraPos);
    float3 R = reflect(V, normalWS);
    float4 reflectionCubemap = SAMPLE_TEXTURECUBE(_ReflectionCubemap, sampler_ReflectionCubemap, R);
    reflectionCubemap.rgb *= reflectionCubemap.a;
    reflectionCubemap.rgb = ApplyPrecisionColorToColorSRGB(float4(reflectionCubemap.rgb, 0.0f)).rgb;
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

    float4 fog = EvaluateFogPerPixel(i.positionWS, i.positionVS, positionSS, i.fog, interpolatorNormalization, _FogWeight);
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
    color.rgb = ComputeFramebufferDiscretization(color.rgb, positionSS);
#endif

    return (half4)color;
}

#endif
