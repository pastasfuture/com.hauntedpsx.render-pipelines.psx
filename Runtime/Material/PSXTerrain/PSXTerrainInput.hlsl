#ifndef PSX_TERRAIN_INPUT
#define PSX_TERRAIN_INPUT

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(_Terrain)
half4 _DiffuseRemapScale0, _DiffuseRemapScale1, _DiffuseRemapScale2, _DiffuseRemapScale3;
// half4 _MaskMapRemapOffset0, _MaskMapRemapOffset1, _MaskMapRemapOffset2, _MaskMapRemapOffset3;
// half4 _MaskMapRemapScale0, _MaskMapRemapScale1, _MaskMapRemapScale2, _MaskMapRemapScale3;

float4 _Control_ST;
float4 _Control_TexelSize;
half _DiffuseHasAlpha0, _DiffuseHasAlpha1, _DiffuseHasAlpha2, _DiffuseHasAlpha3;
// half _LayerHasMask0, _LayerHasMask1, _LayerHasMask2, _LayerHasMask3;
half4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
half4 _Splat0_TexelSize, _Splat1_TexelSize, _Splat2_TexelSize, _Splat3_TexelSize;
half _NumLayersCount;

// #ifdef UNITY_INSTANCING_ENABLED
float4 _TerrainHeightmapRecipSize;   // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
float4 _TerrainHeightmapScale;       // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
// #endif
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif
CBUFFER_END

TEXTURE2D(_Control);    SAMPLER(sampler_Control);
TEXTURE2D(_Splat0);     SAMPLER(sampler_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);

// #ifdef _MASKMAP
// TEXTURE2D(_Mask0);      SAMPLER(sampler_Mask0);
// TEXTURE2D(_Mask1);
// TEXTURE2D(_Mask2);
// TEXTURE2D(_Mask3);
// #endif

TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);

CBUFFER_START(UnityPerMaterial)
float4 _MainTex_ST;
half4 _BaseColor;
// half _Cutoff;

float _AffineTextureWarpingWeight;
float _FogWeight;
int _PrecisionGeometryOverrideMode;
float3 _PrecisionGeometryOverrideParameters;
int _PrecisionColorOverrideMode;
float3 _PrecisionColorOverrideParameters;
CBUFFER_END

#if defined(METAPASS)
CBUFFER_START(UnityMetaPass)
// x = use uv1 as raster position
// y = use uv2 as raster position
bool4 unity_MetaVertexControl;

// x = return albedo
// y = return normal
bool4 unity_MetaFragmentControl;
CBUFFER_END


// This was not in constant buffer in original unity, so keep outiside. But should be in as ShaderRenderPass frequency
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;
float unity_UseLinearSpace;
#endif // METAPASS

#endif
