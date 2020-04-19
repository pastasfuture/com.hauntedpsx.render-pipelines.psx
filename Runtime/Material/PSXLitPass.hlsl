#ifndef PSX_LIT_PASS
#define PSX_LIT_PASS

#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderFunctions.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/BakedLighting.hlsl"

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
    float3 uvw : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float3 positionVS : TEXCOORD1;
    float3 lighting : TEXCOORD2;
    float4 fog : TEXCOORD3;
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
        o.uvw.xy = uv * positionCS.w;
        o.uvw.z = positionCS.w;
    }
    else
    {
        o.uvw = float3(uv.x, uv.y, 1.0f);
    }

    o.positionVS = positionVS;

    if (_IsPSXQualityEnabled)
    {
        float3 normalWS = TransformObjectToWorldNormal(v.normal);

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

        o.lighting += SRGBToLinear(v.color.rgb) * _VertexColorLightingMultiplier;

        // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
        o.lighting *= positionCS.w;

    }
    else
    {
        o.lighting = 1.0f;
    }

    if (_IsPSXQualityEnabled)
    {
        float fogAlpha = saturate(abs(positionVS.z) * _FogDistanceScaleBias.x + _FogDistanceScaleBias.y);
        fogAlpha *= _FogColor.a;

        // TODO: We could perform this discretization and transform to linear space on the CPU side and pass in.
        // For now just do it here to make this code easier to refactor as we figure out the architecture.
        float3 fogColor = floor(_FogColor.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
        fogColor = SRGBToLinear(fogColor);

        o.fog = float4(fogColor, fogAlpha);

        // Premultiply UVs by W component to reverse the perspective divide that the hardware will automatically perform when interpolating varying attributes.
        o.fog *= positionCS.w;
    }

    return o;
}

half4 LitPassFragment(Varyings i) : SV_Target
{
    float2 positionSS = i.vertex.xy;

    // Remember, we multipled all interpolated vertex values by positionCS.w in order to counter contemporary hardware's perspective correct interpolation.
    // We need to post multiply all interpolated vertex values by the interpolated positionCS.w (stored in uvw.z) component to "normalize" the interpolated values.
    float interpolatorNormalization = 1.0f / i.uvw.z;

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
    color.rgb = SRGBToLinear(color.rgb);

#if defined(_ALPHAPREMULTIPLY_ON)
    color.rgb *= color.a;
#elif defined(_ALPHAMODULATE_ON)
    color.rgb = lerp(float3(1.0f, 1.0f, 1.0f), color.rgb, color.a);
#endif

    if (_LightingIsEnabled > 0.5f)
    {
        color.xyz *= i.lighting * interpolatorNormalization;
    }

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

    float3 fogColor = i.fog.rgb * interpolatorNormalization;
    float fogAlpha = i.fog.a * interpolatorNormalization;
    color.rgb = lerp(color.rgb, fogColor, fogAlpha);
    
    // Apply tonemapping and gamma correction.
    // This is a departure from classic PS1 games, but it allows for greater flexibility, giving artists more controls for creating the final look and feel of their game.
    // Otherwise, they would need to spend a lot more time in the texturing phase, getting the textures alone to produce the mood they are aiming for.
    if (_TonemapperIsEnabled)
    {
        color.rgb = TonemapperGeneric(color.rgb);
    }
    
    color.rgb = LinearToSRGB(color.rgb);

    // Convert the final color value to 5:6:5 color space (default) - this will actually be whatever color space the user specified in the Precision Volume Override.
    // This emulates a the limited bit-depth frame buffer.
    color.rgb = ComputeFramebufferDiscretization(color.rgb, positionSS);

    return (half4)color;
}

#endif
