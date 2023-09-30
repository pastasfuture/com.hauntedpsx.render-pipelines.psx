#ifndef PSX_LIT_META_PASS
#define PSX_LIT_META_PASS

#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderFunctions.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/MaterialFunctions.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/BakedLighting.hlsl"

CBUFFER_START(UnityMetaPass)
// x = use uv1 as raster position
// y = use uv2 as raster position
bool4 unity_MetaVertexControl;

// x = return albedo
// y = return normal
bool4 unity_MetaFragmentControl;
CBUFFER_END

float unity_OneOverOutputBoost;
float unity_MaxOutputValue;
float unity_UseLinearSpace;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float2 uv2 : TEXCOORD2;
#if defined(_VERTEX_COLOR_MODE_COLOR) || defined(_VERTEX_COLOR_MODE_COLOR_BACKGROUND) || defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING)
    float4 color : COLOR;
#endif
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
#if defined(_VERTEX_COLOR_MODE_COLOR) || defined(_VERTEX_COLOR_MODE_COLOR_BACKGROUND) || defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING)
    float4 color : TEXCOORD1;
#endif
};

// All of these functions are essentially the Universal Render Pipeline Meta Pass.
float4 MetaVertexPosition(float4 positionOS, float2 uv1, float2 uv2, float4 uv1ST, float4 uv2ST)
{
    if (unity_MetaVertexControl.x)
    {
        positionOS.xy = uv1 * uv1ST.xy + uv1ST.zw;
        // OpenGL right now needs to actually use incoming vertex position,
        // so use it in a very dummy way
        positionOS.z = positionOS.z > 0 ? REAL_MIN : 0.0f;
    }
    if (unity_MetaVertexControl.y)
    {
        positionOS.xy = uv2 * uv2ST.xy + uv2ST.zw;
        // OpenGL right now needs to actually use incoming vertex position,
        // so use it in a very dummy way
        positionOS.z = positionOS.z > 0 ? REAL_MIN : 0.0f;
    }
    return TransformWorldToHClip(positionOS.xyz);
}

half4 MetaFragment(half3 albedoIn, half3 emissionIn)
{
    half4 res = 0;
    if (unity_MetaFragmentControl.x)
    {
        res = half4(albedoIn, 1.0);

        // d3d9 shader compiler doesn't like NaNs and infinity.
        unity_OneOverOutputBoost = saturate(unity_OneOverOutputBoost);

        // Apply Albedo Boost from LightmapSettings.
        res.rgb = clamp(PositivePow(res.rgb, unity_OneOverOutputBoost), 0, unity_MaxOutputValue);

        if (!unity_UseLinearSpace)
            res.rgb = LinearToSRGB(res.rgb);

    }
    if (unity_MetaFragmentControl.y)
    {
        half3 emission;
        if (unity_UseLinearSpace)
            emission = emissionIn;
        else
            emission = LinearToSRGB(emissionIn);

        res = half4(emission, 1.0);
    }
    return res;
}

Varyings LitMetaPassVertex(Attributes v)
{
    Varyings o;

    o.positionCS = MetaVertexPosition(v.positionOS, v.uv1, v.uv2,
        unity_LightmapST, unity_DynamicLightmapST);
    o.uv = v.uv0;

#if defined(_VERTEX_COLOR_MODE_COLOR) || defined(_VERTEX_COLOR_MODE_COLOR_BACKGROUND) || defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING)
    o.color = v.color;
#endif

    return o;
}

half4 LitMetaPassFragment(Varyings i) : SV_Target
{
    float2 colorUV = TRANSFORM_TEX(i.uv, _MainTex);
    half4 color = _MainColor * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, colorUV);

#if defined(_VERTEX_COLOR_MODE_COLOR)
    float4 vertexColorNormalized = i.color;
    color = ApplyVertexColorPerPixelColorVertexColorModeColor(color, vertexColorNormalized, _VertexColorBlendMode);
#elif defined(_VERTEX_COLOR_MODE_COLOR_BACKGROUND)
    color.rgb = lerp(i.color.rgb, color.rgb, color.a);
    color.a = 1.0f;
#elif defined(_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING)
    if (ComputeVertexColorModeSplitColorAndLightingIsLighting(i.color))
    {
        // Lighting mode, still need to apply alpha to support alpha fade.
        color.a *= i.color.a;
    }
    else
    {
        float4 vertexColorNormalized = i.color;
        vertexColorNormalized.rgb = saturate(lerp(0.5f, vertexColorNormalized.rgb, vertexColorNormalized.a) * 2.0f);
        color *= vertexColorNormalized;
    }

#endif

#if _ALPHATEST_ON
    // Perform alpha cutoff transparency (i.e: discard pixels in the holes of a chain link fence texture, or in the negative space of a leaf texture).
    // Any alpha value < alphaClippingDither will trigger the pixel to be discarded, any alpha value greater than or equal to alphaClippingDither will trigger the pixel to be preserved.
    float alphaClippingDither;
    float alphaForClipping;
    float2 positionSS = i.positionCS.xy;
    ComputeAndFetchAlphaClippingParameters(alphaClippingDither, alphaForClipping, color.a, positionSS, _AlphaClippingDitherIsEnabled, _AlphaClippingScaleBiasMinMax);
    clip((alphaForClipping > alphaClippingDither) ? 1.0f : -1.0f);
#endif

#if defined(_ALPHAPREMULTIPLY_ON)
    color.rgb *= color.a;
#elif defined(_ALPHAMODULATE_ON)
    color.rgb = lerp(float3(1.0f, 1.0f, 1.0f), color.rgb, color.a);
#endif

    color.rgb = floor(color.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
    color.rgb = SRGBToLinear(color.rgb);

#ifdef _EMISSION
    half3 emission = _EmissionColor * SAMPLE_TEXTURE2D(_EmissionTexture, sampler_EmissionTexture, colorUV).rgb;
    emission = SRGBToLinear(emission);
    emission *= _EmissionBakedMultiplier;

#if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAMODULATE_ON)
    emission *= color.a;
#endif

#else
    half3 emission = 0.0f;
#endif

    return MetaFragment(color.rgb, emission);
}

#endif
