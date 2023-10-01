#ifndef PSX_SHADER_FUNCTIONS
#define PSX_SHADER_FUNCTIONS

#include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

// In Core versions less than 14.0 (2021 and older), GetViewToWorldMatrix and TransformViewToWorld are not defined yet in SpaceTransforms.hlsl.
// But in 14.0, 2022 and up, they are.
// Currently we have no way of detecting the version of core and static branching in our shaders.
// Instead, we simply define our own PSX variants that mimic the functions in newer versions of SpaceTransforms.hlsl, and use those everywhere so that we always have compatability.
float4x4 PSXGetViewToWorldMatrix()
{
    return UNITY_MATRIX_I_V;
}

float3 PSXTransformViewToWorld(float3 positionVS)
{
    return mul(PSXGetViewToWorldMatrix(), float4(positionVS, 1.0)).xyz;
}


float TonemapperGenericScalar(float x)
{
    return saturate(
        pow(x, _TonemapperContrast)
        / (pow(x, _TonemapperContrast * _TonemapperShoulder) * _TonemapperGraypointCoefficients.x + _TonemapperGraypointCoefficients.y)
    );
}

// Improved crosstalk - maintaining saturation.
// http://gpuopen.com/wp-content/uploads/2016/03/GdcVdrLottes.pdf
// https://www.shadertoy.com/view/XljBRK
float3 TonemapperGeneric(float3 rgb)
{
    float peak = max(max(rgb.r, max(rgb.g, rgb.b)), 1.0f / (256.0f * 65536.0f));
    float3 ratio = rgb / peak;
    peak = TonemapperGenericScalar(peak);

    ratio = pow(max(0.0f, ratio), (_TonemapperSaturation + _TonemapperContrast) / _TonemapperCrossTalkSaturation);
    ratio = lerp(ratio, float3(1.0f, 1.0f, 1.0f), pow(peak, _TonemapperCrossTalk));
    ratio = pow(max(0.0f, ratio), _TonemapperCrossTalkSaturation);

    return ratio * peak;
}

float HashShadertoy(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898f, 78.233f))) * 43758.5453123f);
}

// https://en.wikipedia.org/wiki/YUV
// YUV color space used in PAL RF and composite signals.
// Note: These transforms expect linear RGB, not perceptual sRGB.
// Convert [0, 1] range RGB values to Y[0, 1], U[-0.436, 0.436], V[-0.615, 0.615] space.
float3 YUVFromRGB(float3 rgb)
{
    float3 yuv;

    yuv.x = rgb.x * 0.299 + 0.587 * rgb.y + 0.114 * rgb.z;
    yuv.y = (rgb.z - yuv.x) * (0.436 / (1.0 - 0.114));
    yuv.z = (rgb.x - yuv.x) * (0.615 / (1.0 - 0.299));

    return yuv;
}

// Convert [0, 1] range RGB values to YUV[0, 1] range normalized values (for convenient discretization and storage).
float3 YUVNormalizedFromRGB(float3 rgb)
{
    float3 yuv = YUVFromRGB(rgb);

    const float2 UV_MAX = float2(0.436, 0.615);
    const float2 UV_MIN = -UV_MAX;
    const float2 UV_RANGE = UV_MAX - UV_MIN;
    const float2 UV_SCALE = 1.0 / UV_RANGE;
    const float2 UV_BIAS = -UV_MIN / UV_RANGE;

    yuv.yz = yuv.yz * UV_SCALE + UV_BIAS;

    return yuv;
}

float3 RGBFromYUV(float3 yuv)
{
    return float3(
        yuv.x * 1.0 + yuv.y * 0.0 + yuv.z * 1.1383,
        yuv.x * 1.0 + yuv.y * -0.39465 + yuv.z * -0.5806,
        yuv.x * 1.0 + yuv.y * 2.03211 + yuv.z * 0.0
    );
}

float3 RGBFromYUVNormalized(float3 yuv)
{
    const float2 UV_MAX = float2(0.436, 0.615);
    const float2 UV_MIN = -UV_MAX;
    const float2 UV_RANGE = UV_MAX - UV_MIN;
    const float2 UV_SCALE = UV_RANGE;
    const float2 UV_BIAS = UV_MIN;

    yuv.yz  = yuv.yz * UV_SCALE + UV_BIAS;

    float3 rgb = RGBFromYUV(yuv);

    return rgb;
}

// https://en.wikipedia.org/wiki/YIQ
// YIQ color space used in NTSC RF and composite signals.

// FCC NTSC YIQ Standard
// Converts from perceptual / gamma corrected sRGB color space to gamma corrected YIQ space.
float3 FCCYIQFromSRGB(float3 srgb)
{
    float3 yiq = float3(
        srgb.r * 0.30 + srgb.g * 0.59 + srgb.b * 0.11,
        srgb.r * 0.599 + srgb.g * -0.2773 + srgb.b * -0.3217,
        srgb.r * 0.213 + srgb.g * -0.5251 + srgb.b * 0.3121
    );

    return yiq;
}

float3 SRGBFromFCCYIQ(float3 yiq)
{
    float3 srgb = float3(
        yiq.x + yiq.y * 0.9469 + yiq.z * 0.6236,
        yiq.x + yiq.y * -0.2748 + yiq.z * -0.6357,
        yiq.x + yiq.y * -1.1 + yiq.z * 1.7
    );

    return srgb;
}

// Converts from ycocg color space to rgb color space.
// This is a purely linear transform, so it can be run on sRGB or RGB data, it has no notion of gamma.
// https://en.wikipedia.org/wiki/YCoCg
// https://scc.ustc.edu.cn/zlsc/sugon/intel/ipp/ipp_manual/IPPI/ippi_ch6/ch6_color_models.htm
// Y is in range [0, 1], Co and Cg are in range [-0.5, 0.5]
float3 RGBFromYCOCG(float3 rgb)
{
    return float3(
        dot(rgb, float3(0.25, 0.5, 0.25)),
        dot(rgb.rb, float2(0.5, -0.5)),
        dot(rgb, float3(-0.25, 0.5, -0.25))
    );
}

float3 YCOCGFromRGB(float3 ycocg)
{
    return float3(
        ycocg.x + ycocg.y - ycocg.z,
        ycocg.x + ycocg.z,
        ycocg.x - ycocg.y - ycocg.z
    );
}

// Returns ycocg in normalized range [0, 1] for all components.
// Used for unorm storage. Often called YCOCG-R colorspace in literature.
// https://en.wikipedia.org/wiki/YCoCg
float3 YCOCGNormalizedFromRGB(float3 rgb)
{
    float3 res;
    res.y = rgb.r - rgb.b;
    float tmp = res.y * 0.5 + rgb.b;
    res.z = rgb.g - tmp;
    res.x = res.z * 0.5 + tmp;
    return res;
}

float3 RGBFromYCOCGNormalized(float3 ycocgNormalized)
{
    float3 res;
    float tmp = ycocgNormalized.z * -0.5 + ycocgNormalized.x;
    res.g = ycocgNormalized.z + tmp;
    res.b = ycocgNormalized.y * -0.5 + tmp;
    res.r = res.b + ycocgNormalized.y;
    return res;
}

// https://scc.ustc.edu.cn/zlsc/sugon/intel/ipp/ipp_manual/IPPI/ippi_ch6/ch6_color_models.htm
float3 YCBCRFromSRGB(float3 srgb)
{
    return float3(
        srgb.r * 0.257 + srgb.g * 0.504 + srgb.b * 0.098 + (16.0 / 255.0),
        srgb.r * -0.148 + srgb.g * -0.291 + srgb.b * 0.439 + (128.0 / 255.0),
        srgb.r * 0.439 + srgb.g * -0.368 + srgb.b * -0.071  + (128.0 / 255.0)
    );
}

float3 SRGBFromYCBCR(float3 ycbcr)
{
    float3 ycbcrScaled = ycbcr - float3(16.0 / 255.0, 128.0 / 255.0, 128.0 / 255.0);
    ycbcrScaled.x *= 1.164;
    return float3(
        ycbcrScaled.z * 1.596 + ycbcrScaled.x,
        ycbcrScaled.z * -0.813 + ycbcrScaled.y * -0.392 + ycbcrScaled.x,
        ycbcrScaled.y * 2.017 + ycbcrScaled.x
    );
}

// https://scc.ustc.edu.cn/zlsc/sugon/intel/ipp/ipp_manual/IPPI/ippi_ch6/ch6_color_models.htm
// Intel IPP reccomends this slightly different YCBCR color space for jpeg compression.
float3 YCBCRJPEGFromSRGB(float3 srgb)
{
    return float3(
        srgb.r * 0.299 + srgb.g * 0.587 + srgb.b * 0.114,
        srgb.r * -0.16874 + srgb.g * -0.33126 + srgb.b * 0.5 + (128.0 / 255.0),
        srgb.r * 0.5 + srgb.g * -0.41869 + srgb.b * -0.08131 + (128.0 / 255.0)
    );
}

float3 SRGBFromYCBCRJPEG(float3 ycbcr)
{
    return float3(
        ycbcr.x + ycbcr.z * 1.402 + (-179.456 / 255.0),
        ycbcr.x + ycbcr.y * -0.34414 + ycbcr.z * -0.71414 + (135.45984 / 255.0),
        ycbcr.x + ycbcr.y * 1.772 + (-226.816 / 255.0)
    );
}

// Low Complexity, High Fidelity: The Rendering of INSIDE
// https://youtu.be/RdN06E6Xn9E?t=1337
// Remaps a [0, 1] value to [-0.5, 1.5] range with a triangular distribution.
float NoiseDitherRemapTriangularDistribution(float v)
{
    float orig = v * 2.0 - 1.0;
    float c0 = 1.0 - sqrt(saturate(1.0 - abs(orig)));
    return 0.5 + ((orig >= 0.0) ? c0 : -c0);
}

float3 NoiseDitherRemapTriangularDistribution(float3 v)
{
    float3 orig = v * 2.0 - 1.0;
    float3 c0 = 1.0 - sqrt(saturate(1.0 - abs(orig)));
    return 0.5 + float3(
        (orig.x >= 0.0) ? c0.x : -c0.x,
        (orig.y >= 0.0) ? c0.y : -c0.y,
        (orig.z >= 0.0) ? c0.z : -c0.z
    );
}

float3 ComputeFramebufferDiscretization(float3 color, float2 positionSS, float ditherWeight, float3 precisionColor, float3 precisionColorInverse)
{
    float framebufferDither = 0.5f;
    float framebufferDitherWeight = _FramebufferDither * ditherWeight;
    if (framebufferDitherWeight > 0.0f)
    {
        uint2 framebufferDitherTexelCoord = (uint2)floor(frac(positionSS * _FramebufferDitherScaleAndInverse.yy * _FramebufferDitherSize.zw) * _FramebufferDitherSize.xy);
        framebufferDither = LOAD_TEXTURE2D_LOD(_FramebufferDitherTexture, framebufferDitherTexelCoord, 0).a;
        framebufferDither = NoiseDitherRemapTriangularDistribution(framebufferDither);
        framebufferDither = lerp(0.5f, framebufferDither, framebufferDitherWeight);
    }
    return floor(color.xyz * precisionColor + framebufferDither) * precisionColorInverse;
}

float3 ComputeFramebufferDiscretization(float3 color, float2 positionSS, float3 precisionColor, float3 precisionColorInverse)
{
    return ComputeFramebufferDiscretization(color, positionSS, 1.0f, precisionColor, precisionColorInverse);
}



void ComputeAndFetchAlphaClippingParameters(out float alphaClippingDither, out float alphaForClipping, float alpha, float2 positionSS, float alphaClippingDitherIsEnabled, float4 alphaClippingScaleBiasMinMax)
{
    if (alphaClippingDitherIsEnabled > 0.5f)
    {
        uint2 alphaClippingDitherTexelCoord = (uint2)floor(frac(positionSS * _FramebufferDitherScaleAndInverse.yy * _AlphaClippingDitherSize.zw) * _AlphaClippingDitherSize.xy);
        alphaClippingDither = LOAD_TEXTURE2D_LOD(_AlphaClippingDitherTexture, alphaClippingDitherTexelCoord, 0).a;
        alphaClippingDither = min(0.999f, alphaClippingDither);
        alphaForClipping = saturate(alpha * alphaClippingScaleBiasMinMax.x + alphaClippingScaleBiasMinMax.y);
    }
    else
    {
        alphaClippingDither = alphaClippingScaleBiasMinMax.z;
        alphaForClipping = alpha;
    }
}

struct FogFalloffData
{
    float falloff;
    float falloffDistance;
    float falloffHeight;
    float3 falloffDirectionWS;
};

FogFalloffData EvaluateFogFalloffData(float3 positionWS, float3 cameraPositionWS, float3 positionVS, int fogFalloffMode, bool fogHeightFalloffMirrored, float4 fogDistanceScaleBias, float fogFalloffCurvePower)
{
    float falloffDepth = 0.0f;

    if (fogFalloffMode == PSX_FOG_FALLOFF_MODE_PLANAR)
    {
        falloffDepth = abs(positionVS.z);
    }
    else if (fogFalloffMode == PSX_FOG_FALLOFF_MODE_CYLINDRICAL)
    {
        falloffDepth = length(positionWS.xz - cameraPositionWS.xz);
    }
    else // fogFalloffMode == PSX_FOG_FALLOFF_MODE_SPHERICAL
    {
        falloffDepth = length(positionVS);
    }

    // fogDistanceScaleBias.xy contains distance falloff scale bias terms.
    // fogDistanceScaleBias.zw contains height falloff scale bias terms.
    float falloffDistance = falloffDepth * fogDistanceScaleBias.x + fogDistanceScaleBias.y;
    falloffDistance = saturate(falloffDistance);
    falloffDistance = pow(falloffDistance, fogFalloffCurvePower);

    float falloffHeight = positionWS.y;
    falloffHeight = falloffHeight * fogDistanceScaleBias.z + fogDistanceScaleBias.w;
    float falloffHeightSign = (falloffHeight >= 0.0f) ? 1.0f : -1.0f;
    falloffHeight = fogHeightFalloffMirrored ? abs(falloffHeight) : falloffHeight;
    falloffHeight = saturate(falloffHeight);
    falloffHeight = pow(falloffHeight, fogFalloffCurvePower);

    float falloff = falloffDistance * falloffHeight;

    float falloffHeightForLUT = fogHeightFalloffMirrored
        ? (falloffHeight * falloffHeightSign * 0.5f + 0.5f)
        : falloffHeight;

    FogFalloffData res;
    res.falloff = falloff;
    res.falloffDistance = falloffDistance;
    res.falloffHeight = falloffHeightForLUT;
    res.falloffDirectionWS = mul(float3x3(_FogColorLUTRotationTangent, _FogColorLUTRotationBitangent, _FogColorLUTRotationNormal), normalize(positionWS - cameraPositionWS));

    return res;
}

// SAMPLE_TEXTURECUBE_LOD() is not supported in vertex shaders in SM 5.0 or lower (i.e: WebGL2)
// Rather than branching feature support per platform, for consistency we force fog to be evaluated per pixel if _FOG_COLOR_LUT_MODE_TEXTURECUBE is requested.
#if defined(_FOG_COLOR_LUT_MODE_TEXTURECUBE)
#define _FOG_EVALUATION_MODE_FORCE_PER_PIXEL
#endif

float4 EvaluateFogFalloffColorPerVertex(FogFalloffData fogFalloffData)
{
    float4 fogColorLUTSample = float4(1.0f, 1.0f, 1.0f, 1.0f);

#if defined(_FOG_COLOR_LUT_MODE_TEXTURE2D_DISTANCE_AND_HEIGHT)
    // Flip Y so that LUT authoring is more intuitive: height fog min value is at the bottom of the texture and height fog max value is at the top.
    float2 colorLUTUV = float2(fogFalloffData.falloffDistance, 1.0f - fogFalloffData.falloffHeight);
    fogColorLUTSample = SAMPLE_TEXTURE2D_LOD(_FogColorLUTTexture2D, s_linear_clamp_sampler, colorLUTUV, 0);

#else // defined(_FOG_COLOR_LUT_MODE_DISABLED) || defined(_FOG_COLOR_LUT_MODE_TEXTURECUBE)
    fogColorLUTSample = float4(1.0f, 1.0f, 1.0f, 1.0f);
#endif

    return fogColorLUTSample;
}

float4 EvaluateFogFalloffColorPerPixel(FogFalloffData fogFalloffData)
{
    float4 fogColorLUTSample = float4(1.0f, 1.0f, 1.0f, 1.0f);

#if defined(_FOG_COLOR_LUT_MODE_TEXTURE2D_DISTANCE_AND_HEIGHT)
    // Flip Y so that LUT authoring is more intuitive: height fog min value is at the bottom of the texture and height fog max value is at the top.
    float2 colorLUTUV = float2(fogFalloffData.falloffDistance, 1.0f - fogFalloffData.falloffHeight);
    fogColorLUTSample = SAMPLE_TEXTURE2D(_FogColorLUTTexture2D, s_linear_clamp_sampler, colorLUTUV);

#elif defined(_FOG_COLOR_LUT_MODE_TEXTURECUBE)
    fogColorLUTSample = SAMPLE_TEXTURECUBE(_FogColorLUTTextureCube, s_linear_clamp_sampler,  fogFalloffData.falloffDirectionWS);

#else // defined(_FOG_COLOR_LUT_MODE_DISABLED)
    fogColorLUTSample = float4(1.0f, 1.0f, 1.0f, 1.0f);
#endif

    return fogColorLUTSample;
}

float ComputeFogAlphaDiscretization(float alpha, float2 positionSS)
{
    float dither = 0.5f;
    if (_FogPrecisionAlphaDither > 0.0f)
    {
        uint2 ditherTexelCoord = (uint2)floor(frac(positionSS * _FramebufferDitherScaleAndInverse.yy * _FogPrecisionAlphaDitherSize.zw) * _FogPrecisionAlphaDitherSize.xy);
        dither = LOAD_TEXTURE2D_LOD(_FogPrecisionAlphaDitherTexture, ditherTexelCoord, 0).a;
        dither = NoiseDitherRemapTriangularDistribution(dither);
        dither = lerp(0.5f, dither, _FogPrecisionAlphaDither);
    }

    return saturate(floor(alpha * _FogPrecisionAlphaAndInverse.x + dither) * _FogPrecisionAlphaAndInverse.y);
}

bool EvaluateDrawDistanceIsVisible(float3 positionWS, float3 cameraPositionWS, float3 positionVS, int drawDistanceFalloffMode, float drawDistance, float drawDistanceSquared)
{
    if (drawDistanceFalloffMode == PSX_DRAW_DISTANCE_FALLOFF_MODE_PLANAR)
    {
        return abs(positionVS.z) < drawDistance;
    }
    else if (drawDistanceFalloffMode == PSX_DRAW_DISTANCE_FALLOFF_MODE_CYLINDRICAL)
    {
        float2 offset = positionWS.xz - cameraPositionWS.xz;
        return dot(offset, offset) < drawDistanceSquared;
    }
    else // drawDistanceFalloffMode == PSX_DRAW_DISTANCE_FALLOFF_MODE_SPHERICAL
    {
        float3 offset = positionVS;
        return dot(offset, offset) < drawDistanceSquared;
    }
}

float ComputeLODFromTexelDDXDDY(float2 texelDDX, float2 texelDDY)
{
    // In isotropic mipmapping, we simply take the max magnitude of x and y.
    float ddMaxSquared = max(dot(texelDDX, texelDDX), dot(texelDDY, texelDDY));

    // log2(1) == 0
    // clamp to lod 0 and guard against -infinity at the same time via max(1, x).
    ddMaxSquared = max(1.0f, ddMaxSquared);

    // 0.5 * log2(x) == log2(sqrt(x))
    float lod = 0.5f * log2(ddMaxSquared);

    return lod;
}

void ComputeLODAndTexelSizeFromEvaluateDDXDDY(out float4 texelSizeLod, out float lod, float2 uv, float4 texelSize)
{
    // WARNING: Because we are calling ddx(), we need to ensure we do not discard any fragments via clip() before this function call.
    // Otherwise, the result of ddx() will be undefined.
    float2 texel = uv * texelSize.zw;
    float2 texelDDX = ddx(texel);
    float2 texelDDY = ddy(texel);
    lod = ComputeLODFromTexelDDXDDY(texelDDX, texelDDY);

    // Simulate bilinear filtering where we simply round to the nearest, rather then blending between neighboring lod steps.
    // Rounding seems to give closest results to hardware in our tests.
    // ceil() would be more conservative, and would guarentee less aliasing, but also would overblur more compared to round.
    // floor() would be least conservative and would guarentee less blurring, but would also alias more compared to round.
    lod = floor(lod + 0.5f);

    // Alternatively, we could perform exp2(lod) on xy rather than rcp(). Unsure which is faster in practice.
    texelSizeLod.zw = texelSize.zw * exp2(-lod);
    texelSizeLod.xy = rcp(texelSizeLod.zw);
}

float4 SampleTextureWithFilterModePoint(TEXTURE2D_PARAM(tex, samp), float2 uv, float4 texelSizeLod, float lod)
{
    float2 texel = floor(uv * texelSizeLod.zw) + 0.5f;
    float2 uvPoint = texel * texelSizeLod.xy;

    return SAMPLE_TEXTURE2D_LOD(tex, samp, uvPoint, lod);
}

float4 SampleTextureWithFilterModeN64(TEXTURE2D_PARAM(tex, samp), float2 uv, float4 texelSizeLod, float lod)
{
    float2 texel = uv * texelSizeLod.zw - 0.5f;

    // Calculate weights for the three points of our sample triangle.
    //
    //     0    1
    //   0 +----+
    //     |   /|
    //     |  / |
    //     | /  |
    //     |/   |
    //   1 +----+
    //
    float2 texelFrac = frac(texel);
    float3 weights = float3(
        abs(texelFrac.x + texelFrac.y - 1.0f),
        min(
            abs(texelFrac.xx - float2(0.0f, 1.0f)),
            abs(texelFrac.yy - float2(1.0f, 0.0f))
        )
    );

    // Calculate pixel center uvs for the three points of our sample triangle.
    float2 uvLL = (floor(texel + texelFrac.yx) + 0.5f) * texelSizeLod.xy;
    float2 uvLR = (floor(texel) + float2(1.5f, 0.5f)) * texelSizeLod.xy;
    float2 uvUL = (floor(texel) + float2(0.5f, 1.5f)) * texelSizeLod.xy;

#if 1
    // sample points
    // Rely on hardware filtering to blend samples A and B so that we only need to take 2 samples instead of 3.
    // This comes at the cost of some additional ALU and should be profiled.
    float weightAB = weights.x + weights.y;
    float weightABInverse = (weightAB > 1e-5f) ? rcp(weightAB) : 0.0f;
    float2 uvAB = (uvLL * weights.x + uvLR * weights.y) * weightABInverse;

    float4 AB = SAMPLE_TEXTURE2D_LOD(tex, samp, uvAB, lod);
    float4 C = SAMPLE_TEXTURE2D_LOD(tex, samp, uvUL, lod);

    // blend and return
    return AB * weightAB + C * weights.z;
#else

    // sample points
    float4 A = SAMPLE_TEXTURE2D_LOD(tex, samp, uvLL, lod);
    float4 B = SAMPLE_TEXTURE2D_LOD(tex, samp, uvLR, lod);
    float4 C = SAMPLE_TEXTURE2D_LOD(tex, samp, uvUL, lod);

    // blend and return
    return A * weights.x + B * weights.y + C * weights.z;
#endif
}

float2 ComputeRasterizationRTUV(float2 uv)
{
    return uv * _RasterizationRTScaledMaxSSAndUV.zw;
}

float2 ClampRasterizationRTUV(float2 uv)
{
    return clamp(uv, _RasterizationRTScaledClampBoundsUV.xy, _RasterizationRTScaledClampBoundsUV.zw);
}

bool ComputeRasterizationRTUVIsInBounds(float2 uv)
{
    return (min(uv.x, uv.y) >= 0.0f)
        && (uv.x <= _RasterizationRTScaledMaxSSAndUV.z)
        && (uv.y <= _RasterizationRTScaledMaxSSAndUV.w);
}

bool ComputeRasterizationRTPositionSSIsInBounds(float2 positionSS)
{
    return (min(positionSS.x, positionSS.y) >= 0.0f)
        && (positionSS.x <= _RasterizationRTScaledMaxSSAndUV.x)
        && (positionSS.y <= _RasterizationRTScaledMaxSSAndUV.y);
}

float2 ComputeRasterizationRTUVFlipVerticallyInBounds(float2 uv)
{
    return ComputeRasterizationRTUVIsInBounds(uv)
        ? float2(
            uv.x,
            _RasterizationRTScaledMaxSSAndUV.w - uv.y
        )
        : uv;
}

float2 ComputeRasterizationRTPositionSSFlipVerticallyInBounds(float2 positionSS)
{
    return ComputeRasterizationRTPositionSSIsInBounds(positionSS)
        ? float2(
            positionSS.x,
            _RasterizationRTScaledMaxSSAndUV.y - positionSS.y
        )
        : positionSS;
}

float2 ComputeRasterizationRTUVNormalizedFromAbsolute(float2 uvAbsolute)
{
    return uvAbsolute / _RasterizationRTScaledMaxSSAndUV.zw;
}

float2 ComputeRasterizationRTUVAbsoluteFromNormalized(float2 uvNormalized)
{
    return uvNormalized * _RasterizationRTScaledMaxSSAndUV.xy * _ScreenSizeRasterizationRTScaled.zw;
}

float3 EvaluateReflectionDirectionMode(int reflectionDirectionMode, float3 R, float3 N, float3 V)
{
    float3 reflectionDirection = R;
    if (reflectionDirectionMode == PSX_REFLECTION_DIRECTION_MODE_NORMAL)
    {
        reflectionDirection = N;
    }
    else if (reflectionDirectionMode == PSX_REFLECTION_DIRECTION_MODE_VIEW)
    {
        reflectionDirection = V;
    }
    return reflectionDirection;
}

#endif