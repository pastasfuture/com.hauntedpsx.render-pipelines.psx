#ifndef PSX_SHADER_VARIABLES
#define PSX_SHADER_VARIABLES

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// Warning: These definitions must stay in sync with PrecisionVolume.DrawDistanceFalloffMode enum.
#define PSX_DRAW_DISTANCE_FALLOFF_MODE_PLANAR (0)
#define PSX_DRAW_DISTANCE_FALLOFF_MODE_CYLINDRICAL (1)
#define PSX_DRAW_DISTANCE_FALLOFF_MODE_SPHERICAL (2)

// Warning: These definitions must stay in sync with FogVolume.FogFalloffMode enum.
#define PSX_FOG_FALLOFF_MODE_PLANAR (0)
#define PSX_FOG_FALLOFF_MODE_CYLINDRICAL (1)
#define PSX_FOG_FALLOFF_MODE_SPHERICAL (2)

// Warning: These definitions must stay in sync with ReflectionDirectionMode enum.
#define PSX_REFLECTION_DIRECTION_MODE_REFLECTION (0)
#define PSX_REFLECTION_DIRECTION_MODE_NORMAL (1)
#define PSX_REFLECTION_DIRECTION_MODE_VIEW (2)

// Warning: These definitions must stay in sync with ReflectionBlendMode enum.
#define PSX_REFLECTION_BLEND_MODE_ADDITIVE (0)
#define PSX_REFLECTION_BLEND_MODE_SUBTRACTIVE (1)
#define PSX_REFLECTION_BLEND_MODE_MULTIPLY (2)

// Warning: These definitions must stay in sync with DrawDistanceOverrideMode enum.
#define PSX_DRAW_DISTANCE_OVERRIDE_MODE_NONE (0)
#define PSX_DRAW_DISTANCE_OVERRIDE_MODE_DISABLED (1)
#define PSX_DRAW_DISTANCE_OVERRIDE_MODE_OVERRIDE (2)
#define PSX_DRAW_DISTANCE_OVERRIDE_MODE_ADD (3)
#define PSX_DRAW_DISTANCE_OVERRIDE_MODE_MULTIPLY (4)

// Warning: These definitions must stay in sync with UVAnimationMode enum.
#define PSX_UV_ANIMATION_MODE_NONE (0)
#define PSX_UV_ANIMATION_MODE_PAN_LINEAR (1)
#define PSX_UV_ANIMATION_MODE_PAN_SIN (2)
#define PSX_UV_ANIMATION_MODE_FLIPBOOK (3)

// Warning: These definitions must stay in sync with PrecisionGeometryOverrideMode enum.
#define PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_NONE (0)
#define PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_DISABLED (1)
#define PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_OVERRIDE (2)
#define PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_ADD (3)
#define PSX_PRECISION_GEOMETRY_OVERRIDE_MODE_MULTIPLY (4)

// Warning: These definitions must stay in sync with PrecisionColorOverrideMode enum.
#define PSX_PRECISION_COLOR_OVERRIDE_MODE_NONE (0)
#define PSX_PRECISION_COLOR_OVERRIDE_MODE_DISABLED (1)
#define PSX_PRECISION_COLOR_OVERRIDE_MODE_OVERRIDE (2)
#define PSX_PRECISION_COLOR_OVERRIDE_MODE_ADD (3)
#define PSX_PRECISION_COLOR_OVERRIDE_MODE_MULTIPLY (4)

// Warning: These definitions must stay in sync with FogVolume.FogBlendMode enum.
#define PSX_FOG_BLEND_MODE_OVER (0)
#define PSX_FOG_BLEND_MODE_ADDITIVE (1)
#define PSX_FOG_BLEND_MODE_SUBTRACTIVE (2)
#define PSX_FOG_BLEND_MODE_MULTIPLY (3)

// Warning: These definitions must stay in sync with VertexColorBlendMode enum.
#define PSX_VERTEX_COLOR_BLEND_MODE_MULTIPLY (0)
#define PSX_VERTEX_COLOR_BLEND_MODE_ADDITIVE (1)
#define PSX_VERTEX_COLOR_BLEND_MODE_SUBTRACTIVE (2)

// Globals:
// Unity Standard:
//
// Time (t = time since current level load) values from Unity
float4 _Time; // (t/20, t, t*2, t*3)
float3 _WorldSpaceCameraPos;
float4 _ProjectionParams;

// PSXQuality
int _IsPSXQualityEnabled;
int _DrawDistanceFalloffMode;
float2 _DrawDistance;
float4 _GeometryPushbackParameters;
float4 _PrecisionGeometry;
float4 _PrecisionColor; // [scaleR, scaleG, scaleB, precisionColorIndexNormalized]
float4 _PrecisionColorInverse; // [1 / scaleR, 1 / scaleG, 1 / scaleB, precisionChromaBit]
float2 _PrecisionAlphaAndInverse;
float _AffineTextureWarping;
int _FogBlendMode;
int _FogFalloffMode;
int _FogHeightFalloffMirrored;
int _FogHeightFalloffMirroredLayer1;
float4 _FogColor;
float4 _FogDistanceScaleBias;
float _FogFalloffCurvePower;
float2 _FogPrecisionAlphaAndInverse;
TEXTURE2D(_FogPrecisionAlphaDitherTexture);
float4 _FogPrecisionAlphaDitherSize;
float _FogPrecisionAlphaDither;
int _FogIsAdditionalLayerEnabled;
int _FogFalloffModeLayer1;
float4 _FogColorLayer1;
float4 _FogDistanceScaleBiasLayer1;
float _FogFalloffCurvePowerLayer1;
float2 _FogColorLUTWeight;
float3 _FogColorLUTRotationTangent;
float3 _FogColorLUTRotationBitangent;
float3 _FogColorLUTRotationNormal;
TEXTURE2D(_FogColorLUTTexture2D);
TEXTURECUBE(_FogColorLUTTextureCube);

// Lighting (In the future this might be moved to UnityPerMaterial if we get multiple lights):
int _LightingIsEnabled;
float _BakedLightingMultiplier;
float _VertexColorLightingMultiplier;
float _DynamicLightingMultiplier;

// Post Processing
int _TonemapperIsEnabled;
float _TonemapperContrast;
float _TonemapperShoulder;
float _TonemapperWhitepoint;
float2 _TonemapperGraypointCoefficients;
float _TonemapperCrossTalk;
float _TonemapperSaturation;
float _TonemapperCrossTalkSaturation;

float4 _ScreenSize;
float4 _ScreenSizeRasterization;
float4 _ScreenSizeRasterizationRTScaled;
float4 _RasterizationRTScaledClampBoundsUV;
float4 _RasterizationRTScaledMaxSSAndUV;

float4x4 glstate_matrix_projection;
float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixInvVP;

CBUFFER_START(UnityPerDraw)
// Space block Feature
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
real4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms

// Render Layer block feature
// Only the first channel (x) contains valid data and the float must be reinterpreted using asuint() to extract the original 32 bits values.
float4 unity_RenderingLayer;

// Light Indices block feature
// These are set internally by the engine upon request by RendererConfiguration.
real4 unity_LightData;
real4 unity_LightIndices[2];

float4 unity_ProbesOcclusion;

// Lightmap block feature
float4 unity_LightmapST;
float4 unity_DynamicLightmapST;

// SH block feature
real4 unity_SHAr;
real4 unity_SHAg;
real4 unity_SHAb;
real4 unity_SHBr;
real4 unity_SHBg;
real4 unity_SHBb;
real4 unity_SHC;

// Velocity
float4x4 unity_MatrixPreviousM;
float4x4 unity_MatrixPreviousMI;
//X : Use last frame positions (right now skinned meshes are the only objects that use this
//Y : Force No Motion
//Z : Z bias value
//W : Camera only
float4 unity_MotionVectorsParams;
CBUFFER_END

// Dynamic Lighting:
#if defined(SHADER_API_MOBILE) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
    // Platforms like WebGL will error out with Error: Too many uniforms if the max visible lights is too large.
    #define MAX_VISIBLE_LIGHTS 32
#else
    #define MAX_VISIBLE_LIGHTS 256
#endif
// float4 _MainLightPosition;
// half4 _MainLightColor;
half4 _AdditionalLightsCount;
// GLES3 causes a performance regression in some devices when using CBUFFER.
#ifndef SHADER_API_GLES3
CBUFFER_START(AdditionalLights)
#endif
float4 _AdditionalLightsPosition[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsColor[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsAttenuation[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsSpotDir[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightOcclusionProbeChannel[MAX_VISIBLE_LIGHTS];
#ifndef SHADER_API_GLES3
CBUFFER_END
#endif

float4x4 OptimizeProjectionMatrix(float4x4 M)
{
    // Matrix format (x = non-constant value).
    // Orthographic Perspective  Combined(OR)
    // | x 0 0 x |  | x 0 x 0 |  | x 0 x x |
    // | 0 x 0 x |  | 0 x x 0 |  | 0 x x x |
    // | x x x x |  | x x x x |  | x x x x | <- oblique projection row
    // | 0 0 0 1 |  | 0 0 x 0 |  | 0 0 x x |
    // Notice that some values are always 0.
    // We can avoid loading and doing math with constants.
    M._21_41 = 0;
    M._12_42 = 0;
    return M;
}

#define UNITY_MATRIX_M     unity_ObjectToWorld
#define UNITY_MATRIX_I_M   unity_WorldToObject
#define UNITY_MATRIX_V     unity_MatrixV
#define UNITY_MATRIX_I_V   unity_MatrixInvV
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(glstate_matrix_projection)
#define UNITY_MATRIX_I_P   ERROR_UNITY_MATRIX_I_P_IS_NOT_DEFINED
#define UNITY_MATRIX_VP    unity_MatrixVP
#define UNITY_MATRIX_I_VP  unity_MatrixInvVP
#define UNITY_MATRIX_MV    mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV  transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP   mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)
#define UNITY_PREV_MATRIX_M unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

// These are the samplers available in the HDRenderPipeline.
// Avoid declaring extra samplers as they are 4x SGPR each on GCN.
SAMPLER(s_point_clamp_sampler);
SAMPLER(s_linear_clamp_sampler);
SAMPLER(s_linear_repeat_sampler);
SAMPLER(s_trilinear_clamp_sampler);
SAMPLER(s_trilinear_repeat_sampler);
SAMPLER_CMP(s_linear_clamp_compare_sampler);

// Main lightmap
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
// Dual or directional lightmap (always used with unity_Lightmap, so can share sampler)
TEXTURE2D(unity_LightmapInd);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

TEXTURE2D(_FramebufferDitherTexture);
float4 _FramebufferDitherSize;
float _FramebufferDither;
float2 _FramebufferDitherScaleAndInverse;


TEXTURE2D(_AlphaClippingDitherTexture);
float4 _AlphaClippingDitherSize;

// Note: #include order is important here.
// unity_* globals and UNITY_MATRIX definitions must be made before UnityInstancing.hlsl
// so constant buffer declarations don't fail because of instancing macros.
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#endif