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
#else
    // Optimize for resize.
    #define res ((_ScreenSize.xy / 6.0f * _CRTGrateMaskScale.y))
#endif

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
        float3 noiseSignalYUV = FetchNoise(posNoiseSignal, noiseTextureSampler);
        float3 noiseCRTYUV = FetchNoise(posNoiseCRT, noiseTextureSampler);

        return float4(saturate(SRGBFromFCCYIQ((noiseSignalYUV + noiseCRTYUV) * c.a + cyuv)), c.a);
    }

    float4 FetchFrameBuffer(float2 uv)
    {
        float4 color = SAMPLE_TEXTURE2D_LOD(_FrameBufferTexture, s_point_clamp_sampler, uv, 0);

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
    float4 Fetch(float2 pos, float2 off, TEXTURE2D(noiseTextureSampler), float4 noiseTextureSize)
    {
        float2 posNoiseSignal = floor(pos * res + off) * noiseTextureSize.zw;
        float2 posNoiseCRT = floor(pos * _ScreenSize.xy + off * res * _ScreenSize.zw) * noiseTextureSize.zw;
        pos = (floor(pos * res + off) + 0.5) / res;
        if (!ComputeRasterizationRTUVIsInBounds(pos)) { return float4(0.0, 0.0, 0.0, 0.0f); }
        float4 value = CompositeSignalAndNoise(noiseTextureSampler, posNoiseSignal, posNoiseCRT, off, FetchFrameBuffer(pos));
        value.rgb = SRGBToLinear(value.rgb);
        return value;
    }

    // Distance in emulated pixels to nearest texel.
    float2 Dist(float2 pos)
    {
        pos = pos * res;
        return -((pos - floor(pos)) - 0.5);
    }

    // 1D Gaussian.
    float Gaus(float pos, float sharpness)
    {
        return exp2(sharpness * pos * pos);
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

    // 3-tap Gaussian filter along horz line.
    float4 Horz3(float2 pos,float off)
    {
        float4 b=Fetch(pos,float2(-1.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 c=Fetch(pos,float2( 0.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 d=Fetch(pos,float2( 1.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float dst=Dist(pos).x;

        // Use gaussian as windowing function for lanczos filter.
        // TODO: Use more efficient / less agressive windowing function.
        float scale=_CRTImageSharpness;
        float wb = Gaus(dst-1.0,scale);
        float wc = Gaus(dst+0.0,scale);
        float wd = Gaus(dst+1.0,scale);

        // Return filtered sample.
        return (b*wb+c*wc+d*wd)/(wb+wc+wd);
    }

    // 5-tap Gaussian filter along horz line.
    float4 Horz5(float2 pos,float off)
    {
        float4 a=Fetch(pos,float2(-2.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 b=Fetch(pos,float2(-1.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 c=Fetch(pos,float2( 0.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 d=Fetch(pos,float2( 1.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 e=Fetch(pos,float2( 2.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float dst=Dist(pos).x;

        // Use gaussian as windowing function for lanczos filter.
        // TODO: Use more efficient / less agressive windowing function.
        float scale=_CRTImageSharpness;
        float wa = Gaus(dst-2.0,scale);
        float wb = Gaus(dst-1.0,scale);
        float wc = Gaus(dst+0.0,scale);
        float wd = Gaus(dst+1.0,scale);
        float we = Gaus(dst+2.0,scale);

        // Return filtered sample.
        return (a*wa+b*wb+c*wc+d*wd+e*we)/(wa+wb+wc+wd+we);
    }

    // 7-tap Gaussian filter along horz line.
    float4 Horz7(float2 pos,float off)
    {
        float4 a=Fetch(pos,float2(-3.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 b=Fetch(pos,float2(-2.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 c=Fetch(pos,float2(-1.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 d=Fetch(pos,float2( 0.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 e=Fetch(pos,float2( 1.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 f=Fetch(pos,float2( 2.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float4 g=Fetch(pos,float2( 3.0,off), _WhiteNoiseTexture, _WhiteNoiseSize);
        float dst=Dist(pos).x;

        // Convert distance to weight.
        float scale=_CRTBloomSharpness.x;

        // Use gaussian as windowing function for lanczos filter.
        // TODO: Use more efficient / less agressive windowing function.
        float wa = Gaus(dst-3.0,scale);
        float wb = Gaus(dst-2.0,scale);
        float wc = Gaus(dst-1.0,scale);
        float wd = Gaus(dst+0.0,scale);
        float we = Gaus(dst+1.0,scale);
        float wf = Gaus(dst+2.0,scale);
        float wg = Gaus(dst+3.0,scale);

        // Return filtered sample.
        return (a*wa+b*wb+c*wc+d*wd+e*we+f*wf+g*wg)/(wa+wb+wc+wd+we+wf+wg);
    }

    // Return scanline weight.
    float Scan(float2 pos,float off)
    {
        float dst=Dist(pos).y;
        return Gaus(dst+off,_CRTScanlineSharpness);
    }

    // Return scanline weight for bloom.
    float BloomScan(float2 pos,float off)
    {
        float dst=Dist(pos).y;
        return Gaus(dst+off,_CRTBloomSharpness.y);
    }

    // Allow nearest three lines to effect pixel.
    float4 Tri(float2 pos)
    {
        float2 positionPixelsMax = pos * res + float2(5.0, 1.0);
        float2 positionPixelsMin = pos * res - float2(5.0, 1.0);

        if (positionPixelsMax.x <= 0.0 || positionPixelsMax.y <= 0.0
            || positionPixelsMin.x >= res.x || positionPixelsMin.y >= res.y)
        {
            return 0.0;
        }

        float4 a=Horz3(pos,-1.0);
        float4 b=Horz5(pos, 0.0);
        float4 c=Horz3(pos, 1.0);
        float wa=Scan(pos,-1.0);
        float wb=Scan(pos, 0.0);
        float wc=Scan(pos, 1.0);
        return a*wa+b*wb+c*wc;
    }

    // Small bloom.
    float4 Bloom(float2 pos)
    {
        float2 positionPixelsMax = pos * res + float2(7.0, 2.0);
        float2 positionPixelsMin = pos * res - float2(7.0, 2.0);

        if (positionPixelsMax.x <= 0.0 || positionPixelsMax.y <= 0.0
            || positionPixelsMin.x >= res.x || positionPixelsMin.y >= res.y)
        {
            return 0.0;
        }

        float4 a=Horz5(pos,-2.0);
        float4 b=Horz7(pos,-1.0);
        float4 c=Horz7(pos, 0.0);
        float4 d=Horz7(pos, 1.0);
        float4 e=Horz5(pos, 2.0);
        float wa=BloomScan(pos,-2.0);
        float wb=BloomScan(pos,-1.0);
        float wc=BloomScan(pos, 0.0);
        float wd=BloomScan(pos, 1.0);
        float we=BloomScan(pos, 2.0);
        return (a*wa+b*wb+c*wc+d*wd+e*we) / (wa + wb + wc + wd + we);
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

        crt = float4(CRTMask(positionScreenSS * _CRTGrateMaskScale.y), 1.0f) * Tri(crtUVAbsolute);

        #if 1
        // Energy conserving normalized bloom.
        crt = lerp(crt, Bloom(crtUVAbsolute), _CRTBloom);    
        #else
        // Additive bloom.
        crt += Bloom(crtUVAbsolute) * _CRTBloom;
        crt.a = saturate(crt.a);   
        #endif

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
            return ComputeRasterizationRTUVIsInBounds(positionFramebufferNDC.xy)
                ? SAMPLE_TEXTURE2D_LOD(_FrameBufferTexture, s_point_clamp_sampler, positionFramebufferNDC.xy, 0)
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
