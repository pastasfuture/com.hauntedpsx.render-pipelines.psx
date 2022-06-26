#ifndef PSX_WAVING_GRASS_PASSES_INCLUDED
#define PSX_WAVING_GRASS_PASSES_INCLUDED

// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderFunctions.hlsl"

#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/BakedLighting.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/DynamicLighting.hlsl"
#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/MaterialFunctions.hlsl"

struct GrassAttributes
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float4 tangent : TANGENT; // Tangent is needed for defining the offset in our grass procedural wind animation.
    half4 color : COLOR;
    float2 lightmapUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct GrassVaryings
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
    UNITY_VERTEX_OUTPUT_STEREO
};

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

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Grass: appdata_full usage
// color        - .xyz = color, .w = wave scale
// normal       - normal
// tangent.xy   - billboard extrusion
// texcoord     - UV coords
// texcoord1    - 2nd UV coords

GrassVaryings WavingGrassVert(GrassAttributes v)
{
    GrassVaryings o = (GrassVaryings)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    // MeshGrass v.color.a: 1 on top vertices, 0 on bottom vertices
    // _WaveAndDistance.z == 0 for MeshLit
    float waveAmount = v.color.a * _WaveAndDistance.z;
    o.color = TerrainWaveGrass(v.vertex, waveAmount, SRGBToLinear(v.color));

    VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);

    o.vertex = vertexInput.positionCS;
    o.normalWS = TransformObjectToWorldNormal(v.normal); // TODO: Apply affine warping?

    o.positionVS = vertexInput.positionVS; // TODO: Apply affine warping?
    o.positionWS = vertexInput.positionWS; // TODO: Apply affine warping?
    o.lightmapUV = v.lightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw; // TODO: Apply affine warping?

    const float4 vertexColor = 0.0f; // Grass does not support vertex color based lighting.

    // We currently have no methods of assigning specific user controllable materials to our grass surfaces,
    // and therefore have no way of controlling the _PrecisionGeometryWeight as in other surfaces.
    // for now we simply hardcode this at full strength, which should be a reasonable default.
    // TODO: Figure out the best way to expose these to the user.
    const int precisionGeometryOverrideMode = PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_NONE;
    const float3 precisionGeometryOverrideParameters = 0.0f;
    const float affineTextureWarpingWeight = 1.0f;
    const float fogWeight = 1.0f;

    o.vertex = ApplyPrecisionGeometryToPositionCS(vertexInput.positionWS, vertexInput.positionVS, o.vertex, precisionGeometryOverrideMode, precisionGeometryOverrideParameters);
    o.uvw = ApplyAffineTextureWarpingToUVW(v.uv, vertexInput.positionCS.w, affineTextureWarpingWeight);
    o.lighting = EvaluateLightingPerVertex(vertexInput.objectPositionWS, vertexInput.positionWS, o.normalWS, vertexColor, o.lightmapUV, o.uvw.z);
    o.fog = EvaluateFogPerVertex(vertexInput.objectPositionWS, vertexInput.objectPositionVS, vertexInput.positionWS, vertexInput.positionVS, o.uvw.z, fogWeight, _PrecisionColor.rgb, _PrecisionColorInverse.rgb);

    return o;
}

GrassVaryings WavingGrassBillboardVert(GrassAttributes v)
{
    GrassVaryings o = (GrassVaryings)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    TerrainBillboardGrass(v.vertex, v.tangent.xy);
    // wave amount defined by the grass height
    float waveAmount = v.tangent.y;
    o.color = TerrainWaveGrass(v.vertex, waveAmount, SRGBToLinear(v.color));

    VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);

    o.vertex = vertexInput.positionCS;
    o.normalWS = TransformObjectToWorldNormal(v.normal); // TODO: Apply affine warping?

    o.positionVS = vertexInput.positionVS; // TODO: Apply affine warping?
    o.positionWS = vertexInput.positionWS; // TODO: Apply affine warping?
    o.lightmapUV = v.lightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw; // TODO: Apply affine warping?

    const float4 vertexColor = 0.0f; // Grass does not support vertex color based lighting.

    // We currently have no methods of assigning specific user controllable materials to our grass surfaces,
    // and therefore have no way of controlling the _PrecisionGeometryWeight as in other surfaces.
    // for now we simply hardcode this at full strength, which should be a reasonable default.
    // TODO: Figure out the best way to expose these to the user.
    const int precisionGeometryOverrideMode = PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_NONE;
    const float3 precisionGeometryOverrideParameters = 0.0f;
    const float affineTextureWarpingWeight = 1.0f;
    const float fogWeight = 1.0f;

    o.vertex = ApplyPrecisionGeometryToPositionCS(vertexInput.positionWS, vertexInput.positionVS, o.vertex, precisionGeometryOverrideMode, precisionGeometryOverrideParameters);
    o.uvw = ApplyAffineTextureWarpingToUVW(v.uv, vertexInput.positionCS.w, affineTextureWarpingWeight);
    // o.lighting = EvaluateLightingPerVertex(vertexInput.objectPositionWS, vertexInput.positionWS, o.normalWS, vertexColor, o.lightmapUV, o.uvw.z);
    o.fog = EvaluateFogPerVertex(vertexInput.objectPositionWS, vertexInput.objectPositionVS, vertexInput.positionWS, vertexInput.positionVS, o.uvw.z, fogWeight, _PrecisionColor.rgb, _PrecisionColorInverse.rgb);

    return o;
}

// Used for StandardSimpleLighting shader
half4 LitPassFragmentGrass(GrassVaryings i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    float2 positionSS = i.vertex.xy;

    // Remember, we multipled all interpolated vertex values by positionCS.w in order to counter contemporary hardware's perspective correct interpolation.
    // We need to post multiply all interpolated vertex values by the interpolated positionCS.w (stored in uvw.z) component to "normalize" the interpolated values.
    float interpolatorNormalization = 1.0f / i.uvw.z;

    float3 normalWS = normalize(i.normalWS);

    float2 uv = i.uvw.xy * interpolatorNormalization;

    float2 uvColor = TRANSFORM_TEX(uv, _MainTex);

    float4 texelSizeLod;
    float lod;
    ComputeLODAndTexelSizeMaybeCallDDX(texelSizeLod, lod, uvColor, _MainTex_TexelSize);
    
    float4 color = SampleTextureWithFilterMode(TEXTURE2D_ARGS(_MainTex, sampler_MainTex), uvColor, texelSizeLod, lod);

#if defined(_ALPHATEST_ON)
    // Perform alpha cutoff transparency (i.e: discard pixels in the holes of a chain link fence texture, or in the negative space of a leaf texture).
    // Any alpha value < alphaClippingDither will trigger the pixel to be discarded, any alpha value greater than or equal to alphaClippingDither will trigger the pixel to be preserved.
    //
    // We currently have no methods of assigning specific user controllable materials to our grass surfaces,
    // and therefore have no way of controlling the _AlphaClippingDitherIsEnabled as in other surfaces.
    // for now we simply hardcode this as disabled which should be a reasonable default.
    // TODO: Figure out the best way to expose these to the user.
    const float alphaClippingDitherIsEnabled = 0.0f;
    const float4 alphaClippingScaleBiasMinMax = float4(1.0f, 0.0f, 0.5f, 0.5f + 1e-5f);
    float alphaClippingDither;
    float alphaForClipping;
    ComputeAndFetchAlphaClippingParameters(alphaClippingDither, alphaForClipping, color.a, positionSS, alphaClippingDitherIsEnabled, alphaClippingScaleBiasMinMax);
    clip((alphaForClipping > alphaClippingDither) ? 1.0f : -1.0f);
#endif

    if (!_IsPSXQualityEnabled)
    {
        // TODO: Handle premultiply alpha case here?
        return color;
    }

    color = ApplyPrecisionColorToColorSRGB(color, _PrecisionColor.rgb, _PrecisionColorInverse.rgb);
    color.rgb = SRGBToLinear(color.rgb);
    color.rgb *= i.color.rgb;

    float3 lighting = EvaluateLightingPerPixel(i.positionWS, normalWS, i.lighting, i.lightmapUV, interpolatorNormalization);
    color = ApplyLightingToColor(lighting, color);

    // We currently have no methods of assigning specific user controllable materials to our grass surfaces,
    // and therefore have no way of controlling the _PrecisionGeometryWeight as in other surfaces.
    // for now we simply hardcode this at full strength, which should be a reasonable default.
    // TODO: Figure out the best way to expose these to the user.
    const float fogWeight = 1.0f;
    float4 fog = EvaluateFogPerPixel(i.positionWS, i.positionVS, positionSS, i.fog, interpolatorNormalization, fogWeight, _PrecisionColor.rgb, _PrecisionColorInverse.rgb);

    color = ApplyFogToColor(fog, color);

#if defined(_OUTPUT_LDR)
    
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
    color.rgb = ComputeFramebufferDiscretization(color.rgb, positionSS, _PrecisionColor.rgb, _PrecisionColorInverse.rgb);
#endif

    return color;
};

struct VertexInput
{
    float4 position     : POSITION;
    half4 color         : COLOR;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float2 uv           : TEXCOORD0;
    half4 color         : TEXCOORD1;
    float4 vertex      : SV_POSITION;
};

VertexOutput DepthOnlyVertex(VertexInput v)
{
    VertexOutput o = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);

    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    // MeshGrass v.color.a: 1 on top vertices, 0 on bottom vertices
    // _WaveAndDistance.z == 0 for MeshLit
    float waveAmount = v.color.a * _WaveAndDistance.z;
    o.color = TerrainWaveGrass(v.position, waveAmount, v.color);
    o.vertex = TransformObjectToHClip(v.position.xyz);
    return o;
}

half4 DepthOnlyFragment(VertexOutput IN) : SV_TARGET
{
    // TODO: 
    // Alpha(SampleAlbedoAlpha(IN.uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex)).a, IN.color, _Cutoff);
return 0;
}

#endif
