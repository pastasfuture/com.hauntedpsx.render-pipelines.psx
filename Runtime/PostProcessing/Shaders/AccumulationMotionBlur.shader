Shader "Hidden/HauntedPS1/AccumulationMotionBlur"
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

    float _RasterizationHistoryWeight;
    float _RasterizationHistoryCompositeDither;
    float4 _AccumulationMotionBlurParameters;
    TEXTURE2D(_RasterizationHistoryRT);

    struct Attributes
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vertex(Attributes input)
    {
        Varyings output;
        output.positionCS = input.vertex;
        output.positionCS.y *= -_ProjectionParams.x;

        output.uv = input.uv;

        return output;
    }

    struct AccumulationMotionBlurData
    {
        float blurSignedDistancePixels;
        float vignette;
        float dither;
        float blurAnisotropy;
    };

    AccumulationMotionBlurData GetAccumulationMotionBlurData()
    {
        AccumulationMotionBlurData data;
        data.blurSignedDistancePixels = _AccumulationMotionBlurParameters.x;
        data.vignette = _AccumulationMotionBlurParameters.y; 
        data.dither = _AccumulationMotionBlurParameters.z;
        data.blurAnisotropy = _AccumulationMotionBlurParameters.w;
        return data;
    }

    float ComputeVignetteWeight(AccumulationMotionBlurData data, float2 historyUV)
    {
        float2 direction = historyUV * 2.0 - 1.0;
        float vignette = smoothstep(0.0, 1.0, dot(direction, direction));
        float weight = saturate(vignette);
        weight = (data.vignette >= 0.0) ? weight : (1.0 - weight);
        weight = lerp(1.0f, weight, abs(data.vignette));

        return weight;
    }

    float2 ComputeHistoryUV(AccumulationMotionBlurData data, float2 historyUV, float zoomDither, float vignetteWeight)
    {
        float2 blurDirection = historyUV * 2.0 - 1.0;
        blurDirection = dot(blurDirection, blurDirection) > 1e-5f ? normalize(blurDirection) : float2(1.0, 1.0);
        blurDirection.y *= 1.0f - saturate(data.blurAnisotropy);
        blurDirection.x *= 1.0f - saturate(-data.blurAnisotropy);
        blurDirection *= vignetteWeight;
        float blurSignedDistancePixels = data.blurSignedDistancePixels * lerp(1.0, zoomDither, data.dither);
        historyUV = ComputeRasterizationRTUV(historyUV);
        historyUV -= blurDirection * _ScreenSizeRasterization.zw * blurSignedDistancePixels;
        historyUV = ClampRasterizationRTUV(historyUV);
        return historyUV;
    }

    float4 Fragment(Varyings input) : SV_Target0
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 positionSS = input.positionCS.xy;
        uint2 framebufferDitherTexelCoord = (uint2)floor(frac(positionSS * _FramebufferDitherScaleAndInverse.yy * _FramebufferDitherSize.zw) * _FramebufferDitherSize.xy);
        float framebufferDither = LOAD_TEXTURE2D_LOD(_FramebufferDitherTexture, framebufferDitherTexelCoord, 0).a;
        float historyAlphaDither = NoiseDitherRemapTriangularDistribution(framebufferDither);
        historyAlphaDither = lerp(0.5, framebufferDither, _RasterizationHistoryCompositeDither);
        
        AccumulationMotionBlurData data = GetAccumulationMotionBlurData();

        float vignetteWeight = ComputeVignetteWeight(data, input.uv);
        float2 historyUV = ComputeHistoryUV(data, input.uv, framebufferDither, vignetteWeight);
        float4 history = SAMPLE_TEXTURE2D_LOD(_RasterizationHistoryRT, s_linear_clamp_sampler, historyUV, 0);
        history.a *= _RasterizationHistoryWeight * vignetteWeight;

        float precisionHistory = 255.0f;
        float precisionHistoryInverse = 1.0f / precisionHistory;
        history.a = saturate(floor(history.a * precisionHistory + historyAlphaDither) * precisionHistoryInverse);

        return history;
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "PSXRenderPipeline" }

        Pass
        {
            Cull Off ZWrite Off ZTest Always

            // Use "Over" blend mode to blend history with current frame.
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #pragma vertex Vertex
            #pragma fragment Fragment

            ENDHLSL
        }
    }
}
