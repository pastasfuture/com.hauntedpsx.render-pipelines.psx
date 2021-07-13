#ifndef PSX_TERRAIN_PASS
#define PSX_TERRAIN_PASS

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderFunctions.hlsl"

#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/BakedLighting.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/DynamicLighting.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/MaterialFunctions.hlsl"


// #if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
//     #define ENABLE_TERRAIN_PERPIXEL_NORMAL
// #endif

#ifdef UNITY_INSTANCING_ENABLED
    TEXTURE2D(_TerrainHeightmapTexture);
    TEXTURE2D(_TerrainNormalmapTexture);
    SAMPLER(sampler_TerrainNormalmapTexture);
#endif

UNITY_INSTANCING_BUFFER_START(Terrain)
    UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
UNITY_INSTANCING_BUFFER_END(Terrain)

#ifdef _ALPHATEST_ON
TEXTURE2D(_TerrainHolesTexture);
SAMPLER(sampler_TerrainHolesTexture);

void ClipHoles(float2 uv)
{
	float hole = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, uv).r;
	clip(hole == 0.0f ? -1 : 1);
}
#endif

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Used in Standard Terrain shader SceneSelectionPass
struct AttributesDepthOnly
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#if defined(METAPASS)
struct AttributesMeta
{
    float4 positionOS : POSITION;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float2 uv2 : TEXCOORD2;
};
#endif

struct Varyings
{
    float4 vertex : SV_POSITION;
    float3 uvw : TEXCOORD0;
    float3 positionVS : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    float3 normalWS : TEXCOORD3;
    float4 fog : TEXCOORD4;
    float3 lighting : TEXCOORD5;
};

// Used in Standard Terrain shader SceneSelectionPass
struct VaryingsDepthOnly
{
    float4 vertex : SV_POSITION;
    float3 uvw : TEXCOORD0;
};

#if defined(METAPASS)
struct VaryingsMeta
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};
#endif

// struct InputData copied from URP Input.hlsl
struct InputData
{
    float3 positionWS;
    float3 positionVS;
    float3 normalWS;
    float4 fog;
    float3 lighting;
};

#ifndef TERRAIN_SPLAT_BASEPASS

#if defined(_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS) || defined(_TEXTURE_FILTER_MODE_POINT) || defined(_TEXTURE_FILTER_MODE_POINT_MIPMAPS) || defined(_TEXTURE_FILTER_MODE_N64) || defined(_TEXTURE_FILTER_MODE_N64_MIPMAPS)
#define _TEXTURE_FILTER_MODE_ENABLED
#endif

void SplatmapMix(
    float4 uvMainAndLM,
    float4 uvSplat01,
    float4 uvSplat23,
    float3 precisionColor,
    float3 precisionColorInverse,
#if defined(_TEXTURE_FILTER_MODE_ENABLED)
    float4 splat0TexelSizeLod,
    float splat0LOD,
    float4 splat1TexelSizeLod,
    float splat1LOD,
    float4 splat2TexelSizeLod,
    float splat2LOD,
    float4 splat3TexelSizeLod,
    float splat3LOD,
#endif
    inout half4 splatControl,
    out half weight,
    out half4 mixedDiffuse)
{
    half4 diffAlbedo[4];

#if defined(_TEXTURE_FILTER_MODE_ENABLED)
    diffAlbedo[0] = SampleTextureWithFilterMode(TEXTURE2D_ARGS(_Splat0, sampler_Splat0), uvSplat01.xy, splat0TexelSizeLod, splat0LOD);
    diffAlbedo[1] = SampleTextureWithFilterMode(TEXTURE2D_ARGS(_Splat1, sampler_Splat0), uvSplat01.zw, splat1TexelSizeLod, splat1LOD);
    diffAlbedo[2] = SampleTextureWithFilterMode(TEXTURE2D_ARGS(_Splat2, sampler_Splat0), uvSplat23.xy, splat2TexelSizeLod, splat2LOD);
    diffAlbedo[3] = SampleTextureWithFilterMode(TEXTURE2D_ARGS(_Splat3, sampler_Splat0), uvSplat23.zw, splat3TexelSizeLod, splat3LOD);
#else
    diffAlbedo[0] = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uvSplat01.xy);
    diffAlbedo[1] = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, uvSplat01.zw);
    diffAlbedo[2] = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, uvSplat23.xy);
    diffAlbedo[3] = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, uvSplat23.zw);
#endif

// #ifndef _TERRAIN_BLEND_HEIGHT
    // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
    half4 opacityAsDensity = saturate((half4(diffAlbedo[0].a, diffAlbedo[1].a, diffAlbedo[2].a, diffAlbedo[3].a) - (half4(1.0, 1.0, 1.0, 1.0) - splatControl)) * 20.0);
    opacityAsDensity += 0.001h * splatControl;      // if all weights are zero, default to what the blend mask says
    half4 useOpacityAsDensityParam = { _DiffuseRemapScale0.w, _DiffuseRemapScale1.w, _DiffuseRemapScale2.w, _DiffuseRemapScale3.w }; // 1 is off
    splatControl = lerp(opacityAsDensity, splatControl, useOpacityAsDensityParam);
// #endif

    // Now that splatControl has changed, we can compute the final weight and normalize
    weight = dot(splatControl, 1.0h);

#ifdef TERRAIN_SPLAT_ADDPASS
    clip(weight <= 0.005h ? -1.0h : 1.0h);
#endif

#ifndef _TERRAIN_BASEMAP_GEN
    // Normalize weights before lighting and restore weights in final modifier functions so that the overal
    // lighting result can be correctly weighted.
    splatControl /= (weight + HALF_MIN);
#endif

    diffAlbedo[0].rgb *= _DiffuseRemapScale0.rgb;
    diffAlbedo[1].rgb *= _DiffuseRemapScale1.rgb;
    diffAlbedo[2].rgb *= _DiffuseRemapScale2.rgb;
    diffAlbedo[3].rgb *= _DiffuseRemapScale3.rgb;

    if (_IsPSXQualityEnabled)
    {
        diffAlbedo[0] = ApplyPrecisionColorToColorSRGB(diffAlbedo[0], precisionColor, precisionColorInverse);
        diffAlbedo[0].rgb = SRGBToLinear(diffAlbedo[0].rgb);

        diffAlbedo[1] = ApplyPrecisionColorToColorSRGB(diffAlbedo[1], precisionColor, precisionColorInverse);
        diffAlbedo[1].rgb = SRGBToLinear(diffAlbedo[1].rgb);

        diffAlbedo[2] = ApplyPrecisionColorToColorSRGB(diffAlbedo[2], precisionColor, precisionColorInverse);
        diffAlbedo[2].rgb = SRGBToLinear(diffAlbedo[2].rgb);

        diffAlbedo[3] = ApplyPrecisionColorToColorSRGB(diffAlbedo[3], precisionColor, precisionColorInverse);
        diffAlbedo[3].rgb = SRGBToLinear(diffAlbedo[3].rgb);
    }

    mixedDiffuse = 0.0h;
    mixedDiffuse += diffAlbedo[0] * half4(splatControl.rrr, 1.0h);
    mixedDiffuse += diffAlbedo[1] * half4(splatControl.ggg, 1.0h);
    mixedDiffuse += diffAlbedo[2] * half4(splatControl.bbb, 1.0h);
    mixedDiffuse += diffAlbedo[3] * half4(splatControl.aaa, 1.0h);
}

#endif
float4 ApplyFogToColorSplatmap(float4 color, float4 fog)
{
    color.rgb *= color.a;

    #ifdef TERRAIN_SPLAT_ADDPASS
        // Add pass needs to handle transparency from additive blending.
        color = ApplyFogToColorWithHardwareAdditiveBlend(fog, color);
    #else
        // Apply fog normally:
        color = ApplyFogToColor(fog, color);
    #endif

    return color;
}

void TerrainInstancing(inout float4 positionOS, inout float3 normal, inout float2 uv)
{
#ifdef UNITY_INSTANCING_ENABLED
    float2 patchVertex = positionOS.xy;
    float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

    float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
    float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

    positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
    positionOS.y = height * _TerrainHeightmapScale.y;

    // #ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    //     normal = float3(0, 1, 0);
    // #else
        normal = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
    // #endif
    uv = sampleCoords * _TerrainHeightmapRecipSize.zw;
#endif
}

void TerrainInstancing(inout float4 positionOS, inout float3 normal)
{
    float2 uv = { 0, 0 };
    TerrainInstancing(positionOS, normal, uv);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Structs stolen from URP Core.hlsl
// Structs
struct VertexPositionInputs
{
    float3 objectPositionWS;
    float3 objectPositionVS;
    float3 positionWS; // World space position
    float3 positionVS; // View space position
    float4 positionCS; // Homogeneous clip space position
    float4 positionNDC;// Homogeneous normalized device coordinates
};

VertexPositionInputs GetVertexPositionInputs(float3 positionOS)
{
    VertexPositionInputs input;

    input.objectPositionWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
    input.objectPositionVS = TransformWorldToView(input.objectPositionWS);

    input.positionWS = TransformObjectToWorld(positionOS);
    input.positionVS = TransformWorldToView(input.positionWS);

    ApplyGeometryPushbackToPosition(input.positionWS, input.positionVS, _GeometryPushbackParameters);

    input.positionCS = TransformWorldToHClip(input.positionWS);

    float4 ndc = input.positionCS * 0.5f;
    input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    input.positionNDC.zw = input.positionCS.zw;

    return input;
}

// Used in Standard Terrain shader
Varyings TerrainLitPassVert(Attributes v)
{
    Varyings o = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TerrainInstancing(v.positionOS, v.normalOS, v.uv);
    VertexPositionInputs vertexPositionInputs = GetVertexPositionInputs(v.positionOS.xyz);

    o.normalWS = TransformObjectToWorldNormal(v.normalOS);

    // PSX Standard Material Transforms:
    o.vertex = ApplyPrecisionGeometryToPositionCS(vertexPositionInputs.positionWS, vertexPositionInputs.positionVS, vertexPositionInputs.positionCS, _PrecisionGeometryOverrideMode, _PrecisionGeometryOverrideParameters);
    o.uvw = ApplyAffineTextureWarpingToUVW(v.uv, o.vertex.w, _AffineTextureWarpingWeight);

    float3 precisionColor;
    float3 precisionColorInverse;
    float precisionColorIndexNormalized = _PrecisionColor.w;
    float precisionChromaBit = _PrecisionColorInverse.w;
    ApplyPrecisionColorOverride(precisionColor, precisionColorInverse, _PrecisionColor.rgb, _PrecisionColorInverse.rgb, precisionColorIndexNormalized, precisionChromaBit, _PrecisionColorOverrideMode, _PrecisionColorOverrideParameters);

    o.positionVS = vertexPositionInputs.positionVS;
    o.positionWS = vertexPositionInputs.positionWS;

    float4 vertexColor = 0.0f; // Terrain does not support vertex color based lighting.
    float2 lightmapUV = v.uv * unity_LightmapST.xy + unity_LightmapST.zw;
    o.lighting = EvaluateLightingPerVertex(vertexPositionInputs.objectPositionWS, vertexPositionInputs.positionWS, o.normalWS, vertexColor, lightmapUV, o.uvw.z);
    o.fog = EvaluateFogPerVertex(vertexPositionInputs.objectPositionWS, vertexPositionInputs.objectPositionVS, vertexPositionInputs.positionWS, vertexPositionInputs.positionVS, o.uvw.z, _FogWeight, precisionColor, precisionColorInverse);

    return o;
}

// Used in Standard Terrain shader
half4 TerrainLitPassFrag(Varyings i) : SV_TARGET
{
    float2 positionSS = i.vertex.xy;

    // Remember, we multipled all interpolated vertex values by positionCS.w in order to counter contemporary hardware's perspective correct interpolation.
    // We need to post multiply all interpolated vertex values by the interpolated positionCS.w (stored in uvw.z) component to "normalize" the interpolated values.
    float interpolatorNormalization = 1.0f / i.uvw.z;

    float3 normalWS = normalize(i.normalWS);

    float2 uv = i.uvw.xy * interpolatorNormalization;
    float4 uvMainAndLM = float4(uv, uv * unity_LightmapST.xy + unity_LightmapST.zw);

    // Need to compute UVs and LOD before ClipHoles() because we rely on DDX and DDY calls
    // which have undefined behavior after clip() calls.
    float4 uvSplat01 = float4(TRANSFORM_TEX(uv, _Splat0), TRANSFORM_TEX(uv, _Splat1));
    float4 uvSplat23 = float4(TRANSFORM_TEX(uv, _Splat2), TRANSFORM_TEX(uv, _Splat3));

#if defined(_TEXTURE_FILTER_MODE_ENABLED)
#ifdef TERRAIN_SPLAT_BASEPASS
    float4 mainTexTexelSizeLod;
    float mainTexLOD;
    ComputeLODAndTexelSizeMaybeCallDDX(mainTexTexelSizeLod, mainTexLOD, uvMainAndLM.xy, _MainTex_TexelSize);
#endif

    float4 splat0TexelSizeLod;
    float splat0LOD;
    ComputeLODAndTexelSizeMaybeCallDDX(splat0TexelSizeLod, splat0LOD, uvSplat01.xy, _Splat0_TexelSize);

    float4 splat1TexelSizeLod;
    float splat1LOD;
    ComputeLODAndTexelSizeMaybeCallDDX(splat1TexelSizeLod, splat1LOD, uvSplat01.zw, _Splat1_TexelSize);

    float4 splat2TexelSizeLod;
    float splat2LOD;
    ComputeLODAndTexelSizeMaybeCallDDX(splat2TexelSizeLod, splat2LOD, uvSplat23.xy, _Splat2_TexelSize);

    float4 splat3TexelSizeLod;
    float splat3LOD;
    ComputeLODAndTexelSizeMaybeCallDDX(splat3TexelSizeLod, splat3LOD, uvSplat23.zw, _Splat3_TexelSize);
#endif

#ifdef _ALPHATEST_ON
    ClipHoles(uvMainAndLM.xy);
#endif

    // Rather than paying the cost of interpolating our 6 floats for precision color per vertex, we simply recompute them per pixel here.
    float3 precisionColor;
    float3 precisionColorInverse;
    float precisionColorIndexNormalized = _PrecisionColor.w;
    float precisionChromaBit = _PrecisionColorInverse.w;
    ApplyPrecisionColorOverride(precisionColor, precisionColorInverse, _PrecisionColor.rgb, _PrecisionColorInverse.rgb, precisionColorIndexNormalized, precisionChromaBit, _PrecisionColorOverrideMode, _PrecisionColorOverrideParameters);

#ifdef TERRAIN_SPLAT_BASEPASS
#if defined(_TEXTURE_FILTER_MODE_ENABLED)
    float4 color = SampleTextureWithFilterMode(TEXTURE2D_ARGS(_MainTex, sampler_MainTex), uvMainAndLM.xy, mainTexLOD, mainTexTexelSizeLod);
#else
    float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvMainAndLM.xy);
#endif
    color *= _BaseColor;
#else

    float2 splatUV = (uvMainAndLM.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

    splatControl = float4(
        ApplyPrecisionAlphaToAlpha(splatControl.x),
        ApplyPrecisionAlphaToAlpha(splatControl.y),
        ApplyPrecisionAlphaToAlpha(splatControl.z),
        ApplyPrecisionAlphaToAlpha(splatControl.w)
    );

    half weight;
    half4 mixedDiffuse;
    SplatmapMix(
        uvMainAndLM,
        uvSplat01, uvSplat23,
        precisionColor,
        precisionColorInverse,
    #if defined(_TEXTURE_FILTER_MODE_ENABLED)
        splat0TexelSizeLod,
        splat0LOD,
        splat1TexelSizeLod,
        splat1LOD,
        splat2TexelSizeLod,
        splat2LOD,
        splat3TexelSizeLod,
        splat3LOD,
    #endif
        splatControl,
        weight,
        mixedDiffuse
    );

    float4 color = float4(mixedDiffuse.rgb, weight);
#endif // !TERRAIN_SPLAT_BASEPASS

    if (!_IsPSXQualityEnabled)
    {
        return color;
    }

    float3 lighting = EvaluateLightingPerPixel(i.positionWS, normalWS, i.lighting, uvMainAndLM.zw, interpolatorNormalization);
    color = ApplyLightingToColor(lighting, color);

    float4 fog = EvaluateFogPerPixel(i.positionWS, i.positionVS, positionSS, i.fog, interpolatorNormalization, _FogWeight, precisionColor, precisionColorInverse);
    color = ApplyFogToColorSplatmap(color, fog);

#ifndef _OUTPUT_HDR
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
    color.rgb = ComputeFramebufferDiscretization(color.rgb, positionSS, precisionColor, precisionColorInverse);
#endif

    return half4(color.rgb, 1.0h);
}

// Used in Standard Terrain shader SceneSelectionPass
VaryingsDepthOnly TerrainLitPassVertDepthOnly(AttributesDepthOnly v)
{
    VaryingsDepthOnly o = (VaryingsDepthOnly)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float3 normalOSUnused = 0.0f;
    TerrainInstancing(v.positionOS, normalOSUnused, v.uv);
    VertexPositionInputs vertexPositionInputs = GetVertexPositionInputs(v.positionOS.xyz);

    // TODO: Need to implement a global for handling X and Y scaling in SCENESELECTIONPASS
    // so that scene selection lines up with black bars introduced from fixed aspect ratio set in camera volume override.
    // o.vertex.y *= 0.9f;

    // PSX Standard Material Transforms:
    o.vertex = ApplyPrecisionGeometryToPositionCS(vertexPositionInputs.positionWS, vertexPositionInputs.positionVS, vertexPositionInputs.positionCS, _PrecisionGeometryOverrideMode, _PrecisionGeometryOverrideParameters);
    o.uvw = ApplyAffineTextureWarpingToUVW(v.uv, o.vertex.w, _AffineTextureWarpingWeight);

    return o;
}

// Used in Standard Terrain shader SceneSelectionPass
half4 TerrainLitPassFragDepthOnly(VaryingsDepthOnly i) : SV_TARGET
{
    // Remember, we multipled all interpolated vertex values by positionCS.w in order to counter contemporary hardware's perspective correct interpolation.
    // We need to post multiply all interpolated vertex values by the interpolated positionCS.w (stored in uvw.z) component to "normalize" the interpolated values.
    float interpolatorNormalization = 1.0f / i.uvw.z;

    float2 uv = i.uvw.xy * interpolatorNormalization;
    float4 uvMainAndLM = float4(uv, uv * unity_LightmapST.xy + unity_LightmapST.zw);

#ifdef _ALPHATEST_ON
    ClipHoles(uvMainAndLM.xy);
#endif

#ifdef SCENESELECTIONPASS
    // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
    return half4(_ObjectId, _PassValue, 1.0, 1.0);
#endif
    return 0;
}

#if defined(METAPASS)
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

VaryingsMeta TerrainVertexMeta(AttributesMeta i)
{
    VaryingsMeta o;
    o.positionCS = MetaVertexPosition(i.positionOS, i.uv1, i.uv2,
        unity_LightmapST, unity_DynamicLightmapST);
    o.uv = TRANSFORM_TEX(i.uv0, _MainTex);
    return o;
}

half4 TerrainFragmentMeta(VaryingsMeta i) : SV_Target
{
    // Rather than paying the cost of interpolating our 6 floats for precision color per vertex, we simply recompute them per pixel here.
    float3 precisionColor;
    float3 precisionColorInverse;
    float precisionColorIndexNormalized = _PrecisionColor.w;
    float precisionChromaBit = _PrecisionColorInverse.w;
    ApplyPrecisionColorOverride(precisionColor, precisionColorInverse, _PrecisionColor.rgb, _PrecisionColorInverse.rgb, precisionColorIndexNormalized, precisionChromaBit, _PrecisionColorOverrideMode, _PrecisionColorOverrideParameters);

    half3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
    color.rgb = ApplyPrecisionColorToColorSRGB(half4(color, 0.0h), precisionColor, precisionColorInverse).rgb;
    color.rgb = SRGBToLinear(color.rgb);

    const half3 emission = 0.0h;

    return MetaFragment(color, emission);
}
#endif // endof defined(METAPASS)

#endif
