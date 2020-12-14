#ifndef PSX_LIT_INPUT
#define PSX_LIT_INPUT

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

CBUFFER_START(UnityPerMaterial)
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_ST;
float4 _MainTex_TexelSize;
float4 _MainColor;
TEXTURE2D(_EmissionTexture);
SAMPLER(sampler_EmissionTexture);
float3 _EmissionColor;
float _EmissionBakedMultiplier;
float _AlphaClippingDitherIsEnabled;
float4 _AlphaClippingScaleBiasMinMax;
float _AffineTextureWarpingWeight;
float _PrecisionGeometryWeight;
float _FogWeight;
int _DrawDistanceOverrideMode;
float2 _DrawDistanceOverride;
TEXTURECUBE(_ReflectionCubemap);
SAMPLER(sampler_ReflectionCubemap);
TEXTURE2D(_ReflectionTexture);
SAMPLER(sampler_ReflectionTexture);
float4 _ReflectionColor;
int _ReflectionBlendMode;
float4 _DoubleSidedConstants;

#if defined(SCENESELECTIONPASS)
// Following two variables are feeded by the C++ Editor for Scene selection
int _ObjectId;
int _PassValue;
#endif

CBUFFER_END

#endif