using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using UnityEditor;
using System.ComponentModel;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    public partial class PSXRenderPipeline : UnityEngine.Rendering.RenderPipeline
    {
        readonly PSXRenderPipelineAsset m_Asset;
        public PSXRenderPipelineAsset asset { get { return m_Asset; }}

        internal const PerObjectData k_RendererConfigurationBakedLighting = PerObjectData.LightProbe | PerObjectData.Lightmaps | PerObjectData.LightProbeProxyVolume;
        internal const PerObjectData k_RendererConfigurationBakedLightingWithShadowMask = k_RendererConfigurationBakedLighting | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ShadowMask;
        internal const PerObjectData k_RendererConfigurationDynamicLighting = PerObjectData.LightData | PerObjectData.LightIndices;

        Material skyMaterial;
        Material accumulationMotionBlurMaterial;
        Material copyColorRespectFlipYMaterial;
        Material crtMaterial;
        int[] compressionCSKernels;

        // Use to detect frame changes (for accurate frame count in editor, consider using psxCamera.GetCameraFrameCount)
        int frameCount;

        public static PSXRenderPipeline instance = null;
        
        internal PSXRenderPipeline(PSXRenderPipelineAsset asset)
        {
            instance = this;
            m_Asset = asset;
            Build();
            Allocate();
        }

        internal protected void Build()
        {
            ConfigureGlobalRenderPipelineTag();
            ConfigureSRPBatcherFromAsset(m_Asset);
        }

        static void ConfigureGlobalRenderPipelineTag()
        {
            // https://docs.unity3d.com/ScriptReference/Shader-globalRenderPipeline.html
            // Set globalRenderPipeline so that only subshaders with Tags{ "RenderPipeline" = "PSXRenderPipeline" } will be rendered.
            Shader.globalRenderPipeline = PSXStringConstants.s_GlobalRenderPipelineStr;
        }

        static void ConfigureSRPBatcherFromAsset(PSXRenderPipelineAsset asset)
        {
            // TODO: Re-enable SRP Batcher support once PSXLit materials are SRP Batcher compatible.
            // Currently they are incompatible due to different variants sampling lightmap textures in the vertex shader and others sampling in the fragment shader.
            // GraphicsSettings.useScriptableRenderPipelineBatching = asset.isSRPBatcherEnabled;
            GraphicsSettings.useScriptableRenderPipelineBatching = false;
        }

        void FindComputeKernels()
        {
            if (!IsComputeShaderSupportedPlatform()) { return; }
            compressionCSKernels = FindCompressionKernels(m_Asset);
        }

        internal protected void Allocate()
        {
            this.skyMaterial = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.skyPS);
            this.accumulationMotionBlurMaterial = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.accumulationMotionBlurPS);
            this.copyColorRespectFlipYMaterial = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.copyColorRespectFlipYPS);
            this.crtMaterial = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.crtPS);

            FindComputeKernels();
            AllocateLighting();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            PSXCamera.ClearAll();

            CoreUtils.Destroy(skyMaterial);
            CoreUtils.Destroy(accumulationMotionBlurMaterial);
            CoreUtils.Destroy(copyColorRespectFlipYMaterial);
            CoreUtils.Destroy(crtMaterial);
            compressionCSKernels = null;
            DisposeLighting();
        }

        void PushCameraParameters(Camera camera, PSXCamera psxCamera, CommandBuffer cmd, out int rasterizationWidth, out int rasterizationHeight, out Vector4 cameraAspectModeUVScaleBias, bool isPSXQualityEnabled)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushCameraParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<CameraVolume>();
                if (!volumeSettings) volumeSettings = CameraVolume.@default;

                if (isPSXQualityEnabled && volumeSettings.isFrameLimitEnabled.value)
                {
                    QualitySettings.vSyncCount = 0; // VSync must be disabled
                    Application.targetFrameRate = volumeSettings.frameLimit.value;                
                }
                else
                {
                    // Render at platform's default framerate with vsync ON.
                    QualitySettings.vSyncCount = 1;
                    Application.targetFrameRate = -1;
                }

                // Trigger camera to reset it's aspect ratio back to the screens aspect ratio.
                // We force this reset here in case a previous frame had overridden the camera's aspect ratio via AspectMode.Locked.
                camera.ResetAspect();
                rasterizationWidth = camera.pixelWidth;
                rasterizationHeight = camera.pixelHeight;
                cameraAspectModeUVScaleBias = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
                if (isPSXQualityEnabled && (volumeSettings.aspectMode.value != CameraVolume.CameraAspectMode.Native))
                {
                    rasterizationWidth = Mathf.Min(rasterizationWidth, volumeSettings.targetRasterizationResolutionWidth.value);
                    rasterizationHeight = Mathf.Min(rasterizationHeight, volumeSettings.targetRasterizationResolutionHeight.value);
                    
                    // Only render locked aspect ratio in main game view.
                    // Force scene view to render with free aspect ratio so that users edit area is not cropped.
                    // This also works around an issue with the aspect ratio discrepancy between locked mode, and
                    // the built in unity gizmos that are rendered outside of the context of this render loop.
                    if (!IsMainGameView(camera)
                        || (volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.FreeStretch)
                        || (volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.FreeFitPixelPerfect)
                        || (volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.FreeCropPixelPerfect)
                        || (volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.FreeBleedPixelPerfect))
                    {
                        // Rather than explicitly hardcoding PSX framebuffer resolution, and enforcing 1.333 aspect ratio
                        // we allow arbitrary aspect ratios and match requested PSX framebuffer pixel density. (targetRasterizationResolutionWidth and targetRasterizationResolutionHeight).
                        if (camera.pixelWidth >= camera.pixelHeight)
                        {
                            // Horizontal aspect.
                            rasterizationWidth = Mathf.FloorToInt((float)rasterizationHeight * (float)camera.pixelWidth / (float)camera.pixelHeight + 0.5f);

                        }
                        else
                        {
                            // Vertical aspect.
                            rasterizationHeight = Mathf.FloorToInt((float)rasterizationHeight * (float)camera.pixelHeight / (float)camera.pixelWidth + 0.5f);
                        }
                    }

                    if (volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.FreeBleedPixelPerfect)
                    {
                        // With Free Bleed Pixel Perfect, rasterization width and height are just targets.
                        // The actual width and height are adjusted to get as close as possible to the target, while still remaining pixel perfect, and avoiding black bars.
                        // This results in a change of aspect ratio unless the screen resolution is a perfect multiple of the rasterization resolution target.
                        rasterizationWidth = camera.pixelWidth / Mathf.CeilToInt((float)camera.pixelWidth / (float)rasterizationWidth);
                        rasterizationHeight = camera.pixelHeight / Mathf.CeilToInt((float)camera.pixelHeight / (float)rasterizationHeight);
                    }

                    // Compute uniform for handling the potential rasterization render target aspect ratio vs CRT shader render target aspect ratio discrepancy.
                    // This occurs due to rounding error, or due to locked rasterization aspect ratio (i.e: to simulate CRT 1.33).
                    if ((volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.FreeFitPixelPerfect)
                        || (volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.LockedFitPixelPerfect))
                    {
                        float ratioX = (float)rasterizationWidth / (float)camera.pixelWidth;
                        float ratioY = (float)rasterizationHeight / (float)camera.pixelHeight;
                        float ratioXYMax = Mathf.Max(ratioX, ratioY);
                        float uvScaleFitX = 1.0f / (ratioX * Mathf.Floor(1.0f / ratioXYMax));
                        float uvScaleFitY = 1.0f / (ratioY * Mathf.Floor(1.0f / ratioXYMax));
                        cameraAspectModeUVScaleBias = new Vector4(uvScaleFitX, uvScaleFitY, 0.5f - (0.5f * uvScaleFitX), 0.5f - (0.5f * uvScaleFitY));
                    }
                    else if (volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.FreeCropPixelPerfect)
                    {
                        float ratioX = (float)rasterizationWidth / (float)camera.pixelWidth;
                        float ratioY = (float)rasterizationHeight / (float)camera.pixelHeight;
                        float ratioXYMin = Mathf.Min(ratioX, ratioY);
                        float uvScaleCropX = 1.0f / (ratioX * Mathf.Ceil(1.0f / ratioXYMin));
                        float uvScaleCropY = 1.0f / (ratioY * Mathf.Ceil(1.0f / ratioXYMin));
                        cameraAspectModeUVScaleBias = new Vector4(uvScaleCropX, uvScaleCropY, 0.5f - (0.5f * uvScaleCropX), 0.5f - (0.5f * uvScaleCropY));
                    }
                    else if (volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.LockedFit)
                    {
                        float ratioX = (float)rasterizationWidth / (float)camera.pixelWidth;
                        float ratioY = (float)rasterizationHeight / (float)camera.pixelHeight;
                        float ratioXYMax = Mathf.Max(ratioX, ratioY);
                        float uvScaleStretchX = 1.0f / (ratioX / ratioXYMax);
                        float uvScaleStretchY = 1.0f / (ratioY / ratioXYMax);
                        cameraAspectModeUVScaleBias = new Vector4(uvScaleStretchX, uvScaleStretchY, 0.5f - (0.5f * uvScaleStretchX), 0.5f - (0.5f * uvScaleStretchY));
                    }
                    else
                    {
                        Debug.Assert((volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.FreeStretch)
                            || (volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.FreeBleedPixelPerfect));

                        // No work to be done.
                        // In the case of Free Stretch: just perform naive upscale, and accept the artifacts.
                        // In the case of Free Bleed Pixel Perfect: No work to be done, we already ensured rasterization resolution is an even divisor of screen resolution.
                        cameraAspectModeUVScaleBias = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
                    }

                    // Force the camera into the aspect ratio described by the targetRasterizationResolution parameters.
                    // While this can be approximately be equal to the fullscreen aspect ratio (in CameraAspectMode.FreeX modes),
                    // subtle change can occur from pixel rounding error.
                    camera.aspect = (float)rasterizationWidth / (float)rasterizationHeight;
                }

                bool rasterizationHistoryRequested = false;
                bool rasterizationPreUICopyRequested = false;
                {
                    var accumulationMotionBlurVolumeSettings = VolumeManager.instance.stack.GetComponent<AccumulationMotionBlurVolume>();
                    if (!accumulationMotionBlurVolumeSettings) accumulationMotionBlurVolumeSettings = AccumulationMotionBlurVolume.@default;

                    rasterizationHistoryRequested = accumulationMotionBlurVolumeSettings.weight.value > 1e-5f;
                    rasterizationPreUICopyRequested = rasterizationHistoryRequested && !accumulationMotionBlurVolumeSettings.applyToUIOverlay.value;
                }

                psxCamera.UpdateBeginFrame(new PSXCamera.PSXCameraUpdateContext()
                {
                    rasterizationWidth = rasterizationWidth,
                    rasterizationHeight = rasterizationHeight,
                    rasterizationHistoryRequested = rasterizationHistoryRequested,
                    rasterizationPreUICopyRequested = rasterizationPreUICopyRequested,
                    rasterizationRandomWriteRequested = IsComputeShaderSupportedPlatform(),
                    rasterizationDepthBufferRequested = EvaluateIsDepthBufferEnabledFromVolume()
                });
            }
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            if (TryUpdateFrameCount(cameras))
            {
                PSXCamera.CleanUnused();
            }

            if (cameras.Length == 0) { return; }

            UnityEngine.Rendering.RenderPipeline.BeginFrameRendering(context, cameras);

            foreach (var camera in cameras)
            {
                if (camera == null) { continue; }

                UnityEngine.Rendering.RenderPipeline.BeginCameraRendering(context, camera);

                // TODO: Should we move this after we set the rasterization render target so that scene view UI is also pixelated?
                DrawSceneViewUI(camera);

                ScriptableCullingParameters cullingParameters;
                if (!camera.TryGetCullingParameters(camera.stereoEnabled, out cullingParameters)) { continue; }

                // Need to update the volume manager for the current camera before querying any volume parameter results.
                // This triggers the volume manager to blend volume parameters spatially, based on the camera position. 
                VolumeManager.instance.Update(camera.transform, camera.cullingMask);

                bool isPSXQualityEnabled = EvaluateIsPSXQualityEnabledFromVolume();

                // Disable PSX Quality effects if the editor post processing checkbox is disabled.
                // This allows users to easily preview their raw geometry and textures without needing to toggle it in the volume system.
                // This is important, since changes to the volume system will trigger changes to the volume profile on disk.
                // We do not want to force diffs on files when users are just temporarily previewing things. 
                isPSXQualityEnabled &= CoreUtils.ArePostProcessesEnabled(camera);

                var cmd = CommandBufferPool.Get(PSXStringConstants.s_CommandBufferRenderForwardStr);
                PSXCamera psxCamera = PSXCamera.GetOrCreate(camera);
                PushCameraParameters(camera, psxCamera, cmd, out int rasterizationWidth, out int rasterizationHeight, out Vector4 cameraAspectModeUVScaleBias, isPSXQualityEnabled);

                // Disable shadow casters completely as we currently do not support dynamic light sources.
                cullingParameters.cullingOptions &= ~CullingOptions.ShadowCasters;

                if (!ComputeDynamicLightingIsEnabled(camera))
                {
                    // Disable lighting completely as we currently do not support dynamic light sources.
                    cullingParameters.cullingOptions &= ~CullingOptions.NeedsLighting;
                }

                // Disable stereo rendering as we currently do not support it.
                cullingParameters.cullingOptions &= ~CullingOptions.Stereo;

                // Disable reflection probes as they are currently not part of the psx render pipeline.
                cullingParameters.cullingOptions &= ~CullingOptions.NeedsReflectionProbes;

                CullingResults cullingResults = context.Cull(ref cullingParameters);

                // Setup camera for rendering (sets render target, view/projection matrices and other
                // per-camera built-in shader variables).
                context.SetupCameraProperties(camera);

                bool hdrIsSupported = false;
                RTHandle rasterizationRT = psxCamera.GetCurrentFrameRT((int)PSXCameraFrameHistoryType.Rasterization);
                RTHandle rasterizationDepthStencilRT = psxCamera.GetCurrentFrameRT((int)PSXCameraFrameHistoryType.RasterizationDepthStencil);
                cmd.SetRenderTarget(rasterizationRT.rt, rasterizationDepthStencilRT.rt);
                PSXRenderPipeline.SetViewport(cmd, rasterizationRT);
                {
                    // Clear background to fog color to create seamless blend between forward-rendered fog, and "sky" / infinity.
                    PushGlobalRasterizationParameters(camera, cmd, rasterizationRT, rasterizationWidth, rasterizationHeight, hdrIsSupported);
                    PushQualityOverrideParameters(camera, cmd, isPSXQualityEnabled);
                    PushPrecisionParameters(camera, cmd, m_Asset);
                    PushFogParameters(camera, cmd);
                    PushLightingParameters(camera, cmd);
                    PushTonemapperParameters(camera, cmd);
                    PushDynamicLightingParameters(camera, cmd, ref cullingResults);
                    PushSkyParameters(camera, cmd, skyMaterial, m_Asset, rasterizationWidth, rasterizationHeight);
                    PushTerrainGrassParameters(camera, cmd, m_Asset, rasterizationWidth, rasterizationHeight);
                    PSXRenderPipeline.DrawFullScreenQuad(cmd, skyMaterial);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Release();

                    DrawBackgroundOpaque(context, camera, ref cullingResults);
                    DrawBackgroundTransparent(context, camera, ref cullingResults);

                    cmd = CommandBufferPool.Get(PSXStringConstants.s_CommandBufferRenderPreMainStr);
                    PushPreMainParameters(camera, cmd);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Release();
                    
                    DrawMainOpaque(context, camera, ref cullingResults);
                    DrawMainTransparent(context, camera, ref cullingResults);

                    cmd = CommandBufferPool.Get(PSXStringConstants.s_CommandBufferRenderPreUIOverlayStr);
                    TryDrawAccumulationMotionBlurPreUIOverlay(psxCamera, cmd, accumulationMotionBlurMaterial, copyColorRespectFlipYMaterial);
                    PushPreUIOverlayParameters(camera, cmd);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Release();

                    DrawUIOverlayOpaque(context, camera, ref cullingResults);
                    DrawUIOverlayTransparent(context, camera, ref cullingResults);

                    DrawLegacyCanvasUI(context, camera, ref cullingResults);

                    // TODO: Draw post image effect gizmos before or after CRT filter?
                    DrawGizmos(context, camera, GizmoSubset.PreImageEffects);
                    DrawGizmos(context, camera, GizmoSubset.PostImageEffects);
                }

                cmd = CommandBufferPool.Get(PSXStringConstants.s_CommandBufferRenderPostProcessStr);
                {
                    TryDrawAccumulationMotionBlurPostUIOverlay(psxCamera, cmd, accumulationMotionBlurMaterial);
                }
                cmd.SetRenderTarget(camera.targetTexture);
                PSXRenderPipeline.SetViewport(cmd, camera, camera.targetTexture);
                {
                    PushGlobalPostProcessingParameters(camera, cmd, m_Asset, rasterizationRT, rasterizationWidth, rasterizationHeight, cameraAspectModeUVScaleBias);
                    PushCompressionParameters(camera, cmd, m_Asset, rasterizationRT, compressionCSKernels);
                    PushCathodeRayTubeParameters(camera, cmd, crtMaterial);
                    PSXRenderPipeline.DrawFullScreenQuad(cmd, crtMaterial);

                    TryDrawAccumulationMotionBlurFinalBlit(psxCamera, cmd, camera.targetTexture, copyColorRespectFlipYMaterial);
                }
                
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();
                context.Submit();

                psxCamera.UpdateEndFrame();

                // Reset any modifications to the cameras aspect ratio so that built in unity handles draw normally.
                camera.ResetAspect();

                UnityEngine.Rendering.RenderPipeline.EndCameraRendering(context, camera);
            }
        }

        private bool TryUpdateFrameCount(Camera[] cameras)
        {
#if UNITY_EDITOR
            int newCount = frameCount;
            foreach (var c in cameras)
            {
                if (c.cameraType != CameraType.Preview)
                {
                    newCount++;
                    break;
                }
            }
#else
            int newCount = Time.frameCount;
#endif
            if (newCount != frameCount)
            {
                frameCount = newCount;
                return true;
            }
            else
            {
                return false;
            }
        }

        static bool IsMainGameView(Camera camera)
        {
            return camera.cameraType == CameraType.Game; 
        }

        static Color ComputeClearColorFromVolume()
        {
            Color fogColorSRGB = GetFogColorFromFogVolume();

            ComputeTonemapperSettingsFromVolume(
                out bool isEnabled,
                out float contrast,
                out float shoulder,
                out float whitepoint,
                out Vector2 graypointCoefficients,
                out float crossTalk,
                out float saturation,
                out float crossTalkSaturation
            );

            Vector3 fogColorLinearPremultipliedAlpha = PSXColor.RGBFromSRGB(new Vector3(fogColorSRGB.r, fogColorSRGB.g, fogColorSRGB.b)) * fogColorSRGB.a;

            if (!isEnabled)
            {
                // Tonemapper is disabled, simply premultiply alpha and return.
                Vector3 fogColorSRGBPremultipliedAlpha = PSXColor.SRGBFromRGB(fogColorLinearPremultipliedAlpha);
                return new Color(fogColorSRGBPremultipliedAlpha.x, fogColorSRGBPremultipliedAlpha.y, fogColorSRGBPremultipliedAlpha.z, fogColorSRGB.a);
            }
            else
            {
                Vector3 fogColorSRGBPremultipliedAlphaTonemapped = PSXColor.SRGBFromRGB(
                    PSXColor.TonemapperGeneric(
                        fogColorLinearPremultipliedAlpha,
                        contrast,
                        shoulder,
                        graypointCoefficients,
                        crossTalk,
                        saturation,
                        crossTalkSaturation
                    )
                );

                return new Color(fogColorSRGBPremultipliedAlphaTonemapped.x, fogColorSRGBPremultipliedAlphaTonemapped.y, fogColorSRGBPremultipliedAlphaTonemapped.z, fogColorSRGB.a);
            }
        }

        static Color GetFogColorFromFogVolume()
        {
            var volumeSettings = VolumeManager.instance.stack.GetComponent<FogVolume>();
            if (!volumeSettings) volumeSettings = FogVolume.@default;
            return volumeSettings.color.value;
        }

        static void ComputeTonemapperSettingsFromVolume(
            out bool isEnabled,
            out float contrast,
            out float shoulder,
            out float whitepoint,
            out Vector2 graypointCoefficients,
            out float crossTalk,
            out float saturation,
            out float crossTalkSaturation)
        {
            var volumeSettings = VolumeManager.instance.stack.GetComponent<TonemapperVolume>();
            if (!volumeSettings) volumeSettings = TonemapperVolume.@default;

            isEnabled = volumeSettings.isEnabled.value;

            contrast = Mathf.Lerp(1e-5f, 1.95f, volumeSettings.contrast.value);
            shoulder = Mathf.Lerp(0.9f, 1.1f, volumeSettings.shoulder.value);
            whitepoint = volumeSettings.whitepoint.value;

            float a = contrast;
            float d = shoulder;
            float m = whitepoint;
            float i = volumeSettings.graypointIn.value;
            float o = volumeSettings.graypointOut.value;

            float b = -(o * Mathf.Pow(m, a) - Mathf.Pow(i, a)) / (o * (Mathf.Pow(i, a * d) - Mathf.Pow(m, a * d)));
            float c = ((o * Mathf.Pow(i, a * d)) * Mathf.Pow(m, a) - Mathf.Pow(i, a) * Mathf.Pow(m, a * d))
                / (o * (Mathf.Pow(i, a * d) - Mathf.Pow(m, a * d)));

            graypointCoefficients = new Vector2(b, c + 1e-5f);

            crossTalk = Mathf.Lerp(1e-5f, 32.0f, volumeSettings.crossTalk.value);
            saturation = Mathf.Lerp(0.0f, 32.0f, volumeSettings.saturation.value);
            crossTalkSaturation = Mathf.Lerp(1e-5f, 32.0f, volumeSettings.crossTalkSaturation.value);
        }

        static bool EvaluateIsPSXQualityEnabledFromVolume()
        {
            var volumeSettings = VolumeManager.instance.stack.GetComponent<QualityOverrideVolume>();
            if (!volumeSettings) volumeSettings = QualityOverrideVolume.@default;
            return volumeSettings.isPSXQualityEnabled.value;
        }

        static bool EvaluateIsDepthBufferEnabledFromVolume()
        {
            var volumeSettings = VolumeManager.instance.stack.GetComponent<CameraVolume>();
            if (!volumeSettings) volumeSettings = CameraVolume.@default;
            return volumeSettings.isDepthBufferEnabled.value;
        }

        static void PushQualityOverrideParameters(Camera cmaera, CommandBuffer cmd, bool isPSXQualityEnabled)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushQualityOverrideParameters))
            {
                // Note: We do not read the quality setting directly on the volume here, as we have already constructed this value previously
                // outside of this function.
                // The volume system is not the only parameter that effects if PSX Quality is enabled or disabled.
                // We also have things like the post processing toggle in the scene view. 

                cmd.SetGlobalInt(PSXShaderIDs._IsPSXQualityEnabled, isPSXQualityEnabled ? 1 : 0);
            }
        }

        static void PushTonemapperParameters(Camera camera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushTonemapperParameters))
            {
                ComputeTonemapperSettingsFromVolume(
                    out bool isEnabled,
                    out float contrast,
                    out float shoulder,
                    out float whitepoint,
                    out Vector2 graypointCoefficients,
                    out float crossTalk,
                    out float saturation,
                    out float crossTalkSaturation
                );

                cmd.SetGlobalInt(PSXShaderIDs._TonemapperIsEnabled, isEnabled ? 1 : 0);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperContrast, contrast);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperShoulder, shoulder);
                cmd.SetGlobalVector(PSXShaderIDs._TonemapperGraypointCoefficients, graypointCoefficients);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperWhitepoint, whitepoint);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperCrossTalk, crossTalk);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperSaturation, saturation);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperCrossTalkSaturation, crossTalkSaturation);
            }
        }

        static void PushLightingParameters(Camera camera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushLightingParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<LightingVolume>();
                if (!volumeSettings) volumeSettings = LightingVolume.@default;

                bool lightingIsEnabled = volumeSettings.lightingIsEnabled.value;

                // Respect the sceneview lighting enabled / disabled toggle.
                lightingIsEnabled &= !CoreUtils.IsSceneLightingDisabled(camera);

                cmd.SetGlobalInt(PSXShaderIDs._LightingIsEnabled, lightingIsEnabled ? 1 : 0);
                cmd.SetGlobalFloat(PSXShaderIDs._BakedLightingMultiplier, volumeSettings.bakedLightingMultiplier.value);
                cmd.SetGlobalFloat(PSXShaderIDs._VertexColorLightingMultiplier, volumeSettings.vertexColorLightingMultiplier.value);
                cmd.SetGlobalFloat(PSXShaderIDs._DynamicLightingMultiplier, volumeSettings.dynamicLightingMultiplier.value);
            }
        }

        static void PushPreMainParameters(Camera camera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PreMainParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<CameraVolume>();
                if (!volumeSettings) volumeSettings = CameraVolume.@default;

                bool isClearDepthAfterBackgroundEnabled = volumeSettings.isClearDepthAfterBackgroundEnabled.value;
                if (isClearDepthAfterBackgroundEnabled)
                {
                    Color clearColorUnused = Color.black;
                    CoreUtils.ClearRenderTarget(cmd, ClearFlag.Depth, clearColorUnused);
                }
            }
        }

        static void PushPreUIOverlayParameters(Camera camera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PreUIOverlayParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<CameraVolume>();
                if (!volumeSettings) volumeSettings = CameraVolume.@default;

                bool isClearDepthBeforeUIEnabled = volumeSettings.isClearDepthBeforeUIEnabled.value;
                if (isClearDepthBeforeUIEnabled)
                {
                    Color clearColorUnused = Color.black;
                    CoreUtils.ClearRenderTarget(cmd, ClearFlag.Depth, clearColorUnused);
                }
            }
        }

        static PerObjectData ComputePerObjectDataFromLightingVolume(Camera camera)
        {
            var volumeSettings = VolumeManager.instance.stack.GetComponent<LightingVolume>();
            if (!volumeSettings) volumeSettings = LightingVolume.@default;

            bool lightingIsEnabled = volumeSettings.lightingIsEnabled.value;

            // Respect the sceneview lighting enabled / disabled toggle.
            lightingIsEnabled &= !CoreUtils.IsSceneLightingDisabled(camera);

            PerObjectData perObjectData = (PerObjectData)0;
            if (lightingIsEnabled)
            {
                if (volumeSettings.bakedLightingMultiplier.value > 0.0f)
                {
                    perObjectData |= k_RendererConfigurationBakedLightingWithShadowMask;
                }

                if (volumeSettings.dynamicLightingMultiplier.value > 0.0f)
                {
                    perObjectData |= k_RendererConfigurationDynamicLighting;
                }
            }

            return perObjectData;
        }

        static bool ComputeDynamicLightingIsEnabled(Camera camera)
        {
            var volumeSettings = VolumeManager.instance.stack.GetComponent<LightingVolume>();
            if (!volumeSettings) volumeSettings = LightingVolume.@default;

            bool lightingIsEnabled = volumeSettings.lightingIsEnabled.value;

            // Respect the sceneview lighting enabled / disabled toggle.
            lightingIsEnabled &= !CoreUtils.IsSceneLightingDisabled(camera);

            lightingIsEnabled &= (volumeSettings.dynamicLightingMultiplier.value > 0.0f);

            return lightingIsEnabled;
        }

        // Needs to be accessible to PSXMaterialUtils for PrecisionGeometryOverrideMode.Override calculations.
        public static Vector2 ComputePrecisionGeometryParameters(float precisionGeometryNormalized)
        {
            // Warning: This function needs to stay in sync with MaterialFunctions.hlsl::ApplyPrecisionGeometryToPositionCS().
            // The these exponents will be dynamically calculated in the shader if a material chooses to override the global precision geometry value.
            float precisionGeometryExponent = Mathf.Lerp(6.0f, 0.0f, precisionGeometryNormalized);
            float precisionGeometryScaleInverse = Mathf.Pow(2.0f, precisionGeometryExponent);
            float precisionGeometryScale = 1.0f / precisionGeometryScaleInverse;

            return new Vector2(precisionGeometryScale, precisionGeometryScaleInverse);
        }

        static void PushPrecisionParameters(Camera camera, CommandBuffer cmd, PSXRenderPipelineAsset asset)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushPrecisionParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<PrecisionVolume>();
                if (!volumeSettings) volumeSettings = PrecisionVolume.@default;

                cmd.SetGlobalVector(PSXShaderIDs._GeometryPushbackParameters, new Vector4(
                    volumeSettings.geometryPushbackEnabled.value ? 1.0f : 0.0f,
                    volumeSettings.geometryPushbackMinMax.value.x,
                    volumeSettings.geometryPushbackMinMax.value.y,
                    0.0f
                ));

                bool precisionGeometryEnabled = volumeSettings.geometryEnabled.value;
                if (precisionGeometryEnabled)
                {
                    Vector2 precisionGeometryScaleAndScaleInverse = ComputePrecisionGeometryParameters(volumeSettings.geometry.value);

                    // Note: The raw volumeSettings.geometry [0, 1] slider value is needed in the shader for per-material override modes, since the blending needs to happen on the raw parameter.
                    // The fast path transformed scale and scaleInverse terms are still used when no per-material override is used.
                    cmd.SetGlobalVector(PSXShaderIDs._PrecisionGeometry, new Vector4(precisionGeometryScaleAndScaleInverse.x, precisionGeometryScaleAndScaleInverse.y, volumeSettings.geometry.value, precisionGeometryEnabled ? 1.0f : 0.0f));
                }
                else
                {
                    cmd.SetGlobalVector(PSXShaderIDs._PrecisionGeometry, Vector4.zero);
                }
                
                int precisionColorIndex = Mathf.FloorToInt(volumeSettings.color.value * 7.0f + 0.5f);
                float precisionChromaBit = volumeSettings.chroma.value;
                Vector3 precisionColor = Vector3.zero; // Silence the compiler warnings.
                switch (precisionColorIndex)
                {
                    case 7: precisionColor = new Vector3(255.0f, 255.0f, 255.0f); break;
                    case 6: precisionColor = new Vector3(127.0f, Mathf.Pow(2.0f, 7.0f + (precisionChromaBit * (8.0f - 7.0f))) - 1.0f, 127.0f); break;
                    case 5: precisionColor = new Vector3(63.0f, Mathf.Pow(2.0f, 6.0f + (precisionChromaBit * (8.0f - 6.0f))) - 1.0f, 63.0f); break;
                    case 4: precisionColor = new Vector3(31.0f, Mathf.Pow(2.0f, 5.0f + (precisionChromaBit * (8.0f - 5.0f))) - 1.0f, 31.0f); break; // Standard PS1 5:6:5 color space.
                    case 3: precisionColor = new Vector3(15.0f, Mathf.Pow(2.0f, 4.0f + (precisionChromaBit * (8.0f - 4.0f))) - 1.0f, 15.0f); break;
                    case 2: precisionColor = new Vector3(7.0f, Mathf.Pow(2.0f, 3.0f + (precisionChromaBit * (8.0f - 3.0f))) - 1.0f, 7.0f); break;
                    case 1: precisionColor = new Vector3(3.0f, Mathf.Pow(2.0f, 2.0f + (precisionChromaBit * (8.0f - 2.0f))) - 1.0f, 3.0f); break;
                    case 0: precisionColor = new Vector3(1.0f, Mathf.Pow(2.0f, 1.0f + (precisionChromaBit * (8.0f - 1.0f))) - 1.0f, 1.0f); break;
                    default: Debug.Assert(false); break;
                }

                cmd.SetGlobalVector(PSXShaderIDs._PrecisionColor, new Vector4(precisionColor.x, precisionColor.y, precisionColor.z, (float)precisionColorIndex / 7.0f)); // .w term needed for per-material override of precision color.
                cmd.SetGlobalVector(PSXShaderIDs._PrecisionColorInverse, new Vector4(1.0f / precisionColor.x, 1.0f / precisionColor.y, 1.0f / precisionColor.z, precisionChromaBit)); // .w term needed for per-material override of precision color.

                int precisionAlphaIndex = Mathf.FloorToInt(volumeSettings.alpha.value * 7.0f + 0.5f);
                float precisionAlpha = 0.0f; // Silence the compiler warnings.
                switch (precisionAlphaIndex)
                {
                    case 7: precisionAlpha = 255.0f; break;
                    case 6: precisionAlpha = 127.0f; break;
                    case 5: precisionAlpha = 63.0f; break;
                    case 4: precisionAlpha = 31.0f; break;
                    case 3: precisionAlpha = 15.0f; break;
                    case 2: precisionAlpha = 7.0f; break;
                    case 1: precisionAlpha = 3.0f; break;
                    case 0: precisionAlpha = 1.0f; break;
                    default: Debug.Assert(false); break;
                }

                cmd.SetGlobalVector(PSXShaderIDs._PrecisionAlphaAndInverse, new Vector2(precisionAlpha, 1.0f / precisionAlpha));

                float affineTextureWarping = volumeSettings.affineTextureWarping.value;
                cmd.SetGlobalFloat(PSXShaderIDs._AffineTextureWarping, affineTextureWarping);

                cmd.SetGlobalFloat(PSXShaderIDs._FramebufferDither, volumeSettings.framebufferDither.value);
                Texture2D framebufferDitherTex = GetFramebufferDitherTexFromAssetAndFrame(asset, (uint)Time.frameCount);
                cmd.SetGlobalTexture(PSXShaderIDs._FramebufferDitherTexture, framebufferDitherTex);
                cmd.SetGlobalVector(PSXShaderIDs._FramebufferDitherSize, new Vector4(
                    framebufferDitherTex.width,
                    framebufferDitherTex.height,
                    1.0f / framebufferDitherTex.width,
                    1.0f / framebufferDitherTex.height
                ));
                cmd.SetGlobalVector(PSXShaderIDs._FramebufferDitherScaleAndInverse, new Vector2(volumeSettings.ditherSize.value, 1.0f / volumeSettings.ditherSize.value));

                int drawDistanceFalloffMode = (int)volumeSettings.drawDistanceFalloffMode.value;
                cmd.SetGlobalInt(PSXShaderIDs._DrawDistanceFalloffMode, drawDistanceFalloffMode);
                cmd.SetGlobalVector(PSXShaderIDs._DrawDistance, new Vector2(volumeSettings.drawDistance.value, volumeSettings.drawDistance.value * volumeSettings.drawDistance.value));
            }
        }

        static void PushFogParameters(Camera camera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushFogParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<FogVolume>();
                if (!volumeSettings) volumeSettings = FogVolume.@default;

                Vector4 fogDistanceScaleBias = new Vector4(
                    1.0f / (volumeSettings.distanceMax.value - volumeSettings.distanceMin.value), 
                    -volumeSettings.distanceMin.value / (volumeSettings.distanceMax.value - volumeSettings.distanceMin.value),
                    -1.0f / (volumeSettings.heightMax.value - volumeSettings.heightMin.value),
                    volumeSettings.heightMax.value / (volumeSettings.heightMax.value - volumeSettings.heightMin.value)
                );

                float fogFalloffCurvePower = (volumeSettings.fogFalloffCurve.value > 0.0f)
                    ? (1.0f - Mathf.Min(0.999f, volumeSettings.fogFalloffCurve.value)) // shoulder increases as value increases from [0, 1]
                    : (1.0f / (1.0f + Mathf.Max(-0.999f, volumeSettings.fogFalloffCurve.value))); // toe increases as value decreases from [0, -1]

                // Respect the Scene View fog enable / disable toggle.
                bool isFogEnabled = volumeSettings.isEnabled.value && CoreUtils.IsSceneViewFogEnabled(camera);
                if (!isFogEnabled)
                {
                    // To visually disable fog, we simply throw fog start and end to infinity.
                    fogDistanceScaleBias = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                    fogFalloffCurvePower = 1.0f;
                }
                else if (!volumeSettings.heightFalloffEnabled.value)
                {
                    fogDistanceScaleBias.z = 0.0f;
                    fogDistanceScaleBias.w = 1.0f;
                }

                FogVolume.FogBlendMode blendMode = volumeSettings.blendMode.value;
                cmd.SetGlobalInt(PSXShaderIDs._FogBlendMode, (int)blendMode);

                cmd.SetGlobalInt(PSXShaderIDs._FogHeightFalloffMirrored, volumeSettings.heightFalloffMirrored.value ? 1 : 0);
                cmd.SetGlobalInt(PSXShaderIDs._FogHeightFalloffMirroredLayer1, volumeSettings.heightFalloffMirroredLayer1.value ? 1 : 0);

                int fogFalloffMode = (int)volumeSettings.fogFalloffMode.value;
                cmd.SetGlobalInt(PSXShaderIDs._FogFalloffMode, fogFalloffMode);
                cmd.SetGlobalVector(PSXShaderIDs._FogColor, new Vector4(volumeSettings.color.value.r, volumeSettings.color.value.g, volumeSettings.color.value.b, volumeSettings.color.value.a));

                int precisionAlphaIndex = Mathf.FloorToInt(volumeSettings.precisionAlpha.value * 7.0f + 0.5f);
                float precisionAlpha = 0.0f; // Silence the compiler warnings.
                switch (precisionAlphaIndex)
                {
                    case 7: precisionAlpha = 255.0f; break;
                    case 6: precisionAlpha = 127.0f; break;
                    case 5: precisionAlpha = 63.0f; break;
                    case 4: precisionAlpha = 31.0f; break;
                    case 3: precisionAlpha = 15.0f; break;
                    case 2: precisionAlpha = 7.0f; break;
                    case 1: precisionAlpha = 3.0f; break;
                    case 0: precisionAlpha = 1.0f; break;
                    default: Debug.Assert(false); break;
                }

                cmd.SetGlobalVector(PSXShaderIDs._FogPrecisionAlphaAndInverse, new Vector2(precisionAlpha, 1.0f / precisionAlpha));

                Texture precisionAlphaDitherTexture = (volumeSettings.precisionAlphaDitherTexture.value != null) ? volumeSettings.precisionAlphaDitherTexture.value : Texture2D.grayTexture;
                cmd.SetGlobalTexture(PSXShaderIDs._FogPrecisionAlphaDitherTexture, precisionAlphaDitherTexture);
                cmd.SetGlobalVector(PSXShaderIDs._FogPrecisionAlphaDitherSize, new Vector4(
                    precisionAlphaDitherTexture.width,
                    precisionAlphaDitherTexture.height,
                    1.0f / (float)precisionAlphaDitherTexture.width,
                    1.0f / (float)precisionAlphaDitherTexture.height
                ));
                cmd.SetGlobalFloat(PSXShaderIDs._FogPrecisionAlphaDither, volumeSettings.precisionAlphaDither.value);

                cmd.SetGlobalVector(PSXShaderIDs._FogDistanceScaleBias, fogDistanceScaleBias);
                cmd.SetGlobalFloat(PSXShaderIDs._FogFalloffCurvePower, fogFalloffCurvePower);

                switch (volumeSettings.colorLUTMode.value)
                {
                    case FogVolume.FogColorLUTMode.Disabled:
                    {
                        cmd.SetGlobalTexture(PSXShaderIDs._FogColorLUTTexture2D, Texture2D.whiteTexture);
                        cmd.SetGlobalTexture(PSXShaderIDs._FogColorLUTTextureCube, whiteCubemap);

                        cmd.SetGlobalVector(PSXShaderIDs._FogColorLUTRotationTangent, Vector3.zero);
                        cmd.SetGlobalVector(PSXShaderIDs._FogColorLUTRotationBitangent, Vector3.zero);
                        cmd.SetGlobalVector(PSXShaderIDs._FogColorLUTRotationNormal, Vector3.zero);

                        cmd.EnableShaderKeyword(PSXShaderKeywords.s_FOG_COLOR_LUT_MODE_DISABLED);
                        cmd.DisableShaderKeyword(PSXShaderKeywords.s_FOG_COLOR_LUT_MODE_TEXTURE2D_DISTANCE_AND_HEIGHT);
                        cmd.DisableShaderKeyword(PSXShaderKeywords.s_FOG_COLOR_LUT_MODE_TEXTURECUBE);
                        break;
                    }

                    case FogVolume.FogColorLUTMode.Texture2DDistanceAndHeight:
                    {
                        cmd.SetGlobalTexture(PSXShaderIDs._FogColorLUTTexture2D, volumeSettings.colorLUTTexture.value);
                        cmd.SetGlobalTexture(PSXShaderIDs._FogColorLUTTextureCube, whiteCubemap);

                        cmd.SetGlobalVector(PSXShaderIDs._FogColorLUTRotationTangent, Vector3.zero);
                        cmd.SetGlobalVector(PSXShaderIDs._FogColorLUTRotationBitangent, Vector3.zero);
                        cmd.SetGlobalVector(PSXShaderIDs._FogColorLUTRotationNormal, Vector3.zero);

                        cmd.DisableShaderKeyword(PSXShaderKeywords.s_FOG_COLOR_LUT_MODE_DISABLED);
                        cmd.EnableShaderKeyword(PSXShaderKeywords.s_FOG_COLOR_LUT_MODE_TEXTURE2D_DISTANCE_AND_HEIGHT);
                        cmd.DisableShaderKeyword(PSXShaderKeywords.s_FOG_COLOR_LUT_MODE_TEXTURECUBE);
                        break;
                    }

                    case FogVolume.FogColorLUTMode.TextureCube:
                    {
                        cmd.SetGlobalTexture(PSXShaderIDs._FogColorLUTTexture2D, Texture2D.whiteTexture);
                        cmd.SetGlobalTexture(PSXShaderIDs._FogColorLUTTextureCube, volumeSettings.colorLUTTexture.value);

                        Quaternion cubemapRotation = Quaternion.Euler(volumeSettings.colorLUTRotationDegrees.value);
                        Vector3 cubemapRotationTangent = cubemapRotation * Vector3.right;
                        Vector3 cubemapRotationBitangent = cubemapRotation * Vector3.up;
                        Vector3 cubemapRotationNormal = cubemapRotation * Vector3.forward;
                        cmd.SetGlobalVector(PSXShaderIDs._FogColorLUTRotationTangent, cubemapRotationTangent);
                        cmd.SetGlobalVector(PSXShaderIDs._FogColorLUTRotationBitangent, cubemapRotationBitangent);
                        cmd.SetGlobalVector(PSXShaderIDs._FogColorLUTRotationNormal, cubemapRotationNormal);

                        cmd.DisableShaderKeyword(PSXShaderKeywords.s_FOG_COLOR_LUT_MODE_DISABLED);
                        cmd.DisableShaderKeyword(PSXShaderKeywords.s_FOG_COLOR_LUT_MODE_TEXTURE2D_DISTANCE_AND_HEIGHT);
                        cmd.EnableShaderKeyword(PSXShaderKeywords.s_FOG_COLOR_LUT_MODE_TEXTURECUBE);
                        break;
                    }

                    default:
                    {
                        Debug.AssertFormat(false, "Encountered unsupported FogColorLUTMode {0} in Volume", volumeSettings.colorLUTMode.value);
                        break;
                    }
                }

                cmd.SetGlobalVector(PSXShaderIDs._FogColorLUTWeight, new Vector2(volumeSettings.colorLUTWeight.value, volumeSettings.colorLUTWeightLayer1.value));

                bool isAdditionalLayerEnabled = volumeSettings.isAdditionalLayerEnabled.value; 
                cmd.SetGlobalInt(PSXShaderIDs._FogIsAdditionalLayerEnabled, isAdditionalLayerEnabled ? 1 : 0);
                if (isAdditionalLayerEnabled)
                {
                    int fogFalloffModeLayer1 = (int)volumeSettings.fogFalloffModeLayer1.value;
                    cmd.SetGlobalInt(PSXShaderIDs._FogFalloffModeLayer1, fogFalloffModeLayer1);
                    cmd.SetGlobalVector(PSXShaderIDs._FogColorLayer1, new Vector4(volumeSettings.colorLayer1.value.r, volumeSettings.colorLayer1.value.g, volumeSettings.colorLayer1.value.b, volumeSettings.colorLayer1.value.a));
                    
                    Vector4 fogDistanceScaleBiasLayer1 = new Vector4(
                        1.0f / (volumeSettings.distanceMaxLayer1.value - volumeSettings.distanceMinLayer1.value), 
                        -volumeSettings.distanceMinLayer1.value / (volumeSettings.distanceMaxLayer1.value - volumeSettings.distanceMinLayer1.value),
                        -1.0f / (volumeSettings.heightMaxLayer1.value - volumeSettings.heightMinLayer1.value),
                        volumeSettings.heightMaxLayer1.value / (volumeSettings.heightMaxLayer1.value - volumeSettings.heightMinLayer1.value)
                    );

                    float fogFalloffCurvePowerLayer1 = (volumeSettings.fogFalloffCurveLayer1.value > 0.0f)
                        ? (1.0f - Mathf.Min(0.999f, volumeSettings.fogFalloffCurveLayer1.value)) // shoulder increases as value increases from [0, 1]
                        : (1.0f / (1.0f + Mathf.Max(-0.999f, volumeSettings.fogFalloffCurveLayer1.value))); // toe increases as value decreases from [0, -1]

                    if (!isFogEnabled)
                    {
                        fogDistanceScaleBiasLayer1 = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                        fogFalloffCurvePowerLayer1 = 1.0f;
                    }
                    else if (!volumeSettings.heightFalloffEnabledLayer1.value)
                    {
                        fogDistanceScaleBiasLayer1.z = 0.0f;
                        fogDistanceScaleBiasLayer1.w = 1.0f;
                    }

                    cmd.SetGlobalVector(PSXShaderIDs._FogDistanceScaleBiasLayer1, fogDistanceScaleBiasLayer1);
                    cmd.SetGlobalFloat(PSXShaderIDs._FogFalloffCurvePowerLayer1, fogFalloffCurvePowerLayer1);
                }
            }
        }

        void PushGlobalRasterizationParameters(Camera camera, CommandBuffer cmd, RTHandle rasterizationRT, int rasterizationWidth, int rasterizationHeight, bool hdrIsSupported)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushGlobalRasterizationParameters))
            {
                Color clearColorUnused = Color.black;
                cmd.ClearRenderTarget(clearDepth: true, clearColor: false, backgroundColor: clearColorUnused);
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSize, new Vector4(rasterizationWidth, rasterizationHeight, 1.0f / (float)rasterizationWidth, 1.0f / (float)rasterizationHeight));
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSizeRasterization, new Vector4(rasterizationWidth, rasterizationHeight, 1.0f / (float)rasterizationWidth, 1.0f / (float)rasterizationHeight));
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSizeRasterizationRTScaled, new Vector4(rasterizationRT.rt.width, rasterizationRT.rt.height, 1.0f / (float)rasterizationRT.rt.width, 1.0f / (float)rasterizationRT.rt.height));

                // Clamp our uv to software emulate a clamped wrap mode within the context of our potentially scaled RT
                // (where the RT viewport may be significantly smaller than the actual RT resolution due to resizing events).
                // Note: When useScaling is false, this is the same as: new Vector4(0.5f / rasterizationWidth, 0.5f / rasterizationHeight, (rasterizationWidth - 0.5f) / rasterizationWidth, (rasterizationHeight - 0.5f) / rasterizationHeight);
                Vector4 rasterizationRTScaledClampBoundsUV = new Vector4(0.5f / rasterizationRT.rt.width, 0.5f / rasterizationRT.rt.height, (rasterizationWidth - 0.5f) / rasterizationRT.rt.width, (rasterizationHeight - 0.5f) / rasterizationRT.rt.height);
                cmd.SetGlobalVector(PSXShaderIDs._RasterizationRTScaledClampBoundsUV, rasterizationRTScaledClampBoundsUV);

                Vector4 rasterizationRTScaledMaxSSAndUV = rasterizationRT.useScaling
                    ? new Vector4(rasterizationWidth, rasterizationHeight, (float)rasterizationWidth / rasterizationRT.rt.width, (float)rasterizationHeight / rasterizationRT.rt.height)
                    : new Vector4(rasterizationRT.rt.width, rasterizationRT.rt.height, 1.0f, 1.0f);
                cmd.SetGlobalVector(PSXShaderIDs._RasterizationRTScaledMaxSSAndUV, rasterizationRTScaledMaxSSAndUV);


                cmd.SetGlobalVector(PSXShaderIDs._WorldSpaceCameraPos, camera.transform.position);
                
                float time = GetAnimatedMaterialsTime(camera);
                cmd.SetGlobalVector(PSXShaderIDs._Time, new Vector4(time / 20.0f, time, time * 2.0f, time * 3.0f));
            
                Texture2D alphaClippingDitherTex = GetAlphaClippingDitherTexFromAssetAndFrame(asset, (uint)Time.frameCount);
                cmd.SetGlobalTexture(PSXShaderIDs._AlphaClippingDitherTexture, alphaClippingDitherTex);
                cmd.SetGlobalVector(PSXShaderIDs._AlphaClippingDitherSize, new Vector4(
                    alphaClippingDitherTex.width,
                    alphaClippingDitherTex.height,
                    1.0f / alphaClippingDitherTex.width,
                    1.0f / alphaClippingDitherTex.height
                ));

                bool flipProj = ComputeCameraProjectionIsFlippedY(camera);
                float n = camera.nearClipPlane;
                float f = camera.farClipPlane;
                Vector4 projectionParams = new Vector4(flipProj ? -1 : 1, n, f, 1.0f / f);
                cmd.SetGlobalVector(PSXShaderIDs._ProjectionParams, projectionParams);

                if (hdrIsSupported)
                {
                    Shader.EnableKeyword(PSXShaderKeywords.s_OUTPUT_HDR);
                }
                else
                {
                    Shader.EnableKeyword(PSXShaderKeywords.s_OUTPUT_LDR);
                }
            }  
        }

        static void PushGlobalPostProcessingParameters(Camera camera, CommandBuffer cmd, PSXRenderPipelineAsset asset, RTHandle rasterizationRT, int rasterizationWidth, int rasterizationHeight, Vector4 cameraAspectModeUVScaleBias)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushGlobalPostProcessingParameters))
            {
                bool flipY = ComputeCameraProjectionIsFlippedY(camera);
                if (flipY && IsMainGameView(camera) && camera.targetTexture == null)
                {
                    // in DirectX mode (flip Y), there is an additional flip that needs to occur for the game view,
                    // as an extra blit seems to occur.
                    flipY = true;
                }
                else
                {
                    flipY = false;
                }

                // RTHandleSystem may allocate RTs that are larger than requested size (to improve performance of scaling RTs by minimizing reallocs).
                // Need to apply and RTHandleSystem scaling to our cameraAspectModeUVScaleBias terms.
                Vector4 cameraAspectModeUVScaleBiasWithRTScale = cameraAspectModeUVScaleBias;
                if (rasterizationRT.useScaling)
                {
                    Vector2 scale = new Vector2((float)rasterizationWidth / rasterizationRT.rt.width, (float)rasterizationHeight / rasterizationRT.rt.height);
                    cameraAspectModeUVScaleBiasWithRTScale.x *= scale.x;
                    cameraAspectModeUVScaleBiasWithRTScale.y *= scale.y;
                    cameraAspectModeUVScaleBiasWithRTScale.z *= scale.x;
                    cameraAspectModeUVScaleBiasWithRTScale.w *= scale.y;
                }
                
                cmd.SetGlobalInt(PSXShaderIDs._FlipY, flipY ? 1 : 0);
                cmd.SetGlobalVector(PSXShaderIDs._CameraAspectModeUVScaleBias, cameraAspectModeUVScaleBiasWithRTScale);
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSize, new Vector4(camera.pixelWidth, camera.pixelHeight, 1.0f / (float)camera.pixelWidth, 1.0f / (float)camera.pixelHeight));
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSizeRasterization, new Vector4(rasterizationWidth, rasterizationHeight, 1.0f / (float)rasterizationWidth, 1.0f / (float)rasterizationHeight));
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSizeRasterizationRTScaled, new Vector4(rasterizationRT.rt.width, rasterizationRT.rt.height, 1.0f / (float)rasterizationRT.rt.width, 1.0f / (float)rasterizationRT.rt.height));
                cmd.SetGlobalTexture(PSXShaderIDs._FrameBufferTexture, rasterizationRT);

                Texture2D whiteNoiseTexture = GetWhiteNoise1024RGBTexFromAssetAndFrame(asset, (uint)Time.frameCount);
                cmd.SetGlobalTexture(PSXShaderIDs._WhiteNoiseTexture, whiteNoiseTexture);
                cmd.SetGlobalVector(PSXShaderIDs._WhiteNoiseSize, new Vector4(whiteNoiseTexture.width, whiteNoiseTexture.height, 1.0f / (float)whiteNoiseTexture.width, 1.0f / (float)whiteNoiseTexture.height));
                
                Texture2D blueNoiseTexture = GetBlueNoise16RGBTexFromAssetAndFrame(asset, (uint)Time.frameCount);
                cmd.SetGlobalTexture(PSXShaderIDs._BlueNoiseTexture, blueNoiseTexture);
                cmd.SetGlobalVector(PSXShaderIDs._BlueNoiseSize, new Vector4(blueNoiseTexture.width, blueNoiseTexture.height, 1.0f / (float)blueNoiseTexture.width, 1.0f / (float)blueNoiseTexture.height));
                
                float time = GetAnimatedMaterialsTime(camera);
                cmd.SetGlobalVector(PSXShaderIDs._Time, new Vector4(time / 20.0f, time, time * 2.0f, time * 3.0f));
            }
        }

        static Texture2D GetFramebufferDitherTexFromAssetAndFrame(PSXRenderPipelineAsset asset, uint frameCount)
        {
            Texture2D ditherTexture = Texture2D.grayTexture;
            if (asset.renderPipelineResources.textures.framebufferDitherTex != null && asset.renderPipelineResources.textures.framebufferDitherTex.Length > 0)
            {
                uint ditherTextureIndex = frameCount % (uint)asset.renderPipelineResources.textures.framebufferDitherTex.Length;
                ditherTexture = asset.renderPipelineResources.textures.framebufferDitherTex[ditherTextureIndex];
            }

            return ditherTexture;
        }

        static Texture2D GetAlphaClippingDitherTexFromAssetAndFrame(PSXRenderPipelineAsset asset, uint frameCount)
        {
            Texture2D ditherTexture = Texture2D.grayTexture;
            if (asset.renderPipelineResources.textures.alphaClippingDitherTex != null && asset.renderPipelineResources.textures.alphaClippingDitherTex.Length > 0)
            {
                uint ditherTextureIndex = frameCount % (uint)asset.renderPipelineResources.textures.alphaClippingDitherTex.Length;
                ditherTexture = asset.renderPipelineResources.textures.alphaClippingDitherTex[ditherTextureIndex];
            }

            return ditherTexture;
        }

        static Texture2D GetWhiteNoise1024RGBTexFromAssetAndFrame(PSXRenderPipelineAsset asset, uint frameCount)
        {
            Texture2D whiteNoiseTexture = Texture2D.grayTexture;
            if (asset.renderPipelineResources.textures.whiteNoise1024RGBTex != null && asset.renderPipelineResources.textures.whiteNoise1024RGBTex.Length > 0)
            {
                uint whiteNoiseTextureIndex = frameCount % (uint)asset.renderPipelineResources.textures.whiteNoise1024RGBTex.Length;
                whiteNoiseTexture = asset.renderPipelineResources.textures.whiteNoise1024RGBTex[whiteNoiseTextureIndex];
            }

            return whiteNoiseTexture;
        }

        static Texture2D GetBlueNoise16RGBTexFromAssetAndFrame(PSXRenderPipelineAsset asset, uint frameCount)
        {
            Texture2D blueNoiseTexture = Texture2D.grayTexture;
            if (asset.renderPipelineResources.textures.blueNoise16RGBTex != null && asset.renderPipelineResources.textures.blueNoise16RGBTex.Length > 0)
            {
                uint blueNoiseTextureIndex = frameCount % (uint)asset.renderPipelineResources.textures.blueNoise16RGBTex.Length;
                blueNoiseTexture = asset.renderPipelineResources.textures.blueNoise16RGBTex[blueNoiseTextureIndex];
            }

            return blueNoiseTexture;
        }

        static int ComputeCompressionKernelIndex(CompressionVolume.CompressionMode mode, CompressionVolume.CompressionColorspace colorspace)
        {
            // WARNING: this kernel LUT calculation needs to stay in sync with both the kernel declarations in compression.compute,
            // and the enum definitions in CompressionVolume.
            // We need to manually compute and bookkeep kernel indices this way because 2019.3 does not support multi-compile keywords in compute shaders.
            int compressionKernelIndex = (int)mode * 6 + (int)colorspace;
            return compressionKernelIndex;
        }

        int[] FindCompressionKernels(PSXRenderPipelineAsset asset)
        {
            Debug.Assert(asset.renderPipelineResources.shaders.compressionCS, "Error: CompressionCS compute shader is unassigned in render pipeline resources. Assign a valid reference to this compute shader inside of render pipeline resources in the inspector.");
            
            int[] kernels = new int[PSXComputeKernels.s_COMPRESSION.Length];
            
            for (int i = 0, iLen = PSXComputeKernels.s_COMPRESSION.Length; i < iLen; ++i)
            {
                kernels[i] = asset.renderPipelineResources.shaders.compressionCS.FindKernel(PSXComputeKernels.s_COMPRESSION[i]);
            }

            return kernels;
        }

        static void PushCompressionParameters(Camera camera, CommandBuffer cmd, PSXRenderPipelineAsset asset, RenderTexture rasterizationRT, int[] compressionCSKernels)
        {
            if (!IsComputeShaderSupportedPlatform()) { return; }

            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushCompressionParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<CompressionVolume>();
                if (!volumeSettings) volumeSettings = CompressionVolume.@default;

                bool isEnabled = volumeSettings.isEnabled.value || (volumeSettings.weight.value < 1e-5f);
                if (!isEnabled) { return; }

                int compressionKernelIndex = ComputeCompressionKernelIndex(volumeSettings.mode.value, volumeSettings.colorspace.value);
                int compressionKernel = compressionCSKernels[compressionKernelIndex];

                cmd.SetComputeFloatParam(asset.renderPipelineResources.shaders.compressionCS, PSXShaderIDs._CompressionWeight, volumeSettings.weight.value);

                float compressionAccuracyThreshold = Mathf.Lerp(4.0f, 1e-5f, volumeSettings.accuracy.value);
                float compressionAccuracyThresholdInverse = 1.0f / compressionAccuracyThreshold;
                cmd.SetComputeVectorParam(asset.renderPipelineResources.shaders.compressionCS, PSXShaderIDs._CompressionAccuracyThresholdAndInverse,
                    new Vector2(compressionAccuracyThreshold, compressionAccuracyThresholdInverse)
                );
                cmd.SetComputeVectorParam(asset.renderPipelineResources.shaders.compressionCS, PSXShaderIDs._CompressionSourceIndicesMinMax,
                    new Vector4(0, 0, rasterizationRT.width - 1, rasterizationRT.height - 1)
                );

                float chromaQuantizationScale = Mathf.Lerp(16.0f, 256.0f, volumeSettings.accuracy.value);
                cmd.SetComputeVectorParam(asset.renderPipelineResources.shaders.compressionCS, PSXShaderIDs._CompressionChromaQuantizationScaleAndInverse,
                    new Vector2(chromaQuantizationScale, 1.0f / chromaQuantizationScale)
                );

                cmd.SetComputeTextureParam(asset.renderPipelineResources.shaders.compressionCS, compressionKernel, PSXShaderIDs._CompressionSource, rasterizationRT);
            
                // TODO: Fix this when the rounding doesn't work.
                cmd.DispatchCompute(asset.renderPipelineResources.shaders.compressionCS, compressionKernel, (rasterizationRT.width + 7) / 8, (rasterizationRT.height + 7) / 8, 1);
            }
        }

        static void PushSkyParameters(Camera camera, CommandBuffer cmd, Material skyMaterial, PSXRenderPipelineAsset asset, int rasterizationWidth, int rasterizationHeight)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushSkyParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<SkyVolume>();
                if (!volumeSettings) volumeSettings = SkyVolume.@default;

                SkyVolume.SkyMode skyMode = volumeSettings.skyMode.value;

                switch (skyMode)
                {
                    case SkyVolume.SkyMode.FogColor:
                    {
                        skyMaterial.EnableKeyword(PSXShaderKeywords.s_SKY_MODE_FOG_COLOR);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_BACKGROUND_COLOR);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_SKYBOX);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_TILED_LAYERS);
                        break;
                    }
                    case SkyVolume.SkyMode.BackgroundColor:
                    {
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_FOG_COLOR);
                        skyMaterial.EnableKeyword(PSXShaderKeywords.s_SKY_MODE_BACKGROUND_COLOR);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_SKYBOX);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_TILED_LAYERS);
                        break;
                    }
                    case SkyVolume.SkyMode.Skybox:
                    {
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_FOG_COLOR);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_BACKGROUND_COLOR);
                        skyMaterial.EnableKeyword(PSXShaderKeywords.s_SKY_MODE_SKYBOX);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_TILED_LAYERS);
                        break;
                    }
                    case SkyVolume.SkyMode.TiledLayers:
                    {
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_FOG_COLOR);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_BACKGROUND_COLOR);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_SKY_MODE_SKYBOX);
                        skyMaterial.EnableKeyword(PSXShaderKeywords.s_SKY_MODE_TILED_LAYERS);
                        break;
                    }
                    default:
                    {
                        Debug.Assert(false, "Error: Encountered unsupported SkyMode.");
                        break;
                    }
                }

                SkyVolume.TextureFilterMode textureFilterMode = volumeSettings.textureFilterMode.value;
                switch (textureFilterMode)
                {
                    case SkyVolume.TextureFilterMode.TextureImportSettings:
                    {
                        skyMaterial.EnableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_POINT);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_N64);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_N64_MIPMAPS);
                        break;
                    }

                    case SkyVolume.TextureFilterMode.Point:
                    {
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                        skyMaterial.EnableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_POINT);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_N64);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_N64_MIPMAPS);
                        break;
                    }

                    case SkyVolume.TextureFilterMode.PointMipmaps:
                    {
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_POINT);
                        skyMaterial.EnableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_N64);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_N64_MIPMAPS);
                        break;
                    }

                    case SkyVolume.TextureFilterMode.N64:
                    {
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_POINT);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                        skyMaterial.EnableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_N64);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_N64_MIPMAPS);
                        break;
                    }

                    case SkyVolume.TextureFilterMode.N64Mipmaps:
                    {
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_POINT);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                        skyMaterial.DisableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_N64);
                        skyMaterial.EnableKeyword(PSXShaderKeywords.s_TEXTURE_FILTER_MODE_N64_MIPMAPS);
                        break;
                    }

                    default:
                    {
                        Debug.Assert(false, "Error: Encountered unsupported TextureFilterMode.");
                        break;
                    }
                }

                Color skyColor;
                switch (skyMode)
                {
                    case SkyVolume.SkyMode.FogColor: skyColor = GetFogColorFromFogVolume(); break;
                    case SkyVolume.SkyMode.BackgroundColor: skyColor = camera.backgroundColor; break;
                    default: skyColor = Color.black; break;
                }

                cmd.SetGlobalVector(PSXShaderIDs._SkyColor, skyColor);

                if (skyMode == SkyVolume.SkyMode.Skybox)
                {
                    Texture skyboxTexture = volumeSettings.skyboxTexture.value;
                    if (skyboxTexture == null)
                    {
                        skyboxTexture = asset.renderPipelineResources.textures.skyboxTextureCubeDefault;
                    }

                    cmd.SetGlobalTexture(PSXShaderIDs._SkyboxTextureCube, skyboxTexture);
                }

                if (skyMode == SkyVolume.SkyMode.Skybox || skyMode == SkyVolume.SkyMode.TiledLayers)
                {
                    Vector4 rasterizationResolution = new Vector4(rasterizationWidth, rasterizationHeight, 1.0f / (float)rasterizationWidth, 1.0f / (float)rasterizationHeight);
                    Matrix4x4 skyPixelCoordToWorldSpaceViewDirectionMatrix = ComputePixelCoordToWorldSpaceViewDirectionMatrix(camera, camera.worldToCameraMatrix, rasterizationResolution);

                    Vector3 skyRotationEulerDegrees = volumeSettings.skyRotation.value;
                    Quaternion skyRotationQuaternion = Quaternion.Euler(skyRotationEulerDegrees.x, skyRotationEulerDegrees.y, skyRotationEulerDegrees.z);
                    Matrix4x4 skyRotationMatrix = Matrix4x4.Rotate(skyRotationQuaternion);

                    cmd.SetGlobalMatrix(PSXShaderIDs._SkyPixelCoordToWorldSpaceViewDirectionMatrix, skyPixelCoordToWorldSpaceViewDirectionMatrix * skyRotationMatrix);
                }
                else
                {
                    cmd.SetGlobalMatrix(PSXShaderIDs._SkyPixelCoordToWorldSpaceViewDirectionMatrix, Matrix4x4.identity);
                }

                if (skyMode == SkyVolume.SkyMode.TiledLayers)
                {
                    cmd.SetGlobalFloat(PSXShaderIDs._SkyTiledLayersSkyHeightScaleInverse, 1.0f / volumeSettings.tiledLayersSkyHeightScale.value);
                    cmd.SetGlobalFloat(PSXShaderIDs._SkyTiledLayersSkyHorizonOffset, volumeSettings.tiledLayersSkyHorizonOffset.value);
                    cmd.SetGlobalVector(PSXShaderIDs._SkyTiledLayersSkyColorLayer0, volumeSettings.tiledLayersSkyColorLayer0.value);
                    cmd.SetGlobalTexture(PSXShaderIDs._SkyTiledLayersSkyTextureLayer0, volumeSettings.tiledLayersSkyTextureLayer0.value);
                    cmd.SetGlobalVector(PSXShaderIDs._SkyTiledLayersSkyTextureScaleOffsetLayer0, volumeSettings.tiledLayersSkyTextureScaleOffsetLayer0.value);
                    cmd.SetGlobalFloat(PSXShaderIDs._SkyTiledLayersSkyRotationLayer0, Mathf.Deg2Rad * volumeSettings.tiledLayersSkyRotationLayer0.value);
                    cmd.SetGlobalVector(PSXShaderIDs._SkyTiledLayersSkyScrollScaleLayer0, volumeSettings.tiledLayersSkyScrollScaleLayer0.value * 0.1f);
                    cmd.SetGlobalFloat(PSXShaderIDs._SkyTiledLayersSkyScrollRotationLayer0, Mathf.Deg2Rad * volumeSettings.tiledLayersSkyScrollRotationLayer0.value);
                    cmd.SetGlobalVector(PSXShaderIDs._SkyTiledLayersSkyColorLayer1, volumeSettings.tiledLayersSkyColorLayer1.value);
                    cmd.SetGlobalTexture(PSXShaderIDs._SkyTiledLayersSkyTextureLayer1, volumeSettings.tiledLayersSkyTextureLayer1.value);
                    cmd.SetGlobalVector(PSXShaderIDs._SkyTiledLayersSkyTextureScaleOffsetLayer1, volumeSettings.tiledLayersSkyTextureScaleOffsetLayer1.value);
                    cmd.SetGlobalFloat(PSXShaderIDs._SkyTiledLayersSkyRotationLayer1, Mathf.Deg2Rad * volumeSettings.tiledLayersSkyRotationLayer1.value);
                    cmd.SetGlobalVector(PSXShaderIDs._SkyTiledLayersSkyScrollScaleLayer1, volumeSettings.tiledLayersSkyScrollScaleLayer1.value * 0.1f);
                    cmd.SetGlobalFloat(PSXShaderIDs._SkyTiledLayersSkyScrollRotationLayer1, Mathf.Deg2Rad * volumeSettings.tiledLayersSkyScrollRotationLayer1.value);
                }

                cmd.SetGlobalFloat(PSXShaderIDs._SkyFramebufferDitherWeight, volumeSettings.framebufferDitherWeight.value);
            }
        }

        static void PushTerrainGrassParameters(Camera camera, CommandBuffer cmd, PSXRenderPipelineAsset asset, int rasterizationWidth, int rasterizationHeight)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushTerrainGrassParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<TerrainGrassVolume>();
                if (!volumeSettings) volumeSettings = TerrainGrassVolume.@default;

                TerrainGrassVolume.TextureFilterMode textureFilterMode = volumeSettings.textureFilterMode.value;
                switch (textureFilterMode)
                {
                    case TerrainGrassVolume.TextureFilterMode.TextureImportSettings:
                        {
                            cmd.EnableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64_MIPMAPS);
                            break;
                        }

                    case TerrainGrassVolume.TextureFilterMode.Point:
                        {
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                            cmd.EnableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64_MIPMAPS);
                            break;
                        }

                    case TerrainGrassVolume.TextureFilterMode.PointMipmaps:
                        {
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT);
                            cmd.EnableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64_MIPMAPS);
                            break;
                        }

                    case TerrainGrassVolume.TextureFilterMode.N64:
                        {
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                            cmd.EnableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64_MIPMAPS);
                            break;
                        }

                    case TerrainGrassVolume.TextureFilterMode.N64Mipmaps:
                        {
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                            cmd.DisableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64);
                            cmd.EnableShaderKeyword(PSXShaderKeywords.s_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64_MIPMAPS);
                            break;
                        }

                    default:
                        {
                            Debug.Assert(false, "Error: Encountered unsupported TextureFilterMode.");
                            break;
                        }
                }
            }
        }

        static void TryDrawAccumulationMotionBlurPreUIOverlay(PSXCamera psxCamera, CommandBuffer cmd, Material accumulationMotionBlurMaterial, Material copyColorRespectFlipYMaterial)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_DrawAccumulationMotionBlurPreUIOverlay))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<AccumulationMotionBlurVolume>();
                if (!volumeSettings) volumeSettings = AccumulationMotionBlurVolume.@default;

                if (volumeSettings.weight.value <= 1e-5f)
                {
                    psxCamera.ResetAccumulationMotionBlurFrameCount();
                }

                if ((psxCamera.GetCameraAccumulationMotionBlurFrameCount() > 0) && (!volumeSettings.applyToUIOverlay.value))
                {
                    PSXRenderPipeline.PushAccumulationMotionBlurParameters(psxCamera, cmd, volumeSettings);
                    PSXRenderPipeline.DrawFullScreenQuad(cmd, accumulationMotionBlurMaterial);

                    RTHandle rasterizationCurrentRT = psxCamera.GetCurrentFrameRT((int)PSXCameraFrameHistoryType.Rasterization);
                    RTHandle rasterizationPreUICopyRT = psxCamera.GetCurrentFrameRT((int)PSXCameraFrameHistoryType.RasterizationPreUICopy);

                    PSXRenderPipeline.CopyColorRespectFlipY(psxCamera.camera, cmd, rasterizationCurrentRT, rasterizationPreUICopyRT, copyColorRespectFlipYMaterial);

                    // The above blit changes the global state of our bound render target.
                    // Switch the global bound render target back to the one we were rendering with before.
                    RTHandle rasterizationDepthStencilCurrentRT = psxCamera.GetCurrentFrameRT((int)PSXCameraFrameHistoryType.RasterizationDepthStencil);
                    cmd.SetRenderTarget(rasterizationCurrentRT, rasterizationDepthStencilCurrentRT);
                    PSXRenderPipeline.SetViewport(cmd, rasterizationCurrentRT);
                }
            }
        }

        static void TryDrawAccumulationMotionBlurPostUIOverlay(PSXCamera psxCamera, CommandBuffer cmd, Material accumulationMotionBlurMaterial)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_DrawAccumulationMotionBlurPostUIOverlay))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<AccumulationMotionBlurVolume>();
                if (!volumeSettings) volumeSettings = AccumulationMotionBlurVolume.@default;

                if (volumeSettings.weight.value <= 1e-5f)
                {
                    psxCamera.ResetAccumulationMotionBlurFrameCount();
                }

                if ((psxCamera.GetCameraAccumulationMotionBlurFrameCount() > 0) && volumeSettings.applyToUIOverlay.value)
                {
                    PSXRenderPipeline.PushAccumulationMotionBlurParameters(psxCamera, cmd, volumeSettings);
                    PSXRenderPipeline.DrawFullScreenQuad(cmd, accumulationMotionBlurMaterial);
                }
            }
        }

        static void TryDrawAccumulationMotionBlurFinalBlit(PSXCamera psxCamera, CommandBuffer cmd, RenderTexture renderTargetCurrent, Material copyColorRespectFlipYMaterial)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_DrawAccumulationMotionBlurFinalBlit))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<AccumulationMotionBlurVolume>();
                if (!volumeSettings) volumeSettings = AccumulationMotionBlurVolume.@default;

                if ((psxCamera.GetCameraAccumulationMotionBlurFrameCount() > 0) && (!volumeSettings.applyToUIOverlay.value))
                {
                    // Copy our pre-ui data back to the current RT so that it will get correctly swapped into the history back buffer for sampling next frame.
                    RTHandle rasterizationPreUICopyRT = psxCamera.GetCurrentFrameRT((int)PSXCameraFrameHistoryType.RasterizationPreUICopy);
                    RTHandle rasterizationCurrentRT = psxCamera.GetCurrentFrameRT((int)PSXCameraFrameHistoryType.Rasterization);

                    PSXRenderPipeline.CopyColorRespectFlipY(psxCamera.camera, cmd, rasterizationPreUICopyRT, rasterizationCurrentRT, copyColorRespectFlipYMaterial);

                    // Our blit call will change the global state of the bound render target.
                    // Bind back our previous render target to avoid non-obvious side effects.
                    // This actually is necessary in the editor. It seems the final camera render target needs to be bound before the context is submitted,
                    // otherwise we get this error spilling: Dimensions of color surface does not match dimensions of depth surface.
                    cmd.SetRenderTarget(renderTargetCurrent);
                    PSXRenderPipeline.SetViewport(cmd, psxCamera.camera, renderTargetCurrent);
                }
            }
        }

        static void PushAccumulationMotionBlurParameters(PSXCamera psxCamera, CommandBuffer cmd, AccumulationMotionBlurVolume volumeSettings)
        {
            RTHandle rasterizationHistoryRT = psxCamera.GetPreviousFrameRT((int)PSXCameraFrameHistoryType.Rasterization);

            float rasterizationHistoryWeight = volumeSettings.weight.value;

            // Treat our volume history weight as a target rather than using it directly.
            // Lerp up history based on the number of frames we have accumulated until we reach out target.
            // This avoids incorrectly biasing toward earlier frames which would result in first frame burn in.
            rasterizationHistoryWeight = Mathf.Min(rasterizationHistoryWeight, 0.9999f);
            rasterizationHistoryWeight = Mathf.Min(rasterizationHistoryWeight, 1.0f - 1.0f / (float)(psxCamera.GetCameraAccumulationMotionBlurFrameCount() + 1));

            cmd.SetGlobalFloat(PSXShaderIDs._RasterizationHistoryWeight, rasterizationHistoryWeight);
            cmd.SetGlobalFloat(PSXShaderIDs._RasterizationHistoryCompositeDither, volumeSettings.dither.value);
            cmd.SetGlobalVector(PSXShaderIDs._AccumulationMotionBlurParameters, new Vector4(
                volumeSettings.zoom.value * 10.0f,
                volumeSettings.vignette.value,
                volumeSettings.zoomDither.value,
                volumeSettings.anisotropy.value
            ));
            cmd.SetGlobalTexture(PSXShaderIDs._RasterizationHistoryRT, rasterizationHistoryRT);
        }

        static void CopyColorRespectFlipY(Camera camera, CommandBuffer cmd, RTHandle source, RTHandle destination, Material copyColorRespectFlipYMaterial)
        {
            // Flip logic taken from URP:
            // Blit has logic to flip projection matrix when rendering to render texture.
            // Currently the y-flip is handled in CopyColorRespectFlipY.shader by checking _ProjectionParams.x
            // If you replace this Blit with a Draw* that sets projection matrix double check
            // to also update shader.
            //
            // We need to handle y-flip in a way that all existing shaders using _ProjectionParams.x work.
            // Otherwise we get flipping issues like this one (case https://issuetracker.unity3d.com/issues/lwrp-depth-texture-flipy)

            // Unity flips projection matrix in non-OpenGL platforms and when rendering to a render texture.
            // If HPSXRP is rendering to RT:
            //  - Source is upside down. We need to copy using a shader that has flipped matrix as well so we have same orientation for source and destination.
            //  - When shaders render objects that sample screen space textures they adjust the uv sign with  _ProjectionParams.x. (https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
            // If HPSXRP is NOT rendering to RT and is NOT rendering with OpenGL:
            //  - Source is NOT flipped. We CANNOT flip when copying and don't flip when sampling. (ProjectionParams.x == 1)

            cmd.SetGlobalVector(PSXShaderIDs._CopyColorSourceRTSize, new Vector4(source.rt.width, source.rt.height, 1.0f / source.rt.width, 1.0f / source.rt.height));
            cmd.SetGlobalTexture(PSXShaderIDs._CopyColorSourceRT, source);
            cmd.SetRenderTarget(destination);
            PSXRenderPipeline.SetViewport(cmd, destination);
            PSXRenderPipeline.DrawFullScreenQuad(cmd, copyColorRespectFlipYMaterial);
        }

        static void PushCathodeRayTubeParameters(Camera camera, CommandBuffer cmd, Material crtMaterial)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushCathodeRayTubeParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<CathodeRayTubeVolume>();
                if (!volumeSettings) volumeSettings = CathodeRayTubeVolume.@default;

                cmd.SetGlobalInt(PSXShaderIDs._CRTIsEnabled, volumeSettings.isEnabled.value ? 1 : 0);

                cmd.SetGlobalFloat(PSXShaderIDs._CRTBloom, volumeSettings.bloom.value);

                switch (volumeSettings.grateMaskMode.value)
                {
                    case CathodeRayTubeVolume.CRTGrateMaskMode.CompressedTV:
                        crtMaterial.EnableKeyword(PSXShaderKeywords.s_CRT_MASK_COMPRESSED_TV);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_APERATURE_GRILL);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA_STRETCHED);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_TEXTURE);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_DISABLED);
                        break;

                    case CathodeRayTubeVolume.CRTGrateMaskMode.ApertureGrill:
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_COMPRESSED_TV);
                        crtMaterial.EnableKeyword(PSXShaderKeywords.s_CRT_MASK_APERATURE_GRILL);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA_STRETCHED);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_TEXTURE);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_DISABLED);
                        break;

                    case CathodeRayTubeVolume.CRTGrateMaskMode.VGA:
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_COMPRESSED_TV);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_APERATURE_GRILL);
                        crtMaterial.EnableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA_STRETCHED);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_TEXTURE);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_DISABLED);
                        break;

                    case CathodeRayTubeVolume.CRTGrateMaskMode.VGAStretched:
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_COMPRESSED_TV);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_APERATURE_GRILL);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA);
                        crtMaterial.EnableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA_STRETCHED);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_TEXTURE);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_DISABLED);
                        break;

                    case CathodeRayTubeVolume.CRTGrateMaskMode.Texture:
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_COMPRESSED_TV);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_APERATURE_GRILL);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA_STRETCHED);
                        crtMaterial.EnableKeyword(PSXShaderKeywords.s_CRT_MASK_TEXTURE);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_DISABLED);
                        break;

                    case CathodeRayTubeVolume.CRTGrateMaskMode.Disabled:
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_COMPRESSED_TV);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_APERATURE_GRILL);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_VGA_STRETCHED);
                        crtMaterial.DisableKeyword(PSXShaderKeywords.s_CRT_MASK_TEXTURE);
                        crtMaterial.EnableKeyword(PSXShaderKeywords.s_CRT_MASK_DISABLED);
                        break;

                    default:
                        break;
                }

                if (volumeSettings.grateMaskMode.value == CathodeRayTubeVolume.CRTGrateMaskMode.Texture
                    && volumeSettings.grateMaskTexture.value != null)
                {
                    Texture2D grateMaskTexture = (Texture2D)volumeSettings.grateMaskTexture.value;
                    cmd.SetGlobalTexture(PSXShaderIDs._CRTGrateMaskTexture, grateMaskTexture);
                    cmd.SetGlobalVector(PSXShaderIDs._CRTGrateMaskSize, new Vector4(
                        grateMaskTexture.width,
                        grateMaskTexture.height,
                        1.0f / (float)grateMaskTexture.width,
                        1.0f / (float)grateMaskTexture.height
                    ));
                }
                else
                {
                    cmd.SetGlobalTexture(PSXShaderIDs._CRTGrateMaskTexture, Texture2D.whiteTexture);
                    cmd.SetGlobalVector(PSXShaderIDs._CRTGrateMaskSize, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                }
                

                cmd.SetGlobalVector(PSXShaderIDs._CRTGrateMaskScale, new Vector2(volumeSettings.grateMaskScale.value, 1.0f / volumeSettings.grateMaskScale.value));

                // Hardness of scanline.
                //  -8.0 = soft
                // -16.0 = medium
                cmd.SetGlobalFloat(PSXShaderIDs._CRTScanlineSharpness, Mathf.Lerp(-8.0f, -32.0f, volumeSettings.scanlineSharpness.value));

                // Hardness of pixels in scanline.
                // -2.0 = soft
                // -4.0 = hard
                cmd.SetGlobalFloat(PSXShaderIDs._CRTImageSharpness, Mathf.Lerp(-2.0f, -4.0f, volumeSettings.imageSharpness.value));

                cmd.SetGlobalVector(PSXShaderIDs._CRTBloomSharpness, new Vector2(
                    // Hardness of short horizontal bloom.
                    //  -0.5 = wide to the point of clipping (bad)
                    //  -1.0 = wide
                    //  -2.0 = not very wide at all
                    Mathf.Lerp(-1.0f, -2.0f, volumeSettings.bloomSharpnessX.value),

                    // Hardness of short vertical bloom.
                    //  -1.0 = wide to the point of clipping (bad)
                    //  -1.5 = wide
                    //  -4.0 = not very wide at all
                    Mathf.Lerp(-1.5f, -4.0f, volumeSettings.bloomSharpnessY.value)
                ));

                cmd.SetGlobalFloat(PSXShaderIDs._CRTNoiseIntensity, volumeSettings.noiseIntensity.value);
                cmd.SetGlobalFloat(PSXShaderIDs._CRTNoiseSaturation, volumeSettings.noiseSaturation.value);

                cmd.SetGlobalVector(PSXShaderIDs._CRTGrateMaskIntensityMinMax, new Vector2(volumeSettings.grateMaskIntensityMin.value * 2.0f, volumeSettings.grateMaskIntensityMax.value * 2.0f));

                // Display warp.
                // 0.0 = none
                // 1.0/8.0 = extreme
                cmd.SetGlobalVector(PSXShaderIDs._CRTBarrelDistortion, new Vector2(volumeSettings.barrelDistortionX.value * 0.125f, volumeSettings.barrelDistortionY.value * 0.125f));
            
                cmd.SetGlobalFloat(PSXShaderIDs._CRTVignetteSquared, volumeSettings.vignette.value * volumeSettings.vignette.value);
            }
        }

        static void DrawSceneViewUI(Camera camera)
        {
        #if UNITY_EDITOR
            // Emit scene view UI
            if (camera.cameraType == CameraType.SceneView)
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        #endif
        }

        static void DrawBackgroundOpaque(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            DrawOpaque(context, camera, PSXRenderQueue.k_RenderQueue_BackgroundAllOpaque, ref cullingResults);
        }

        static void DrawBackgroundTransparent(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            DrawTransparent(context, camera, PSXRenderQueue.k_RenderQueue_BackgroundTransparent, ref cullingResults);
        }

        static void DrawMainOpaque(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            DrawOpaque(context, camera, PSXRenderQueue.k_RenderQueue_MainAllOpaque, ref cullingResults);
        }

        static void DrawMainTransparent(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            DrawTransparent(context, camera, PSXRenderQueue.k_RenderQueue_MainTransparent, ref cullingResults);
        }

        static void DrawUIOverlayOpaque(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            DrawOpaque(context, camera, PSXRenderQueue.k_RenderQueue_UIOverlayAllOpaque, ref cullingResults);
        }

        static void DrawUIOverlayTransparent(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            DrawTransparent(context, camera, PSXRenderQueue.k_RenderQueue_UIOverlayTransparent, ref cullingResults);
        }

        static void DrawOpaque(ScriptableRenderContext context, Camera camera, RenderQueueRange range, ref CullingResults cullingResults)
        {
            // If the depth buffer is disabled, trigger back to front rendering, instead of QuantizedFrontToBack rendering.
            // Note there are additional criteria flags that we always care about, such as SortingLayer, RenderQueue, etc, which are outlined here:
            // https://docs.unity3d.com/ScriptReference/Rendering.SortingCriteria.CommonOpaque.html
            //
            SortingCriteria criteria = EvaluateIsDepthBufferEnabledFromVolume()
                ? SortingCriteria.CommonOpaque
                : ((SortingCriteria.CommonOpaque & (~SortingCriteria.QuantizedFrontToBack)) | SortingCriteria.BackToFront);

            // Draw opaque objects using PSX shader pass
            var sortingSettings = new SortingSettings(camera)
            {
                criteria = criteria
            };

            var drawingSettings = new DrawingSettings(PSXShaderPassNames.s_PSXLit, sortingSettings)
            {
                perObjectData = ComputePerObjectDataFromLightingVolume(camera)
            };
            
            var filteringSettings = new FilteringSettings()
            {
                renderQueueRange = range,
                layerMask = camera.cullingMask, // Respect the culling mask specified on the camera so that users can selectively omit specific layers from rendering to this camera.
                renderingLayerMask = UInt32.MaxValue, // Everything
                excludeMotionVectorObjects = false
            };

            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        static void DrawTransparent(ScriptableRenderContext context, Camera camera, RenderQueueRange range, ref CullingResults cullingResults)
        {
            // Draw transparent objects using PSX shader pass
            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };

            var drawingSettings = new DrawingSettings(PSXShaderPassNames.s_PSXLit, sortingSettings)
            {
                perObjectData = ComputePerObjectDataFromLightingVolume(camera)
            };
            
            var filteringSettings = new FilteringSettings()
            {
                renderQueueRange = range,
                layerMask = camera.cullingMask, // Respect the culling mask specified on the camera so that users can selectively omit specific layers from rendering to this camera.
                renderingLayerMask = UInt32.MaxValue, // Everything
                excludeMotionVectorObjects = false
            };

            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        static void DrawSkybox(ScriptableRenderContext context, Camera camera)
        {
            context.DrawSkybox(camera);
        }

        static void DrawLegacyCanvasUI(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            // Draw legacy Canvas UI meshes.
            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };
            var drawSettings = new DrawingSettings(PSXShaderPassNames.s_SRPDefaultUnlit, sortingSettings);
            var filterSettings = new FilteringSettings()
            {
                renderQueueRange = RenderQueueRange.all,
                layerMask = camera.cullingMask, // Respect the culling mask specified on the camera so that users can selectively omit specific layers from rendering to this camera.
                renderingLayerMask = UInt32.MaxValue, // Everything
                excludeMotionVectorObjects = false
            };
            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
        }

        // Respects RTHandle scaling.
        static void SetViewport(CommandBuffer cmd, RTHandle target)
        {
            CoreUtils.SetViewport(cmd, target);
        }

        static void SetViewport(CommandBuffer cmd, Camera camera, RenderTexture target)
        {
            int width = camera.pixelWidth;
            int height = camera.pixelHeight;
            if (target != null)
            {
                width = target.width;
                height = target.height;
            }
            
            cmd.SetViewport(new Rect(0.0f, 0.0f, width, height));
        }

        static Mesh s_FullscreenMesh = null;
        static Mesh fullscreenMesh
        {
            get
            {
                if (s_FullscreenMesh != null)
                    return s_FullscreenMesh;

                float topV = 0.0f;
                float bottomV = 1.0f;

                s_FullscreenMesh = new Mesh { name = "Fullscreen Quad" };
                s_FullscreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });

                s_FullscreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });

                s_FullscreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                s_FullscreenMesh.UploadMeshData(true);
                return s_FullscreenMesh;
            }
        }

        static Cubemap s_whiteCubemap = null;

        static Cubemap whiteCubemap
        {
            get
            {
                if (s_whiteCubemap != null)
                    return s_whiteCubemap;

                s_whiteCubemap = new Cubemap(1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
                s_whiteCubemap.SetPixel(CubemapFace.NegativeX, 0, 0, Color.white);
                s_whiteCubemap.SetPixel(CubemapFace.NegativeY, 0, 0, Color.white);
                s_whiteCubemap.SetPixel(CubemapFace.NegativeZ, 0, 0, Color.white);
                s_whiteCubemap.SetPixel(CubemapFace.PositiveX, 0, 0, Color.white);
                s_whiteCubemap.SetPixel(CubemapFace.PositiveY, 0, 0, Color.white);
                s_whiteCubemap.SetPixel(CubemapFace.PositiveZ, 0, 0, Color.white);
                s_whiteCubemap.Apply(updateMipmaps: false, makeNoLongerReadable: true);

                return s_whiteCubemap;
            }
        }

        // Draws a fullscreen quad (to maintain webgl build support).
        static void DrawFullScreenQuad(CommandBuffer cmd, Material material,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            cmd.DrawMesh(PSXRenderPipeline.fullscreenMesh, Matrix4x4.identity, material);
        }

        static void DrawGizmos(ScriptableRenderContext context, Camera camera, GizmoSubset gizmoSubset)
        {
        #if UNITY_EDITOR
            if (UnityEditor.Handles.ShouldRenderGizmos())
                context.DrawGizmos(camera, gizmoSubset);
        #endif
        }

        public static bool IsComputeShaderSupportedPlatform()
        {
            if(!SystemInfo.supportsComputeShaders) { return false; }

        #if UNITY_EDITOR
            UnityEditor.BuildTarget buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;

            if (buildTarget == UnityEditor.BuildTarget.StandaloneWindows ||
                buildTarget == UnityEditor.BuildTarget.StandaloneWindows64 ||
                buildTarget == UnityEditor.BuildTarget.StandaloneLinux64 ||
                buildTarget == UnityEditor.BuildTarget.Stadia ||
                buildTarget == UnityEditor.BuildTarget.StandaloneOSX ||
                buildTarget == UnityEditor.BuildTarget.WSAPlayer ||
                buildTarget == UnityEditor.BuildTarget.XboxOne ||
                buildTarget == UnityEditor.BuildTarget.PS4 ||
                buildTarget == UnityEditor.BuildTarget.iOS ||
                buildTarget == UnityEditor.BuildTarget.Switch)
            {
                return true;
            }
            else
            {
                return false;
            }
        #else
            if (Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.LinuxPlayer ||
                Application.platform == RuntimePlatform.PS4 ||
                Application.platform == RuntimePlatform.XboxOne ||
                Application.platform == RuntimePlatform.Switch ||
                Application.platform == RuntimePlatform.Stadia)
            {
                return true;
            }
            else
            {
                return false;
            }
        #endif
        }

        public static RenderTextureFormat GetFrameBufferRenderTextureFormatHDR(out bool hdrIsSupported)
        {
            // TODO: Implement.
            // if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
            // {
            //     hdrIsSupported = true;
            //     return RenderTextureFormat.ARGBHalf;
            // }
            // else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
            // {
            //     hdrIsSupported = true;
            //     return RenderTextureFormat.ARGBFloat;
            // }
            // else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
            {
                hdrIsSupported = false;
                return RenderTextureFormat.ARGB32;
            }
            // else
            // {
            //     hdrIsSupported = false;
            //     return RenderTextureFormat.Default;
            // }

        }


        static bool ComputeCameraProjectionIsFlippedY(Camera camera)
        {
            bool isFlipped = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true).inverse.MultiplyPoint(new Vector3(0, 1, 0)).y < 0;
            return isFlipped;
        }

        // Taken from HDCamera:
        /// <summary>
        /// Compute the matrix from screen space (pixel) to world space direction (RHS).
        ///
        /// You can use this matrix on the GPU to compute the direction to look in a cubemap for a specific
        /// screen pixel.
        /// </summary>
        /// <param name="viewConstants"></param>
        /// <param name="resolution">The target texture resolution.</param>
        /// <param name="aspect">
        /// The aspect ratio to use.
        ///
        /// if negative, then the aspect ratio of <paramref name="resolution"/> will be used.
        ///
        /// It is different from the aspect ratio of <paramref name="resolution"/> for anamorphic projections.
        /// </param>
        /// <returns></returns>
        static Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix(Camera camera, Matrix4x4 viewMatrix, Vector4 resolution, float aspect = -1)
        {
            float verticalFoV = camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
            Vector2 lensShift = camera.GetGateFittedLensShift();

            return ComputePixelCoordToWorldSpaceViewDirectionMatrix(verticalFoV, lensShift, resolution, viewMatrix, renderToCubemap: false, aspect);
        }

        static Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix(float verticalFoV, Vector2 lensShift, Vector4 screenSize, Matrix4x4 worldToViewMatrix, bool renderToCubemap, float aspectRatio = -1)
        {
            aspectRatio = aspectRatio < 0 ? screenSize.x * screenSize.w : aspectRatio;

            // Compose the view space version first.
            // V = -(X, Y, Z), s.t. Z = 1,
            // X = (2x / resX - 1) * tan(vFoV / 2) * ar = x * [(2 / resX) * tan(vFoV / 2) * ar] + [-tan(vFoV / 2) * ar] = x * [-m00] + [-m20]
            // Y = (2y / resY - 1) * tan(vFoV / 2)      = y * [(2 / resY) * tan(vFoV / 2)]      + [-tan(vFoV / 2)]      = y * [-m11] + [-m21]

            float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);

            // Compose the matrix.
            float m21 = (1.0f - 2.0f * lensShift.y) * tanHalfVertFoV;
            float m11 = -2.0f * screenSize.w * tanHalfVertFoV;

            float m20 = (1.0f - 2.0f * lensShift.x) * tanHalfVertFoV * aspectRatio;
            float m00 = -2.0f * screenSize.z * tanHalfVertFoV * aspectRatio;

            if (renderToCubemap)
            {
                // Flip Y.
                m11 = -m11;
                m21 = -m21;
            }

            var viewSpaceRasterTransform = new Matrix4x4(new Vector4(m00, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, m11, 0.0f, 0.0f),
                    new Vector4(m20, m21, -1.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            // Remove the translation component.
            var homogeneousZero = new Vector4(0, 0, 0, 1);
            worldToViewMatrix.SetColumn(3, homogeneousZero);

            // Flip the Z to make the coordinate system left-handed.
            worldToViewMatrix.SetRow(2, -worldToViewMatrix.GetRow(2));

            // Transpose for HLSL.
            return Matrix4x4.Transpose(worldToViewMatrix.transpose * viewSpaceRasterTransform);
        }

        static float GetAnimatedMaterialsTime(Camera camera)
        {
            float time = 0.0f;
            bool animateMaterials = CoreUtils.AreAnimatedMaterialsEnabled(camera);
            if (animateMaterials)
            {
#if UNITY_EDITOR
                time = Application.isPlaying ? Time.timeSinceLevelLoad : Time.realtimeSinceStartup;
#else
            time = Time.timeSinceLevelLoad;
#endif
            }
            else
            {
                time = 0;
            }

            return time;
        }
    }
}