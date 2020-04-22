using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    internal static class PSXStringConstants
    {
        public static readonly string s_PackagePath = "Packages/com.hauntedpsx.render-pipelines.psx";
        public static readonly string s_GlobalRenderPipelineStr = "PSXRenderPipeline";
        public static readonly string s_CommandBufferRenderForwardStr = "PSXRenderPipeline.RenderForward";
        public static readonly string s_CommandBufferRenderPostProcessStr = "PSXRenderPipeline.PostProcessing";
    }

    internal static class PSXProfilingSamplers
    {
        public static readonly string s_PushGlobalRasterizationParametersStr = "Push Global Rasterization Parameters";
        public static readonly string s_PushGlobalPostProcessingParametersStr = "Push Global Rasterization Parameters";
        public static readonly string s_PushQualityOverrideParametersStr = "Push Quality Override Parameters";
        public static readonly string s_PushTonemapperParametersStr = "Push Tonemapper Parameters";
        public static readonly string s_PushLightingParametersStr = "Push Lighting Parameters";
        public static readonly string s_PushPrecisionParametersStr = "Push Precision Parameters";
        public static readonly string s_PushFogParametersStr = "Push Fog Parameters";
        public static readonly string s_PushCathodeRayTubeParametersStr = "Push Cathode Ray Tube Parameters";

        public static ProfilingSampler s_PushGlobalRasterizationParameters = new ProfilingSampler(s_PushGlobalRasterizationParametersStr);
        public static ProfilingSampler s_PushGlobalPostProcessingParameters = new ProfilingSampler(s_PushGlobalPostProcessingParametersStr);
        public static ProfilingSampler s_PushQualityOverrideParameters = new ProfilingSampler(s_PushQualityOverrideParametersStr);
        public static ProfilingSampler s_PushTonemapperParameters = new ProfilingSampler(s_PushTonemapperParametersStr);
        public static ProfilingSampler s_PushLightingParameters = new ProfilingSampler(s_PushLightingParametersStr);
        public static ProfilingSampler s_PushPrecisionParameters = new ProfilingSampler(s_PushPrecisionParametersStr);
        public static ProfilingSampler s_PushFogParameters = new ProfilingSampler(s_PushFogParametersStr);
        public static ProfilingSampler s_PushCathodeRayTubeParameters = new ProfilingSampler(s_PushCathodeRayTubeParametersStr);
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

    // Pre-hashed shader ids to avoid runtime hashing cost, runtime string manipulation, and to ensure we do not have naming conflicts across
    // all global shader uniforms.
    internal static class PSXShaderIDs
    {
        public static readonly int _ScreenSize = Shader.PropertyToID("_ScreenSize");
        public static readonly int _FrameBufferTexture = Shader.PropertyToID("_FrameBufferTexture");
        public static readonly int _FrameBufferScreenSize = Shader.PropertyToID("_FrameBufferScreenSize");
        public static readonly int _FlipY = Shader.PropertyToID("_FlipY");
        public static readonly int _UVTransform = Shader.PropertyToID("_UVTransform");
        public static readonly int _WhiteNoiseTexture = Shader.PropertyToID("_WhiteNoiseTexture");
        public static readonly int _WhiteNoiseSize = Shader.PropertyToID("_WhiteNoiseSize");
        public static readonly int _BlueNoiseTexture = Shader.PropertyToID("_BlueNoiseTexture");
        public static readonly int _BlueNoiseSize = Shader.PropertyToID("_BlueNoiseSize");
        public static readonly int _Time = Shader.PropertyToID("_Time");
        public static readonly int _CRTIsEnabled = Shader.PropertyToID("_CRTIsEnabled");
        public static readonly int _CRTBloom = Shader.PropertyToID("_CRTBloom");
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
        public static readonly int _PrecisionGeometry = Shader.PropertyToID("_PrecisionGeometry");
        public static readonly int _PrecisionColor = Shader.PropertyToID("_PrecisionColor");
        public static readonly int _PrecisionColorInverse = Shader.PropertyToID("_PrecisionColorInverse");
        public static readonly int _FramebufferDitherTexture = Shader.PropertyToID("_FramebufferDitherTexture");
        public static readonly int _FramebufferDitherSize = Shader.PropertyToID("_FramebufferDitherSize");
        public static readonly int _FramebufferDitherIsEnabled = Shader.PropertyToID("_FramebufferDitherIsEnabled");
        public static readonly int _DrawDistance = Shader.PropertyToID("_DrawDistance");
        public static readonly int _FogFalloffMode = Shader.PropertyToID("_FogFalloffMode");
        public static readonly int _FogColor = Shader.PropertyToID("_FogColor");
        public static readonly int _FogDistanceScaleBias = Shader.PropertyToID("_FogDistanceScaleBias");
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
        public static readonly int _AlphaClippingDitherTexture = Shader.PropertyToID("_AlphaClippingDitherTexture");
        public static readonly int _AlphaClippingDitherSize = Shader.PropertyToID("_AlphaClippingDitherSize");
    }
}
