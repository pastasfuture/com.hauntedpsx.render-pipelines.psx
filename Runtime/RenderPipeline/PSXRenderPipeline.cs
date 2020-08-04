using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    public partial class PSXRenderPipeline : UnityEngine.Rendering.RenderPipeline
    {
        readonly PSXRenderPipelineAsset m_Asset;
        public PSXRenderPipelineAsset asset { get { return m_Asset; }}

        internal const PerObjectData k_RendererConfigurationBakedLighting = PerObjectData.LightProbe | PerObjectData.Lightmaps | PerObjectData.LightProbeProxyVolume;
        internal const PerObjectData k_RendererConfigurationBakedLightingWithShadowMask = k_RendererConfigurationBakedLighting | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ShadowMask;
        internal const PerObjectData k_RendererConfigurationDynamicLighting = PerObjectData.LightData | PerObjectData.LightIndices;

        Material crtMaterial;
        int[] compressionCSKernels;

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
            if (asset.isSRPBatcherEnabled)
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = true;
            }
        }

        void FindComputeKernels()
        {
            if (!IsComputeShaderSupportedPlatform()) { return; }
            compressionCSKernels = FindCompressionKernels(m_Asset);
        }

        internal protected void Allocate()
        {
            this.crtMaterial = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.crtPS);
            FindComputeKernels();
            AllocateLighting();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            CoreUtils.Destroy(crtMaterial);
            compressionCSKernels = null;
            DisposeLighting();
        }

        void PushCameraParameters(Camera camera, CommandBuffer cmd, out int rasterizationWidth, out int rasterizationHeight, bool isPSXQualityEnabled)
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
                    // Render at platform's default framerate.
                    // Note we disable vsync here as well.
                    // If a user wants to enable vsync, they should specify a target frame rate.
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = -1;
                }

                // Trigger camera to reset it's aspect ratio back to the screens aspect ratio.
                // We force this reset here in case a previous frame had overridden the camera's aspect ratio via AspectMode.Locked.
                camera.ResetAspect();
                rasterizationWidth = camera.pixelWidth;
                rasterizationHeight = camera.pixelHeight;

                if (isPSXQualityEnabled)
                {
                    rasterizationWidth = volumeSettings.targetRasterizationResolutionWidth.value;
                    rasterizationHeight = volumeSettings.targetRasterizationResolutionHeight.value;
                    
                    // Only render locked aspect ratio in main game view.
                    // Force scene view to render with free aspect ratio so that users edit area is not cropped.
                    // This also works around an issue with the aspect ratio discrepancy between locked mode, and
                    // the built in unity gizmos that are rendered outside of the context of this render loop.
                    if (volumeSettings.aspectMode.value == CameraVolume.CameraAspectMode.Free
                        || !IsMainGameView(camera))
                    {
                        // Rather than explicitly hardcoding PSX framebuffer resolution, and enforcing 1.333 aspect ratio
                        // we allow arbitrary aspect ratios and match requested PSX framebuffer pixel density. (targetRasterizationResolutionWidth and targetRasterizationResolutionHeight).
                        // TODO: Could create an option for this in the RenderPipelineAsset that allows users to enforce aspect with black bars.
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

                    // Force the camera into the aspect ratio described by the targetRasterizationResolution parameters.
                    // While this can be approximately be equal to the fullscreen aspect ratio (in CameraAspectMode.Free),
                    // subtle change can occur from pixel rounding error.
                    camera.aspect = (float)rasterizationWidth / (float)rasterizationHeight;
                }
            }
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
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
                PushCameraParameters(camera, cmd, out int rasterizationWidth, out int rasterizationHeight, isPSXQualityEnabled);

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
                RenderTexture rasterizationRT = RenderTexture.GetTemporary(
                    new RenderTextureDescriptor
                    {
                        dimension = TextureDimension.Tex2D,
                        width = rasterizationWidth,
                        height = rasterizationHeight,
                        volumeDepth = 1,
                        depthBufferBits = EvaluateIsDepthBufferEnabledFromVolume() ? 24 : 0,
                        graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,// GetFrameBufferRenderTextureFormatHDR(out bool hdrIsSupported),
                        sRGB = false,
                        msaaSamples = 1,
                        memoryless = RenderTextureMemoryless.None,
                        vrUsage = VRTextureUsage.None,
                        useDynamicScale = false,
                        enableRandomWrite = IsComputeShaderSupportedPlatform(),
                        autoGenerateMips = false,
                        mipCount = 1
                    }
                ); 

                // var rasterizationRTDescriptor = new RenderTextureDescriptor(
                //     rasterizationWidth,
                //     rasterizationHeight,
                //     GetFrameBufferRenderTextureFormatHDR(out bool hdrIsSupported),
                //     EvaluateIsDepthBufferEnabledFromVolume() ? 24 : 0
                // );
                // // rasterizationRTDescriptor.useMipMap = false;
                // // rasterizationRTDescriptor.autoGenerateMips = false;
                // // rasterizationRTDescriptor.enableRandomWrite = IsComputeShaderSupportedPlatform();
                // // rasterizationRTDescriptor.sRGB = false;
                // // rasterizationRTDescriptor.vrUsage = VRTextureUsage.None;
                // // rasterizationRTDescriptor.useDynamicScale = false;

                // Debug.Log("rasterizationRTDescriptor.graphicsFormat = " + rasterizationRTDescriptor.graphicsFormat);

                // RenderTexture rasterizationRT = RenderTexture.GetTemporary(rasterizationRTDescriptor);

                cmd.SetRenderTarget(rasterizationRT);
                {
                    // Clear background to fog color to create seamless blend between forward-rendered fog, and "sky" / infinity.
                    PushGlobalRasterizationParameters(camera, cmd, rasterizationWidth, rasterizationHeight, hdrIsSupported);
                    PushQualityOverrideParameters(camera, cmd, isPSXQualityEnabled);
                    PushPrecisionParameters(camera, cmd, m_Asset);
                    PushFogParameters(camera, cmd);
                    PushLightingParameters(camera, cmd);
                    PushTonemapperParameters(camera, cmd);
                    PushDynamicLightingParameters(camera, cmd, ref cullingResults);
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

                    // TODO: DrawSkybox(context, camera);

                    cmd = CommandBufferPool.Get(PSXStringConstants.s_CommandBufferRenderPreUIOverlayStr);
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
                cmd.SetRenderTarget(camera.targetTexture);
                {
                    PushGlobalPostProcessingParameters(camera, cmd, m_Asset, rasterizationRT, rasterizationWidth, rasterizationHeight);
                    PushCompressionParameters(camera, cmd, m_Asset, rasterizationRT, compressionCSKernels);
                    PushCathodeRayTubeParameters(camera, cmd, crtMaterial);
                    PSXRenderPipeline.DrawFullScreenQuad(cmd, crtMaterial);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Release();
                context.Submit();
                RenderTexture.ReleaseTemporary(rasterizationRT);

                // Reset any modifications to the cameras aspect ratio so that built in unity handles draw normally.
                camera.ResetAspect();

                UnityEngine.Rendering.RenderPipeline.EndCameraRendering(context, camera);
            }
        }

        static bool IsMainGameView(Camera camera)
        {
            return camera.cameraType == CameraType.Game; 
        }

        static Color GetFogColorFromFogVolume()
        {
            var volumeSettings = VolumeManager.instance.stack.GetComponent<FogVolume>();
            if (!volumeSettings) volumeSettings = FogVolume.@default;
            return volumeSettings.color.value;
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
                var volumeSettings = VolumeManager.instance.stack.GetComponent<TonemapperVolume>();
                if (!volumeSettings) volumeSettings = TonemapperVolume.@default;

                int isEnabled = volumeSettings.isEnabled.value ? 1 : 0;
                float contrast = Mathf.Lerp(1e-5f, 1.95f, volumeSettings.contrast.value);
                float shoulder = Mathf.Lerp(0.9f, 1.1f, volumeSettings.shoulder.value);
                float whitepoint = volumeSettings.whitepoint.value;

                float a = contrast;
                float d = shoulder;
                float m = whitepoint;
                float i = volumeSettings.graypointIn.value;
                float o = volumeSettings.graypointOut.value;

                float b = -(o * Mathf.Pow(m, a) - Mathf.Pow(i, a)) / (o * (Mathf.Pow(i, a * d) - Mathf.Pow(m, a * d)));
                float c = ((o * Mathf.Pow(i, a * d)) * Mathf.Pow(m, a) - Mathf.Pow(i, a) * Mathf.Pow(m, a * d))
                    / (o * (Mathf.Pow(i, a * d) - Mathf.Pow(m, a * d)));

                Vector2 graypointCoefficients = new Vector2(b, c + 1e-5f);

                float crossTalk = Mathf.Lerp(1e-5f, 32.0f, volumeSettings.crossTalk.value);
                float saturation = Mathf.Lerp(0.0f, 32.0f, volumeSettings.saturation.value);
                float crossTalkSaturation = Mathf.Lerp(1e-5f, 32.0f, volumeSettings.crossTalkSaturation.value);

                cmd.SetGlobalInt(PSXShaderIDs._TonemapperIsEnabled, isEnabled);
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
                    perObjectData |= k_RendererConfigurationBakedLighting;
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

        static void PushPrecisionParameters(Camera camera, CommandBuffer cmd, PSXRenderPipelineAsset asset)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushPrecisionParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<PrecisionVolume>();
                if (!volumeSettings) volumeSettings = PrecisionVolume.@default;

                float precisionGeometryExponent = Mathf.Lerp(6.0f, 0.0f, volumeSettings.geometry.value);
                float precisionGeometryScaleInverse = Mathf.Pow(2.0f, precisionGeometryExponent);
                float precisionGeometryScale = 1.0f / precisionGeometryScaleInverse;

                cmd.SetGlobalVector(PSXShaderIDs._PrecisionGeometry, new Vector3(precisionGeometryScale, precisionGeometryScaleInverse));

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

                cmd.SetGlobalVector(PSXShaderIDs._PrecisionColor, precisionColor);
                cmd.SetGlobalVector(PSXShaderIDs._PrecisionColorInverse, new Vector3(1.0f / precisionColor.x, 1.0f / precisionColor.y, 1.0f / precisionColor.z));

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

                // Respect the Scene View fog enable / disable toggle.
                bool isFogEnabled = volumeSettings.isEnabled.value && CoreUtils.IsSceneViewFogEnabled(camera);
                if (!isFogEnabled)
                {
                    // To visually disable fog, we simply throw fog start and end to infinity.
                    fogDistanceScaleBias = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                }
                else if (!volumeSettings.heightFalloffEnabled.value)
                {
                    fogDistanceScaleBias.z = 0.0f;
                    fogDistanceScaleBias.w = 1.0f;
                }

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

                    if (!isFogEnabled)
                    {
                        fogDistanceScaleBiasLayer1 = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                    }
                    else if (!volumeSettings.heightFalloffEnabledLayer1.value)
                    {
                        fogDistanceScaleBiasLayer1.z = 0.0f;
                        fogDistanceScaleBiasLayer1.w = 1.0f;
                    }

                    cmd.SetGlobalVector(PSXShaderIDs._FogDistanceScaleBiasLayer1, fogDistanceScaleBiasLayer1);
                }
            }
        }

        void PushGlobalRasterizationParameters(Camera camera, CommandBuffer cmd, int rasterizationWidth, int rasterizationHeight, bool hdrIsSupported)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushGlobalRasterizationParameters))
            {
                cmd.ClearRenderTarget(clearDepth: true, clearColor: true, backgroundColor: GetFogColorFromFogVolume());
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSize, new Vector4(rasterizationWidth, rasterizationHeight, 1.0f / (float)rasterizationWidth, 1.0f / (float)rasterizationHeight));
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSizeRasterization, new Vector4(rasterizationWidth, rasterizationHeight, 1.0f / (float)rasterizationWidth, 1.0f / (float)rasterizationHeight));
                cmd.SetGlobalVector(PSXShaderIDs._Time, new Vector4(Time.timeSinceLevelLoad / 20.0f, Time.timeSinceLevelLoad, Time.timeSinceLevelLoad * 2.0f, Time.timeSinceLevelLoad * 3.0f));
                cmd.SetGlobalVector(PSXShaderIDs._WorldSpaceCameraPos, camera.transform.position);
            
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

        static void PushGlobalPostProcessingParameters(Camera camera, CommandBuffer cmd, PSXRenderPipelineAsset asset, RenderTexture rasterizationRT, int rasterizationWidth, int rasterizationHeight)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushGlobalPostProcessingParameters))
            {
                bool flipY = ComputeCameraProjectionIsFlippedY(camera);
                if (flipY && IsMainGameView(camera) && camera.targetTexture == null)
                {
                    // in DirectX mode (flip Y), there is an additional flip that needs to occur for the game view,
                    // as an extra blit seems to occur.
                    flipY = false;
                }

                cmd.SetGlobalInt(PSXShaderIDs._FlipY, flipY ? 1 : 0);
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSize, new Vector4(camera.pixelWidth, camera.pixelHeight, 1.0f / (float)camera.pixelWidth, 1.0f / (float)camera.pixelHeight));
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSizeRasterization, new Vector4(rasterizationWidth, rasterizationHeight, 1.0f / (float)rasterizationWidth, 1.0f / (float)rasterizationHeight));
                cmd.SetGlobalTexture(PSXShaderIDs._FrameBufferTexture, rasterizationRT);

                Texture2D whiteNoiseTexture = GetWhiteNoise1024RGBTexFromAssetAndFrame(asset, (uint)Time.frameCount);
                cmd.SetGlobalTexture(PSXShaderIDs._WhiteNoiseTexture, whiteNoiseTexture);
                cmd.SetGlobalVector(PSXShaderIDs._WhiteNoiseSize, new Vector4(whiteNoiseTexture.width, whiteNoiseTexture.height, 1.0f / (float)whiteNoiseTexture.width, 1.0f / (float)whiteNoiseTexture.height));
                
                Texture2D blueNoiseTexture = GetBlueNoise16RGBTexFromAssetAndFrame(asset, (uint)Time.frameCount);
                cmd.SetGlobalTexture(PSXShaderIDs._BlueNoiseTexture, blueNoiseTexture);
                cmd.SetGlobalVector(PSXShaderIDs._BlueNoiseSize, new Vector4(blueNoiseTexture.width, blueNoiseTexture.height, 1.0f / (float)blueNoiseTexture.width, 1.0f / (float)blueNoiseTexture.height));
                

                cmd.SetGlobalVector(PSXShaderIDs._Time, new Vector4(Time.timeSinceLevelLoad / 20.0f, Time.timeSinceLevelLoad, Time.timeSinceLevelLoad * 2.0f, Time.timeSinceLevelLoad * 3.0f));
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

                bool isEnabled = volumeSettings.isEnabled.value;
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
            var sortingSettings = new SortingSettings(camera);
            var drawSettings = new DrawingSettings(PSXShaderPassNames.s_SRPDefaultUnlit, sortingSettings);
            var filterSettings = FilteringSettings.defaultValue;
            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
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
            return GL.GetGPUProjectionMatrix(camera.projectionMatrix, true).inverse.MultiplyPoint(new Vector3(0, 1, 0)).y < 0;
        }
    }
}