Shader "Hidden/HauntedPS1/CRT"
{
    HLSLINCLUDE

    // #pragma target 4.5
    // #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #pragma prefer_hlslcc gles
    #pragma exclude_renderers d3d11_9x
    #pragma target 3.0

    // -------------------------------------
    // Global Keywords (set by render pipeline)
    #pragma multi_compile _OUTPUT_LDR _OUTPUT_HDR

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

    #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/ShaderLibrary/ShaderFunctions.hlsl"

    float4 _CameraAspectModeUVScaleBias;
    int _FlipY;
    int _UpscaleFilterMode;
    float4 _BlueNoiseSize;
    float4 _WhiteNoiseSize;
    float4 _CRTGrateMaskSize;
    int _CRTIsEnabled;
    float _CRTBloom;
    float2 _CRTGrateMaskScale;
    float _CRTScanlineSharpness;
    float _CRTImageSharpness;
    float2 _CRTBloomSharpness;
    float _CRTNoiseIntensity;
    float _CRTNoiseSaturation;
    float2 _CRTGrateMaskIntensityMinMax;
    float2 _CRTBarrelDistortion;
    float _CRTVignetteSquared;
    TEXTURE2D(_FrameBufferTexture);
    TEXTURE2D(_WhiteNoiseTexture);
    TEXTURE2D(_BlueNoiseTexture);
    TEXTURE2D(_CRTGrateMaskTexture);

    // Emulated input resolution.
#if 1
    // Fix resolution to set amount.
    #define res (_ScreenSizeRasterizationRTScaled.xy)
    #define resInverse (_ScreenSizeRasterizationRTScaled.zw)
#else
    // Optimize for resize.
    #define res ((_ScreenSize.xy / 6.0f * _CRTGrateMaskScale.y))
    #define resInverse (1.0 / (_ScreenSize.xy / 6.0f * _CRTGrateMaskScale.y))
#endif

    float2 ComputeUVTransformFromUpscaleFilter(float2 uv)
    {
        // No uv modification necessary for bilinear.
        // All other upscalers use the same uv modification routine, with different constants.
        if (_UpscaleFilterMode != PSX_UPSCALE_FILTER_MODE_BILINEAR)
        {
            float2 upscaleFactor = float2(1.0, 1.0);
            float2 subpixelOffset = float2(0.5, 0.5);
            if (_UpscaleFilterMode == PSX_UPSCALE_FILTER_MODE_POINT)
            {
                upscaleFactor = float2(1.0, 1.0);
                subpixelOffset = float2(0.5, 0.5);
            }
            else if (_UpscaleFilterMode == PSX_UPSCALE_FILTER_MODE_N64_DOUBLER_X)
            {
                upscaleFactor = float2(2.0, 1.0);
                subpixelOffset = float2(0.0, 0.5);
            }
            else if (_UpscaleFilterMode == PSX_UPSCALE_FILTER_MODE_N64_DOUBLER_Y)
            {
                upscaleFactor = float2(1.0, 2.0);
                subpixelOffset = float2(0.5, 0.0);
            }
            else if (_UpscaleFilterMode == PSX_UPSCALE_FILTER_MODE_N64_DOUBLER_XY)
            {
                upscaleFactor = float2(2.0, 2.0);
                subpixelOffset = float2(0.0, 0.0);
            }
            else if (_UpscaleFilterMode == PSX_UPSCALE_FILTER_MODE_N64_BLUR_BOX_2_X_2)
            {
                upscaleFactor = float2(1.0, 1.0);
                subpixelOffset = float2(0.0, 0.0);
            }
            float2 upscaleFactorInverse = 1.0 / upscaleFactor;
            float2 positionUpscaledSS = (clamp(floor(uv.xy * _ScreenSizeRasterization.xy * upscaleFactor), 0.0, (upscaleFactor * _ScreenSizeRasterization.xy) - 1.0));

            uv = (positionUpscaledSS * upscaleFactorInverse + subpixelOffset) * _ScreenSizeRasterization.zw;
        }

        return uv;
    }

    float2 ComputeRasterizationResolutionScaleFromUpscaleFilter()
    {
        float2 upscaleFactor = float2(1.0, 1.0);
        if (_UpscaleFilterMode == PSX_UPSCALE_FILTER_MODE_POINT)
        {
            upscaleFactor = float2(1.0, 1.0);
        }
        else if (_UpscaleFilterMode == PSX_UPSCALE_FILTER_MODE_N64_DOUBLER_X)
        {
            upscaleFactor = float2(2.0, 1.0);
        }
        else if (_UpscaleFilterMode == PSX_UPSCALE_FILTER_MODE_N64_DOUBLER_Y)
        {
            upscaleFactor = float2(1.0, 2.0);
        }
        else if (_UpscaleFilterMode == PSX_UPSCALE_FILTER_MODE_N64_DOUBLER_XY)
        {
            upscaleFactor = float2(2.0, 2.0);
        }
        else if (_UpscaleFilterMode == PSX_UPSCALE_FILTER_MODE_N64_BLUR_BOX_2_X_2)
        {
            upscaleFactor = float2(0.5, 0.5);
        }
        else if (_UpscaleFilterMode != PSX_UPSCALE_FILTER_MODE_BILINEAR)
        {
            upscaleFactor = float2(1.0, 1.0);
        }
        return upscaleFactor;
    }

    float EvaluatePBRVignette(float distanceFromCenterSquaredNDC, float vignetteAtOffsetOneSquared)
    {
        // Use cosine based falloff
        // This simulates the energy loss when firing the electron beam to the corner of the screen, where the distance to the screen is greatest
        // compared to firing the electron beam to the center of the screen, where the distance to the center of the screen is least.
        // To make this cosine falloff artist friendly, we parameterize it by the desired amount of vignette aka energy loss at a NDC distance of 1 (side of screen)
        // In order to parameterize it this way, we solve:
        //
        // vignette = cos(angle)
        // vignetteMax = cos(angleMax)
        // acos(vignetteMax) = angleMax
        //          +
        //         /| <--- angleMax
        //        / |
        //       /  |
        //      /   |
        //     /    |
        //    /     |   TOA = tan(angleMax) == opposite / adjacent where opposite is 1.0 = (1.0 / tan(angleMax) == adjacent)
        //   /      |
        //  /       |
        // +--------+ [0, 1] NDC space

        // adjacent = 1.0 / tan(angleMax) = 1.0 / tan(acos(vignetteMax))
        // opposite = offsetNDC [0, 1]
        // vignette = cos(angleCurrent)
        // angleCurrent = atan(offsetNDC / adjacent)
        // angleCurrent = atan(offsetNDC / (1.0 / tan(acos(vignetteMax))))
        // angleCurrent = atan(offsetNDC * tan(acos(vignetteMax)))
        // vignette = cos(atan(offsetNDC * tan(acos(vignetteMax))))
        // vignette = rsqrt((offsetNDC * offsetNDC * (1.0 - vignetteMax * vignetteMax)) / (vignetteMax * vignetteMax) + 1.0)
        
        return rsqrt((-vignetteAtOffsetOneSquared * distanceFromCenterSquaredNDC + distanceFromCenterSquaredNDC) / vignetteAtOffsetOneSquared + 1.0);
    }

    float3 FetchNoise(float2 p, TEXTURE2D(noiseTextureSampler))
    {
        float2 uv = float2(1.0f, cos(_Time.y)) * _Time.y * 8.0f + p;

        // Unfortunately, WebGL builds will ignore the sampler state passed into SAMPLE_TEXTURE2D functions, so we cannot force a texture to repeat
        // that was not specified to repeat in it's own settings via the sampler.
        // Instead, we just spend a frac() instruction to force repeat in software.
        uv = frac(uv);

        // Noise texture is treated as data texture - noise is expected to be distributed in linear space, not perceptual / gamma space.
        float3 s = SAMPLE_TEXTURE2D_LOD(noiseTextureSampler, s_linear_repeat_sampler, uv, 0).rgb;
        s = s * 2.0 - 1.0;
        s.yz *= _CRTNoiseSaturation;
        s *= _CRTNoiseIntensity;
        return s;
    }

    float4 CompositeSignalAndNoise(TEXTURE2D(noiseTextureSampler), float2 posNoiseSignal, float2 posNoiseCRT, float2 off, float4 c)
    {
    #if 0
        float3 steps = float3(64.0, 32.0, 32.0);
        float3 cyuv = floor(FCCYIQFromSRGB(c.rgb) * steps + 0.5) / steps;
    #else
        float3 cyuv = FCCYIQFromSRGB(c.rgb);
    #endif
        float3 noiseSignalYUV = 0.0;//FetchNoise(posNoiseSignal, noiseTextureSampler);
        float3 noiseCRTYUV = FetchNoise(posNoiseCRT, noiseTextureSampler);

        return float4(saturate(SRGBFromFCCYIQ((noiseSignalYUV + noiseCRTYUV) * c.a + cyuv)), c.a);
    }

    float4 FetchFrameBuffer(float2 uv)
    {
        float4 color = SAMPLE_TEXTURE2D_LOD(_FrameBufferTexture, s_linear_clamp_sampler, uv, 0);

    #if defined(_OUTPUT_HDR)

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
        color.rgb = ComputeFramebufferDiscretization(color.rgb, uv * _ScreenSize.xy, _PrecisionColor.rgb, _PrecisionColorInverse.rgb);
    #endif

        return color;
    }

    // Nearest emulated sample given floating point position and texel offset.
    // Also zero's off screen.
    float4 Fetch(float2 pos, float2 posUnsnappedForNoise, float2 off, TEXTURE2D(noiseTextureSampler), float4 noiseTextureSize, float2 upscaleScaleInverse)
    {
        float2 posNoiseSignal = floor(posUnsnappedForNoise * res + off) * noiseTextureSize.zw;
        float2 posNoiseCRT = floor(posUnsnappedForNoise * _ScreenSize.xy + off * res * _ScreenSize.zw) * noiseTextureSize.zw;
        
        bool isInBounds = ComputeRasterizationRTUVIsInBounds(pos);
        pos = off * resInverse * upscaleScaleInverse + pos;
        pos = ComputeUVTransformFromUpscaleFilter(pos);
        if (!isInBounds) { return float4(0.0, 0.0, 0.0, 0.0f); }
        float4 value = CompositeSignalAndNoise(noiseTextureSampler, posNoiseSignal, posNoiseCRT, off, FetchFrameBuffer(pos));
        value.rgb = SRGBToLinear(value.rgb);
        return value;
    }

    // Lanczos filter will be used for simulating overshoot ringing.
    // Waiting until the big cleanup pass on this post process.
    float FilterWeightLanczos(const in float x, const in float widthInverse)
    {
        float c1 = PI * x;
        float c2 = widthInverse * c1;
        return (c2 > PI)
            ? 0.0f
            : (x < 1e-5f)
                ? 1.0
                : (sin(c2) * sin(c1) / (c2 * c1));
    }

    float4 Tri(float2 pos)
    {
        float2 upscaleFactor = ComputeRasterizationResolutionScaleFromUpscaleFilter();
        float2 upscaleFactorInverse = 1.0 / upscaleFactor;

        // Take 4 horizontal taps across each of our 2 nearest scanlines, for 8 taps total.
        float2 positionRasterizedImagePixels = pos * res * upscaleFactor;
        float2 positionRasterizedImageStartPixels = float2(
            floor(positionRasterizedImagePixels.x - 1.5) + 0.5,
            floor(positionRasterizedImagePixels.y - 0.5) + 0.5
        );
        float2 positionRasterizedImageStartPixelsUnsnapped = float2(
            positionRasterizedImagePixels.x - 1.5,
            positionRasterizedImagePixels.y - 0.5
        );
        float2 positionRasterizedImageStartUV = positionRasterizedImageStartPixels * resInverse * upscaleFactorInverse;
        float2 positionRasterizedImageStartUnsnappedUV = positionRasterizedImageStartPixelsUnsnapped * resInverse * upscaleFactorInverse;

        float4 sampleSouth0 = Fetch(positionRasterizedImageStartUV, positionRasterizedImageStartUnsnappedUV, float2(0, 0), _WhiteNoiseTexture, _WhiteNoiseSize, upscaleFactorInverse);
        float4 sampleSouth1 = Fetch(positionRasterizedImageStartUV, positionRasterizedImageStartUnsnappedUV, float2(1, 0), _WhiteNoiseTexture, _WhiteNoiseSize, upscaleFactorInverse);
        float4 sampleSouth2 = Fetch(positionRasterizedImageStartUV, positionRasterizedImageStartUnsnappedUV, float2(2, 0), _WhiteNoiseTexture, _WhiteNoiseSize, upscaleFactorInverse);
        float4 sampleSouth3 = Fetch(positionRasterizedImageStartUV, positionRasterizedImageStartUnsnappedUV, float2(3, 0), _WhiteNoiseTexture, _WhiteNoiseSize, upscaleFactorInverse);
        float4 sampleNorth0 = Fetch(positionRasterizedImageStartUV, positionRasterizedImageStartUnsnappedUV, float2(0, 1), _WhiteNoiseTexture, _WhiteNoiseSize, upscaleFactorInverse);
        float4 sampleNorth1 = Fetch(positionRasterizedImageStartUV, positionRasterizedImageStartUnsnappedUV, float2(1, 1), _WhiteNoiseTexture, _WhiteNoiseSize, upscaleFactorInverse);
        float4 sampleNorth2 = Fetch(positionRasterizedImageStartUV, positionRasterizedImageStartUnsnappedUV, float2(2, 1), _WhiteNoiseTexture, _WhiteNoiseSize, upscaleFactorInverse);
        float4 sampleNorth3 = Fetch(positionRasterizedImageStartUV, positionRasterizedImageStartUnsnappedUV, float2(3, 1), _WhiteNoiseTexture, _WhiteNoiseSize, upscaleFactorInverse);

        float scanlineOffsetSouth = positionRasterizedImagePixels.y - positionRasterizedImageStartPixels.y;
        float scanlineWeightSouth = cos(min(0.5, scanlineOffsetSouth * _CRTScanlineSharpness) * 2.0 * PI) * 0.5 + 0.5;
        float scanlineWeightNorth = cos(min(0.5,(-scanlineOffsetSouth) * _CRTScanlineSharpness + _CRTScanlineSharpness) * 2.0 * PI) * 0.5 + 0.5;

        float offsetX0 = positionRasterizedImagePixels.x - positionRasterizedImageStartPixels.x;
        float offsetX1 = offsetX0 - 1.0;
        float offsetX2 = offsetX0 - 2.0;
        float offsetX3 = offsetX0 - 3.0;

        float weightX0 = exp2(_CRTImageSharpness * offsetX0 * offsetX0);
        float weightX1 = exp2(_CRTImageSharpness * offsetX1 * offsetX1);
        float weightX2 = exp2(_CRTImageSharpness * offsetX2 * offsetX2);
        float weightX3 = exp2(_CRTImageSharpness * offsetX3 * offsetX3);
        float weightXNormalization = 1.0 / (weightX0 + weightX1 + weightX2 + weightX3);

        float4 accumulatedColor =
            (sampleSouth0 * weightX0 + sampleSouth1 * weightX1 + sampleSouth2 * weightX2 + sampleSouth3 * weightX3) * scanlineWeightSouth * weightXNormalization
            + (sampleNorth0 * weightX0 + sampleNorth1 * weightX1 + sampleNorth2 * weightX2 + sampleNorth3 * weightX3) * scanlineWeightNorth * weightXNormalization;
        
        return accumulatedColor;
    }

    // Distortion of scanlines, and end of screen alpha.
    float2 Warp(float2 pos)
    {
        pos = pos * 2.0 - 1.0;
        pos *= float2(1.0 + (pos.y * pos.y) * _CRTBarrelDistortion.x, 1.0 + (pos.x * pos.x) * _CRTBarrelDistortion.y);
        return pos * 0.5 + 0.5;
    }

    // Very compressed TV style mask.
    float3 CRTMaskCompressedTV(float2 pos)
    {
        float line0 = _CRTGrateMaskIntensityMinMax.y;
        float odd=0.0;
        if(frac(pos.x/6.0)<0.5)odd=1.0;
        if(frac((pos.y+odd)/2.0)<0.5)line0=_CRTGrateMaskIntensityMinMax.x;  
        pos.x=frac(pos.x/3.0);
        float3 mask=float3(_CRTGrateMaskIntensityMinMax.x,_CRTGrateMaskIntensityMinMax.x,_CRTGrateMaskIntensityMinMax.x);
        if(pos.x<0.333)mask.r=_CRTGrateMaskIntensityMinMax.y;
        else if(pos.x<0.666)mask.g=_CRTGrateMaskIntensityMinMax.y;
        else mask.b=_CRTGrateMaskIntensityMinMax.y;
        mask*=line0;
        return mask;
    }

    // Aperture-grille.
    float3 CRTMaskApertureGrill(float2 pos)
    {
        pos.x=frac(pos.x/3.0);
        float3 mask=float3(_CRTGrateMaskIntensityMinMax.x,_CRTGrateMaskIntensityMinMax.x,_CRTGrateMaskIntensityMinMax.x);
        if(pos.x<0.333)mask.r=lerp(_CRTGrateMaskIntensityMinMax.y, _CRTGrateMaskIntensityMinMax.x, abs(pos.x - 0.5 * 0.333) / 0.333);
        else if(pos.x<0.666)mask.g=lerp(_CRTGrateMaskIntensityMinMax.y, _CRTGrateMaskIntensityMinMax.x, abs(pos.x - 1.5 * 0.333) / 0.333);
        else mask.b=lerp(_CRTGrateMaskIntensityMinMax.y, _CRTGrateMaskIntensityMinMax.x, abs(pos.x - 2.5 * 0.333) / 0.333);
        return mask;
    }        

    // VGA style mask.
    float3 CRTMaskVGA(float2 pos)
    {
        pos.xy=floor(pos.xy*float2(1.0,0.5));
        pos.x+=pos.y*3.0;
        float3 mask=float3(_CRTGrateMaskIntensityMinMax.x,_CRTGrateMaskIntensityMinMax.x,_CRTGrateMaskIntensityMinMax.x);
        pos.x=frac(pos.x/6.0);
        if(pos.x<0.333)mask.r=_CRTGrateMaskIntensityMinMax.y;
        else if(pos.x<0.666)mask.g=_CRTGrateMaskIntensityMinMax.y;
        else mask.b=_CRTGrateMaskIntensityMinMax.y;
        return mask;
    }  

    // Stretched VGA style mask.
    float3 CRTMaskVGAStretched(float2 pos)
    {
        pos.x+=pos.y*3.0;
        float3 mask=float3(_CRTGrateMaskIntensityMinMax.x,_CRTGrateMaskIntensityMinMax.x,_CRTGrateMaskIntensityMinMax.x);
        pos.x=frac(pos.x/6.0);
        if(pos.x<0.333)mask.r=_CRTGrateMaskIntensityMinMax.y;
        else if(pos.x<0.666)mask.g=_CRTGrateMaskIntensityMinMax.y;
        else mask.b=_CRTGrateMaskIntensityMinMax.y;
        return mask;
    }

    float3 CRTMaskTexture(float2 pos)
    {
        float2 uv = frac(pos * _CRTGrateMaskSize.zw);
        return SAMPLE_TEXTURE2D_LOD(_CRTGrateMaskTexture, s_linear_repeat_sampler, uv, 0).rgb
            * (_CRTGrateMaskIntensityMinMax.y - _CRTGrateMaskIntensityMinMax.x) + _CRTGrateMaskIntensityMinMax.x;
    }

    float3 CRTMask(float2 pos)
    {
    #if defined(_CRT_MASK_COMPRESSED_TV)
        return CRTMaskCompressedTV(pos);
    #elif defined(_CRT_MASK_APERTURE_GRILL)
        return CRTMaskApertureGrill(pos);
    #elif defined(_CRT_MASK_VGA)
        return CRTMaskVGA(pos);
    #elif defined(_CRT_MASK_VGA_STRETCHED)
        return CRTMaskVGAStretched(pos);
    #elif defined(_CRT_MASK_TEXTURE)
        return CRTMaskTexture(pos);
    #elif defined(_CRT_MASK_DISABLED)
        return 1.0;
    #else
        #error "Error: CRT.shader: Encountered undefined CRTMask.";
        return 1.0;
    #endif
    }

    // Entry.
    float4 EvaluateCRT(float2 framebufferUVAbsolute, float2 positionScreenSS)
    {
        float4 crt = 0.0;

        // Carefully handle potentially scaled RT here:
        // Need the normalized aka viewport bounds UV here for converting the warp.
        float2 framebufferUVNormalized = ComputeRasterizationRTUVNormalizedFromAbsolute(framebufferUVAbsolute);
        float2 crtUVNormalized = Warp(framebufferUVNormalized);
        float2 crtUVAbsolute = ComputeRasterizationRTUVAbsoluteFromNormalized(crtUVNormalized);

        // Note: if we use the pure NDC coordinates, our vignette will be an ellipse, since we do not take into account physical distance differences from the aspect ratio.
        // Apply aspect ratio to get circular, physically based vignette:
        float2 crtNDCNormalized = crtUVNormalized * 2.0 - 1.0;
        if (_ScreenSize.x > _ScreenSize.y)
        {
            // X axis is max:
            crtNDCNormalized.y *= _ScreenSize.y / _ScreenSize.x;
        }
        else
        {
            // Y axis is max:
            crtNDCNormalized.x *= _ScreenSize.x / _ScreenSize.y;
        }
        float distanceFromCenterSquaredNDC = dot(crtNDCNormalized, crtNDCNormalized);
        float vignette = EvaluatePBRVignette(distanceFromCenterSquaredNDC, _CRTVignetteSquared);

        crt = Tri(crtUVAbsolute) * float4(CRTMask(positionScreenSS * _CRTGrateMaskScale.y), 1.0f);

        crt.rgb *= vignette;

        return float4(crt.rgb, crt.a);
    }

    struct Attributes
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uvFramebuffer : TEXCOORD0;
        float2 uvScreen : TEXCOORD1;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vertex(Attributes input)
    {
        Varyings output;
        output.positionCS = input.vertex;

        output.uvScreen = input.uv;
        output.uvFramebuffer = input.uv * _CameraAspectModeUVScaleBias.xy + _CameraAspectModeUVScaleBias.zw;

        return output;
    }

    bool ShouldFlipY()
    {
        #if UNITY_UV_STARTS_AT_TOP
            return (_FlipY == 1) ? true : false;
        #else
            return (_FlipY == 1) ? false : true;
        #endif

    }

    float4 Fragment(Varyings input) : SV_Target0
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 positionFramebufferNDC = input.uvFramebuffer;
        float2 positionScreenNDC = input.uvScreen;
        
        if (ShouldFlipY())
        {
            positionFramebufferNDC = ComputeRasterizationRTUVFlipVerticallyInBounds(positionFramebufferNDC);
            positionScreenNDC.y = 1.0 - positionScreenNDC.y;
        }

        float2 positionScreenSS = positionScreenNDC * _ScreenSize.xy;

        if (!_IsPSXQualityEnabled || !_CRTIsEnabled)
        {
            bool isInBounds = ComputeRasterizationRTUVIsInBounds(positionFramebufferNDC.xy);
            positionFramebufferNDC = ComputeUVTransformFromUpscaleFilter(positionFramebufferNDC);
            return isInBounds
                ? SAMPLE_TEXTURE2D_LOD(_FrameBufferTexture, s_linear_clamp_sampler, positionFramebufferNDC.xy, 0)
                : float4(0.0, 0.0, 0.0, 1.0);
        }

        float4 outColor = EvaluateCRT(positionFramebufferNDC, positionScreenSS);
        outColor.rgb = saturate(outColor.rgb);
        outColor.rgb = LinearToSRGB(outColor.rgb);

        return float4(outColor.rgb, outColor.a);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "PSXRenderPipeline" }

        Pass
        {
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #pragma multi_compile _CRT_MASK_COMPRESSED_TV _CRT_MASK_APERTURE_GRILL _CRT_MASK_VGA _CRT_MASK_VGA_STRETCHED _CRT_MASK_TEXTURE _CRT_MASK_DISABLED

            #pragma vertex Vertex
            #pragma fragment Fragment

            ENDHLSL
        }
    }
}
