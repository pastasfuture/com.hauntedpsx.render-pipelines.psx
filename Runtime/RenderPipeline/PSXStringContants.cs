using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    internal static class PSXStringConstants
    {
        public static readonly string s_PackagePath = "Packages/com.hauntedpsx.render-pipelines.psx";
        public static readonly string s_GlobalRenderPipelineStr = "PSXRenderPipeline";
        public static readonly string s_CommandBufferRenderForwardStr = "PSXRenderPipeline.RenderForward";
        public static readonly string s_CommandBufferRenderPreMainStr = "PSXRenderPipeline.RenderPreMain";
        public static readonly string s_CommandBufferRenderPreUIOverlayStr = "PSXRenderPipeline.RenderPreUIOverlay";
        public static readonly string s_CommandBufferRenderPostProcessStr = "PSXRenderPipeline.PostProcessing";
    }

    internal static class PSXProfilingSamplers
    {
        public static readonly string s_PushCameraParametersStr = "Push Camera Parameters";
        public static readonly string s_PushGlobalRasterizationParametersStr = "Push Global Rasterization Parameters";
        public static readonly string s_PushGlobalPostProcessingParametersStr = "Push Global Rasterization Parameters";
        public static readonly string s_PushSkyParametersStr = "Push Sky Parameters";
        public static readonly string s_PushQualityOverrideParametersStr = "Push Quality Override Parameters";
        public static readonly string s_PushTonemapperParametersStr = "Push Tonemapper Parameters";
        public static readonly string s_PushLightingParametersStr = "Push Lighting Parameters";
        public static readonly string s_PushDynamicLightingParametersStr = "Push Dynamic Lighting Parameters";
        public static readonly string s_PreMainParametersStr = "Push Pre Main Parameters";
        public static readonly string s_PreUIOverlayParametersStr = "Push Pre UI Overlay Parameters";
        public static readonly string s_PushPrecisionParametersStr = "Push Precision Parameters";
        public static readonly string s_PushFogParametersStr = "Push Fog Parameters";
        public static readonly string s_PushCompressionParametersStr = "Push Compression Parameters";
        public static readonly string s_PushCathodeRayTubeParametersStr = "Push Cathode Ray Tube Parameters";
        public static readonly string s_DrawAccumulationMotionBlurPreUIOverlayStr = "Accumulation Motion Blur Pre UI Overlay";
        public static readonly string s_DrawAccumulationMotionBlurPostUIOverlayStr = "Accumulation Motion Blur Post UI Overlay";
        public static readonly string s_DrawAccumulationMotionBlurFinalBlitStr = "Accumulation Motion Blur Final Blit";
        public static readonly string s_PushTerrainGrassParametersStr = "Push Terrain Grass Parameters";

        public static ProfilingSampler s_PushCameraParameters = new ProfilingSampler(s_PushCameraParametersStr);
        public static ProfilingSampler s_PushGlobalRasterizationParameters = new ProfilingSampler(s_PushGlobalRasterizationParametersStr);
        public static ProfilingSampler s_PushGlobalPostProcessingParameters = new ProfilingSampler(s_PushGlobalPostProcessingParametersStr);
        public static ProfilingSampler s_PushSkyParameters = new ProfilingSampler(s_PushSkyParametersStr);
        public static ProfilingSampler s_PushQualityOverrideParameters = new ProfilingSampler(s_PushQualityOverrideParametersStr);
        public static ProfilingSampler s_PushTonemapperParameters = new ProfilingSampler(s_PushTonemapperParametersStr);
        public static ProfilingSampler s_PushLightingParameters = new ProfilingSampler(s_PushLightingParametersStr);
        public static ProfilingSampler s_PushDynamicLightingParameters = new ProfilingSampler(s_PushDynamicLightingParametersStr);
        public static ProfilingSampler s_PreMainParameters = new ProfilingSampler(s_PreMainParametersStr);
        public static ProfilingSampler s_PreUIOverlayParameters = new ProfilingSampler(s_PreUIOverlayParametersStr);
        public static ProfilingSampler s_PushPrecisionParameters = new ProfilingSampler(s_PushPrecisionParametersStr);
        public static ProfilingSampler s_PushFogParameters = new ProfilingSampler(s_PushFogParametersStr);
        public static ProfilingSampler s_PushCompressionParameters = new ProfilingSampler(s_PushCompressionParametersStr);
        public static ProfilingSampler s_PushCathodeRayTubeParameters = new ProfilingSampler(s_PushCathodeRayTubeParametersStr);
        public static ProfilingSampler s_DrawAccumulationMotionBlurPreUIOverlay = new ProfilingSampler(s_DrawAccumulationMotionBlurPreUIOverlayStr);
        public static ProfilingSampler s_DrawAccumulationMotionBlurPostUIOverlay = new ProfilingSampler(s_DrawAccumulationMotionBlurPostUIOverlayStr);
        public static ProfilingSampler s_DrawAccumulationMotionBlurFinalBlit = new ProfilingSampler(s_DrawAccumulationMotionBlurFinalBlitStr);
        public static ProfilingSampler s_PushTerrainGrassParameters = new ProfilingSampler(s_PushTerrainGrassParametersStr);
    }

    internal static class PSXShaderPassNames
    {
        // ShaderPass string - use to have consistent naming through the codebase.
        public static readonly string s_PSXLitStr = "PSXLit";
        public static readonly string s_SRPDefaultUnlitStr = "SRPDefaultUnlit";

        // ShaderPass name
        public static readonly ShaderTagId s_PSXLit = new ShaderTagId(s_PSXLitStr);
        public static readonly ShaderTagId s_SRPDefaultUnlit = new ShaderTagId(s_SRPDefaultUnlitStr);
    }

    internal static class PSXShaderKeywords
    {
        public static readonly string s_OUTPUT_LDR = "_OUTPUT_LDR";
        public static readonly string s_OUTPUT_HDR = "_OUTPUT_HDR";
        public static readonly string s_CRT_MASK_COMPRESSED_TV = "_CRT_MASK_COMPRESSED_TV";
        public static readonly string s_CRT_MASK_APERATURE_GRILL = "_CRT_MASK_APERTURE_GRILL";
        public static readonly string s_CRT_MASK_VGA = "_CRT_MASK_VGA";
        public static readonly string s_CRT_MASK_VGA_STRETCHED = "_CRT_MASK_VGA_STRETCHED";
        public static readonly string s_CRT_MASK_TEXTURE = "_CRT_MASK_TEXTURE";
        public static readonly string s_CRT_MASK_DISABLED = "_CRT_MASK_DISABLED";
        public static readonly string s_SKY_MODE_FOG_COLOR = "_SKY_MODE_FOG_COLOR"; 
        public static readonly string s_SKY_MODE_BACKGROUND_COLOR = "_SKY_MODE_BACKGROUND_COLOR";
        public static readonly string s_SKY_MODE_SKYBOX = "_SKY_MODE_SKYBOX";
        public static readonly string s_SKY_MODE_TILED_LAYERS = "_SKY_MODE_TILED_LAYERS";
        public static readonly string s_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS = "_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS";
        public static readonly string s_TEXTURE_FILTER_MODE_POINT = "_TEXTURE_FILTER_MODE_POINT";
        public static readonly string s_TEXTURE_FILTER_MODE_POINT_MIPMAPS = "_TEXTURE_FILTER_MODE_POINT_MIPMAPS";
        public static readonly string s_TEXTURE_FILTER_MODE_N64 = "_TEXTURE_FILTER_MODE_N64";
        public static readonly string s_TEXTURE_FILTER_MODE_N64_MIPMAPS = "_TEXTURE_FILTER_MODE_N64_MIPMAPS";
        public static readonly string s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS = "_TERRAIN_GRASS_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS";
        public static readonly string s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT = "_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT";
        public static readonly string s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT_MIPMAPS = "_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT_MIPMAPS";
        public static readonly string s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64 = "_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64";
        public static readonly string s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64_MIPMAPS = "_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64_MIPMAPS";
        public static readonly string s_FOG_COLOR_LUT_MODE_DISABLED = "_FOG_COLOR_LUT_MODE_DISABLED";
        public static readonly string s_FOG_COLOR_LUT_MODE_TEXTURE2D_DISTANCE_AND_HEIGHT = "_FOG_COLOR_LUT_MODE_TEXTURE2D_DISTANCE_AND_HEIGHT";
        public static readonly string s_FOG_COLOR_LUT_MODE_TEXTURECUBE = "_FOG_COLOR_LUT_MODE_TEXTURECUBE";
        public static readonly string k_LIGHTMAP_SHADOW_MASK = "LIGHTMAP_SHADOW_MASK";
    }

    internal static class PSXComputeKernels
    {
        // WARNING: this kernel name LUT calculation needs to stay in sync the kernel declarations in compression.compute,
        // We need to manually compute and bookkeep kernel indices this way because 2019.3 does not support multi-compile keywords in compute shaders.
        public static readonly string[] s_COMPRESSION =
        {
            "CSMain0",
            "CSMain1",
            "CSMain2",
            "CSMain3",
            "CSMain4",
            "CSMain5",
            "CSMain6",
            "CSMain7",
            "CSMain8",
            "CSMain9",
            "CSMain10",
            "CSMain11"
        };
    }

    // Pre-hashed shader ids to avoid runtime hashing cost, runtime string manipulation, and to ensure we do not have naming conflicts across
    // all global shader uniforms.
    internal static class PSXShaderIDs
    {
        public static readonly int _ScreenSize = Shader.PropertyToID("_ScreenSize");
        public static readonly int _ScreenSizeRasterization = Shader.PropertyToID("_ScreenSizeRasterization");
        public static readonly int _ScreenSizeRasterizationRTScaled = Shader.PropertyToID("_ScreenSizeRasterizationRTScaled");
        public static readonly int _RasterizationRTScaledClampBoundsUV = Shader.PropertyToID("_RasterizationRTScaledClampBoundsUV");
        public static readonly int _RasterizationRTScaledMaxSSAndUV = Shader.PropertyToID("_RasterizationRTScaledMaxSSAndUV");
        public static readonly int _FrameBufferTexture = Shader.PropertyToID("_FrameBufferTexture");
        public static readonly int _WhiteNoiseTexture = Shader.PropertyToID("_WhiteNoiseTexture");
        public static readonly int _WhiteNoiseSize = Shader.PropertyToID("_WhiteNoiseSize");
        public static readonly int _BlueNoiseTexture = Shader.PropertyToID("_BlueNoiseTexture");
        public static readonly int _BlueNoiseSize = Shader.PropertyToID("_BlueNoiseSize");
        public static readonly int _Time = Shader.PropertyToID("_Time");
        public static readonly int _ProjectionParams = Shader.PropertyToID("_ProjectionParams");
        public static readonly int _WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");
        public static readonly int _FlipY = Shader.PropertyToID("_FlipY");
        public static readonly int _CameraAspectModeUVScaleBias = Shader.PropertyToID("_CameraAspectModeUVScaleBias");
        public static readonly int _CRTIsEnabled = Shader.PropertyToID("_CRTIsEnabled");
        public static readonly int _CRTBloom = Shader.PropertyToID("_CRTBloom");
        public static readonly int _CRTGrateMaskTexture = Shader.PropertyToID("_CRTGrateMaskTexture");
        public static readonly int _CRTGrateMaskSize = Shader.PropertyToID("_CRTGrateMaskSize");
        public static readonly int _CRTGrateMaskScale = Shader.PropertyToID("_CRTGrateMaskScale");
        public static readonly int _CRTScanlineSharpness = Shader.PropertyToID("_CRTScanlineSharpness");
        public static readonly int _CRTImageSharpness = Shader.PropertyToID("_CRTImageSharpness");
        public static readonly int _CRTBloomSharpness = Shader.PropertyToID("_CRTBloomSharpness");
        public static readonly int _CRTNoiseIntensity = Shader.PropertyToID("_CRTNoiseIntensity");
        public static readonly int _CRTNoiseSaturation = Shader.PropertyToID("_CRTNoiseSaturation");
        public static readonly int _CRTGrateMaskIntensityMinMax = Shader.PropertyToID("_CRTGrateMaskIntensityMinMax");
        public static readonly int _CRTBarrelDistortion = Shader.PropertyToID("_CRTBarrelDistortion");
        public static readonly int _CRTVignetteSquared = Shader.PropertyToID("_CRTVignetteSquared");
        public static readonly int _CRTIsTelevisionOverlayEnabled = Shader.PropertyToID("_CRTIsTelevisionOverlayEnabled");
        public static readonly int _GeometryPushbackParameters = Shader.PropertyToID("_GeometryPushbackParameters");
        public static readonly int _PrecisionGeometry = Shader.PropertyToID("_PrecisionGeometry");
        public static readonly int _PrecisionColor = Shader.PropertyToID("_PrecisionColor");
        public static readonly int _PrecisionColorInverse = Shader.PropertyToID("_PrecisionColorInverse");
        public static readonly int _PrecisionAlphaAndInverse = Shader.PropertyToID("_PrecisionAlphaAndInverse");
        public static readonly int _AffineTextureWarping = Shader.PropertyToID("_AffineTextureWarping");
        public static readonly int _FramebufferDitherTexture = Shader.PropertyToID("_FramebufferDitherTexture");
        public static readonly int _FramebufferDitherSize = Shader.PropertyToID("_FramebufferDitherSize");
        public static readonly int _FramebufferDither = Shader.PropertyToID("_FramebufferDither");
        public static readonly int _FramebufferDitherScaleAndInverse = Shader.PropertyToID("_FramebufferDitherScaleAndInverse");
        public static readonly int _DrawDistanceFalloffMode = Shader.PropertyToID("_DrawDistanceFalloffMode");
        public static readonly int _DrawDistance = Shader.PropertyToID("_DrawDistance");
        public static readonly int _FogBlendMode = Shader.PropertyToID("_FogBlendMode");
        public static readonly int _FogFalloffMode = Shader.PropertyToID("_FogFalloffMode");
        public static readonly int _FogHeightFalloffMirrored = Shader.PropertyToID("_FogHeightFalloffMirrored");
        public static readonly int _FogColor = Shader.PropertyToID("_FogColor");
        public static readonly int _FogPrecisionAlphaAndInverse = Shader.PropertyToID("_FogPrecisionAlphaAndInverse");
        public static readonly int _FogPrecisionAlphaDitherTexture = Shader.PropertyToID("_FogPrecisionAlphaDitherTexture");
        public static readonly int _FogPrecisionAlphaDitherSize = Shader.PropertyToID("_FogPrecisionAlphaDitherSize");
        public static readonly int _FogPrecisionAlphaDither = Shader.PropertyToID("_FogPrecisionAlphaDither");
        public static readonly int _FogDistanceScaleBias = Shader.PropertyToID("_FogDistanceScaleBias");
        public static readonly int _FogFalloffCurvePower = Shader.PropertyToID("_FogFalloffCurvePower");
        public static readonly int _FogColorLUTWeight = Shader.PropertyToID("_FogColorLUTWeight");
        public static readonly int _FogIsAdditionalLayerEnabled = Shader.PropertyToID("_FogIsAdditionalLayerEnabled");
        public static readonly int _FogHeightFalloffMirroredLayer1 = Shader.PropertyToID("_FogHeightFalloffMirroredLayer1");
        public static readonly int _FogFalloffModeLayer1 = Shader.PropertyToID("_FogFalloffModeLayer1");
        public static readonly int _FogColorLayer1 = Shader.PropertyToID("_FogColorLayer1");
        public static readonly int _FogDistanceScaleBiasLayer1 = Shader.PropertyToID("_FogDistanceScaleBiasLayer1");
        public static readonly int _FogFalloffCurvePowerLayer1 = Shader.PropertyToID("_FogFalloffCurvePowerLayer1");
        public static readonly int _FogColorLUTTexture2D = Shader.PropertyToID("_FogColorLUTTexture2D");
        public static readonly int _FogColorLUTTextureCube = Shader.PropertyToID("_FogColorLUTTextureCube");
        public static readonly int _FogColorLUTRotationTangent = Shader.PropertyToID("_FogColorLUTRotationTangent");
        public static readonly int _FogColorLUTRotationBitangent = Shader.PropertyToID("_FogColorLUTRotationBitangent");
        public static readonly int _FogColorLUTRotationNormal = Shader.PropertyToID("_FogColorLUTRotationNormal");
        public static readonly int _LightingIsEnabled = Shader.PropertyToID("_LightingIsEnabled");
        public static readonly int _IsPSXQualityEnabled = Shader.PropertyToID("_IsPSXQualityEnabled");
        public static readonly int _TonemapperIsEnabled = Shader.PropertyToID("_TonemapperIsEnabled");
        public static readonly int _TonemapperContrast = Shader.PropertyToID("_TonemapperContrast");
        public static readonly int _TonemapperShoulder = Shader.PropertyToID("_TonemapperShoulder");
        public static readonly int _TonemapperWhitepoint = Shader.PropertyToID("_TonemapperWhitepoint"); 
        public static readonly int _TonemapperGraypointCoefficients = Shader.PropertyToID("_TonemapperGraypointCoefficients");
        public static readonly int _TonemapperCrossTalk = Shader.PropertyToID("_TonemapperCrossTalk");
        public static readonly int _TonemapperSaturation = Shader.PropertyToID("_TonemapperSaturation");
        public static readonly int _TonemapperCrossTalkSaturation = Shader.PropertyToID("_TonemapperCrossTalkSaturation");
        public static readonly int _BakedLightingMultiplier = Shader.PropertyToID("_BakedLightingMultiplier");
        public static readonly int _VertexColorLightingMultiplier = Shader.PropertyToID("_VertexColorLightingMultiplier");
        public static readonly int _DynamicLightingMultiplier = Shader.PropertyToID("_DynamicLightingMultiplier");
        public static readonly int _AlphaClippingDitherTexture = Shader.PropertyToID("_AlphaClippingDitherTexture");
        public static readonly int _AlphaClippingDitherSize = Shader.PropertyToID("_AlphaClippingDitherSize");
        public static readonly int _AdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
        public static readonly int _AdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
        public static readonly int _AdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
        public static readonly int _AdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");
        public static readonly int _AdditionalLightOcclusionProbeChannel = Shader.PropertyToID("_AdditionalLightOcclusionProbeChannel");
        public static readonly int _AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");
        public static readonly int _CompressionAccuracyThresholdAndInverse = Shader.PropertyToID("_CompressionAccuracyThresholdAndInverse");
        public static readonly int _CompressionSource = Shader.PropertyToID("_CompressionSource");
        public static readonly int _CompressionSourceIndicesMinMax = Shader.PropertyToID("_CompressionSourceIndicesMinMax");
        public static readonly int _CompressionWeight = Shader.PropertyToID("_CompressionWeight");
        public static readonly int _CompressionChromaQuantizationScaleAndInverse = Shader.PropertyToID("_CompressionChromaQuantizationScaleAndInverse");
        public static readonly int _SkyColor = Shader.PropertyToID("_SkyColor");
        public static readonly int _SkyboxTextureCube = Shader.PropertyToID("_SkyboxTextureCube");
        public static readonly int _SkyPixelCoordToWorldSpaceViewDirectionMatrix = Shader.PropertyToID("_SkyPixelCoordToWorldSpaceViewDirectionMatrix");
        public static readonly int _SkyFramebufferDitherWeight = Shader.PropertyToID("_SkyFramebufferDitherWeight");
        public static readonly int _SkyTiledLayersSkyHeightScaleInverse = Shader.PropertyToID("_SkyTiledLayersSkyHeightScaleInverse");
        public static readonly int _SkyTiledLayersSkyHorizonOffset = Shader.PropertyToID("_SkyTiledLayersSkyHorizonOffset");
        public static readonly int _SkyTiledLayersSkyColorLayer0 = Shader.PropertyToID("_SkyTiledLayersSkyColorLayer0");
        public static readonly int _SkyTiledLayersSkyTextureLayer0 = Shader.PropertyToID("_SkyTiledLayersSkyTextureLayer0");
        public static readonly int _SkyTiledLayersSkyTextureScaleOffsetLayer0 = Shader.PropertyToID("_SkyTiledLayersSkyTextureScaleOffsetLayer0");
        public static readonly int _SkyTiledLayersSkyRotationLayer0 = Shader.PropertyToID("_SkyTiledLayersSkyRotationLayer0"); 
        public static readonly int _SkyTiledLayersSkyScrollScaleLayer0 = Shader.PropertyToID("_SkyTiledLayersSkyScrollScaleLayer0");
        public static readonly int _SkyTiledLayersSkyScrollRotationLayer0 = Shader.PropertyToID("_SkyTiledLayersSkyScrollRotationLayer0");
        public static readonly int _SkyTiledLayersSkyColorLayer1 = Shader.PropertyToID("_SkyTiledLayersSkyColorLayer1");
        public static readonly int _SkyTiledLayersSkyTextureLayer1 = Shader.PropertyToID("_SkyTiledLayersSkyTextureLayer1");
        public static readonly int _SkyTiledLayersSkyTextureScaleOffsetLayer1 = Shader.PropertyToID("_SkyTiledLayersSkyTextureScaleOffsetLayer1");
        public static readonly int _SkyTiledLayersSkyRotationLayer1 = Shader.PropertyToID("_SkyTiledLayersSkyRotationLayer1");
        public static readonly int _SkyTiledLayersSkyScrollScaleLayer1 = Shader.PropertyToID("_SkyTiledLayersSkyScrollScaleLayer1");
        public static readonly int _SkyTiledLayersSkyScrollRotationLayer1 = Shader.PropertyToID("_SkyTiledLayersSkyScrollRotationLayer1");
        public static readonly int _RasterizationHistoryWeight = Shader.PropertyToID("_RasterizationHistoryWeight");
        public static readonly int _RasterizationHistoryCompositeDither = Shader.PropertyToID("_RasterizationHistoryCompositeDither");
        public static readonly int _RasterizationHistoryRT = Shader.PropertyToID("_RasterizationHistoryRT");
        public static readonly int _AccumulationMotionBlurParameters = Shader.PropertyToID("_AccumulationMotionBlurParameters");
        public static readonly int _CopyColorSourceRT = Shader.PropertyToID("_CopyColorSourceRT");
        public static readonly int _CopyColorSourceRTSize = Shader.PropertyToID("_CopyColorSourceRTSize");
    }
}
