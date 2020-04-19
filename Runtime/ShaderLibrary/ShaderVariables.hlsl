#ifndef PSX_SHADER_VARIABLES
#define PSX_SHADER_VARIABLES

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// Globals:
// Unity Standard:
//
// Time (t = time since current level load) values from Unity
float4 _Time; // (t/20, t, t*2, t*3)
float3 _WorldSpaceCameraPos;

// PSXQuality
int _IsPSXQualityEnabled;
float _DrawDistance;
float2 _PrecisionGeometry;
float3 _PrecisionColor;
float3 _PrecisionColorInverse;
float4 _FogColor;
float2 _FogDistanceScaleBias;

// Lighting (In the future this might be moved to UnityPerMaterial if we get multiple lights):
int _LightingIsEnabled;
float _BakedLightingMultiplier;
float _VertexColorLightingMultiplier;

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

float4x4 glstate_matrix_projection;
float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixInvVP;

CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms

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
CBUFFER_END

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

CBUFFER_START(UnityPerMaterial)
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_ST;
float4 _MainColor;
TEXTURE2D(_EmissionTexture);
SAMPLER(sampler_EmissionTexture);
float4 _EmissionTexture_ST;
float3 _EmissionColor;
float _AlphaClippingDitherIsEnabled;
CBUFFER_END

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

TEXTURE2D(_FramebufferDitherTexture);
float4 _FramebufferDitherSize;
int _FramebufferDitherIsEnabled;


TEXTURE2D(_AlphaClippingDitherTexture);
float4 _AlphaClippingDitherSize;
#endif