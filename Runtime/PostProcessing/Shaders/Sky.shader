Shader "Hidden/HauntedPS1/Sky"
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

    float4 _SkyColor;
    float _SkyFramebufferDitherWeight;
    float4x4 _SkyPixelCoordToWorldSpaceViewDirectionMatrix;

    TEXTURECUBE(_SkyboxTextureCube);
    SAMPLER(sampler_SkyboxTextureCube);

    float _SkyTiledLayersSkyHeightScaleInverse;
    float _SkyTiledLayersSkyHorizonOffset;
    float4 _SkyTiledLayersSkyColorLayer0;
    TEXTURE2D(_SkyTiledLayersSkyTextureLayer0);
    SAMPLER(sampler_SkyTiledLayersSkyTextureLayer0);
    float4 _SkyTiledLayersSkyTextureLayer0_TexelSize;
    float4 _SkyTiledLayersSkyTextureScaleOffsetLayer0;
    float _SkyTiledLayersSkyRotationLayer0;
    float2 _SkyTiledLayersSkyScrollScaleLayer0;
    float _SkyTiledLayersSkyScrollRotationLayer0;
    float4 _SkyTiledLayersSkyColorLayer1;
    TEXTURE2D(_SkyTiledLayersSkyTextureLayer1);
    SAMPLER(sampler_SkyTiledLayersSkyTextureLayer1);
    float4 _SkyTiledLayersSkyTextureLayer1_TexelSize;
    float4 _SkyTiledLayersSkyTextureScaleOffsetLayer1;
    float _SkyTiledLayersSkyRotationLayer1;
    float2 _SkyTiledLayersSkyScrollScaleLayer1;
    float _SkyTiledLayersSkyScrollRotationLayer1;

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

    float3 ComputeViewDirectionWSFromPositionSS(float2 positionSS)
    {
        float3 viewDirectionWS = mul(float4(positionSS, 1.0f, 1.0f), _SkyPixelCoordToWorldSpaceViewDirectionMatrix).xyz;
        viewDirectionWS = -normalize(viewDirectionWS);
        return viewDirectionWS;
    }

    // Copied from MaterialFunctions.hlsl
    // TODO: Create an include that can be added to materials, and fullscreen passes like this, without needing to pull in all material specific functions.
    void ComputeLODAndTexelSizeMaybeCallDDX(out float4 texelSizeLod, out float lod, float2 uv, float4 texelSize)
    {
    #if defined(_TEXTURE_FILTER_MODE_N64_MIPMAPS) || defined(_TEXTURE_FILTER_MODE_POINT_MIPMAPS)
        ComputeLODAndTexelSizeFromEvaluateDDXDDY(texelSizeLod, lod, uv, texelSize);
    #else // defined(_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS)
        // No modifications.
        texelSizeLod = texelSize;
        lod = 0.0f;
    #endif
    }

    float4 SampleTextureWithFilterMode(TEXTURE2D_PARAM(tex, samp), float2 uv, float4 texelSizeLod, float lod)
    {
    #if defined(_TEXTURE_FILTER_MODE_POINT) || defined(_TEXTURE_FILTER_MODE_POINT_MIPMAPS) 
        return SampleTextureWithFilterModePoint(TEXTURE2D_ARGS(tex, samp), uv, texelSizeLod, lod);
    #elif defined(_TEXTURE_FILTER_MODE_N64) || defined(_TEXTURE_FILTER_MODE_N64_MIPMAPS)
        return SampleTextureWithFilterModeN64(TEXTURE2D_ARGS(tex, samp), uv, texelSizeLod, lod);
    #else // defined(_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS)
        return SAMPLE_TEXTURE2D(tex, samp, uv);
    #endif
    }

    float4 Fragment(Varyings input) : SV_Target0
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 positionSS = input.uv * _ScreenSizeRasterization.xy;

    #if defined(_SKY_MODE_FOG_COLOR)
        float4 color = _SkyColor; // _SkyColor will be loaded with the fog color cpu side.

    #elif defined(_SKY_MODE_BACKGROUND_COLOR)
        float4 color = _SkyColor; // _SkyColor will be loaded with the background color cpu side.

    #elif defined(_SKY_MODE_SKYBOX)
        float3 viewDirectionWS = ComputeViewDirectionWSFromPositionSS(positionSS);
        float4 color = SAMPLE_TEXTURECUBE(_SkyboxTextureCube, sampler_SkyboxTextureCube, viewDirectionWS);

    #elif defined(_SKY_MODE_TILED_LAYERS)
        float3 viewDirectionWS = ComputeViewDirectionWSFromPositionSS(positionSS);

        // Inpsired by Quake 1 sky tech.
        // http://www.mralligator.com/sky/
        float3 s = viewDirectionWS;
        s.y = _SkyTiledLayersSkyHeightScaleInverse * (s.y + _SkyTiledLayersSkyHorizonOffset);
        s = normalize(s);

        // TODO: Could precompute these 2D rotation matrices cpu side and feed in.
        float rotationLayer0 = _SkyTiledLayersSkyScrollRotationLayer0 * _Time.y + _SkyTiledLayersSkyRotationLayer0;
        float2 uvLayer0 = float2(
            s.x * cos(rotationLayer0) - s.z * sin(rotationLayer0),
            s.x * sin(rotationLayer0) + s.z * cos(rotationLayer0)
        );

        float rotationLayer1 = _SkyTiledLayersSkyScrollRotationLayer1 * _Time.y + _SkyTiledLayersSkyRotationLayer1;
        float2 uvLayer1 = float2(
            s.x * cos(rotationLayer1) - s.z * sin(rotationLayer1),
            s.x * sin(rotationLayer1) + s.z * cos(rotationLayer1)
        );

        uvLayer0 = uvLayer0 * _SkyTiledLayersSkyTextureScaleOffsetLayer0.xy + _SkyTiledLayersSkyTextureScaleOffsetLayer0.zw;
        uvLayer0 += _Time.y * _SkyTiledLayersSkyScrollScaleLayer0;

        float4 texelSizeLodLayer0;
        float lodLayer0;
        ComputeLODAndTexelSizeMaybeCallDDX(texelSizeLodLayer0, lodLayer0, uvLayer0, _SkyTiledLayersSkyTextureLayer0_TexelSize);
        float4 colorLayer0 = _SkyTiledLayersSkyColorLayer0 * SampleTextureWithFilterMode(
            TEXTURE2D_ARGS(_SkyTiledLayersSkyTextureLayer0, sampler_SkyTiledLayersSkyTextureLayer0),
            uvLayer0,
            texelSizeLodLayer0,
            lodLayer0
        );

        uvLayer1 = uvLayer1 * _SkyTiledLayersSkyTextureScaleOffsetLayer1.xy + _SkyTiledLayersSkyTextureScaleOffsetLayer1.zw;
        uvLayer1 += _Time.y * _SkyTiledLayersSkyScrollScaleLayer1;

        float4 texelSizeLodLayer1;
        float lodLayer1;
        ComputeLODAndTexelSizeMaybeCallDDX(texelSizeLodLayer1, lodLayer1, uvLayer1, _SkyTiledLayersSkyTextureLayer1_TexelSize);
        float4 colorLayer1 = _SkyTiledLayersSkyColorLayer1 * SampleTextureWithFilterMode(
            TEXTURE2D_ARGS(_SkyTiledLayersSkyTextureLayer1, sampler_SkyTiledLayersSkyTextureLayer1),
            uvLayer1,
            texelSizeLodLayer1,
            lodLayer1
        );

        float4 color = float4(
            (colorLayer1.a >= 0.5f) ? colorLayer1.rgb : colorLayer0.rgb,
            (colorLayer1.a >= 0.5f) ? 1.0f : colorLayer0.a
        );

    #else
        #error "Error: Encountered unsupported sky mode.";
    #endif

        if (!_IsPSXQualityEnabled)
        {
            color.rgb = SRGBToLinear(color.rgb);
            color.rgb *= color.a;
            color.rgb = LinearToSRGB(color.rgb);
            return color;
        }

        color.rgb = floor(color.rgb * _PrecisionColor.rgb + 0.5f) * _PrecisionColorInverse.rgb;
        color.rgb = SRGBToLinear(color.rgb);
        color.rgb *= color.a;

    #if defined(_OUTPUT_LDR)
        
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
        color.rgb = ComputeFramebufferDiscretization(color.rgb, positionSS, _SkyFramebufferDitherWeight, _PrecisionColor.rgb, _PrecisionColorInverse.rgb);
    #endif

        return color;
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

            #pragma multi_compile _SKY_MODE_FOG_COLOR _SKY_MODE_BACKGROUND_COLOR _SKY_MODE_SKYBOX _SKY_MODE_TILED_LAYERS
            #pragma shader_feature _TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS _TEXTURE_FILTER_MODE_POINT _TEXTURE_FILTER_MODE_POINT_MIPMAPS _TEXTURE_FILTER_MODE_N64 _TEXTURE_FILTER_MODE_N64_MIPMAPS

            #pragma vertex Vertex
            #pragma fragment Fragment

            ENDHLSL
        }
    }
}
